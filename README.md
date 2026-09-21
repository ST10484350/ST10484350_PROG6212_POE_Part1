# ST10484350_PROG6212_POE_Part1
# RaceDay - Event Management System
**PROG6212 Portfolio of Evidence - Part 1**
**Author:** Cassidy Leigh Moonian

Project Overview
This repository contains my system design for RaceDay, a platform to manage road events like running, walking, and cycling. Approaching this from a systems analyst perspective, I focused on clearly separating the functional requirements for two main roles:
* Organisers: Can create and manage events, set up race categories, and record the final finish times and positions.
* Participants: Can browse events, enrol in specific categories, pay their entry fees, and check their past race results.

My /docs Folder
You can find all my required Part 1 system design deliverables in the [`/docs`](./docs) folder:
1. ERD (`RaceDay_ERD.drawio.png`): My database design showing the 6 main tables and their relationships.
2. API Endpoint Plan (`Race Day API Endpoint Plan.pdf`): A breakdown of the routes, HTTP methods, and security roles I will need when I build the actual backend API in Part 2.
3. Database Script (`RaceDay_Database_Script.sql`): The SQL Server script that creates the database and inserts sample seed data so it is ready for testing.

Automated Testing (CI/CD)
I configured a GitHub Actions workflow that runs automatically whenever code is pushed. It verifies that my `/docs` folder and all three required files are exactly where they should be. 

Successful Build Screenshot:
![GitHub Actions Build Status](build_status.png)

---
*AI Disclosure: I used an AI assistant as a sounding board to help format my documentation tables, and to generate realistic sample data. I reviewed all outputs to ensure they match my own system logic and the exact rubric requirements.*
