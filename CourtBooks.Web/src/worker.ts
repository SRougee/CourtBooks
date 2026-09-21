import { Hono } from "hono";

type Env = {
  DB: D1Database;
};

const api = new Hono<{ Bindings: Env }>().basePath("/api");

api.get("/health", (c) => c.json({ status: "ok", service: "courtbooks-api" }));

api.get("/students", async (c) => {
  const result = await c.env.DB.prepare(
    "SELECT Id, FirstName, LastName, Phone, Email, DateOfBirth, Notes, Active FROM Students ORDER BY LastName, FirstName"
  ).all();
  return c.json(result.results);
});

export default api;
