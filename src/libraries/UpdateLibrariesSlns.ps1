# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

$ProjContent = @'
<Project Sdk="Microsoft.Build.Traversal">
  <PropertyGroup>
    <IncludeInSolutionFile>false</IncludeInSolutionFile>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$(MSBuildThisFileDirectory)ref\*.*proj" />
    <ProjectReference Include="$(MSBuildThisFileDirectory)src\*.*proj" />
    <ProjectReference Include="$(MSBuildThisFileDirectory)tests\**\*.Tests.*proj" />
  </ItemGroup>
</Project>
'@

foreach ($file in Get-ChildItem *\*.sln)
{
    $ProjFilePath = "$(Join-Path $file.DirectoryName $file.BaseName).proj"

    $ProjContent | Out-File -FilePath $ProjFilePath
    dotnet slngen "$ProjFilePath -p SlnGenMainProject=$($file.BaseName) --launch false --nologo"
    Remove-Item $ProjFilePath
}
