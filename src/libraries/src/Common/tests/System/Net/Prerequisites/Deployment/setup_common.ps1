# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

. .\config.ps1

# The following variable names should match the ones read by Configuration.*.cs code.
$script:ClientConfiguration = @(

    # Configuration.Http:
    @{Name = "COREFX_HTTPHOST"; Value = $script:iisServerFQDN},
    @{Name = "COREFX_SECUREHTTPHOST"; Value = $script:iisServerFQDN},
    @{Name = "COREFX_HTTP2HOST"; Value = $script:iisServerFQDN},                # Requires Windows 10 and above.
    @{Name = "COREFX_DOMAINJOINED_HTTPHOST"; Value = $script:iisServerFQDN},
    @{Name = "COREFX_DOMAINJOINED_PROXYHOST"; Value = $null},
    @{Name = "COREFX_DOMAINJOINED_PROXYPORT"; Value = $null},
    @{Name = "COREFX_HTTPHOST_SSL2"; Value = $null},
    @{Name = "COREFX_HTTPHOST_SSL3"; Value = $null},
    @{Name = "COREFX_HTTPHOST_TLS10"; Value = $null},
    @{Name = "COREFX_HTTPHOST_TLS11"; Value = $null},
    @{Name = "COREFX_HTTPHOST_TLS12"; Value = $null},
    @{Name = "COREFX_HTTPHOST_EXPIREDCERT"; Value = $null},
    @{Name = "COREFX_HTTPHOST_WRONGHOSTNAME"; Value = $null},
    @{Name = "COREFX_HTTPHOST_SELFSIGNEDCERT"; Value = $null},
    @{Name = "COREFX_HTTPHOST_REVOKEDCERT"; Value = $null},
    @{Name = "COREFX_STRESS_HTTP"; Value = "1"},

    # Configuration.WebSockets:
    @{Name = "COREFX_WEBSOCKETHOST"; Value = $script:iisServerFQDN},
    @{Name = "COREFX_SECUREWEBSOCKETHOST"; Value = $script:iisServerFQDN},

    # Configuration.Security:
    @{Name = "COREFX_NET_AD_DOMAINNAME"; Value = $script:domainNetbios},
    @{Name = "COREFX_NET_AD_USERNAME"; Value = $script:domainUserName},
    @{Name = "COREFX_NET_AD_PASSWORD"; Value = $script:domainUserPassword},
    @{Name = "COREFX_NET_SECURITY_NEGOSERVERURI"; Value = "http://$($script:iisServerFQDN)"},
    @{Name = "COREFX_NET_SECURITY_TLSSERVERURI"; Value = "https://$($script:iisServerFQDN)"},

    @{Name = "COREFX_NET_SOCKETS_SERVERURI"; Value = "http://$($script:iisServerFQDN)"}
)

Function GetRoleForMachine($machineName)
{
    return $script:Roles | where {$_.MachineName.ToUpper() -eq $machineName.ToUpper()}
}

Function GetPreRebootRoleForMachine($machineName)
{
    return $script:PreRebootRoles | where {$_.MachineName.ToUpper() -eq $machineName.ToUpper()}
}

Function GetRole($roleName)
{
    return $script:Roles | where {$_.Name.ToUpper() -eq $roleName.ToUpper()}
}

Function CheckPreRebootMachineInfo
{
    $role = GetPreRebootRoleForMachine $Env:COMPUTERNAME

    if ($role.Name -ne $script:COREFX_ROLE_NAME)
    {
        throw "This script needs to run on machines part of the $($role.Name) role."
    }

    if ((-not [string]::IsNullOrWhiteSpace($role.MachineIP)) -and ((Get-NetIPAddress | where {$_.IPAddress -eq $role.MachineIP}).Count -eq 0))
    {
        throw "The current machine doesn't have the expected Static IP address: $($role.MachineIP)"
    }
}

Function CheckMachineInfo
{
    $role = GetRoleForMachine $Env:COMPUTERNAME

    if ($role.Name -ne $script:COREFX_ROLE_NAME)
    {
        throw "This script needs to run on machines part of the $($role.Name) role."
    }

    if ((-not [string]::IsNullOrWhiteSpace($role.MachineIP)) -and ((Get-NetIPAddress | where {$_.IPAddress -eq $role.MachineIP}).Count -eq 0))
    {
        throw "The current machine doesn't have the expected Static IP address: $($role.MachineIP)"
    }
}

Function EnvironmentAddRoleStatus($status)
{
    [Environment]::SetEnvironmentVariable($script:COREFX_ROLE_NAME, $status, "Machine")
}

Function EnvironmentSetInstalledRoleStatus
{
    EnvironmentAddRoleStatus "Installed"
}

Function EnvironmentSetRebootPendingRoleStatus
{
    EnvironmentAddRoleStatus "PendingReboot"
}

Function EnvironmentRemoveRoleStatus
{
    [Environment]::SetEnvironmentVariable($script:COREFX_ROLE_NAME, $null, "Machine")
}

Function EnvironmentCheckUninstallRoleStatus
{
    if ([Environment]::GetEnvironmentVariable($script:COREFX_ROLE_NAME, "Machine") -ne "Installed")
    {
        Write-Warning "The machine doesn't appear to be in the $($script:COREFX_ROLE_NAME) role."
        $continue = Read-Host "Do you want to continue? [Y/N]"
        if ($continue.ToUpper() -ne "Y")
        {
            Write-Warning "Aborted by user."
            exit
        }
    }
}

Function EnvironmentIsRoleRebootPending
{
    return [Environment]::GetEnvironmentVariable($script:COREFX_ROLE_NAME, "Machine") -eq "PendingReboot"
}

Function EnvironmentIsRoleInstalled
{
    return [Environment]::GetEnvironmentVariable($script:COREFX_ROLE_NAME, "Machine") -eq "Installed"
}

Function DownloadFile($source, $destination)
{
    # BITS remoting doesn't work on systems <= TH2.
    if ([System.Environment]::OSVersion.Version -gt (new-object 'Version' 10,0,10586,0))
    {
        Start-BitsTransfer -Source $source -Destination $destination
    }
    else 
    {
        # BUG: taking very long: Invoke-WebRequest $source -OutFile $destination
        $fqDestination = Join-Path (pwd) $destination
        $wc = New-Object System.Net.WebClient
        $wc.Downloadfile($source, $fqDestination.ToString())
    }
}

Function GetIISCodePath
{
    return ".\IISApplications"
}
