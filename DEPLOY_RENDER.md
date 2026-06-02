# MyMiniLMS: Docker and Render deployment

## Local Docker run

1. Install Docker Desktop.
2. Copy `.env.example` to `.env`.
3. Change `POSTGRES_PASSWORD` in `.env`.
4. Run:

```bash
docker compose up --build
```

5. Open:

```text
http://localhost:8080
```

The compose file starts two services:

- `app` - ASP.NET Core MVC application
- `postgres` - local PostgreSQL database

On startup, the app applies EF Core migrations and then seeds demo data.

## Render deployment

1. Push the project to GitHub.
2. In Render, create a new PostgreSQL database.
3. Copy its internal database URL.
4. Create a new Web Service from the GitHub repository.
5. Select Docker as the runtime.
6. Add environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
DATABASE_URL=<Render internal PostgreSQL URL>
```

The app reads `DATABASE_URL` automatically and converts it to the PostgreSQL connection string format used by Npgsql.

Render provides a dynamic `PORT` variable for web services. The Dockerfile uses it automatically and falls back to `8080` for local runs.
