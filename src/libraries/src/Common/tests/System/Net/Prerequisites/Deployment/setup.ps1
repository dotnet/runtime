# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

#Requires -RunAsAdministrator

Param (
    [switch] $uninstall=$false
)

# Import configuration.
. .\setup_common.ps1

Function TestMachineStatus($role)
{
    try 
    {
        $status = Invoke-Command -ComputerName $role.MachineName -ArgumentList $role.Name -ErrorAction Stop { [Environment]::GetEnvironmentVariable($args[0], "Machine") }
        
        $role.Reachable = $true
        if ($status -eq "Installed")
        {
            $role.Installed = $true
        }
        else 
        {
            if (-not [string]::IsNullOrWhiteSpace($status))
            {
                Write-Warning "Role $($role.Name) found to have status of: $status."
                Write-Warning "The script will try to resume the installation. To manually resume installation, manually run the $($role.Script) on $($role.MachineName) as Administrator."
            }
        }
    }
    catch [System.Exception] 
    {
        $role.Reachable = $false
    }
}

Function CheckRoles
{
    Write-Host "Verifying server applications:"
    $iisApplications = GetIISCodePath
    if (-not (Test-Path (Join-Path $iisApplications "index.html")))
    {
        throw "Cannot find index.html within the $iisApplications path. Make sure that you have built and copied the Server code before running this script."
    }

    Write-Host "Verifying roles:"
    foreach ($role in ($script:Roles + $script:PreRebootRoles))
    {
        Write-Host -ForegroundColor DarkGray "`t" $role.Name
        if (-not (Test-Path $role.Script))
        {
            throw "Cannot find installation script for $($role.Name): $($role.Script)"
        }

        if ([string]::IsNullOrWhiteSpace($role.MachineName))
        {
            throw "Please edit config.ps1 and set a valid value name for $($role.Name)_Machine."
        }

        TestMachineStatus $role
    }

    Write-Host "OK."
}

Function EnsurePreRebootForCurrentMachine
{
    $machineInfo = Get-WmiObject win32_computersystem
    $currentRole = GetPreRebootRoleForMachine($Env:COMPUTERNAME)

    if (($machineInfo.PartOfDomain -eq $true) -and `
        ($machineInfo.Domain -eq $script:domainName) -and `
        ($currentRole.Installed -eq $true))
    {
        return $true
    }
    elseif (($machineInfo.PartOfDomain -eq $true) -and ` 
            ($machineInfo.Domain -ne $script:domainName))
    {
        Write-Error "The current machine is already joiend to the $($machineInfo.Domain) domain."
        Write-Error "Either change config.ps1 to use the correct domain information, select a different machine or remove the machine from the current domain."
        throw "Cannot use the current machine: already joined to a domain."
    }

    & (".\" + $currentRole.Script)
}

Function CreateDestinationPath($s)
{
    return Invoke-Command -Session $s `
    {
        $destPath = Join-Path $env:SystemDrive "COREFX_NET_Scripts"
        mkdir $destPath -ErrorAction SilentlyContinue | Out-Null  
        return $destPath  
    }    
}

Function CopyScripts($s, $remotePath)
{
    Copy-Item -Recurse -Force -Path ".\*" -Destination $remotePath -ToSession $s -ErrorAction Stop
}

Function InstallRoles
{
    Write-Host -ForegroundColor Cyan "Remotely installing all roles."

    foreach ($role in $script:Roles)
    {
        Write-Host -ForegroundColor DarkCyan "Installing role [$($role.Name)]: $($role.MachineName)"

        Write-Host -ForegroundColor DarkGray "`tConnecting"
        $s = New-PSSession -ComputerName $role.MachineName
                 
        Write-Host -ForegroundColor DarkGray "`tCopying scripts"
        # Copy scripts
        $remotePath = CreateDestinationPath $s
        CopyScripts $s $remotePath

        Write-Host -ForegroundColor DarkGray "`tInstalling"
        # Run remote scripts
        Invoke-Command -Session $s -ArgumentList $remotePath, $role.Script `
        { 
            $path = $args[0]
            $script = $args[1]
            
            cd $path
            & (".\" + $script)
        }

        Write-Host -ForegroundColor DarkCyan "Role [$($role.Name)]: $($role.MachineName) installation complete."
        Write-Host
        Write-Host
    }
}

Function Install
{
    Write-Host -ForegroundColor Cyan "Install/Update CoreFX Networking multi-machine prerequisites"
    Write-Host
    CheckRoles

    EnsurePreRebootForCurrentMachine

    if (($script:Roles | where {$_.Reachable -ne $true}).Count -ne 0)
    {
        Write-Warning "Not all roles are reachable from this host."
        Write-Host "- If all machines are joined to the Domain, make sure you are logged on as $($script:domainNetbios)\Administrator and not as a local Administrator."
        Write-Host "- If not all machines are joined to the Domain: Log-on to the following machines, copy all scripts and run .\setup.ps1 as Administrator:"
        $script:Roles | where {$_.Reachable -ne $true}
        Write-Host -ForegroundColor Cyan "Rerun this command after all machines have been started and joined to the $($script:domainNetBios) domain."
        return
    }

    InstallRoles

    Write-Host -ForegroundColor Cyan "Role installation complete."
}

Function UnistallMachines
{
    Write-Host -ForegroundColor Cyan "Remotely uninstalling roles."

    foreach ($role in $script:Roles)
    {
        Write-Host -ForegroundColor DarkCyan "Uninstalling role [$($role.Name)]: $($role.MachineName)"

        Write-Host -ForegroundColor DarkGray "`tConnecting"
        $s = New-PSSession -ComputerName $role.MachineName
                 
        Write-Host -ForegroundColor DarkGray "`tCopying scripts"
        # Copy scripts
        $remotePath = CreateDestinationPath $s
        CopyScripts $s $remotePath

        $preRebootRole = GetPreRebootRoleForMachine $role.MachineName

        Write-Host -ForegroundColor DarkGray "`tUninstalling"
        # Run remote scripts
        Invoke-Command -Session $s -ArgumentList $remotePath, $role.Script, $preRebootRole.Script `
        {
            $path = $args[0]
            $script = $args[1]
            $preRebootScript = $args[2]
            
            cd $path
            & (".\" + $script) -uninstall
            & (".\" + $preRebootScript) -uninstall
        }

        Write-Host -ForegroundColor DarkCyan "Role [$($role.Name)]: $($role.MachineName) uninstall complete."
        Write-Host
        Write-Host
    }
}

Function Uninstall
{
    Write-Host -ForegroundColor Cyan "Uninstall CoreFX Networking multi-machine prerequisites"
    Write-Host
    CheckRoles
    Write-Host
    
    Write-Warning "Some of the installed components may have existed before or got changes outside of this setup script."
    Write-Warning "The scripts will attempt to remove all components regardless of these changes."
    $continue = Read-Host "Do you want to continue? [Y/N]"

    if ($continue.ToUpper() -ne "Y")
    {
        Write-Warning "Aborted by user."
        return
    }

    UnistallMachines
}

if ($uninstall)
{
    Uninstall
}
else
{
    Install
}
