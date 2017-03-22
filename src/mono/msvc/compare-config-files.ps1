##############################################################################
##
## compare-config-files
##
##############################################################################

<#

.SYNOPSIS

Compares mono build configuration files detecting incompatible changes.

#>

param(
    ## winconfig header file.
    $mono_winconfig,

    ## config header file.
    $mono_config,

	## The master configuration file, optional.
	$mono_config_ac
)

## Get the content from each config file
$mono_winconfig_content = Get-Content $mono_winconfig
$mono_config_content = Get-Content $mono_config

## Compare config files.
$comparedLines = Compare-Object $mono_winconfig_content $mono_config_content -IncludeEqual | Sort-Object { $_.InputObject.ReadCount }

$comparedLines | foreach {

    if($_.SideIndicator -ne "==")
    {
		##Look for diffs.
		$mono_version = (Select-String -InputObject $_.InputObject -pattern '#define VERSION \"(.*)\"')
		$mono_corlib_version = (Select-String -InputObject $_.InputObject -pattern '#define MONO_CORLIB_VERSION')
		if ($mono_version -eq $null -And $mono_corlib_version -eq $null) {
			Write-Host "Changes detected, versions doesn't match. Configuration must to be replaced."
			exit 1;
		}
    }
}

if ($mono_config_ac -ne $null -And $mono_config_ac -ne "") {

	$mono_version_ac = (Select-String -path $mono_config_ac -pattern 'AC_INIT\(mono, \[(.*)\]').Matches[0].Groups[1].Value
	$mono_version = (Select-String -path $mono_config -pattern '#define VERSION \"(.*)\"').Matches[0].Groups[1].Value

	if($mono_version_ac -ne $mono_version)
	{
		Write-Host "Changes detected, versions doesn't match. Configuration must to be replaced."
		exit 1;
	}
}

exit 0;
