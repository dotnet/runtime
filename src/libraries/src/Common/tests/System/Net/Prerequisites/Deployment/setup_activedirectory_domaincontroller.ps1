# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

#Requires -RunAsAdministrator

#
#   Run this script on the Active Directory Domain Controller machine.
#   Tested on Windows 2016 TP5
#

Param (
    [switch] $uninstall=$false
)

# Import configuration.
. .\setup_common.ps1

$script:COREFX_ROLE_NAME = "COREFX_NET_AD_DC"

Function EnableAD
{
    Write-Host -ForegroundColor Cyan "Installing Active Directory."
    CheckPreRebootMachineInfo
    
    # From https://technet.microsoft.com/en-us/library/hh472162.aspx
    Install-Windowsfeature -name AD-Domain-Services -IncludeManagementTools
   
    # Will prompt for SafeModeAdministratorPassword:
    Install-ADDSForest -domainname $script:domainName -DomainNetbiosName $script:domainNetbios -NoRebootOnCompletion
}

Function AddUser
{
    Write-Host -ForegroundColor Cyan "Creating domain user."
    Remove-ADUser $script:domainUserName -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
    New-ADUser $script:domainUserName -Enabled $true -PasswordNeverExpires $true -AccountPassword (ConvertTo-SecureString $script:domainUserPassword -AsPlainText -force)
}

Function ConfigureDNS
{
    Write-Host -ForegroundColor Cyan "Configuring DNS."
    
    $iisServer  = GetRole "COREFX_NET_IISSERVER"

    $serverName = ($script:iisServerFQDN).Split('.')[0];
    $zoneName = ($script:iisServerFQDN).Substring($serverName.Length + 1)  
    
    Remove-DnsServerZone -Name $zoneName -Force -ErrorAction SilentlyContinue | Out-Null
    Add-DnsServerPrimaryZone -Name $zoneName -ReplicationScope "Forest"
    Add-DnsServerResourceRecordA -Name $serverName -ZoneName $zoneName -AllowUpdateAny -IPv4Address $iisServer.MachineIP -TimeToLive 01:00:00 
}

Function Install
{
    Write-Host -ForegroundColor Cyan "Installing prerequisites for test role: $($script:COREFX_ROLE_NAME)."

    if ((-not (EnvironmentIsRoleInstalled)) -and (-not (EnvironmentIsRoleRebootPending)))
    {
        EnableAD
        EnvironmentSetRebootPendingRoleStatus
        Write-Host -ForegroundColor Cyan "Please re-start the script after the machine reboots."
        Read-Host "[Press ENTER to reboot.]"
        Restart-Computer
    }
    else
    {
        AddUser
        ConfigureDNS
        Enable-PSRemoting
        EnvironmentSetInstalledRoleStatus
        Write-Host -ForegroundColor Cyan "Prerequisites installed for $($script:COREFX_ROLE_NAME)."
        Write-Host
    }
}

Function Uninstall
{
    Write-Host -ForegroundColor Cyan "Removing prerequisites for test role: $($script:COREFX_ROLE_NAME)."

    EnvironmentCheckUninstallRoleStatus
    EnvironmentRemoveRoleStatus
    
    # Will reboot.
    Uninstall-ADDSDomainController -LastDomainControllerInDomain -RemoveApplicationPartitions -IgnoreLastDNSServerForZone
}

if ($uninstall)
{
    Uninstall
}
else
{
    Install
}
