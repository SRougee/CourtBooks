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
