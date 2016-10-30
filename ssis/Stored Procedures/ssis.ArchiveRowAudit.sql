﻿CREATE PROCEDURE [ssis].[ArchiveRowAudit]
AS
BEGIN
	DECLARE	 @RowAuditRetentionPeriod	INT
			,@RetentionDate				DATE
	
	SELECT	@RowAuditRetentionPeriod = -CONVERT(INT, [ConfigurationValue]) 
	FROM	[admin].[Configurations] 
	WHERE	[ConfigurationCode] = 'BimlFlex' 
		AND [ConfigurationKey] = 'RowAuditRetentionPeriod'
	
	SET		@RetentionDate = DATEADD(DD, @RowAuditRetentionPeriod, GETDATE())

	DELETE	ard
	FROM	[ssis].[AuditRowData] ard
	INNER JOIN [ssis].[AuditRow] ar
		ON	ard.[AuditRowID] = ar.[AuditRowID]
	WHERE	ar.AuditDate < @RetentionDate
	
	RETURN 0

END