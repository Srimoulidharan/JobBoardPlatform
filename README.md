# JobBoardPlatform

A functional ASP.NET Core MVC job board with three roles:

- **Admin**: activate/deactivate users, approve/reject jobs, manage job visibility.
- **Employer**: register, wait for admin activation, create/edit/delete jobs, review applicants.
- **Job Seeker**: register, browse approved active jobs, apply with a resume, track application status.

## Requirements

- .NET 8 SDK
- MySQL 8.x


## Notes

- Uploaded resumes are saved in `wwwroot/uploads`.
- Profile pictures are saved in `wwwroot/uploads/profile_pics`.
- Employer-edited jobs are sent back to `Pending` for admin review.
- Job seekers only see jobs that are `Approved`, active, and not expired.
