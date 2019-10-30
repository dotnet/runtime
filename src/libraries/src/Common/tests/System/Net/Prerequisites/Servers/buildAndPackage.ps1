# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

# Requires Visual Studio Command Prompt
# Requires Azure SDK and .NET Framework SDK installed on the build machine.

$cdir = pwd
$folderName = "CoreFxNetCloudService"
$src = Join-Path $cdir $folderName
$tmp = Join-Path $Env:TEMP $folderName
$tmpOut = Join-Path $tmp "WebServer\PublishToIIS"
$dst = Join-Path $cdir "..\Deployment\IISApplications"

$nugetSrc = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

if (Test-Path $tmp)
{
    rm -Recurse $tmp
}

copy -Recurse $src $Env:TEMP
Start-BitsTransfer -Source $nugetSrc -Destination $tmp

cd $tmp

.\nuget restore
msbuild /p:DeployOnBuild=true /p:PublishProfile=IIS_PublishToLocalPath_RET

copy -Recurse $tmpOut $dst

cd $cdir
rm -Recurse $tmp
