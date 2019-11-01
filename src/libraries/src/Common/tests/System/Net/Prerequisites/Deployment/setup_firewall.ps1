# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

#Requires -RunAsAdministrator

# Firewall configuration
$script:firewallGroup = "CoreFX Testing"
$script:firewallRules = @(
    @{Name = "CoreFXNet - HTTP 80"; Port = 80},
    @{Name = "CoreFXNet - HTTP 443"; Port = 443}
)

Function InstallServerFirewall
{
    Write-Host -ForegroundColor Cyan "Installing Firewall rules."
    foreach ($rule in $script:firewallRules)
    {
        Write-Host -ForegroundColor DarkGray "`t" $rule.Name
        New-NetFirewallRule -DisplayName $rule.Name -Group $script:firewallGroup -Direction Inbound -Protocol TCP -LocalPort $rule.Port -Action Allow | Out-Null
    }
}

Function RemoveServerFirewall
{
    Write-Host -ForegroundColor Cyan "Removing Firewall rules."
    foreach ($rule in $script:firewallRules)
    {
        Write-Host -ForegroundColor DarkGray "`t" $rule.Name
        Remove-NetFirewallRule -DisplayName $rule.Name | Out-Null
    }
}
