# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

# Creates a .sln that includes all of the library src, ref, or test projects.

param (
    [string]$type = "src"
)

$SolutionName = "Libraries." $($type) ".Generated.sln"

# Delete the existing solution if it exists
if (Test-Path $SolutionName)
{
    Remove-Item $SolutionName
}

# Create the new solution
dotnet new sln --name $([System.IO.Path]::GetFileNameWithoutExtension($SolutionName))

# Populate it with all *\src\*.csproj projects
foreach ($f in Get-ChildItem -Path $([System.IO.Path]::Combine("*", $type, "*")) -Filter *.csproj)
{
    dotnet sln $SolutionName add --in-root $f.FullName
}

if ($type -eq "src")
{
    # Also add CoreLib if this is for src projects
    dotnet sln $SolutionName add --in-root $f.FullName $([System.IO.Path]::Combine("..", "coreclr", "src", "System.Private.CoreLib", "System.Private.CoreLib.csproj"))
}
