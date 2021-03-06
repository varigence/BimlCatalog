IF EXISTS(SELECT * FROM sys.columns WHERE  object_id = OBJECT_ID(N'[ssis].[Package]') AND name = 'CreatedBy')
BEGIN
       ALTER TABLE [ssis].[Package] DROP CONSTRAINT [DF_ssisPackage_CreatedBy];
       ALTER TABLE [ssis].[Package] DROP COLUMN [CreatedBy];
END
IF EXISTS(SELECT * FROM sys.columns WHERE  object_id = OBJECT_ID(N'[ssis].[Package]') AND name = 'UpdatedBy')
BEGIN
       ALTER TABLE [ssis].[Package] DROP CONSTRAINT [DF_ssisPackage_UpdatedBy];
       ALTER TABLE [ssis].[Package] DROP COLUMN [UpdatedBy];
END
IF EXISTS(SELECT * FROM sys.columns WHERE  object_id = OBJECT_ID(N'[ssis].[Package]') AND name = 'UpdatedDate')
BEGIN
       ALTER TABLE [ssis].[Package] DROP CONSTRAINT [DF_ssisPackage_UpdatedDate];
       ALTER TABLE [ssis].[Package] DROP COLUMN [UpdatedDate];
END
IF EXISTS(SELECT * FROM sys.columns WHERE  object_id = OBJECT_ID(N'[ssis].[Package]') AND name = 'PackageExecutionDuration')
BEGIN
       ALTER TABLE [ssis].[Package] DROP CONSTRAINT [DF_ssisPackage_PackageExecutionDuration];
       ALTER TABLE [ssis].[Package] DROP COLUMN [PackageExecutionDuration];
END
IF EXISTS(SELECT * FROM sys.columns WHERE  object_id = OBJECT_ID(N'[ssis].[Task]') AND name = 'AvgDuration')
BEGIN
       ALTER TABLE [ssis].[Task] DROP COLUMN [AvgDuration];
END
