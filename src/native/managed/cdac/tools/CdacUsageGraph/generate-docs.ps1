#!/usr/bin/env pwsh
# Thin wrapper over the CdacUsageGraph 'docs' command, which fills the generated marker blocks in
# docs/design/datacontracts/*.md from the analysis and its meanings and overrides sidecars.
# The generation logic lives in the tool (CdacUsageGraph/Docs/DocGenerator.cs) so it stays in lock-
# step with the doc-drift unit test.
#
#   ./generate-docs.ps1          # rewrite marked blocks in place
#   ./generate-docs.ps1 -Check   # fail (exit 1) if any doc would change (for CI/local verification)
#
# Marked regions look like:
#   <!-- BEGIN GENERATED: usage contract=Thread version=c1 -->
#   ...descriptor, global, and contract tables...
#   <!-- END GENERATED: usage contract=Thread version=c1 -->
[CmdletBinding()]
param(
    [switch]$Check,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "src/CdacUsageGraph/CdacUsageGraph.csproj"

$dotnet = "dotnet"
$repoDotnet = Join-Path $PSScriptRoot "../../../../../../.dotnet/dotnet"
if ($IsWindows) { $repoDotnet += ".exe" }
if (Test-Path $repoDotnet) { $dotnet = $repoDotnet }

$toolArgs = @("run", "--project", $project, "-c", $Configuration, "--", "docs")
if ($Check) { $toolArgs += "--check" }

& $dotnet @toolArgs
exit $LASTEXITCODE
