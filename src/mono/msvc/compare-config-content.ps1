##############################################################################
##
## compare-config-content
##
##############################################################################

<#

.SYNOPSIS

Compares mono build configuration content detecting diff's.

#>

param(
    ## first config source to compare.
    $mono_config_source1,

    ## second config source to compare.
    $mono_config_source2
)

if ((Test-Path -isvalid $mono_config_source1) -And (Test-Path $mono_config_source1))
{
	$mono_config_source1_content = Get-Content $mono_config_source1
}
else
{
	$mono_config_source1_content = $mono_config_source1
}

if ((Test-Path -isvalid $mono_config_source2) -And (Test-Path $mono_config_source2))
{
	$mono_config_source2_content = Get-Content $mono_config_source2
}
else
{
	$mono_config_source2_content = $mono_config_source2
}

## Compare content.
$comparedLines = Compare-Object $mono_config_source1_content $mono_config_source2_content -IncludeEqual | Sort-Object { $_.InputObject.ReadCount }
$comparedLines | foreach {
    if($_.SideIndicator -ne "==")
    {
		Write-Host "Changes detected."
		exit 1;
    }
}

exit 0;