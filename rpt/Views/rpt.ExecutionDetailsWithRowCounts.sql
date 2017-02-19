﻿CREATE VIEW [rpt].[ExecutionDetailsWithRowCounts]
AS 

WITH [cteRowCounts]
AS
(
	SELECT   [ExecutionID]
			,MAX(CASE WHEN [CountType] = 'Unknown' THEN [RowCount] ELSE NULL END) AS [UnknownRowCount]
			,MAX(CASE WHEN [CountType] = 'Select' THEN [RowCount] ELSE NULL END) AS [SelectRowCount]
			,MAX(CASE WHEN [CountType] = 'Insert' THEN [RowCount] ELSE NULL END) AS [InsertRowCount]
			,MAX(CASE WHEN [CountType] = 'Update' THEN [RowCount] ELSE NULL END) AS [UpdateRowCount]
			,MAX(CASE WHEN [CountType] = 'Delete' THEN [RowCount] ELSE NULL END) AS [DeleteRowCount]
			,MAX(CASE WHEN [CountType] = 'Unaffected' THEN [RowCount] ELSE NULL END) AS [UnaffectedRowCount]
			,MAX(CASE WHEN [CountType] = 'Intermediate' THEN [RowCount] ELSE NULL END) AS [IntermediateRowCount]
			,MAX(CASE WHEN [CountType] = 'Error' THEN [RowCount] ELSE NULL END) AS [ErrorRowCount]
			,MAX(CASE WHEN [CountType] = 'Exception' THEN [RowCount] ELSE NULL END) AS [ExceptionRowCount]
			,MAX(CASE WHEN [CountType] = 'Control' THEN [RowCount] ELSE NULL END) AS [ControlRowCount]
	FROM	[ssis].[RowCount]
	GROUP BY [ExecutionID]
)

SELECT	 e.[ExecutionID]
		,e.[PackageID]
		,e.[PackageName]
		,e.[ParentPackageID]
		,e.[PackageType]
		,e.[ParentPackageName]
		,e.[ExecutionStatusCode]
		,e.[ExecutionStatus]
		,e.[ExecutionDate]
		,e.[NextLoadStatusCode]
		,e.[NextLoadStatus]
		,e.[StartTime]
		,e.[EndTime]
		,e.[DurationInSeconds]
		,e.[Duration] 
		,e.[ErrorCode]
		,e.[ErrorDescription]
		,rc.[SelectRowCount]
		,rc.[InsertRowCount]
		,rc.[UpdateRowCount]
		,rc.[DeleteRowCount]
		,rc.[UnaffectedRowCount]
		,rc.[IntermediateRowCount]
		,rc.[ErrorRowCount]
		,rc.[ExceptionRowCount]
		,rc.[ControlRowCount]
		,rc.[UnknownRowCount]
		,e.[ParentExecutionID]
		,e.[ServerExecutionID]
		,e.[ParentSourceGUID]
		,e.[ExecutionGUID]
		,e.[SourceGUID]
FROM	[rpt].[ExecutionDetails] e
LEFT OUTER JOIN [cteRowCounts] rc
	ON e.[ExecutionID] = rc.[ExecutionID]

