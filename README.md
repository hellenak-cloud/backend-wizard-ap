# BackendWizardAPI

A REST API that generates demographic profiles based on a given name using external APIs and stores results in a database.

---

## Features

- Create profile using name
- Integrates:
  - Genderize API
  - Agify API
  - Nationalize API
- Stores data in SQLite database
- Prevents duplicate profiles (idempotency)
- Supports filtering by:
  - gender
  - country_id
  - age_group
- Full CRUD operations

---

## Tech Stack

- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- Swagger (API documentation)

---

## API Endpoints

### Create Profile
POST /api/profiles

```json
{ "name": "ella" }