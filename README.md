# Bananarchy — Online Strategy Game — Core Backend Showcase

This repository contains select core backend modules for an **Bananarchy**, built to demonstrate architectural patterns, database integration, and multiplayer business logic using C# and .NET.

> **Note:** This repository is a curated showcase featuring primary controllers and data models. It highlights key code samples from the main codebase while omitting experimental and configuration files.

## 🛠 Tech Stack

* **Language:** C#
* **Framework:** ASP.NET Core
* **Database & ORM:** PostgreSQL, Entity Framework Core

## Featured Modules Included

* `UsersController.cs` — User management, profile flow, and authorization logic.
* `PlayerBuildingsController.cs` — Economy, building management, and grid-placement logic.
* `CombatController.cs` — Core PvP interaction and battle resolution logiс.
* `PlayerBuilding.cs` / `PlacedBuilding.cs` — Entity Framework models for persistent building state and map placements.

## Key Architecture Highlights

* **Clean Separation of Concerns:** RESTful controller setup handling game state and API endpoints.
* **Persistent Game State:** Entity-mapped structures for dynamic world map building placement and player progression.
* **PvP & Economic Logic:** Server-side validation for combat and resource building mechanics.