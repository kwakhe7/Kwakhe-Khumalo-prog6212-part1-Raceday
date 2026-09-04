/* ============================================================
   RaceDay Database Schema
   Target: SQL Server (SSMS)
   This script matches RaceDay_ERD.png exactly - 6 entities:
   Users, Venues, Events, Categories, Enrolments, Results
   ============================================================ */

IF DB_ID('RaceDay') IS NULL
BEGIN
    CREATE DATABASE RaceDay;
END
GO

USE RaceDay;
GO

/* Drop tables if re-running the script, in FK-safe order */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Venues', 'U') IS NOT NULL DROP TABLE dbo.Venues;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* ============================================================
   1. USERS
   ============================================================ */
CREATE TABLE dbo.Users (
    UserID          INT IDENTITY(1,1) PRIMARY KEY,
    Name            VARCHAR(100)    NOT NULL,
    Email           VARCHAR(150)    NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255)    NOT NULL,
    Role            VARCHAR(20)     NOT NULL DEFAULT 'Participant'
                        CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser','Participant')),
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE()
);
GO

/* ============================================================
   2. VENUES
   ============================================================ */
CREATE TABLE dbo.Venues (
    VenueID         INT IDENTITY(1,1) PRIMARY KEY,
    Name            VARCHAR(150)    NOT NULL,
    Address         VARCHAR(200)    NOT NULL,
    City            VARCHAR(100)    NOT NULL,
    Capacity        INT             NOT NULL DEFAULT 0
);
GO

/* ============================================================
   3. EVENTS  (1 Organiser -> M Events, 1 Venue -> M Events)
   ============================================================ */
CREATE TABLE dbo.Events (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID     INT             NOT NULL,
    VenueID         INT             NOT NULL,
    Name            VARCHAR(150)    NOT NULL,
    Description     VARCHAR(500)    NULL,
    EventDate       DATE            NOT NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Events_Venue     FOREIGN KEY (VenueID)     REFERENCES dbo.Venues(VenueID)
);
GO

/* ============================================================
   4. CATEGORIES  (1 Event -> M Categories)
   ============================================================ */
CREATE TABLE dbo.Categories (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT             NOT NULL,
    Name            VARCHAR(100)    NOT NULL,
    DistanceKm      DECIMAL(5,2)    NOT NULL,
    MaxParticipants INT             NOT NULL DEFAULT 100,
    CONSTRAINT FK_Categories_Event FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID)
);
GO

/* ============================================================
   5. ENROLMENTS  (1 User(Participant) -> M Enrolments,
                    1 Category -> M Enrolments)
   ============================================================ */
CREATE TABLE dbo.Enrolments (
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID   INT             NOT NULL,
    CategoryID      INT             NOT NULL,
    EnrolmentDate   DATETIME        NOT NULL DEFAULT GETDATE(),
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Confirmed'
                        CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Confirmed','Cancelled')),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_Category    FOREIGN KEY (CategoryID)    REFERENCES dbo.Categories(CategoryID),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantID, CategoryID)
);
GO

/* ============================================================
   6. RESULTS  (1 Enrolment -> 0..1 Result)
   ============================================================ */
CREATE TABLE dbo.Results (
    ResultID        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID     INT             NOT NULL UNIQUE,
    FinishTime      TIME            NULL,
    Position        INT             NULL,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Finished'
                        CONSTRAINT CK_Results_Status CHECK (Status IN ('Finished','DNF','DQ')),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID)
);
GO

/* ============================================================
   SAMPLE DATA
   ============================================================ */

-- Organisers (2)
INSERT INTO dbo.Users (Name, Email, PasswordHash, Role) VALUES
('Sarah Nkosi',   'sarah.nkosi@raceday.co.za',   'hashed_pw_1', 'Organiser'),
('Mike Delport',  'mike.delport@raceday.co.za',  'hashed_pw_2', 'Organiser');

-- Participants (2)
INSERT INTO dbo.Users (Name, Email, PasswordHash, Role) VALUES
('Thandiwe Mahlangu', 'thandiwe.m@example.com', 'hashed_pw_3', 'Participant'),
('James Botha',       'james.botha@example.com','hashed_pw_4', 'Participant');

-- Venues
INSERT INTO dbo.Venues (Name, Address, City, Capacity) VALUES
('Nelson Mandela Bay Stadium', '1 Prince Alfred Rd', 'Gqeberha', 5000),
('Kings Beach Promenade',      'Marine Dr',          'Gqeberha', 2000),
('Rhodes Park',                'Park Dr',            'Makhanda',  800);

-- Events (3), owned by the two organisers
INSERT INTO dbo.Events (OrganiserID, VenueID, Name, Description, EventDate) VALUES
(1, 1, 'Bay 10K Classic',        'Annual road race around the bay.',           '2026-10-10'),
(1, 2, 'Kings Beach Fun Run',    'Family-friendly run along the beach front.', '2026-11-01'),
(2, 3, 'Makhanda Trail Run',     'Off-road trail run through Rhodes Park.',    '2026-11-22');

-- Categories for each event
INSERT INTO dbo.Categories (EventID, Name, DistanceKm, MaxParticipants) VALUES
(1, '10km Individual', 10.0, 200),
(1, '5km Fun Run',       5.0, 300),
(2, '3km Fun Run',       3.0, 150),
(2, '1km Kids Run',      1.0, 100),
(3, '15km Trail',       15.0,  80),
(3, '8km Trail',         8.0, 120);

-- Sample Enrolments
INSERT INTO dbo.Enrolments (ParticipantID, CategoryID, Status) VALUES
(3, 1, 'Confirmed'),   -- Thandiwe -> Bay 10K Classic, 10km  
(4, 1, 'Confirmed'),   -- James    -> Bay 10K Classic, 10km
(3, 3, 'Confirmed'),   -- Thandiwe -> Kings Beach, 3km
(4, 5, 'Confirmed');   -- James    -> Makhanda Trail, 15km

-- Sample Results (only for finished enrolments)
INSERT INTO dbo.Results (EnrolmentID, FinishTime, Position, Status) VALUES
(1, '00:45:12', 1, 'Finished'),
(2, '00:47:03', 2, 'Finished');
GO