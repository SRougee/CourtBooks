import { useEffect, useState, type FormEvent } from "react";

type Page = "dashboard" | "students" | "schedule" | "curriculum" | "invoices" | "reports";

type Student = {
  Id: number;
  FirstName: string;
  LastName: string;
  Phone: string;
  Email: string;
  DateOfBirth: string;
  Notes: string;
  Active: number;
};

type StudentForm = {
  firstName: string;
  lastName: string;
  phone: string;
  email: string;
  dateOfBirth: string;
  notes: string;
};

type Lesson = {
  Id: number;
  StudentId: number;
  FirstName: string;
  LastName: string;
  StartUtc: string;
  DurationMinutes: number;
  HourlyRate: number;
  Location: string;
  Notes: string;
  Status: number;
};

type LessonForm = {
  studentId: string;
  startUtc: string;
  durationMinutes: string;
  hourlyRate: string;
  location: string;
  notes: string;
};

const emptyStudentForm: StudentForm = {
  firstName: "",
  lastName: "",
  phone: "",
  email: "",
  dateOfBirth: "",
  notes: ""
};

const pages: { id: Page; label: string }[] = [
  { id: "dashboard", label: "Dashboard" },
  { id: "students", label: "Students" },
  { id: "schedule", label: "Schedule" },
  { id: "curriculum", label: "Curriculum" },
  { id: "invoices", label: "Invoices" },
  { id: "reports", label: "Reports" }
];

function App() {
  const [page, setPage] = useState<Page>("dashboard");

  return (
    <div className="app-shell">
      <header className="topbar">
        <button className="brand" onClick={() => setPage("dashboard")}>CourtBooks</button>
        <div className="topbar-actions">
          <button className="text-button">Search</button>
          <button className="profile-button">Coach</button>
        </div>
      </header>

      <div className="layout">
        <aside className="sidebar" aria-label="Primary navigation">
          {pages.map((item) => (
            <button
              key={item.id}
              className={page === item.id ? "nav-item active" : "nav-item"}
              onClick={() => setPage(item.id)}
            >
              {item.label}
            </button>
          ))}
        </aside>

        <main className="content">
          {page === "dashboard" && <Dashboard />}
          {page === "students" && <StudentsPage />}
          {page === "schedule" && <SchedulePage />}
          {page === "curriculum" && <Placeholder title="Curriculum" description="Curriculum progress will connect to the CourtBooks API in the next phase." />}
          {page === "invoices" && <Placeholder title="Invoices" description="Invoices and payments will connect to the CourtBooks API in the next phase." />}
          {page === "reports" && <Placeholder title="Reports" description="Reports will connect to the CourtBooks API in the next phase." />}
        </main>
      </div>

      <nav className="bottom-nav" aria-label="Mobile navigation">
        {pages.slice(0, 5).map((item) => (
          <button key={item.id} className={page === item.id ? "active" : ""} onClick={() => setPage(item.id)}>
            {item.label}
          </button>
        ))}
      </nav>
    </div>
  );
}

function Dashboard() {
  return (
    <>
      <section className="page-heading">
        <div>
          <p className="eyebrow">CourtBooks</p>
          <h1>Dashboard</h1>
          <p className="muted">A mobile-first workspace for managing your coaching practice.</p>
        </div>
        <button className="primary-button">Schedule lesson</button>
      </section>

      <section className="metric-grid">
        <article className="card metric"><span>Today's lessons</span><strong>3</strong><small>2 completed</small></article>
        <article className="card metric"><span>Active students</span><strong>12</strong><small>1 new this month</small></article>
        <article className="card metric"><span>Outstanding</span><strong>R 4,850</strong><small>2 invoices</small></article>
      </section>

      <section className="dashboard-grid">
        <article className="card">
          <div className="card-heading"><h2>Today's schedule</h2><button className="text-button">View schedule</button></div>
          <div className="schedule-list">
            <div><time>09:00</time><span><b>Alex Naidoo</b><small>Private lesson · Court 1</small></span></div>
            <div><time>11:00</time><span><b>Mia Jacobs</b><small>Private lesson · Court 2</small></span></div>
            <div><time>15:00</time><span><b>Jordan Smith</b><small>Assessment · Court 1</small></span></div>
          </div>
        </article>

        <article className="card">
          <div className="card-heading"><h2>Quick actions</h2></div>
          <div className="quick-actions">
            <button>Add student</button>
            <button>Schedule lesson</button>
            <button>Create invoice</button>
            <button>Record payment</button>
          </div>
        </article>
      </section>
    </>
  );
}

function StudentsPage() {
  const [students, setStudents] = useState<Student[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editingStudent, setEditingStudent] = useState<Student | null>(null);

  async function loadStudents() {
    setLoading(true);
    setError("");

    try {
      const response = await fetch("/api/students");

      if (!response.ok) {
        throw new Error(`Unable to load students (HTTP ${response.status}).`);
      }

      const data: unknown = await response.json();

      if (!Array.isArray(data)) {
        throw new Error("The API returned an unexpected response.");
      }

      setStudents(data as Student[]);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to load students.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadStudents();
  }, []);

  function openAddForm() {
    setEditingStudent(null);
    setFormOpen(true);
  }

  function openEditForm(student: Student) {
    setEditingStudent(student);
    setFormOpen(true);
  }

  function closeForm() {
    setFormOpen(false);
    setEditingStudent(null);
  }

  async function saveStudent(form: StudentForm) {
    const isEditing = editingStudent !== null;
    const url = isEditing ? `/api/students/${editingStudent.Id}` : "/api/students";
    const method = isEditing ? "PUT" : "POST";

    const response = await fetch(url, {
      method,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(form)
    });

    const payload: unknown = await response.json().catch(() => null);

    if (!response.ok) {
      const message =
        typeof payload === "object" &&
        payload !== null &&
        "error" in payload &&
        typeof payload.error === "string"
          ? payload.error
          : `Unable to save student (HTTP ${response.status}).`;

      throw new Error(message);
    }

    closeForm();
    await loadStudents();
  }

  async function toggleStudent(student: Student) {
    const response = await fetch(`/api/students/${student.Id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        firstName: student.FirstName,
        lastName: student.LastName,
        phone: student.Phone,
        email: student.Email,
        dateOfBirth: student.DateOfBirth,
        notes: student.Notes,
        active: !Boolean(student.Active)
      })
    });

    if (!response.ok) {
      const payload: unknown = await response.json().catch(() => null);
      const message =
        typeof payload === "object" &&
        payload !== null &&
        "error" in payload &&
        typeof payload.error === "string"
          ? payload.error
          : `Unable to update student (HTTP ${response.status}).`;
      setError(message);
      return;
    }

    await loadStudents();
  }

  return (
    <section>
      <div className="page-heading">
        <div>
          <p className="eyebrow">CourtBooks</p>
          <h1>Students</h1>
          <p className="muted">Students currently stored in your CourtBooks database.</p>
        </div>
        <button className="primary-button" onClick={openAddForm}>Add student</button>
      </div>

      <div className="card students-card">
        <div className="card-heading">
          <div>
            <h2>Student register</h2>
            {!loading && !error && <p className="card-subtitle">{students.length} student{students.length === 1 ? "" : "s"}</p>}
          </div>
          <button className="text-button" onClick={() => void loadStudents()} disabled={loading}>
            {loading ? "Loading" : "Refresh"}
          </button>
        </div>

        {loading && <p className="state-message">Loading students from Cloudflare D1...</p>}

        {!loading && error && (
          <div className="state-message error-state">
            <strong>Could not load students</strong>
            <span>{error}</span>
            <button className="secondary-button" onClick={() => void loadStudents()}>Try again</button>
          </div>
        )}

        {!loading && !error && students.length === 0 && (
          <p className="state-message">No students have been added yet.</p>
        )}

        {!loading && !error && students.length > 0 && (
          <div className="student-list">
            {students.map((student) => (
              <article className="student-row" key={student.Id}>
                <div className="student-avatar" aria-hidden="true">
                  {student.FirstName.charAt(0)}{student.LastName.charAt(0)}
                </div>
                <div className="student-main">
                  <div className="student-name-line">
                    <h3>{student.FirstName} {student.LastName}</h3>
                    <span className={student.Active ? "status-badge active-status" : "status-badge"}>{student.Active ? "Active" : "Inactive"}</span>
                  </div>
                  <div className="student-details">
                    <span>{student.Phone}</span>
                    <span>{student.Email}</span>
                  </div>
                </div>
                <div className="student-actions">
                  <button className="secondary-button compact-button" onClick={() => openEditForm(student)}>Edit</button>
                  <button className="text-button compact-button" onClick={() => void toggleStudent(student)}>
                    {student.Active ? "Deactivate" : "Activate"}
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>

      {formOpen && (
        <StudentFormModal
          student={editingStudent}
          onClose={closeForm}
          onSave={saveStudent}
        />
      )}
    </section>
  );
}

function StudentFormModal({
  student,
  onClose,
  onSave
}: {
  student: Student | null;
  onClose: () => void;
  onSave: (form: StudentForm) => Promise<void>;
}) {
  const [form, setForm] = useState<StudentForm>(() =>
    student
      ? {
          firstName: student.FirstName,
          lastName: student.LastName,
          phone: student.Phone,
          email: student.Email,
          dateOfBirth: student.DateOfBirth,
          notes: student.Notes
        }
      : emptyStudentForm
  );
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  function updateField(field: keyof StudentForm, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError("");

    try {
      await onSave(form);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to save student.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="modal-backdrop" role="presentation">
      <div className="modal-card" role="dialog" aria-modal="true" aria-labelledby="student-form-title">
        <div className="modal-heading">
          <div>
            <p className="eyebrow">Students</p>
            <h2 id="student-form-title">{student ? "Edit student" : "Add student"}</h2>
          </div>
          <button className="text-button" onClick={onClose} disabled={saving}>Close</button>
        </div>

        <form className="student-form" onSubmit={submit}>
          <div className="form-grid">
            <label>
              First name
              <input value={form.firstName} onChange={(event) => updateField("firstName", event.target.value)} required maxLength={100} />
            </label>
            <label>
              Last name
              <input value={form.lastName} onChange={(event) => updateField("lastName", event.target.value)} required maxLength={100} />
            </label>
            <label>
              Phone
              <input value={form.phone} onChange={(event) => updateField("phone", event.target.value)} required maxLength={30} />
            </label>
            <label>
              Email
              <input type="email" value={form.email} onChange={(event) => updateField("email", event.target.value)} required maxLength={254} />
            </label>
            <label>
              Date of birth
              <input type="date" value={form.dateOfBirth} onChange={(event) => updateField("dateOfBirth", event.target.value)} required />
            </label>
            <label className="full-width">
              Notes
              <textarea value={form.notes} onChange={(event) => updateField("notes", event.target.value)} maxLength={2000} rows={4} />
            </label>
          </div>

          {error && <div className="form-error">{error}</div>}

          <div className="modal-actions">
            <button type="button" className="secondary-button" onClick={onClose} disabled={saving}>Cancel</button>
            <button type="submit" className="primary-button" disabled={saving}>
              {saving ? "Saving..." : student ? "Save changes" : "Add student"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

const lessonStatusLabels = ["Scheduled", "Completed", "Cancelled", "No-show"];

function formatLessonDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat(undefined, {
    weekday: "short",
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  }).format(date);
}

function SchedulePage() {
  const [lessons, setLessons] = useState<Lesson[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editingLesson, setEditingLesson] = useState<Lesson | null>(null);

  async function loadSchedule() {
    setLoading(true);
    setError("");

    try {
      const [lessonsResponse, studentsResponse] = await Promise.all([
        fetch("/api/lessons"),
        fetch("/api/students")
      ]);

      if (!lessonsResponse.ok || !studentsResponse.ok) {
        throw new Error("Unable to load schedule data.");
      }

      const lessonData: unknown = await lessonsResponse.json();
      const studentData: unknown = await studentsResponse.json();

      if (!Array.isArray(lessonData) || !Array.isArray(studentData)) {
        throw new Error("The API returned an unexpected response.");
      }

      setLessons(lessonData as Lesson[]);
      setStudents((studentData as Student[]).filter((student) => Boolean(student.Active)));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to load schedule.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadSchedule();
  }, []);

  function openNewLesson() {
    setEditingLesson(null);
    setFormOpen(true);
  }

  function openEditLesson(lesson: Lesson) {
    setEditingLesson(lesson);
    setFormOpen(true);
  }

  async function saveLesson(form: LessonForm) {
    const isEditing = editingLesson !== null;
    const url = isEditing ? `/api/lessons/${editingLesson.Id}` : "/api/lessons";
    const response = await fetch(url, {
      method: isEditing ? "PUT" : "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        studentId: Number(form.studentId),
        startUtc: new Date(form.startUtc).toISOString(),
        durationMinutes: Number(form.durationMinutes),
        hourlyRate: Number(form.hourlyRate),
        location: form.location,
        notes: form.notes
      })
    });

    const payload: unknown = await response.json().catch(() => null);

    if (!response.ok) {
      const message =
        typeof payload === "object" &&
        payload !== null &&
        "error" in payload &&
        typeof payload.error === "string"
          ? payload.error
          : `Unable to schedule lesson (HTTP ${response.status}).`;
      throw new Error(message);
    }

    setFormOpen(false);
    await loadSchedule();
  }

  async function updateStatus(lesson: Lesson, status: number) {
    const response = await fetch(`/api/lessons/${lesson.Id}/status`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ status })
    });

    if (!response.ok) {
      const payload: unknown = await response.json().catch(() => null);
      const message =
        typeof payload === "object" &&
        payload !== null &&
        "error" in payload &&
        typeof payload.error === "string"
          ? payload.error
          : `Unable to update lesson (HTTP ${response.status}).`;
      setError(message);
      return;
    }

    await loadSchedule();
  }

  return (
    <section>
      <div className="page-heading">
        <div>
          <p className="eyebrow">CourtBooks</p>
          <h1>Schedule</h1>
          <p className="muted">Manage lessons stored in your CourtBooks database.</p>
        </div>
        <button className="primary-button" onClick={openNewLesson}>Schedule lesson</button>
      </div>

      <div className="card schedule-card">
        <div className="card-heading">
          <div>
            <h2>Lesson schedule</h2>
            {!loading && !error && <p className="card-subtitle">{lessons.length} lesson{lessons.length === 1 ? "" : "s"}</p>}
          </div>
          <button className="text-button" onClick={() => void loadSchedule()} disabled={loading}>
            {loading ? "Loading" : "Refresh"}
          </button>
        </div>

        {loading && <p className="state-message">Loading lessons from Cloudflare D1...</p>}

        {!loading && error && (
          <div className="state-message error-state">
            <strong>Could not load schedule</strong>
            <span>{error}</span>
            <button className="secondary-button" onClick={() => void loadSchedule()}>Try again</button>
          </div>
        )}

        {!loading && !error && lessons.length === 0 && (
          <p className="state-message">No lessons have been scheduled yet.</p>
        )}

        {!loading && !error && lessons.length > 0 && (
          <div className="lesson-list">
            {lessons.map((lesson) => (
              <article className="lesson-row" key={lesson.Id}>
                <div className="lesson-time">
                  <strong>{formatLessonDate(lesson.StartUtc)}</strong>
                  <span>{lesson.DurationMinutes} min</span>
                </div>
                <div className="lesson-main">
                  <div className="student-name-line">
                    <h3>{lesson.FirstName} {lesson.LastName}</h3>
                    <span className="status-badge">{lessonStatusLabels[lesson.Status] ?? "Unknown"}</span>
                  </div>
                  <div className="student-details">
                    <span>{lesson.Location}</span>
                    <span>R {Number(lesson.HourlyRate).toFixed(2)}/hr</span>
                  </div>
                </div>
                <div className="lesson-actions">
                  {lesson.Status === 0 && (
                    <>
                      <button className="secondary-button compact-button" onClick={() => openEditLesson(lesson)}>Edit</button>\n                      <button className="secondary-button compact-button" onClick={() => void updateStatus(lesson, 1)}>Complete</button>
                      <button className="text-button compact-button" onClick={() => void updateStatus(lesson, 2)}>Cancel</button>
                    </>
                  )}
                  {lesson.Status === 2 && (
                    <button className="text-button compact-button" onClick={() => openEditLesson(lesson)}>Reschedule</button>
                  )}
                </div>
              </article>
            ))}
          </div>
        )}
      </div>

      {formOpen && (
        <LessonFormModal
          students={students}
          onClose={() => setFormOpen(false)}
          onSave={saveLesson}
        />
      )}
    </section>
  );
}

function LessonFormModal({
  students,
  onClose,
  onSave
}: {
  students: Student[];
  onClose: () => void;
  onSave: (form: LessonForm) => Promise<void>;
}) {
  const [form, setForm] = useState<LessonForm>(() => {
    if (lesson) {
      const localStart = new Date(lesson.StartUtc);
      const offset = localStart.getTimezoneOffset();
      const localValue = new Date(localStart.getTime() - offset * 60 * 1000).toISOString().slice(0, 16);
      return {
        studentId: lesson.StudentId.toString(),
        startUtc: localValue,
        durationMinutes: lesson.DurationMinutes.toString(),
        hourlyRate: lesson.HourlyRate.toString(),
        location: lesson.Location,
        notes: lesson.Notes
      };
    }

    return {
      studentId: students[0]?.Id.toString() ?? "",
      startUtc: "",
      durationMinutes: "60",
      hourlyRate: "0",
      location: "",
      notes: ""
    };
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  function updateField(field: keyof LessonForm, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError("");

    try {
      await onSave(form);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to schedule lesson.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="modal-backdrop" role="presentation">
      <div className="modal-card" role="dialog" aria-modal="true" aria-labelledby="lesson-form-title">
        <div className="modal-heading">
          <div>
            <p className="eyebrow">Schedule</p>
            <h2 id="lesson-form-title">{lesson ? "Edit lesson" : "Schedule lesson"}</h2>
          </div>
          <button className="text-button" onClick={onClose} disabled={saving}>Close</button>
        </div>

        {students.length === 0 ? (
          <div className="state-message">
            <strong>No active students available</strong>
            <span>Add or activate a student before scheduling a lesson.</span>
          </div>
        ) : (
          <form className="student-form" onSubmit={submit}>
            <div className="form-grid">
              <label>
                Student
                <select value={form.studentId} onChange={(event) => updateField("studentId", event.target.value)} required>
                  {students.map((student) => (
                    <option key={student.Id} value={student.Id}>{student.FirstName} {student.LastName}</option>
                  ))}
                </select>
              </label>
              <label>
                Date and time
                <input type="datetime-local" value={form.startUtc} onChange={(event) => updateField("startUtc", event.target.value)} required />
              </label>
              <label>
                Duration (minutes)
                <input type="number" min="15" max="240" step="15" value={form.durationMinutes} onChange={(event) => updateField("durationMinutes", event.target.value)} required />
              </label>
              <label>
                Hourly rate
                <input type="number" min="0" step="0.01" value={form.hourlyRate} onChange={(event) => updateField("hourlyRate", event.target.value)} required />
              </label>
              <label className="full-width">
                Location
                <input value={form.location} onChange={(event) => updateField("location", event.target.value)} maxLength={200} required placeholder="Court 1" />
              </label>
              <label className="full-width">
                Notes
                <textarea value={form.notes} onChange={(event) => updateField("notes", event.target.value)} maxLength={2000} rows={3} />
              </label>
            </div>

            {error && <div className="form-error">{error}</div>}

            <div className="modal-actions">
              <button type="button" className="secondary-button" onClick={onClose} disabled={saving}>Cancel</button>
              <button type="submit" className="primary-button" disabled={saving}>
                {saving ? "Saving..." : lesson ? "Save changes" : "Schedule lesson"}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}

function Placeholder({ title, description }: { title: string; description: string }) {
  return (
    <section className="page-heading">
      <div>
        <p className="eyebrow">CourtBooks</p>
        <h1>{title}</h1>
        <p className="muted">{description}</p>
      </div>
    </section>
  );
}

export default App;
