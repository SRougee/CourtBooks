import { useState } from "react";

type Page = "dashboard" | "students" | "schedule" | "curriculum" | "invoices" | "reports";

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
          {page === "students" && <Placeholder title="Students" description="Student management will connect to the CourtBooks API in the next phase." />}
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