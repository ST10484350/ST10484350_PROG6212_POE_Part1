-- Author: Cassidy Leigh Moonian
-- Project: RaceDay PoE Part 1
-- AI Disclosure: I used an AI assistant to help verify the foreign key constraints, 
-- and generate the seed data inserts to match my ERD.
-- I reviewed all logic before submission.

-- Dropping the DB if it already exists so the script runs cleanly every time
IF EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = N'RaceDayDB')
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO
USE RaceDayDB;
GO

-- 1.Creating users table which handles both organisers and participants
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) CHECK (Role IN ('Organiser', 'Participant')) NOT NULL
);
GO

-- 2.Creating events table
CREATE TABLE Events (
    EventId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500),
    EventDate DATETIME2 NOT NULL,
    Location NVARCHAR(150) NOT NULL,
    Distance DECIMAL(5,2) NOT NULL,
    EventType NVARCHAR(50) CHECK (EventType IN ('run', 'walk', 'cycle')) NOT NULL,
    OrganiserId INT FOREIGN KEY REFERENCES Users(UserId)
);
GO

-- 3.creating categories table
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT FOREIGN KEY REFERENCES Events(EventId) ON DELETE CASCADE,
    CategoryName NVARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(5,2) NOT NULL,
    EntryFee DECIMAL(10,2) NOT NULL
);
GO

-- 4.creating enrolments table
CREATE TABLE Enrolments (
    EnrolmentId INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId INT FOREIGN KEY REFERENCES Users(UserId),
    CategoryId INT FOREIGN KEY REFERENCES Categories(CategoryId),
    EnrolmentDate DATETIME2 DEFAULT GETDATE()
);
GO

-- 5.creating payments table 
CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT FOREIGN KEY REFERENCES Enrolments(EnrolmentId) ON DELETE CASCADE,
    AmountPaid DECIMAL(10,2) NOT NULL,
    PaymentStatus NVARCHAR(30) DEFAULT 'Successful',
    PaymentDate DATETIME2 DEFAULT GETDATE()
);
GO

-- 6.creating results table
CREATE TABLE Results (
    ResultId INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT FOREIGN KEY REFERENCES Enrolments(EnrolmentId) ON DELETE CASCADE,
    FinishTime TIME,
    Position INT
);
GO

-- populating database tables with sample data

-- inserting users
INSERT INTO Users (FullName, Email, PasswordHash, Role) VALUES 
('Sipho Mokoena', 'sipho@raceday.co.za', 'hashed_pw_1', 'Organiser'),
('Sarah Organiser', 'sarah@raceday.co.za', 'hashed_pw_2', 'Organiser'),
('Thabo Ndlovu', 'thabo@gmail.com', 'hashed_pw_3', 'Participant'),
('Emma Smith', 'emma@gmail.com', 'hashed_pw_4', 'Participant');
GO

-- inserting events like run, walk, and cycle types
INSERT INTO Events (Title, Description, EventDate, Location, Distance, EventType, OrganiserId) VALUES 
('Soweto Marathon', 'Annual road run through Soweto.', '2026-11-01 06:00:00', 'Soweto, Johannesburg', 42.20, 'run', 1),
('Cape Town Cycle Tour', 'Scenic cycling event.', '2026-12-10 07:00:00', 'Cape Town', 109.00, 'cycle', 2),
('Pretoria Spring Walk', 'Family fun walk.', '2026-10-15 08:00:00', 'Pretoria', 5.00, 'walk', 1);
GO

-- inserting categories 
INSERT INTO Categories (EventId, CategoryName, DistanceKm, EntryFee) VALUES 
(1, 'Full Marathon', 42.20, 350.00),
(1, 'Half Marathon', 21.10, 250.00),
(2, 'Elite Cycle Route', 109.00, 600.00),
(3, '5km Fun Walk', 5.00, 50.00);
GO

-- inserting enrolments
INSERT INTO Enrolments (ParticipantId, CategoryId) VALUES 
(3, 2), 
(4, 3), 
(3, 4); 
GO

-- inserting payments
INSERT INTO Payments (EnrolmentId, AmountPaid, PaymentStatus) VALUES 
(1, 250.00, 'Successful'),
(2, 600.00, 'Successful'),
(3, 50.00, 'Successful');
GO

-- inserting results
INSERT INTO Results (EnrolmentId, FinishTime, Position) VALUES 
(1, '01:45:30', 42),
(2, '02:30:15', 15);
GO
