# 🏁 RaceDay Event Management System
**PROG6212 - Portfolio of Evidence (Part 3)**

* **Student ID:** ST10484350
* **Module:** PROG6212 (Programming 2B)
* **Assessment:** POE Final Submission (Parts 1, 2, and 3)

---

## 📌 Project Overview

For my **PROG6212 POE**, I built **RaceDay**—a full-stack, microservice-inspired web application designed for organizing and registering for athletic events like marathons, trail runs, and cycling races.

Instead of keeping everything in one large project, I separated the system into two independent services to follow clean software architecture principles:
1. **`RaceDay.API` (Backend):** An ASP.NET Core Web API that handles all business logic, database transactions via EF Core, user authentication, and Azure Blob Storage integration.
2. **`RaceDay.MVC` (Frontend):** An ASP.NET Core MVC web app that serves as the user-facing dashboard. It communicates with the backend API asynchronously using `HttpClient`.

The entire solution is fully containerized using **Docker** and **Docker Compose**, and automated using a **GitHub Actions CI/CD pipeline**.

---

## 🏗️ Architecture & How It Works

```
                        ┌─────────────────────────────────┐
                        │      Browser / User Client      │
                        └────────────────┬────────────────┘
                                         │ HTTP
                                         ▼
                        ┌─────────────────────────────────┐
                        │    RaceDay.MVC (Frontend UI)    │
                        │    (Razor Views, Sessions, Auth)│
                        └────────────────┬────────────────┘
                                         │ REST API (HttpClient)
                                         ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                            RaceDay.API (Backend)                             │
│   ┌─────────────────────┐    ┌─────────────────────┐   ┌─────────────────┐   │
│   │ Controllers & DTOs  │ ──►│  Services & Logic   │──►│ Entity Framework│   │
│   └─────────────────────┘    └─────────────────────┘   └─────────────────┘   │
└──────────────┬─────────────────────────────────────────────────┬─────────────┘
               │                                                 │
               ▼                                                 ▼
┌─────────────────────────────┐                   ┌─────────────────────────────┐
│  SQL Server Database        │                   │ Azure Blob Storage / Azurite│
│  (Users, Events, Enrolments)│                   │ (Event Banners & Media)     │
└─────────────────────────────┘                   └─────────────────────────────┘
```

### Key Technical Decisions
* **Decoupled Architecture:** The MVC frontend never queries the SQL database directly. All data access goes through the Web API endpoints.
* **Role-Based Access Control (RBAC):** Users log in as either an **Organiser** or a **Participant**.
  * **Organisers** can create, edit, delete, and manage participant lists for their own events.
  * **Participants** can search, view event details, enrol, and manage their registrations under "My Enrolments".
* **Cloud Media Storage:** Event images are uploaded directly to **Azure Blob Storage** (with local Azurite emulator support for local testing).

---

## ✨ Features Implemented

### 🔐 Authentication & Security
* User registration and login with password hashing.
* Session-based state management on the MVC app.
* Strict authorization checks: Organisers can only modify events that they personally created (returns a `403 Forbidden` otherwise).

### 📅 Event Management (CRUD)
* Organisers can create events, pick event categories (e.g., Running, Cycling, Triathlon), set maximum participant capacity, and upload event banners.
* Automatic capacity calculation (tracks current enrolments vs maximum allowed spots).
* Search and category filtering on the public event listings.

### ✍️ Participant Enrolment System
* Participants can enrol in upcoming events with a single click.
* Live status tracking on the "My Enrolments" dashboard.
* Ability for participants to cancel their registration before an event starts.

---

## 🛠️ Technology Stack

* **Language & Framework:** C# / .NET 8.0 SDK
* **Backend:** ASP.NET Core Web API, Entity Framework Core 8, Swagger OpenAPI
* **Frontend:** ASP.NET Core MVC, Razor Views, Bootstrap 5, JavaScript
* **Database:** SQL Server / MS SQL LocalDB
* **Cloud Services:** Azure Blob Storage Client Library (`Azure.Storage.Blobs`)
* **DevOps & Infrastructure:** Docker Engine, Docker Compose, GitHub Actions (YAML)

---

## 📁 Repository Directory Structure

```text
ST10484350_PROG6212_POE/
├── .github/
│   └── workflows/
│       └── docker-ci.yml           # GitHub Actions CI workflow pipeline
├── RaceDay.API/                    # ASP.NET Core Web API Project
│   ├── Controllers/                # API Controller routes
│   ├── Data/                       # DbContext & EF Migrations
│   ├── Models/                     # Database Domain Models
│   ├── Services/                   # Blob Storage & Business Logic
│   └── Dockerfile                  # Container instructions for Web API
├── RaceDay.MVC/                    # ASP.NET Core MVC Frontend Project
│   ├── Controllers/                # View Controllers consuming Web API
│   ├── Views/                      # Razor UI Templates
│   └── Dockerfile                  # Container instructions for MVC
├── docker-compose.yml              # Multi-container setup file
├── docker-compose.override.yml     # Local environment port overrides
├── RaceDay.API.sln                 # Main Visual Studio Solution
└── README.md                       # Project Documentation
```

---

## 💻 How to Run the Project

You can run this project locally using **Docker Compose** (recommended) or directly through **Visual Studio / .NET CLI**.

### Prerequisites
Make sure you have the following installed:
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) (with ASP.NET & Web Development workload)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) (running in Linux container mode)

---

### Option A: Running via Docker Compose (Recommended)

This is the easiest way to run the entire multi-container stack without worrying about local SQL server configurations or dependency setups.

1. **Open your terminal / PowerShell** in the project root folder (where `docker-compose.yml` is located).

2. **Build and start the containers:**
   ```bash
   docker-compose up --build -d
   ```

3. **Verify the containers are running:**
   ```bash
   docker-compose ps
   ```

4. **Access the applications in your web browser:**
   * **MVC Web Frontend:** `http://localhost:5002` (or `http://localhost:8080`)
   * **Web API / Swagger Docs:** `http://localhost:5000/swagger`

5. **Stop the containers when finished:**
   ```bash
   docker-compose down
   ```

---

### Option B: Running via Visual Studio / .NET CLI

If you want to debug or edit code in real time:

1. **Clone the repository:**
   ```bash
   git clone https://github.com/ST10484350/ST10484350_PROG6212_POE_Part1.git
   cd ST10484350_PROG6212_POE_Part1
   ```

2. **Open the solution file:**
   Open `RaceDay.API.sln` in Visual Studio 2022.

3. **Apply Database Migrations:**
   Open the **Package Manager Console** (`Tools > NuGet Package Manager > Package Manager Console`) and run:
   ```powershell
   Update-Database -Project RaceDay.API
   ```
   *Or using the .NET CLI in your terminal:*
   ```bash
   dotnet ef database update --project RaceDay.API
   ```

4. **Set Up Multiple Startup Projects in Visual Studio:**
   * Right-click the top Solution node in Solution Explorer > **Configure Startup Projects...**
   * Select **Multiple startup projects**.
   * Set **`RaceDay.API`** to `Start`.
   * Set **`RaceDay.MVC`** to `Start`.
   * Press **F5** to start debugging both projects together.

---

## 📡 API Reference Overview

The backend Web API exposes RESTful JSON endpoints. Interactive documentation is available via Swagger at `/swagger` when running the API.

| HTTP Method | Endpoint Route | Required Role | Functionality |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Public | Create a new user account |
| `POST` | `/api/auth/login` | Public | Authenticate user credentials & start session |
| `GET` | `/api/events` | Public | Fetch all active events (supports search/filter) |
| `GET` | `/api/events/{id}` | Public | Fetch single event details |
| `POST` | `/api/events` | Organiser | Create a new event |
| `PUT` | `/api/events/{id}` | Organiser | Update an event owned by the logged-in organiser |
| `DELETE` | `/api/events/{id}` | Organiser | Soft delete an event |
| `GET` | `/api/enrolments/my-enrolments` | Participant | Get list of events registered by current user |
| `POST` | `/api/enrolments/enrol/{eventId}`| Participant | Enrol in an event |
| `DELETE` | `/api/enrolments/cancel/{id}` | Participant | Cancel an active enrolment |
| `POST` | `/api/storage/upload` | Authorized | Upload image file to Azure Blob Storage |

---

## 🔄 GitHub Actions CI/CD Pipeline

As part of Phase 3, I created an automated Continuous Integration pipeline stored in `.github/workflows/docker-ci.yml`.

### Workflow Configuration (`docker-ci.yml`)
```yaml
name: RaceDay Docker CI

on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout repository code
      uses: actions/checkout@v3

    - name: Build Docker Compose Stack
      run: docker-compose -f docker-compose.yml build
```

### What this pipeline does:
* Triggers automatically whenever code is pushed or merged into the `main` branch via a Pull Request.
* Spins up a clean Linux VM (`ubuntu-latest`) on GitHub's cloud.
* Verifies that both Dockerfiles and the multi-container `docker-compose.yml` manifest compile cleanly without syntax or missing dependency errors.

---

## 🧪 Marker / Testing Guide

If you are evaluating this project, you can use these steps to quickly test all functional requirements:

1. Launch the app using **Docker Compose** or **Visual Studio**.
2. **Register a new account as an Organiser:**
   * Click **Register** > Select role **Organiser**.
   * Create an event, attach an image banner, set a maximum participant limit, and save.
3. **Log out and Register a new account as a Participant:**
   * Click **Register** > Select role **Participant**.
   * Browse the homepage, filter events by category, click into your created event, and click **Enrol**.
   * Navigate to **My Enrolments** to verify the event shows up under your active registrations.
4. **Test Security Controls:**
   * Attempt to edit an event while logged in as a participant or as a different organiser to verify the `403 Forbidden` protection.

---

## 👤 Author & Acknowledgments

* **Student:** Cassidy Leigh Moonian (ST10484350)
* **Course:** PROG6212 - Programming 2B
* Built using Microsoft ASP.NET Core, EF Core, Azure Blob Storage, Docker, and GitHub Actions.
