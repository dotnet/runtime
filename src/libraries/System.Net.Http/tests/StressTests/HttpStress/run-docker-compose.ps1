#!/usr/bin/env pwsh
# Runs the stress test using docker-compose
$RepoRoot = $(git -C $PSScriptRoot rev-parse --show-toplevel)
Invoke-Expression "& `"$RepoRoot/src/libraries/Common/tests/System/Net/StressTests/run-docker-compose.ps1`" -TestProjectDir `"$PSScriptRoot`" $args"
