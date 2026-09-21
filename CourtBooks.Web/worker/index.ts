/// <reference types="@cloudflare/workers-types" />

import { Hono } from "hono";

type Env = {
  DB: D1Database;
};

type StudentPayload = {
  firstName?: unknown;
  lastName?: unknown;
  phone?: unknown;
  email?: unknown;
  dateOfBirth?: unknown;
  notes?: unknown;
  active?: unknown;
};

const app = new Hono<{ Bindings: Env }>();

function validateStudentPayload(payload: StudentPayload) {
  const firstName = typeof payload.firstName === "string" ? payload.firstName.trim() : "";
  const lastName = typeof payload.lastName === "string" ? payload.lastName.trim() : "";
  const phone = typeof payload.phone === "string" ? payload.phone.trim() : "";
  const email = typeof payload.email === "string" ? payload.email.trim().toLowerCase() : "";
  const dateOfBirth = typeof payload.dateOfBirth === "string" ? payload.dateOfBirth.trim() : "";
  const notes = typeof payload.notes === "string" ? payload.notes.trim() : "";
  const active = payload.active === undefined ? 1 : payload.active ? 1 : 0;

  if (!firstName || !lastName || !phone || !email || !dateOfBirth) {
    return { error: "First name, last name, phone, email and date of birth are required." };
  }

  if (firstName.length > 100 || lastName.length > 100) {
    return { error: "Names must be 100 characters or fewer." };
  }

  if (phone.length > 30) {
    return { error: "Phone number must be 30 characters or fewer." };
  }

  if (email.length > 254 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    return { error: "Enter a valid email address." };
  }

  if (!/^\d{4}-\d{2}-\d{2}$/.test(dateOfBirth)) {
    return { error: "Date of birth must use YYYY-MM-DD format." };
  }

  if (notes.length > 2000) {
    return { error: "Notes must be 2000 characters or fewer." };
  }

  return { value: { firstName, lastName, phone, email, dateOfBirth, notes, active } };
}

app.get("/api/health", (c) => c.json({ status: "ok", service: "courtbooks-api" }));

app.get("/api/students", async (c) => {
  const result = await c.env.DB
    .prepare(
      "SELECT Id, FirstName, LastName, Phone, Email, DateOfBirth, Notes, Active FROM Students ORDER BY LastName, FirstName"
    )
    .all();

  return c.json(result.results);
});

type LessonPayload = {
  studentId?: unknown;
  startUtc?: unknown;
  durationMinutes?: unknown;
  hourlyRate?: unknown;
  location?: unknown;
  notes?: unknown;
  status?: unknown;
};

function validateLessonPayload(payload: LessonPayload) {
  const studentId = Number(payload.studentId);
  const startUtc = typeof payload.startUtc === "string" ? payload.startUtc.trim() : "";
  const durationMinutes = Number(payload.durationMinutes);
  const hourlyRate = Number(payload.hourlyRate);
  const location = typeof payload.location === "string" ? payload.location.trim() : "";
  const notes = typeof payload.notes === "string" ? payload.notes.trim() : "";
  const status = payload.status === undefined ? 0 : Number(payload.status);

  if (!Number.isInteger(studentId) || studentId <= 0) {
    return { error: "A valid student is required." };
  }

  if (!startUtc || Number.isNaN(Date.parse(startUtc))) {
    return { error: "A valid lesson date and time are required." };
  }

  if (!Number.isInteger(durationMinutes) || durationMinutes < 15 || durationMinutes > 240) {
    return { error: "Lesson duration must be between 15 and 240 minutes." };
  }

  if (!Number.isFinite(hourlyRate) || hourlyRate < 0 || hourlyRate > 1000000) {
    return { error: "Hourly rate must be a valid non-negative amount." };
  }

  if (!location || location.length > 200) {
    return { error: "Location is required and must be 200 characters or fewer." };
  }

  if (notes.length > 2000) {
    return { error: "Notes must be 2000 characters or fewer." };
  }

  if (!Number.isInteger(status) || status < 0 || status > 3) {
    return { error: "Invalid lesson status." };
  }

  return { value: { studentId, startUtc, durationMinutes, hourlyRate, location, notes, status } };
}

app.get("/api/lessons", async (c) => {
  const result = await c.env.DB
    .prepare(
      "SELECT l.Id, l.StudentId, s.FirstName, s.LastName, l.StartUtc, l.DurationMinutes, l.HourlyRate, l.Location, l.Notes, l.Status FROM Lessons l INNER JOIN Students s ON s.Id = l.StudentId ORDER BY l.StartUtc"
    )
    .all();

  return c.json(result.results);
});

app.post("/api/lessons", async (c) => {
  let payload: LessonPayload;

  try {
    payload = await c.req.json<LessonPayload>();
  } catch {
    return c.json({ error: "Request body must be valid JSON." }, 400);
  }

  const validation = validateLessonPayload(payload);

  if ("error" in validation) {
    return c.json({ error: validation.error }, 400);
  }

  const { studentId, startUtc, durationMinutes, hourlyRate, location, notes, status } = validation.value;

  const student = await c.env.DB
    .prepare("SELECT Id FROM Students WHERE Id = ? AND Active = 1")
    .bind(studentId)
    .first();

  if (!student) {
    return c.json({ error: "Active student not found." }, 404);
  }

  const startMs = Date.parse(startUtc);
  const endMs = startMs + durationMinutes * 60 * 1000;

  const scheduledLessons = await c.env.DB
    .prepare("SELECT Id, StartUtc, DurationMinutes FROM Lessons WHERE Status = 0")
    .all();

  const overlapping = scheduledLessons.results.some((row) => {
    const existingStart = Date.parse(String(row.StartUtc));
    const existingDuration = Number(row.DurationMinutes);
    const existingEnd = existingStart + existingDuration * 60 * 1000;

    return startMs < existingEnd && endMs > existingStart;
  });

  if (overlapping) {
    return c.json({ error: "The lesson overlaps an existing scheduled lesson." }, 409);
  }

  const result = await c.env.DB
    .prepare(
      "INSERT INTO Lessons (StudentId, StartUtc, DurationMinutes, HourlyRate, Location, Notes, Status) VALUES (?, ?, ?, ?, ?, ?, ?)"
    )
    .bind(studentId, startUtc, durationMinutes, hourlyRate, location, notes, status)
    .run();

  const lessonId = result.meta.last_row_id;

  const created = await c.env.DB
    .prepare(
      "SELECT l.Id, l.StudentId, s.FirstName, s.LastName, l.StartUtc, l.DurationMinutes, l.HourlyRate, l.Location, l.Notes, l.Status FROM Lessons l INNER JOIN Students s ON s.Id = l.StudentId WHERE l.Id = ?"
    )
    .bind(lessonId)
    .first();

  return c.json(created, 201);
});

app.put("/api/lessons/:id/status", async (c) => {
  const id = Number(c.req.param("id"));

  if (!Number.isInteger(id) || id <= 0) {
    return c.json({ error: "Lesson ID must be a positive integer." }, 400);
  }

  let payload: { status?: unknown };

  try {
    payload = await c.req.json<{ status?: unknown }>();
  } catch {
    return c.json({ error: "Request body must be valid JSON." }, 400);
  }

  const status = Number(payload.status);

  if (!Number.isInteger(status) || status < 0 || status > 3) {
    return c.json({ error: "Invalid lesson status." }, 400);
  }

  const result = await c.env.DB
    .prepare("UPDATE Lessons SET Status = ? WHERE Id = ?")
    .bind(status, id)
    .run();

  if (result.meta.changes === 0) {
    return c.json({ error: "Lesson not found." }, 404);
  }

  return c.json({ success: true });
});

app.post("/api/students", async (c) => {
  let payload: StudentPayload;

  try {
    payload = await c.req.json<StudentPayload>();
  } catch {
    return c.json({ error: "Request body must be valid JSON." }, 400);
  }

  const validation = validateStudentPayload(payload);

  if ("error" in validation) {
    return c.json({ error: validation.error }, 400);
  }

  const { firstName, lastName, phone, email, dateOfBirth, notes, active } = validation.value;

  const result = await c.env.DB
    .prepare(
      "INSERT INTO Students (FirstName, LastName, Phone, Email, DateOfBirth, Notes, Active) VALUES (?, ?, ?, ?, ?, ?, ?)"
    )
    .bind(firstName, lastName, phone, email, dateOfBirth, notes, active)
    .run();

  const studentId = result.meta.last_row_id;

  const created = await c.env.DB
    .prepare(
      "SELECT Id, FirstName, LastName, Phone, Email, DateOfBirth, Notes, Active FROM Students WHERE Id = ?"
    )
    .bind(studentId)
    .first();

  return c.json(created, 201);
});

app.put("/api/students/:id", async (c) => {
  const id = Number(c.req.param("id"));

  if (!Number.isInteger(id) || id <= 0) {
    return c.json({ error: "Student ID must be a positive integer." }, 400);
  }

  let payload: StudentPayload;

  try {
    payload = await c.req.json<StudentPayload>();
  } catch {
    return c.json({ error: "Request body must be valid JSON." }, 400);
  }

  const validation = validateStudentPayload(payload);

  if ("error" in validation) {
    return c.json({ error: validation.error }, 400);
  }

  const { firstName, lastName, phone, email, dateOfBirth, notes, active } = validation.value;

  const result = await c.env.DB
    .prepare(
      "UPDATE Students SET FirstName = ?, LastName = ?, Phone = ?, Email = ?, DateOfBirth = ?, Notes = ?, Active = ? WHERE Id = ?"
    )
    .bind(firstName, lastName, phone, email, dateOfBirth, notes, active, id)
    .run();

  if (result.meta.changes === 0) {
    return c.json({ error: "Student not found." }, 404);
  }

  const updated = await c.env.DB
    .prepare(
      "SELECT Id, FirstName, LastName, Phone, Email, DateOfBirth, Notes, Active FROM Students WHERE Id = ?"
    )
    .bind(id)
    .first();

  return c.json(updated);
});

export default app;
