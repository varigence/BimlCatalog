pushd %~dp0

del "C:\Program Files (x86)\Microsoft SQL Server\100\DTS\PipelineComponents\Varigence.Ssis.2008.dll" /F
del C:\Program Files\Microsoft SQL Server\100\DTS\PipelineComponents\Varigence.Ssis.2008.dll" /F

del "C:\Program Files (x86)\Microsoft SQL Server\100\DTS\PipelineComponents\Vcs.Ssis.*.dll" /F
del "C:\Program Files\Microsoft SQL Server\100\DTS\PipelineComponents\Vcs.Ssis.*.dll" /F

gacutil /uf "Varigence.Ssis.2008"
gacutil /uf "Vcs.SSIS.2008"

gacutil /uf "Vcs.Ssis.AuditRow.2008"
gacutil /uf "Vcs.Ssis.RowCount.2008"
gacutil /uf "Vcs.Ssis.Hash.2008"
gacutil /uf "Vcs.Ssis.HashDual.2008"
gacutil /uf "Vcs.Ssis.ErrorDescription.2008"

gacutil /uf "Vcs.Ssis.AuditRow"
gacutil /uf "Vcs.Ssis.RowCount"
gacutil /uf "Vcs.Ssis.Hash"
gacutil /uf "Vcs.Ssis.HashDual"
gacutil /uf "Vcs.Ssis.ErrorDescription"

PAUSE