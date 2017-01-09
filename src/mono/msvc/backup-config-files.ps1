##############################################################################
##
## backup-config-files
##
##############################################################################

<#

.SYNOPSIS

Backup mono build configuration files, if needed.

#>

param(

    ## config header file.
    $mono_config,

    ## cygconfig header file.
    $mono_cygconfig
)

$include_cygconfig = Get-Content $mono_config | Select-String -pattern '#include[ ]*\"cygconfig.h\"[ ]*'
if($include_cygconfig -eq $null)
{
	## Backup mono_config into mono_cygconfig, overweiring any existing file.
	Write-Host "Backup " $mono_config " -> " $mono_cygconfig
	Copy-Item $mono_config $mono_cygconfig -Force
}

exit 0;
