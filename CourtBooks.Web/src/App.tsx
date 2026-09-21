import { useEffect, useState } from "react";

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
          {page === "schedule" && <Placeholder title="Schedule" description="Lesson scheduling will connect to the CourtBooks API in the next phase." />}
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

  async function submit(event: React.FormEvent<HTMLFormElement>) {
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
