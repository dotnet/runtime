# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

#
# CoreFX - Net configuration
#

# -- Machine selection

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
$COREFX_NET_AD_Machine = ""         #Example: "TESTAD"
$COREFX_NET_AD_MachineIP = ""         #Example: "192.168.0.1" - must be a Static IPv4 address.

# A Windows Client or Server SKU hosting the IIS Server. This machine will be joined to the Domain.
$COREFX_NET_IISSERVER_Machine = ""  #Example: "TESTIIS"
$COREFX_NET_IISSERVER_MachineIP = ""        #Example: "192.168.0.1" - must be a Static IPv4 address.

# A Windows Client or Server SKU hosting the corefx enlistment. This machine will be joined to the Domain.
$COREFX_NET_CLIENT_Machine = ""     #Example: "TESTCLIENT"

# -- Test parameters

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

# The following variable names should match the ones read by Configuration.*.cs code.
$script:ClientConfiguration = @(

# TODO #9048: 
    # Configuration.Http:
#    @{Name = "COREFX_HTTPHOST"; Value = $script:iisServerFQDN},
#    @{Name = "COREFX_SECUREHTTPHOST"; Value = $script:iisServerFQDN},
#    @{Name = "COREFX_HTTP2HOST"; Value = $script:iisServerFQDN},                # Requires Windows 10 and above.
#    @{Name = "COREFX_DOMAINJOINED_HTTPHOST"; Value = $script:iisServerFQDN},
#    @{Name = "COREFX_DOMAINJOINED_PROXYHOST"; Value = $null},                   # TODO 9048: Install & configure ISA/TMG or Squid
#    @{Name = "COREFX_DOMAINJOINED_PROXYPORT"; Value = $null},                   # TODO 9048: Install & configure ISA/TMG or Squid
#    @{Name = "COREFX_HTTPHOST_SSL2"; Value = $null},                            # TODO 9048: move to a localhost server or configure these websites.
#    @{Name = "COREFX_HTTPHOST_SSL3"; Value = $null},
#    @{Name = "COREFX_HTTPHOST_TLS10"; Value = $null},
#    @{Name = "COREFX_HTTPHOST_TLS11"; Value = $null},
#    @{Name = "COREFX_HTTPHOST_TLS12"; Value = $null},
#    @{Name = "COREFX_HTTPHOST_EXPIREDCERT"; Value = $null},
#    @{Name = "COREFX_HTTPHOST_WRONGHOSTNAME"; Value = $null},
#    @{Name = "COREFX_HTTPHOST_SELFSIGNEDCERT"; Value = $null},
#    @{Name = "COREFX_HTTPHOST_REVOKEDCERT"; Value = $null},
#    @{Name = "COREFX_STRESS_HTTP"; Value = "1"},

    # Configuration.WebSockets:
#    @{Name = "COREFX_WEBSOCKETHOST"; Value = $script:domainNetbios},
#    @{Name = "COREFX_SECUREWEBSOCKETHOST"; Value = $script:domainNetbios},

    # Configuration.Security:
    @{Name = "COREFX_NET_AD_DOMAINNAME"; Value = $script:domainNetbios},
    @{Name = "COREFX_NET_AD_USERNAME"; Value = $script:domainUserName},
    @{Name = "COREFX_NET_AD_PASSWORD"; Value = $script:domainUserPassword},
    @{Name = "COREFX_NET_SECURITY_NEGOSERVERURI"; Value = "http://$($script:iisServerFQDN)"},
    @{Name = "COREFX_NET_SECURITY_TLSSERVERURI"; Value = "https://$($script:iisServerFQDN)"},

    @{Name = "COREFX_NET_SOCKETS_SERVERURI"; Value = "http://$($script:iisServerFQDN)"}
)
