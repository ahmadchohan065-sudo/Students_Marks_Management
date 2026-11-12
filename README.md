# Student Marks Management API

A small production-style REST API for a university department. Teachers create students and courses, enroll students in courses and record marks. A student can see only his or her own record and transcript.

Built with **C#, ASP.NET Core (.NET 10), Entity Framework Core and PostgreSQL**. Authentication uses JWT with two roles, **Teacher** and **Student**. The API is tested with a Postman collection and with a built-in smoke test module.

- Author: Ahmad Abdullah, Reg. No. 23f-0019
- Course: Advanced Programming

---

## 1. Requirements

| Tool | Version | Check with |
|---|---|---|
| .NET SDK | 10 | `dotnet --version` |
| PostgreSQL | 16 | `psql --version` (or open pgAdmin) |
| pgAdmin 4 | any | optional, to look at the tables |
| Postman | desktop or web | to run the collection |
| Git | any | `git --version` |

The `dotnet-ef` tool is also needed (installed in step 4 below).

---

## 2. Project structure

```
RegNo_Name_Assignment1/
  src/MarksApi/
    Common/        exceptions, global error handler, validation filter, options
    Data/          AppDbContext, DbSeeder, Entities/
    Dtos/          request and response classes
    Services/      business logic, each behind an interface
    Endpoints/     HTTP routes (no queries, no business rules)
    SmokeTests/    startup smoke test module
    Migrations/    EF Core migrations
    Program.cs
  postman/         collection and environment JSON files
  db/schema.sql    schema-only export of the database
  docs/            erd.png, report.pdf, screenshots/
  appsettings.Example.json
  README.md
  .gitignore
```

---

## 3. Setup from a clean copy

Follow these steps in order. Commands starting with `dotnet` are run in `src/MarksApi` unless stated otherwise.

### Step 1. Clone the repository
```
git clone https://github.com/ahmadchohan065-sudo/Students_Marks_Management  
cd AP_Assignment\src\MarksApi
```

### Step 2. Create the database and the database user
The application must **not** use the administrator account. It uses `marks_user` only.

Using psql:
```
psql -U postgres
CREATE USER marks_user WITH PASSWORD 'choose_a_password';
CREATE DATABASE marks_db OWNER marks_user;
\q
```
If `psql` is not recognised on Windows, open **pgAdmin 4**, connect as `postgres`, open the Query Tool on the `postgres` database and run the two `CREATE` statements (one at a time).

### Step 3. Set the secrets (user-secrets)
No password or key is stored in the repository. Run these in `src/MarksApi`:
```
cd src/MarksApi
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=marks_db;Username=marks_user;Password=choose_a_password"
dotnet user-secrets set "Jwt:Key" "replace-with-a-long-random-text-of-32-or-more-characters"
```
For the smoke test (see section 8) also set the login details of the demo users from section 4:
```
dotnet user-secrets set "Smoke:TeacherUsername" "teacher1"
dotnet user-secrets set "Smoke:TeacherPassword" "Teacher@12345"
dotnet user-secrets set "Smoke:StudentUsername" "student1"
dotnet user-secrets set "Smoke:StudentPassword" "Student1@12345"
```
`appsettings.Example.json` in the repository root shows every setting with fake values. On a real server the same values are supplied as environment variables, for example `ConnectionStrings__Database` (two underscores).

Non-secret settings are in `appsettings.json`: JWT issuer, audience and lifetime, the pass mark (50) and the allowed CORS origins.

### Step 4. Create the tables with migrations
```
dotnet tool install --global dotnet-ef
dotnet ef database update
```
(If the tool is already installed, use `dotnet tool update --global dotnet-ef`.)
Open pgAdmin and check that `students`, `courses`, `enrollments` and `users` exist in `marks_db`. The database is created only by migrations, never by hand.

### Step 5. Trust the HTTPS development certificate (one time)
```
dotnet dev-certs https --trust
```

### Step 6. Run the API
```
dotnet run --launch-profile https
```
The console prints the address, for example `https://localhost:7172`. The port differs on each computer and is stored in `Properties/launchSettings.json`.

In the Development environment the application seeds demo data on startup, but only if the `users` table is empty. Restarting does not duplicate anything.

Quick checks (use your own port):
- `https://localhost:7172/health` should show `{"status":"Healthy"}`
- `https://localhost:7172/scalar/v1` shows the interactive API documentation

---

## 4. Demo users (DEVELOPMENT ONLY)

These accounts are created by the seeder and exist only for development and testing. Their passwords are stored hashed in the database. Never use these accounts or passwords in a real deployment.

| Username | Password | Role | Linked student |
|---|---|---|---|
| teacher1 | Teacher@12345 | Teacher | none |
| student1 | Student1@12345 | Student | CS-2022-001 (Ali Raza) |
| student2 | Student2@12345 | Student | CS-2022-002 (Sara Khan) |

Seed data: 1 teacher, 2 student users linked to 2 students, 3 courses (CS401, MA201, EN101) and 6 enrollments, some graded and some not.

---

## 5. Endpoints

All requests and responses use JSON with camelCase names. Dates are ISO 8601 in UTC. Every error is returned as problem details (`status`, `title`, `detail`, and an `errors` object for validation problems). List endpoints return `items`, `page`, `pageSize` and `totalCount`, and accept `page` (default 1), `pageSize` (default 10, maximum 50), `search` and `sort`.

| Method | Path | Who may call | Success | Error codes |
|---|---|---|---|---|
| POST | /api/auth/login | Anyone | 200 | 400, 401 |
| GET | /api/students | Teacher | 200 | 401, 403 |
| GET | /api/students/{id} | Teacher, or the student himself/herself | 200 | 401, 403, 404 |
| POST | /api/students | Teacher | 201 | 400, 401, 403, 409 |
| PUT | /api/students/{id} | Teacher | 200 | 400, 401, 403, 404, 409 |
| DELETE | /api/students/{id} | Teacher | 204 | 401, 403, 404, 409 |
| GET | /api/courses | Teacher, Student | 200 | 401 |
| GET | /api/courses/{id} | Teacher, Student | 200 | 401, 404 |
| POST | /api/courses | Teacher | 201 | 400, 401, 403, 409 |
| PUT | /api/courses/{id} | Teacher | 200 | 400, 401, 403, 404, 409 |
| DELETE | /api/courses/{id} | Teacher | 204 | 401, 403, 404, 409 |
| POST | /api/courses/{courseId}/enrollments | Teacher | 201 | 400, 401, 403, 404, 409 |
| GET | /api/courses/{courseId}/enrollments | Teacher | 200 | 401, 403, 404 |
| PUT | /api/enrollments/{id}/mark | Teacher | 200 | 400, 401, 403, 404 |
| DELETE | /api/enrollments/{id} | Teacher | 204 | 401, 403, 404 |
| GET | /api/courses/{courseId}/stats | Teacher | 200 | 401, 403, 404 |
| GET | /api/students/{id}/transcript | Teacher, or the student himself/herself | 200 | 401, 403, 404 |
| GET | /health | Anyone | 200 | 503 |

### Business rules
- A mark is a whole number from 0 to 100, or null (not graded yet). Anything else gives 400.
- The pass mark (50) is read from configuration (`Marking:PassMark`), not written in code.
- A course has 1 to 4 credit hours.
- A student can be enrolled in a course only once (409 on a second attempt).
- A student or course that still has enrollments cannot be deleted (409).
- Registration number, course code and email must be unique (409 on a duplicate).
- The course average uses graded enrollments only. If nobody is graded, the average is `null`, not 0.
- The transcript average is weighted by credit hours: the sum of (mark x credit hours) divided by the sum of credit hours, over graded courses only.
- A Student user can read only his or her own record and transcript. Another student's data gives 403.

---

## 6. API documentation

Interactive documentation (Scalar, generated from the OpenAPI description of the API):

- `https://localhost:<port>/scalar/v1`
- Raw OpenAPI JSON: `https://localhost:<port>/openapi/v1.json`

To call protected endpoints from Scalar: log in with `POST /api/auth/login`, copy the token, open the authentication panel, choose Bearer and paste the token.

---

## 7. Running the Postman collection

Files are in the `postman/` folder:
- `MarksApi.postman_collection.json` (Collection v2.1, **43** requests)
- `Local.postman_environment.json` (the token value is removed)

Steps:
1. In Postman, click **Import** and select both files.
2. Choose the **Local** environment (top right).
3. Set `baseUrl` to your API address, for example `https://localhost:7172`.
4. Start the API (`dotnet run --launch-profile https`).
5. Open the collection, click **Run**, select the **Local** environment and run it.

The folders run in this order, because later requests use ids saved by earlier ones: **Auth, Students, Courses, Enrollments and Marks, Reports, Health, Negative Tests**.

Notes:
- The collection sends tokens through `{{token}}` (teacher) and `{{studentToken}}` (student). The login requests save them automatically, so nothing needs to be copied by hand.
- The collection creates records with fixed codes (for example `PM-401` and `PM401`), so it must be run on a **fresh database**. Running it twice in a row on the same database produces 409 conflicts by design. See "Resetting the database" below.
- Every request has at least two tests (status code plus body or header). Negative tests cover 400, 401, 403, 404 and 409.

### Resetting the database
Stop the API first (Ctrl+C), then in `src/MarksApi`:
```
dotnet ef database drop
dotnet ef database update
dotnet run --launch-profile https
```
Type `y` when asked to confirm the drop. The seeder runs again on startup and recreates the demo data.

---

## 8. Smoke test

The smoke test is a module in `src/MarksApi/SmokeTests/`. It starts the real API, then calls **every endpoint** over real HTTP against the real PostgreSQL database. For endpoints that can fail it also sends failing requests and checks the expected code (400, 401, 403, 404 or 409).

Run it (stop any other running copy of the API first):
```
dotnet run --launch-profile https -- --smoke-test
```
(Alternatively set the environment variable `SMOKE_TEST=true`.)

- It runs **only on request**. A normal start never runs it.
- Teacher and student login details are read from configuration (user-secrets or environment variables), never from source code.
- Test data is prefixed with `SMOKE-` or `SMK` and is deleted when the run ends, so the database is the same afterwards. Running it twice in a row gives the same result.
- Output is one line per check (`PASS` or `FAIL`, method, path, expected and actual status code) followed by a summary line. The process exits with code 0 if all checks pass and a non-zero code otherwise.

---

## 9. Design decisions

1. **Layers.** Endpoints only map routes and shape HTTP results. Business rules and queries live in services, each behind an interface and registered with dependency injection. Data access is through the scoped `AppDbContext`. This keeps each part small and replaceable.
2. **DTOs everywhere.** Entities are never returned or accepted. This prevents leaking fields such as `PasswordHash` and prevents over-posting.
3. **409 for duplicates, not 400.** A duplicate request is well formed, so it is not a bad request. It conflicts with the current state of the data, which is what 409 means.
4. **Unique indexes instead of check-then-insert.** The database has unique indexes on registration number, email, course code, username and (student, course). The service inserts and catches the PostgreSQL unique violation (error 23505), so two simultaneous requests cannot both succeed.
5. **Delete restricted.** Foreign keys use `Restrict`, so a student or course with enrollments cannot be deleted. The foreign-key error (23503) is turned into 409 and marks are never lost silently.
6. **Null means not graded.** A mark of null is different from 0, so the course average is null when nobody is graded, and `passed` is null for an ungraded course.
7. **Weighted average.** The transcript uses sum(mark x credits) / sum(credits) over graded courses. It never averages averages.
8. **Own-data rule on the server.** The student id is read from the signed JWT, so a student cannot claim another id. The check happens before the database lookup, so the API does not reveal which ids exist.
9. **One error format.** A global exception handler returns problem details for every failure. Unexpected errors become a clean 500 with no stack trace; the full error goes only to the log.
10. **Safe paging.** The server corrects `page` and `pageSize` (maximum 50) and only accepts known `sort` values.
11. **Configuration.** The pass mark, JWT settings and CORS origins come from configuration. Secrets come from user-secrets or environment variables and are never committed.
12. **UTC everywhere.** `DateTime.UtcNow` is used and columns are `timestamptz`.
13. **Async everywhere.** Every database call is async and receives the `CancellationToken`. Read-only queries use `AsNoTracking` and project to DTOs with `Select`.
14. **API documentation.** Scalar with the built-in .NET 10 OpenAPI support replaces Swagger, which is allowed by the assignment.
---

## 10. Troubleshooting

| Problem | Likely cause and fix |
|---|---|
| `psql` is not recognised | Use pgAdmin's Query Tool, or add PostgreSQL's `bin` folder to PATH |
| `/health` says Unhealthy | Wrong connection string, password or port in user-secrets |
| Login returns 401 | The users table is empty: run `dotnet ef database update`, then restart the API in Development |
| 404 on `/swagger` | Swagger is not used. Open `/scalar/v1` |
| Postman collection shows 409 errors | The database already has the test data. Reset it (section 7) |
| Browser or Postman warns about the certificate | Run `dotnet dev-certs https --trust` |
| `Jwt:Key is not configured` | Set the secret in step 3 |
