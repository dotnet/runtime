#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#

function header([string]$message)
{
    Write-Host -ForegroundColor Green "*** $message ***"
}

function info([string]$message)
{
    Write-Host -ForegroundColor Yellow "*** $message ***"
}

function error([string]$message)
{
    Write-Host -ForegroundColor Red "$message"
}

function setEnvIfDefault([string]$envVarName, [string]$value)
{
    If ([Environment]::GetEnvironmentVariable($envVarName) -eq $null)
    {
        [Environment]::SetEnvironmentVariable($envVarName, $value)
    }
}

function setVarIfDefault([string]$varName, [string]$value)
{
    If (-not (Get-Variable -name $varName -ErrorAction SilentlyContinue))
    {
        Set-Variable -name $varName -value $value -scope 1
    }
}

function setPathAndHomeIfDefault([string]$rootPath)
{
    If ($env:DOTNET_ON_PATH -eq $null)
    {
        setPathAndHome $rootPath
    }
}

function setPathAndHome([string]$rootPath)
{
    $env:DOTNET_ON_PATH=$rootPath
    $env:PATH="$rootPath\bin;$env:PATH"
}

function _([string]$command)
{
    & "$command"
    if (!$?) {
        error "Command Failed: '& $command'"
        Exit 1
    }
}

function _([string]$command, $arguments)
{
    & "$command" @arguments
    if (!$?) {
        error "Command Failed: '& $command'"
        Exit 1
    }
}

function _cmd([string]$command)
{
    cmd /c "$command"
    if (!$?) {
        error "Command Failed: 'cmd /c $command'"
        Exit 1
    }
}
