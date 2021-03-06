﻿/*
# Copyright (C) 2015 Varigence, Inc.
#
# Licensed under the Varigence IP BimlFlex Framework Agreement, Version 1.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License by contacting support@varigence.com.
*/
CREATE PROCEDURE [ssis].[GetAuditRowDetails]
	@ExecutionID	INT
AS

SELECT [RowID]
	,[ColumnName]
	,[ColumnValue]
FROM	[ssis].[AuditRow] ar
INNER JOIN 	[ssis].[AuditRowData] ard
	ON	ar.[AuditRowID] = ard.[AuditRowID]
WHERE	ar.[ExecutionID] = @ExecutionID

