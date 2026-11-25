# Savorly API

The API uses PostgreSQL through Npgsql and can run as a Render Docker Web
Service. Render terminates public HTTPS and supplies the service's `PORT`;
the Docker entrypoint binds Kestrel to that port (default `10000`).

## Provision Supabase

Create a Supabase project, then run
[`database/supabase-setup.sql`](./database/supabase-setup.sql) once in its SQL
Editor against a **new, empty project**. It creates the application tables,
enables row-level security without client policies, creates the public
`menu-images` bucket for read-only images, and records the existing EF migration
history plus the PostgreSQL baseline.

Do not run this setup script against a database that already contains these
tables. Back up the current database before migrating.

The API accesses database tables from the server. Do not put database
credentials or the Supabase service-role key in the Expo app. The SQL script
revokes table access from Supabase's `anon` and `authenticated` roles; RLS is
enabled without policies, so the Data API cannot read or write these tables.
Keep the database connection string and Storage service-role key server-side.

## Migrate data

The setup script creates an empty schema. Export existing rows from the
current database and import them into Supabase in this order:

1. `Restaurants`
2. `Users`
3. `MenuItems`
4. `Orders`
5. `OrderItems`

Preserve `Users.Id` UUIDs, all integer IDs, password hashes, timestamps and
foreign-key values. Convert `DateTimeOffset` values to UTC for PostgreSQL.
After importing, advance each identity sequence to the imported maximum ID:

```sql
SELECT setval(
    pg_get_serial_sequence('public."Restaurants"', 'Id'),
    COALESCE(MAX("Id"), 1),
    MAX("Id") IS NOT NULL
) FROM public."Restaurants";

SELECT setval(
    pg_get_serial_sequence('public."MenuItems"', 'Id'),
    COALESCE(MAX("Id"), 1),
    MAX("Id") IS NOT NULL
) FROM public."MenuItems";

SELECT setval(
    pg_get_serial_sequence('public."Orders"', 'Id'),
    COALESCE(MAX("Id"), 1),
    MAX("Id") IS NOT NULL
) FROM public."Orders";

SELECT setval(
    pg_get_serial_sequence('public."OrderItems"', 'Id'),
    COALESCE(MAX("Id"), 1),
    MAX("Id") IS NOT NULL
) FROM public."OrderItems";
```

The API's PostgreSQL baseline migration is intentionally a no-op: the setup
script creates the schema and stamps the old SQL Server migrations plus the
baseline as already applied. Do not run the old SQL Server migrations against
Supabase. Future changes can use normal EF Core migrations:

```powershell
dotnet ef migrations add DescribeYourChange
dotnet ef database update
```

Set the connection string in user-secrets for local development or in your
Render Web Service's **Environment** settings. Use Supabase's connection
details and Npgsql format:

```text
Host=<supabase-host>;Port=5432;Database=postgres;Username=<database-user>;Password=<database-password>;SSL Mode=Require;Trust Server Certificate=true
```

Set `ConnectionStrings__DefaultConnection` to that value. Use Supabase's
session pooler connection details if the direct database host isn't reachable
from Render. Never commit or paste the real connection string into source
control or chat.

## Menu image storage

Menu image uploads use Supabase Storage in non-development environments; the
local filesystem implementation is development-only. The setup script creates
a public `menu-images` bucket with a 5 MB limit and JPEG, PNG, and WebP types.
Public access is for image downloads only; uploads are made by the API using a
server-side service-role key.

Configure these Render environment variables. Mark credential values as
secrets:

| Name | Required | Notes |
| --- | --- | --- |
| `Storage__SupabaseUrl` | Yes | Supabase project URL, e.g. `https://<project-ref>.supabase.co`. |
| `Storage__SupabaseServiceRoleKey` | Yes | Supabase service-role key; never expose it to clients. |
| `Storage__MenuImageBucket` | No | Defaults to `menu-images`. |
| `ConnectionStrings__DefaultConnection` | Yes | Supabase PostgreSQL connection string. |
| `Jwt__Key` | Yes | Secret of at least 32 UTF-8 bytes. |
| `Admin__UserName` | For admin login | Administrator login name. |
| `Admin__Password` | For admin login | Administrator password. |
| `Cors__AllowedOrigins` | For browser clients | Semicolon-separated browser origins. |
| `Jwt__Issuer` | No | Defaults to `Savorly`. |
| `Jwt__Audience` | No | Defaults to `Savorly`. |

Existing menu image files are not moved by the database import. Copy them into
the `menu-images` bucket and update their `MenuItems.ImageUrl` values to the
corresponding public Storage URLs before switching over.
