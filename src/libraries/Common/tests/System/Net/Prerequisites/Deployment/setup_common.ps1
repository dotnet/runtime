# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

. .\config.ps1

# The following variable names should match the ones read by Configuration.*.cs code.
$script:ClientConfiguration = @(

    # Configuration.Http:
    @{Name = "DOTNET_TEST_HTTPHOST"; Value = $script:iisServerFQDN},
    @{Name = "DOTNET_TEST_SECUREHTTPHOST"; Value = $script:iisServerFQDN},
    @{Name = "DOTNET_TEST_HTTP2HOST"; Value = $script:iisServerFQDN},                # Requires Windows 10 and above.
    @{Name = "DOTNET_TEST_DOMAINJOINED_HTTPHOST"; Value = $script:iisServerFQDN},
    @{Name = "DOTNET_TEST_DOMAINJOINED_PROXYHOST"; Value = $null},
    @{Name = "DOTNET_TEST_DOMAINJOINED_PROXYPORT"; Value = $null},
    @{Name = "DOTNET_TEST_HTTPHOST_SSL2"; Value = $null},
    @{Name = "DOTNET_TEST_HTTPHOST_SSL3"; Value = $null},
    @{Name = "DOTNET_TEST_HTTPHOST_TLS10"; Value = $null},
    @{Name = "DOTNET_TEST_HTTPHOST_TLS11"; Value = $null},
    @{Name = "DOTNET_TEST_HTTPHOST_TLS12"; Value = $null},
    @{Name = "DOTNET_TEST_HTTPHOST_EXPIREDCERT"; Value = $null},
    @{Name = "DOTNET_TEST_HTTPHOST_WRONGHOSTNAME"; Value = $null},
    @{Name = "DOTNET_TEST_HTTPHOST_SELFSIGNEDCERT"; Value = $null},
    @{Name = "DOTNET_TEST_HTTPHOST_REVOKEDCERT"; Value = $null},
    @{Name = "DOTNET_TEST_STRESS_HTTP"; Value = "1"},

    # Configuration.WebSockets:
    @{Name = "DOTNET_TEST_WEBSOCKETHOST"; Value = $script:iisServerFQDN},
    @{Name = "DOTNET_TEST_SECUREWEBSOCKETHOST"; Value = $script:iisServerFQDN},

    # Configuration.Security:
    @{Name = "DOTNET_TEST_NET_AD_DOMAINNAME"; Value = $script:domainNetbios},
    @{Name = "DOTNET_TEST_NET_AD_USERNAME"; Value = $script:domainUserName},
    @{Name = "DOTNET_TEST_NET_AD_PASSWORD"; Value = $script:domainUserPassword},
    @{Name = "DOTNET_TEST_NET_SECURITY_NEGOSERVERURI"; Value = "http://$($script:iisServerFQDN)"},
    @{Name = "DOTNET_TEST_NET_SECURITY_TLSSERVERURI"; Value = "https://$($script:iisServerFQDN)"},

    @{Name = "DOTNET_TEST_NET_SOCKETS_SERVERURI"; Value = "http://$($script:iisServerFQDN)"}
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

    if ($role.Name -ne $script:DOTNET_TEST_ROLE_NAME)
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

    if ($role.Name -ne $script:DOTNET_TEST_ROLE_NAME)
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
    [Environment]::SetEnvironmentVariable($script:DOTNET_TEST_ROLE_NAME, $status, "Machine")
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
    [Environment]::SetEnvironmentVariable($script:DOTNET_TEST_ROLE_NAME, $null, "Machine")
}

Function EnvironmentCheckUninstallRoleStatus
{
    if ([Environment]::GetEnvironmentVariable($script:DOTNET_TEST_ROLE_NAME, "Machine") -ne "Installed")
    {
        Write-Warning "The machine doesn't appear to be in the $($script:DOTNET_TEST_ROLE_NAME) role."
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
    return [Environment]::GetEnvironmentVariable($script:DOTNET_TEST_ROLE_NAME, "Machine") -eq "PendingReboot"
}

Function EnvironmentIsRoleInstalled
{
    return [Environment]::GetEnvironmentVariable($script:DOTNET_TEST_ROLE_NAME, "Machine") -eq "Installed"
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
