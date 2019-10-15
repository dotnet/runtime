# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

Param (
    [switch] $uninstall=$false
)

# Imports:
. .\setup_common.ps1
. .\setup_certificates.ps1

$script:COREFX_ROLE_NAME = "COREFX_NET_CLIENT"

Function InstallClientEnvironmentConfiguration
{
    Write-Host -ForegroundColor Cyan "Installing client configuration."

    foreach ($configEntry in $script:ClientConfiguration)
    {  
        [Environment]::SetEnvironmentVariable($configEntry.Name, $configEntry.Value, "Machine")
    }
}

Function UninstallClientEnvironmentConfiguration
{
    Write-Host -ForegroundColor Cyan "Removing client configuration."

    foreach ($configEntry in $script:ClientConfiguration)
    {  
        [Environment]::SetEnvironmentVariable($configEntry.Name, $null, "Machine")
    }
}

Function Install
{
    Write-Host -ForegroundColor Cyan "Installing prerequisites for test role: $($script:COREFX_ROLE_NAME)."
    CheckMachineInfo

    InstallClientCertificates
    InstallClientEnvironmentConfiguration

    EnvironmentSetInstalledRoleStatus
}

Function Uninstall
{
    Write-Host -ForegroundColor Cyan "Removing prerequisites for test role: $($script:COREFX_ROLE_NAME)."
    EnvironmentCheckUninstallRoleStatus

    RemoveClientCertificates
    UninstallClientEnvironmentConfiguration

    EnvironmentRemoveRoleStatus
}

if ($uninstall)
{
    Uninstall
}
else
{
    Install
}
