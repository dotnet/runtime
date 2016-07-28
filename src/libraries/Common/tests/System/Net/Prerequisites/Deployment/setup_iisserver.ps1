# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

#Requires -RunAsAdministrator

#
#   Run this script on the IIS server machine.
#   Tested on Windows 2016 TP5
# 

Param (
    [switch] $uninstall=$false
)

# Imports:
. .\setup_common.ps1
. .\setup_certificates.ps1
. .\setup_firewall.ps1

# Server application configuration
$script:iisWwwRoot = "$env:systemdrive\inetpub\wwwroot"
$script:defaultWebSite = "Default Web Site"

$script:webApps = @(
    @{Name = "NoAuth"; 
        IISRelativePath = ""; 
        SourceRelativePath = ".\"; 

        Configuration = @()
     },

    @{Name = "BasicAuth"; 
        IISRelativePath = "BasicAuth"; 
        SourceRelativePath = "\"; 
        Configuration  = @(
            @{ Path = "/system.webServer/security/authentication/anonymousAuthentication"; Name = "Enabled"; Value = "False" }
            @{ Path = "/system.webServer/security/authentication/basicAuthentication"; Name = "Enabled"; Value = "True" }
        );
        UserAccess = @( $script:basicUserName )
     },

    @{Name = "DigestAuth"; 
        IISRelativePath = "DigestAuth"; 
        SourceRelativePath = "\"; 
        Configuration  = @(
            @{ Path = "/system.webServer/security/authentication/anonymousAuthentication"; Name = "Enabled"; Value = "False" }
            @{ Path = "/system.webServer/security/authentication/digestAuthentication"; Name = "Enabled"; Value = "True" }
        );
        UserAccess = @( $script:basicUserName )
     },

    @{Name = "WindowsAuth"; 
        IISRelativePath = "WindowsAuth"; 
        SourceRelativePath = "\";
        Configuration  = @(
            @{ Path = "/system.webServer/security/authentication/anonymousAuthentication"; Name = "Enabled"; Value = "False" }
            @{ Path = "/system.webServer/security/authentication/windowsAuthentication"; Name = "Enabled"; Value = "True" }
        );
        UserAccess = @( "$($script:domainNetbios)\$($script:domainUserName)" )
     }
)

$script:COREFX_ROLE_NAME = "COREFX_NET_IISSERVER"

Function InstallIIS
{
    Write-Host -ForegroundColor Cyan "Installing IIS components."
    Install-WindowsFeature -Name Web-Server,Web-Basic-Auth,Web-Digest-Auth,Web-Windows-Auth,Web-Cert-Auth,Web-Asp-Net45,Web-WebSockets -IncludeManagementTools -ErrorAction Stop | Out-Null
}

Function RemoveIIS
{
    Write-Host -ForegroundColor Cyan "Removing IIS components."
    Uninstall-WindowsFeature -Name Web-Server -IncludeManagementTools
}

Function CreateLocalUser
{
    # A local user is required to allow Basic and Digest authentication. (WDigest not supported.)
    Write-Host -ForegroundColor Cyan "Creating local user account."
    Remove-LocalUser $script:basicUserName -Confirm:$false -ErrorAction SilentlyContinue
    New-LocalUser $script:basicUserName -PasswordNeverExpires -Password (ConvertTo-SecureString $script:basicUserPassword -AsPlainText -force) | Out-Null
}

Function RemoveLocalUser
{
    Write-Host -ForegroundColor Cyan "Removing local user account."
    Remove-LocalUser $script:basicUserName -Confirm:$false
}

Function ConfigureWebSites
{
    Write-Host -ForegroundColor Cyan "Configuring IIS websites."

    # SSL Bindings
    $sslCert = GetServerCertificate
    
    Get-WebBinding -Port 443 -Name $script:defaultWebSite | Remove-WebBinding
    New-WebBinding -Name $script:defaultWebSite -Protocol https -Port 443

    Remove-Item -Path "IIS:\SslBindings\*"
    New-Item -Path "IIS:\SslBindings\0.0.0.0!443" -Value $sslCert -Force | Out-Null
}

Function GrantUserAccess($path, $userAccess)
{
    foreach ($user in $userAccess)
    {
        $acl = Get-Acl $path
        $ar = New-Object System.Security.AccessControl.FileSystemAccessRule($user, "ReadAndExecute", "Allow")
        $acl.SetAccessRule($ar)
        Set-Acl $path $acl
    }
}

Function InstallServerCode
{
    Write-Host -ForegroundColor Cyan "Installing applications."
    $serverCodeRootPath = GetIISCodePath

    foreach ($app in $script:webApps)
    {
        Write-Host -ForegroundColor DarkGray "`tInstalling webApp: $($app.Name)"
        
        $appPath = Join-Path $script:iisWwwRoot $app.IISRelativePath

        if ($(Get-WebApplication $app.Name) -ne $null)
        {
            Write-Host "`tRemoving $($app.Name)"
            Remove-WebApplication -Site $script:defaultWebSite -Name $app.Name
            Remove-Item ($appPath + "\*") -Recurse -Force -ErrorAction SilentlyContinue
        }

        Write-Host "`tAdding $($app.Name)"

        $tempPath = Join-Path $serverCodeRootPath $app.SourceRelativePath
        mkdir $appPath -ErrorAction SilentlyContinue | Out-Null
        Copy-Item ($tempPath + "\*") $appPath -Recurse -ErrorAction Stop
        
        New-WebApplication -Site $script:defaultWebSite -Name $app.Name -PhysicalPath $appPath | Out-Null

        foreach ($config in $app.Configuration)
        {
            Set-WebConfigurationProperty -Filter $config.Path -Name $config.Name -Value $config.Value -PSPath IIS:\ -location "$($script:defaultWebSite)/$($app.Name)" -ErrorAction Stop
        }

        GrantUserAccess $appPath $app.UserAccess
    }
}

Function RemoveServerCode
{
    Write-Host -ForegroundColor Cyan "Removing server code."
    foreach ($app in $script:webApps)
    {
        Write-Host -ForegroundColor DarkGray "`tRemoving webApp files: $($app.Name)"
        $appPath = Join-Path $script:iisWwwRoot $app.IISRelativePath
        rmdir -Recurse -Force $appPath -ErrorAction SilentlyContinue
    }
}

Function Install
{
    Write-Host -ForegroundColor Cyan "Installing prerequisites for test role: $($script:COREFX_ROLE_NAME)"
    CheckMachineInfo

    InstallIIS
    InstallServerCertificates
    CreateLocalUser
    ConfigureWebSites
    InstallServerCode
    InstallServerFirewall

    EnvironmentSetInstalledRoleStatus
}

Function Uninstall
{
    Write-Host -ForegroundColor Cyan "Removing prerequisites for test role: $($script:COREFX_ROLE_NAME)"

    EnvironmentCheckUninstallRoleStatus

    RemoveServerFirewall
    RemoveIIS
    RemoveServerCertificates
    RemoveLocalUser
    RemoveServerCode
    
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
