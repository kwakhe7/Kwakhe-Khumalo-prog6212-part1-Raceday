# Kwakhe-Khumalo-prog6212-part1-Raceday

# RaceDay
A race-day management platform for creating events, running enrolments, and publishing results.
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

## CI workflow

<img width="725" height="207" alt="image" src="https://github.com/user-attachments/assets/7bea6bba-270f-4c29-903d-e623b048675c" />


## License
This project was created for academic purposes.

