IF NOT EXISTS (SELECT * FROM sys.databases WHERE [name] = 'Test')
	CREATE DATABASE [Test];

USE [Test];

IF EXISTS ( SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Test]') AND type = N'U')
	DROP TABLE [Test];

CREATE TABLE [TEST]
(
	[SetGuid] uniqueidentifier NOT NULL,
	[SetNullGuid] uniqueidentifier,
	[SetBool] bit NOT NULL DEFAULT(1),
    [SetNullBool] bit,
    [SetString] varchar(50),
    [SetChar] char(1),
    [SetNullChar] char(1),
    [SetInt16] tinyint,
    [SetInt32] int,
    [SetNullInt32] int,
    [SetInt64] bigint,
    [SetSingle] float,
    [SetNullSingle] float,
    [SetDouble] float,
    [SetNullDouble] float,
    [SetDecimal] float,
    [SetNullDecimal] float,
    [SetDateTime] datetime,
    [SetNullDateTime] datetime,
    [SetTestType] int,
    [SetNullTestType] int
);