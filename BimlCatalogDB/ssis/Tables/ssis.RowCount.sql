﻿/*
# Copyright (C) 2015 Varigence, Inc.
#
# Licensed under the Varigence IP BimlFlex Framework Agreement, Version 1.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License by contacting support@varigence.com.
*/
CREATE TABLE [ssis].[RowCount] (
    [RowCountID]			BIGINT				IDENTITY (1, 1) NOT NULL,
    [ExecutionID]			BIGINT				NOT NULL,
    [ComponentName]         NVARCHAR(200)		NOT NULL,
    [ObjectName]			NVARCHAR(200)		NOT NULL,
    [CountType]				VARCHAR(20)			NOT NULL,
    [RowCount]				INT					NOT NULL,
    [ColumnSum]				DECIMAL(38, 4)		NULL,
    [ColumnName]			NVARCHAR(500)		NOT NULL,
	[AuditDate]				DATETIME			CONSTRAINT [DF_ssisRowCount_AuditDate] DEFAULT (GETDATE()) NULL,
    CONSTRAINT [PK_ssis_RowCount] PRIMARY KEY CLUSTERED ([RowCountID] DESC)
);

