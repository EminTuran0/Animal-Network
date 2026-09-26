# Animal Network

A social network for pets, built as an ASP.NET Core 8 MVC application. Owners register,
create profiles for their animals, post photos and updates on their animals' behalf, follow
other users and animals, organise local events and message each other.

## Features

- **Accounts** — cookie-based registration and login with BCrypt-hashed passwords and a
  30-day sliding session.
- **Animal profiles** — full CRUD for animals, each with a type, a breed, a location and an
  adoptable flag. Animals can be followed independently of their owners.
- **Posts** — create, edit and delete posts, optionally attributed to one of your animals;
  like/unlike, comment, and read a personalised feed assembled from the accounts you follow.
- **User profiles** — edit your own profile, follow and unfollow other users, browse follower
  and following lists, and list the animals you own.
- **Events** — create and edit events tied to a location and date, mark yourself as
  attending, and browse past events, your own events and the ones you are attending.
- **Messaging** — one-to-one conversations with an inbox, per-conversation threads and an
  unread view.
- **Search** — site-wide search plus targeted search for animals (by type, breed, location and
  adoptability), events (by text and date range) and users.

## Data model

Thirteen entities under `Models/Entities`, wired up in `AnimalNetworkDbContext`:

`User`, `Animal`, `AnimalType`, `AnimalBreed`, `Location`, `Post`, `Comment`, `Like`,
`Follow`, `AnimalFollow`, `Message`, `Event`, `EventParticipant`.

Relationships, delete behaviour and uniqueness are configured explicitly in `OnModelCreating`.
Unique indexes guard usernames and e-mail addresses, and prevent duplicate likes, duplicate
follows (user→user and user→animal) and duplicate event participation.

## Tech stack

| Concern | Choice |
| --- | --- |
| Framework | ASP.NET Core 8.0 MVC (`net8.0`) |
| ORM | Entity Framework Core 9.0.5 (SQL Server provider) |
| Direct SQL | Dapper 2.1.66 with `Microsoft.Data.SqlClient` 6.0.2 |
| Passwords | BCrypt.Net-Core 1.6.0 |
| Auth | Cookie authentication |
| Frontend | Razor views, Bootstrap 5, jQuery validation |
| Database | SQL Server |

The project deliberately mixes two data-access styles: EF Core handles the domain model,
migrations and the richer relational queries, while the account flow in `AccountController`
uses Dapper against raw SQL.

## Getting started

### Requirements

- .NET 8 SDK
- SQL Server (LocalDB, Express or a full instance)

### Setup

1. Point the connection string at your own instance in
   `Animal Network/AnimalNetwork-master/appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=AnimalNetwork;Integrated Security=True;TrustServerCertificate=True;"
   }
   ```

2. Run the app:

   ```bash
   cd "Animal Network/AnimalNetwork-master"
   dotnet run
   ```

On startup `Program.cs` applies pending migrations and then calls `DbInitializer`, so the
database is created and populated automatically on first run.

### Seed data

`DbInitializer` only runs against an empty `Users` table. When it does run it generates a
sizeable demo dataset:

| Entity | Rows |
| --- | --- |
| Locations | 20 cities (Turkish and international) |
| Users | 500 |
| Animals | 1,000 |
| User follows | 2,000 |
| Animal follows | 2,000 |
| Posts | 3,000 |
| Comments | 5,000 |
| Likes | 3,000 |
| Events | 100 |
| Event participants | 500 |
| Messages | 500 |

## Repository layout

```
Animal Network/
  AnimalNetwork-master/     the application
    Controllers/            Account, Animal, Post, Profile, Event, Message, Home
    Models/
      Entities/             the 13 domain entities
      Logic/                AnimalNetworkDbContext, DbInitializer
      ViewModels/           Login, Register, Error
    Views/                  Razor views per controller plus shared layout
    wwwroot/                site assets, Bootstrap, jQuery
  WebApplication1/          scaffolding leftover, not part of the app
  ConsoleApp1/              scaffolding leftover, not part of the app
```

Only `AnimalNetwork-master` is the real project; the two `*1` folders are empty Visual Studio
templates that were committed alongside it.
