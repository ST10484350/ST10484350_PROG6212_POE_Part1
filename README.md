# ST10484350_PROG6212_POE_Part1
# RaceDay - Event Management System
**PROG6212 Portfolio of Evidence - Part 1**
**Author:** Cassidy Leigh Moonian

---

The RaceDay API is an event management system built with ASP.NET Core 10. It provides a backend interface for managing race events and handles session-based user authentication for different roles, such as Organisers and Participants. The project utilizes Entity Framework Core for data access, SQL Server for data storage, and Swagger for API documentation and testing.

## Video Walkthrough

A complete video demonstration and walkthrough of the program can be found on YouTube here:
[Insert your YouTube video link here]

## Prerequisites

To run this project locally, you will need:
* .NET 10 SDK
* Visual Studio 2022
* SQL Server (e.g., SQL Server Management Studio with a local SQLEXPRESS instance)

## Setup and Installation

1. **Clone the repository:** Download or clone the project files to your local machine and open the solution in Visual Studio.
2. **Configure the Database:** Open the `appsettings.json` file. Ensure the `DefaultConnection` string matches your local SQL Server instance. By default, it is configured for a local express server:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=RaceDayDb;Integrated Security=True;TrustServerCertificate=True;"
   }
