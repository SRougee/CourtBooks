/// <reference types="@cloudflare/workers-types" />

import { Hono } from "hono";

type Env = {
  DB: D1Database;
};

const app = new Hono<{ Bindings: Env }>();

app.get("/api/health", (c) => c.json({ status: "ok", service: "courtbooks-api" }));

app.get("/api/students", async (c) => {
  const result = await c.env.DB
    .prepare(
      "SELECT Id, FirstName, LastName, Phone, Email, DateOfBirth, Notes, Active FROM Students ORDER BY LastName, FirstName"
    )
    .all();

  return c.json(result.results);
});

export default app;
