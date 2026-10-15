# Kwakhe-Khumalo-prog6212-part1-Raceday

# RaceDay
A race-day management platform for creating events, running enrolments, and publishing results created using SQL in SSMS.
## Overview
RaceDay lets event organisers set up races, break each race into categories (e.g. distances),
and open them up for participants to join. Once a race has taken place, organisers capture
results per category so participants can check how they placed.

## User Roles
This system supports two roles, each with different permissions.

### Organiser
Creates events, adds categories to their own events, reviews who has enrolled, and records
results once the race is finished. Can only modify or delete events and categories they own.

### Participant
Browses available events and categories, enrols in a category if space allows, views their own
enrolment history, and checks their published results.

## Repository Layout
docs/
  RaceDay_ERD.png                 - Entity Relationship Diagram
  RaceDay_API_Endpoint_Plan.docx  - API endpoint plan
  raceday_schema.sql              - Database schema and seed data
  ci-success.png                  - Screenshot of a passing CI run
.github/workflows/validate-docs.yml - Checks /docs has the required files

## Continuous Integration
A GitHub Actions workflow runs on every push to confirm the /docs folder exists and contains
the ERD, the endpoint plan, and the SQL script.

## Walkthrough Video
A walkthrough video covering the planning documents, ERD reasoning, endpoint plan choices, and a
live run of the SQL script in SSMS will be attatched below.

https://youtu.be/IQbBa-yHIio

## CI workflow

<img width="725" height="207" alt="image" src="https://github.com/user-attachments/assets/7bea6bba-270f-4c29-903d-e623b048675c" />


 Part 2: RESTful API Development


### Project setup and database
- ASP.NET Core Web API connected to SQL Server through EF Core Code-First, with migrations.
- Six entities that match the Part 1 ERD: Users, Venues, Events, Categories, Enrolments and Results.
- Three venues are seeded automatically.

### Authentication and role-based access
- Registration with a role choice (Organiser or Participant). The server accepts only those two roles.
- Passwords are hashed (PBKDF2 with a salt) and the original password is never stored.
- Login starts a session using an encrypted, HttpOnly cookie that carries the user's ID and role.
- Non-public endpoints return **401** without a session. Wrong-role requests return **403**.
- Ownership checks: an Organiser can only change their own events, categories and results. A Participant can only see their own enrolments and results.

### Endpoints
- **Authentication:** register, login, logout.
- **User profile:** view and update own profile, and view another user's public profile.
- **Events:** create, update and delete (Organiser), and view (public). Each event has a name, description, date, venue (location) and type (Run, Walk or Cycle).
- **Categories:** Organisers define them per event (name, distance, capacity). Everyone can view them.
- **Event enrolments:** Participants enrol in a category, with capacity and duplicate checks. Organisers view enrolments for their events. Either role can cancel where allowed.
- **Results:** Organisers capture finish time, position and status (Finished, DNF or DQ). Participants view their own results, and each category has a public leaderboard.

### Swagger
Every endpoint is visible and testable in the Swagger UI. Each one documents its purpose, the data it expects and the responses it returns. The root URL redirects to `/swagger`.

### Unit and integration tests (xUnit)
- Registration and login (success and failure).
- Event management, including ownership rules.
- Role rejection: 401 for anonymous requests, and 403 for Participants on Organiser endpoints and the other way round.
- Event enrolments, including recording, duplicates and full categories.
- End-to-end test of the whole flow: event, category, enrolment, result, and the participant viewing it.

