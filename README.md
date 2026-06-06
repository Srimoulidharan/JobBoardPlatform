# JobBoardPlatform

A functional ASP.NET Core MVC job board with three roles:

- **Admin**: activate/deactivate users, approve/reject jobs, manage job visibility.
- **Employer**: register, wait for admin activation, create/edit/delete jobs, review applicants.
- **Job Seeker**: register, browse approved active jobs, apply with a resume, track application status.

## Requirements

- .NET 8 SDK
- MySQL 8.x

## Setup

1. Create a MySQL database user or use an existing local MySQL user.
2. Update `appsettings.Development.json` and `appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "server=localhost;database=JobBoardDB;user=root;password=YOUR_MYSQL_PASSWORD;"
   }
   ```

3. From the project folder, run:

   ```bash
   dotnet restore
   dotnet ef database update
   dotnet run
   ```

The application also calls `Database.Migrate()` at startup, so migrations will be applied automatically when the database connection is valid.

## Default admin login

On first successful database startup, the app seeds an admin account if no admin exists:

- Email: `admin@jobboard.local`
- Password: `Admin@123`

Change these in the `SeedAdmin` section of `appsettings.json` before first run, or change the password after first login.

## Main workflow

1. Log in as admin.
2. Register an employer account from the public Register page.
3. In admin, activate the employer account.
4. Employer logs in and posts a job.
5. Admin approves the job.
6. Job seekers can now see and apply to the approved active job.

## Notes

- Uploaded resumes are saved in `wwwroot/uploads`.
- Profile pictures are saved in `wwwroot/uploads/profile_pics`.
- Employer-edited jobs are sent back to `Pending` for admin review.
- Job seekers only see jobs that are `Approved`, active, and not expired.
