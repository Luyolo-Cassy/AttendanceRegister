# INF3003W Attendance Register

ASP.NET Core 8 Razor Pages app replacing the manual paper sign-sheet process with
role-based digital check-in, Excel bulk import, and attendance reporting.

## Running it locally

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
cd AttendanceRegister
dotnet restore
dotnet ef migrations add InitialCreate   # first time only - see note below
dotnet run
```

Open the URL shown in the console (e.g. `https://localhost:7xxx`). The database
(`AttendanceRegister.db`, SQLite) and roles/demo account are created automatically
on first run.

> **Note on migrations:** this project ships without a `Data/Migrations` folder
> because the environment it was built in couldn't reach NuGet to restore packages
> and generate one. Run `dotnet ef migrations add InitialCreate` once before your
> first `dotnet run` (needs `dotnet tool install --global dotnet-ef` if you don't
> have it). After that, `dotnet run` applies migrations automatically via `SeedData`.

### Troubleshooting: `SqliteException: no such table: AspNetRoles`

This means the app started before a migration existed (or against a stale build).
Fix:

```bash
rm -rf bin obj AttendanceRegister.db AttendanceRegister.db-shm AttendanceRegister.db-wal
ls Data/Migrations   # should list InitialCreate.cs etc - if empty, run the migrations add command above
dotnet ef database update
dotnet run
```

**Demo lecturer login:** `lecturer@uct.ac.za` / `ChangeMe123!`
(seeded automatically - change or remove this before submitting/deploying).

## AI features setup

Three features call the Claude API (Anthropic): at-risk nudge drafts, the natural-language
class query box, and the chat widget. They need an API key that is **not** committed to the
project - set it via .NET user secrets:

```bash
dotnet user-secrets init
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."
```

Get a key at https://console.anthropic.com. Without a key set, those three features return a
friendly "not configured" message instead of crashing - everything else in the app works fine
without it.

- **Lecturer > At-Risk**: drafts a short nudge message per below-75% student, grounded in
  their actual attendance pattern (not a generic template). Lecturer reviews/edits before
  sending anything themselves - the AI drafts, it doesn't decide or send.
- **Lecturer > Ask AI**: plain-English questions answered from a real summary of your class
  data (built server-side from the database, not invented).
- **Chat widget** (bottom-right, once logged in): general help + questions about your own
  attendance (students) or course stats (lecturers). Read-only - it can't perform actions.

## Trying the Excel import

`sample-attendance-import.xlsx` in this folder is the provided sample attendance
list reshaped into the format the Upload page expects (Student Name, Student No,
then one column per lecture date). Register student accounts using the same
Student No values (STDNUM001, STDNUM002, ...) first, then upload the file from
the Lecturer > Upload page to bulk-import their historical attendance.

## Project structure

- `Models/` - domain entities: `ApplicationUser`, `Lecture`, `AttendanceRecord`
- `Data/` - `ApplicationDbContext`, `SeedData` (roles + demo lecturer)
- `Services/` - `IAttendanceCodeGenerator` (check-in code generation)
- `Pages/Account/` - custom register/login/logout (not the packaged Identity UI)
- `Pages/Student/` - dashboard, check-in, history, query
- `Pages/Lecturer/` - dashboard, lecture CRUD + check-in windows, Excel upload, edit, chart report

## Design notes / bonus features

- **Time-boxed check-in codes**: a lecturer "opens" attendance for 15 minutes and
  displays a short code; students must be given that code in person and check in
  while the window is open. Mitigates the obvious flaw of a plain sign-in link
  (a student checking in from bed).
- **Attendance streaks + at-risk flagging**: student dashboard shows a live streak
  and highlights when attendance drops under 75%; lecturer report page lists
  at-risk students directly rather than making the lecturer read a spreadsheet.
- **Query flow**: a student can flag a specific record as disputed from `Query`;
  it surfaces on the lecturer dashboard for resolution via `Edit`, closing the loop
  the case study describes ("lecturers need to edit/update attendance depending on
  student queries") without a separate ticketing system.
