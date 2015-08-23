﻿/*
# Copyright (C) 2015 Varigence, Inc.
#
# Licensed under the Varigence IP BimlFlex Framework Agreement, Version 1.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License by contacting support@varigence.com.
*/
CREATE PROCEDURE [ssis].[LogExecution](
	@ExecutionGUID			[nvarchar](40),
	@SourceGUID				[nvarchar](40),
	@PackageName			[varchar](500),
	@IsBatch				[bit],
	@ParentSourceGUID		[nvarchar](40),
	@ParentExecutionID		[bigint],
	@ServerExecutionID		[bigint],
	@ExecutionID			[bigint]        = NULL OUTPUT,
	@ExecutionStatus		[char](1)		= NULL OUTPUT,
	@NextLoadStatus			[char](1)		= NULL OUTPUT,
	@LastExecutionID		[bigint]        = NULL OUTPUT
) 
AS 

DECLARE	 @PackageID				INT
		,@ParentPackageID		INT
		,@CurrentExecutionID	BIGINT
		,@LastNextLoadStatus	CHAR(1)
		,@LastExecutionStatus	CHAR(1)
		,@IsEnabled				BIT

SELECT  @ExecutionID = MAX([ExecutionID])
FROM    [ssis].[Execution]
WHERE   [SourceGUID] = REPLACE(REPLACE(@SourceGUID, '{', ''), '}', '')
AND		[ExecutionGUID] = REPLACE(REPLACE(@ExecutionGUID, '{', ''), '}', '')
AND		[ParentExecutionID] = @ParentExecutionID;

IF @ExecutionID IS NULL
BEGIN
	SELECT	 @PackageID = [PackageID]
			,@IsEnabled = [IsEnabled]
	FROM	[ssis].[Package]
	WHERE	[PackageName] = @PackageName

	IF @PackageID IS NULL
	BEGIN
		INSERT INTO [ssis].[Package] ([PackageName], [IsBatch]) VALUES (@PackageName, @IsBatch)
		SELECT @PackageID = SCOPE_IDENTITY();
	END

	-- Check if Package is already executing
	SELECT	 @CurrentExecutionID = [ExecutionID]
	FROM	[ssis].[Execution]
	WHERE	[ExecutionID] = 
		(
			SELECT	MAX([ExecutionID])
			FROM	[ssis].[Execution]
			WHERE	[PackageID] = @PackageID
			AND		[ExecutionStatus] = 'E'
		)

	-- Get previous execution status
	SELECT	 @LastNextLoadStatus = [NextLoadStatus]
			,@LastExecutionStatus = [ExecutionStatus]
			,@LastExecutionID = [ExecutionID]
	FROM	[ssis].[Execution]
	WHERE	[ExecutionID] = 
		(
			SELECT	MAX([ExecutionID])
			FROM	[ssis].[Execution]
			WHERE	[PackageID] = @PackageID
		)
	-- If the package is currently executing or disabled abort the process.
	IF @IsEnabled = 0
	BEGIN
		SELECT @ExecutionStatus = 'C' -- Disabled Skip Execution
		SELECT @NextLoadStatus = 'P'
	END
	ELSE IF @CurrentExecutionID IS NOT NULL -- Currently Executing
	BEGIN
		SELECT @ExecutionStatus = 'A' -- Abort this instance
		SELECT @NextLoadStatus = 'P'
	END
	ELSE IF ISNULL(@LastNextLoadStatus, 'P') = 'R' -- Failed - Rollback
	BEGIN
		SELECT @ExecutionStatus = 'R' -- RollBack
		SELECT @NextLoadStatus = 'C'
	END
	ELSE IF ISNULL(@LastNextLoadStatus, 'P') = 'C' -- Skip next run. Package completed, but not set for next run by batch.
	BEGIN
		SELECT @ExecutionStatus = 'C' -- Skip Execution
		SELECT @NextLoadStatus = 'C' -- Skip Next Run. Gets reset based on completion or failure.
	END
	ELSE 
	BEGIN
		SELECT	@ExecutionStatus = 'E' -- Execution Started
		SELECT	@NextLoadStatus = 'C' -- Skip Next Run. Gets reset based on completion or failure.
	END
	SELECT	@LastExecutionID
	SELECT	@ExecutionStatus
	SELECT	@NextLoadStatus
	INSERT INTO [ssis].[Execution]
			([ParentExecutionID]
			,[ExecutionGUID]
			,[SourceGUID]
			,[ParentSourceGUID]
			,[PackageID]
			,[ServerExecutionID]
			,[ExecutionStatus]
			,[NextLoadStatus])
	VALUES	(ISNULL(@ParentExecutionID, -1)
			,REPLACE(REPLACE(@ExecutionGUID, '{', ''), '}', '')
			,REPLACE(REPLACE(@SourceGUID, '{', ''), '}', '')
			,ISNULL(REPLACE(REPLACE(@ParentSourceGUID, '{', ''), '}', ''), '')
			,@PackageID
			,@ServerExecutionID
			,@ExecutionStatus
			,@NextLoadStatus);

	SELECT  @ExecutionID = SCOPE_IDENTITY();
	

	IF ISNULL(@ParentExecutionID, -1) <> -1
	BEGIN
		SELECT	@ParentPackageID = MAX([PackageID])
		FROM	[ssis].[Execution] 
		WHERE	[ExecutionID] = @ParentExecutionID

		UPDATE	[ssis].[Package]
		SET		[ParentPackageID] = @ParentPackageID
		WHERE	[PackageID] = @PackageID
	END
END

IF ISNULL(REPLACE(REPLACE(@ParentSourceGUID, '{', ''), '}', ''), '') <> ''
BEGIN
	UPDATE	[ssis].[Execution]
	SET		[ParentSourceGUID] = REPLACE(REPLACE(@ParentSourceGUID, '{', ''), '}', '')
	WHERE	[ExecutionID] = @ExecutionID
END

SELECT	@LastExecutionID = ISNULL(@LastExecutionID, @ExecutionID)
SELECT	@ExecutionStatus = ISNULL(@ExecutionStatus, 'C')
SELECT	@NextLoadStatus = ISNULL(@NextLoadStatus, 'C')
