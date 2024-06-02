schtasks /change /tn "\Microsoft\VisualStudio\VSIX Auto Update" /disable

$vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path -Path "$vswhere" -PathType Leaf))
{
    Write-Error "Couldn't locate vswhere at $vswhere"
    exit 1
}

$vsdir = &"$vswhere" -latest -prerelease -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
$vsregedit = "$vsdir\Common7\IDE\VsRegEdit.exe"

if (-not (Test-Path -Path "$vsregedit" ))
{
    Write-Error "VSWhere returned path: $vsdir, but regedit $vsregedit doesn't exist."
    exit 1
}

Write-Output "VSWhere returned path: $vsdir, using regedit $vsregedit"
Write-Output "Disabling updates through VS Registry:"

&"$vsdir\Common7\IDE\VsRegEdit.exe" set local HKCU ExtensionManager AutomaticallyCheckForUpdates2Override dword 0
&"$vsdir\Common7\IDE\VsRegEdit.exe" read local HKCU ExtensionManager AutomaticallyCheckForUpdates2Override dword
