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

  return (
    <section>
      <div className="page-heading">
        <div>
          <p className="eyebrow">CourtBooks</p>
          <h1>Students</h1>
          <p className="muted">Students currently stored in your CourtBooks database.</p>
        </div>
        <button className="primary-button">Add student</button>
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
              </article>
            ))}
          </div>
        )}
      </div>
    </section>
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
