# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

#
# CoreFX - Net configuration
#

# -- Machine Information

# You can find the host name by logging on to the target machine and typing "hostname" in a cmd console.
# You can find the IP address by logging on to the target machine and typing "ipconfig" in a cmd console.
# Requirements:
#  - The machines below should be within the same LAN network.
#  - All machines must have Powershell >3.0 installed. 

# IMPORTANT: 
#           The state for the following machines will be changed irreversably. 
#           The uninstall step will remove the installed components regardless if it existed before the deployment or
#           was altered/customized after deployment. 
#

# A Windows Server SKU hosting Active Directory services. This machine will become the Domain Controller:
$COREFX_NET_AD_Machine = ""             #Example: "TESTAD"
$COREFX_NET_AD_MachineIP = ""           #Example: "192.168.0.1" - must be a Static IPv4 address.

# A Windows Client or Server SKU hosting the IIS Server. This machine will be joined to the Domain.
$COREFX_NET_IISSERVER_Machine = ""      #Example: "TESTIIS"
$COREFX_NET_IISSERVER_MachineIP = ""    #Example: "192.168.0.1" - must be a Static IPv4 address.

# A Windows Client or Server SKU hosting the corefx repo. This machine will be joined to the Domain.
$COREFX_NET_CLIENT_Machine = ""         #Example: "TESTCLIENT"

# -- Test Parameters

# For security reasons, it's advisable that the default username/password pairs below are changed regularly.

$script:domainName = "corp.contoso.com"
$script:domainNetbios = "corefx-net-ad"

$script:domainUserName = "testaduser"
$script:domainUserPassword = "Test-ADPassword"

$script:basicUserName = "testbasic"
$script:basicUserPassword = "Test-Basic"

# Changing the IISServer FQDN may require changing certificates.
$script:iisServerFQDN = "testserver.contoso.com"

$script:PreRebootRoles = @(
    @{Name = "COREFX_NET_AD_CLIENT"; Script = "setup_activedirectory_client.ps1"; MachineName = $COREFX_NET_IISSERVER_Machine},
    @{Name = "COREFX_NET_AD_CLIENT"; Script = "setup_activedirectory_client.ps1"; MachineName = $COREFX_NET_CLIENT_Machine},
    @{Name = "COREFX_NET_AD_DC"; Script = "setup_activedirectory_domaincontroller.ps1"; MachineName = $COREFX_NET_AD_Machine; MachineIP = $COREFX_NET_AD_MachineIP}
)

$script:Roles = @(
    @{Name = "COREFX_NET_IISSERVER"; Script = "setup_iisserver.ps1"; MachineName = $COREFX_NET_IISSERVER_Machine; MachineIP = $COREFX_NET_IISSERVER_MachineIP},
    @{Name = "COREFX_NET_CLIENT"; Script = "setup_client.ps1"; MachineName = $COREFX_NET_CLIENT_Machine},
    @{Name = "COREFX_NET_AD_DC"; Script = "setup_activedirectory_domaincontroller.ps1"; MachineName = $COREFX_NET_AD_Machine; MachineIP = $COREFX_NET_AD_MachineIP}
)
