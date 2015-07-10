USE [master]
GO

CREATE DATABASE AggregatePatterns
GO

USE AggregatePatterns

CREATE TABLE Clearance(
	Id [bigint] IDENTITY(1,1) NOT NULL,
	Amount [money] NULL,
	CONSTRAINT [PK_Clearance] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO 

CREATE TABLE Trade(
	Id [bigint] IDENTITY(1,1) NOT NULL,
	Amount [money] NULL,
	CONSTRAINT [PK_Trade] PRIMARY KEY CLUSTERED ([Id] ASC)
) 
GO

CREATE TABLE Match(
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Clearance_Id] [bigint] NOT NULL,
	[Trade_Id] [bigint] NOT NULL,
	CONSTRAINT [PK_Match] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE TABLE Adjustment(
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Trade_Id] [bigint] NOT NULL,
	Amount [money] NULL,
	CONSTRAINT [PK_Adjustment] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

ALTER TABLE Match  WITH CHECK ADD  CONSTRAINT [FK_Match_Clearance1] FOREIGN KEY([Clearance_Id])
REFERENCES Clearance ([Id])
GO

ALTER TABLE Match CHECK CONSTRAINT [FK_Match_Clearance1]
GO

ALTER TABLE Match  WITH CHECK ADD  CONSTRAINT [FK_Match_Trade1] FOREIGN KEY([Trade_Id])
REFERENCES Trade ([Id])
GO

ALTER TABLE Match CHECK CONSTRAINT [FK_Match_Trade1]
GO

ALTER TABLE Adjustment  WITH CHECK ADD  CONSTRAINT [FK_Adjustment_Trade1] FOREIGN KEY([Trade_Id])
REFERENCES Trade ([Id])
GO