# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

#Requires -RunAsAdministrator

#
#   Run this script on all Active Directory client machines.
#   Tested on Windows 2016 TP5 and Windows 10.
#
Param (
    [switch] $uninstall=$false
)

# Imports:
. .\setup_common.ps1

$script:COREFX_ROLE_NAME = "COREFX_NET_AD_CLIENT"

Function ConfigureDNS
{
    Write-Host -ForegroundColor Cyan "Configuring DNS"

    $dcRole = GetRole "COREFX_NET_AD_DC"

    if (-not (Test-Connection $dcRole.MachineIP))
    {
        throw "The local machine cannot ping the Domain Controller/DNS Server at $($dcRole.MachineIP).`n" + `
        "Ensure that the setup was completed on the machine then try re-running the script on this machine again."
    }

    $ipv4DnsInterfaces = Get-DnsClientServerAddress | where {($_.AddressFamily -eq 2) -and ($_.InterfaceAlias -eq "Ethernet")}
    if ($ipv4DnsInterfaces.Count -eq 0)
    {
        throw "The setup script cannot find a network adapter named 'Ethernet' that has IPv4 configured."
    }

    $ifIndex = $ipv4DnsInterfaces[0].InterfaceIndex
    
    Set-DnsClientServerAddress -InterfaceIndex $ifIndex -ServerAddresses ($dcRole.MachineIP)
}

Function EnableAD
{
    Write-Host -ForegroundColor Cyan "Adding computer to domain. Please use the domain administrator password for the $($script:domainNetbios) domain."
    Add-Computer -DomainName $script:domainNetbios
}

Function Install
{
    Write-Host -ForegroundColor Cyan "Installing prerequisites for test role: $($script:COREFX_ROLE_NAME)"

    CheckPreRebootMachineInfo
    ConfigureDNS
    EnableAD
    Enable-PSRemoting
    EnvironmentSetRebootPendingRoleStatus
    Write-Host -ForegroundColor Cyan "Please re-start one instance of the script on any of the machines after reboot to resume the installation."
    Read-Host "[Press ENTER to reboot.]"
    Restart-Computer
    EnvironmentSetInstalledRoleStatus
}

Function Uninstall
{
    Write-Host -ForegroundColor Cyan "Removing prerequisites for test role: $($script:COREFX_ROLE_NAME)."
    
    EnvironmentCheckUninstallRoleStatus

    Remove-Computer
    EnvironmentRemoveRoleStatus

    Write-Warning "Current DNS configuration:"
    Get-DnsClientServerAddress
    Write-Warning "To complete the uninstallation, you may need to change the DNS client address configuration."
    Read-Host "[Press ENTER to reboot.]"
    Restart-Computer
}

if ($uninstall)
{
    Uninstall
}
else
{
    Install
}
