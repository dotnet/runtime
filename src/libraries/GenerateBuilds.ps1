
$header = @"
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="12.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="`$([MSBuild]::GetDirectoryNameOfFileAbove(`$(MSBuildThisFileDirectory), dir.props))\dir.props" />
  <ItemGroup>

"@

$footer = @"
  </ItemGroup>
  <Import Project="`$([MSBuild]::GetDirectoryNameOfFileAbove(`$(MSBuildThisFileDirectory), dir.traversal.targets))\dir.traversal.targets" />
</Project>


"@

function WriteBuilds($allConfigs, $srcDir, $projName)
{
  $configs = "";

  # Sort via Target then OS
  $ac = $allConfigs | sort target, os

  foreach($c in $ac)
  {
    if ($c.relpath -eq $null) { continue }

    $config = "    <Project Include=`"$($c.relPath)`""

    if ($c.os -ne $null -or $c.target -ne $null)
    {
      $config += ">`r`n";
      if ($c.os -ne $null) { $config += "      <OSGroup>$($c.os)</OSGroup>`r`n"; }
      if ($c.target -ne $null) { $config += "      <TargetGroup>$($c.target)</TargetGroup>`r`n"; }
      $config += "    </Project>`r`n"
    }
    else
    {
      $config += " />`r`n";
    }

    $configs += $config;
  }

  #Write-Host $($srcDir.FullName +"\"+ $projName + ".builds")
  sc $($srcDir.FullName +"\"+ $projName + ".builds") $($header+$configs+$footer)
  #Write-Host $($header+$configs+$footer)
}

function GetDefaultConfiguration($projectConfigs, $projName)
{
  if ($projectConfigs -eq $null -or $projectConfigs.Count -eq 0)
  {
    Write-Host "No configurations in $projName";
    return $null;
  }

  $defaultConfig = $projectConfigs[0];

  if ($defaultConfig.os -ne $null -or $defaultConfig.target -ne $null)
  {
    $windowsConfig = $projectConfigs | ? { $_.os -eq "Windows_NT" }

    if ($windowsConfig.Count -gt 0)
    {
      $defaultConfig = $windowsConfig[0];
    }
  }
  return $defaultConfig;
}

function WriteDefaultConfiguration($defaultConfig, $proj, $pf)
{
    if ($defaultConfig.os -ne $null -or $defaultConfig.target -ne $null)
    {
      $dc = "";

      if ($defaultConfig.os -ne $null)
      {
        if ($defaultConfig.os -eq "Windows_NT")
        {
          $dc += "Windows_";
        }
        else
        {
          $dc += $defaultConfig.os + "_";
        }
      }
      if ($defaultConfig.target -ne $null) { $dc += $defaultConfig.target + "_"; }

      $dc += "Debug";

      $projectConfigProperty = "    <Configuration Condition=`"'`$(Configuration)'==''`">$dc</Configuration>";

      $projectLines = $pf[0..1];

      $pfContinue = 2;
      if ($pf[2] -match "Import")
      {
        $projectLines += "  <PropertyGroup>";
        $projectLines += $projectConfigProperty;
        $projectLines += "  </PropertyGroup>"
      }
      else
      {
        if ($pf[3] -match "Configuration")
        {
          $pf[3] = $projectConfigProperty;
        }
        elseif ($pf[3] -match "Setting default TargetGroup" -and $pf[4] -match "TargetGroup")
        {
          $projectLines += "  <PropertyGroup>";
          $projectLines += $projectConfigProperty;
          $projectLines += "  </PropertyGroup>"
          $pfContinue = 6;
        }
        else
        {
          Write-Host $($proj.FullName + " doesn't have a Configuration block!");
          s $proj.FullName
        }
      }

      $projectLines += $pf[$pfContinue..$pf.Length];

      sc $proj.FullName $projectLines
    }
}

function CleanupProjects($proj, $pf)
{
    $writeFile = $false;

    $lineCount

    $filtered += $pf | ? {
     $lineCount++;
      if ($lineCount -gt 4 -and $_ -match "<Configuration ") { $writeFile = $true; return $false; }
      if ($_ -match "<Platform ") { $writeFile = $true; return $false; }
      if ($_ -match "<OutputType>Library</OutputType>") { $writeFile = $true; return $false; }
      return $true;
    };

    if ($writeFile)
    {
      sc $proj.FullName $filtered
    }
}

function GetConfigurations($projs, $srcDir, $projName)
{
  $allConfigs = @();
  foreach($proj in $projs)
  {
    $pf = gc $proj;
    $pfcs = @();

    #CleanupProjects $proj $pf

    $pf | ? { $_ -match "'(?<osg>Windows|Linux|OSX|FreeBSD)?_?(?<tg>net\d\d\d?|netcore\d\d|netcore\d\daot|netcoreapp\d\.\d|dnxcore\d\d|dotnet\d\d)?_?Debug\|AnyCPU'" } | % {

      $os = $matches["osg"];
      $target = $matches["tg"];
      $relPath = $proj.FullName.Replace($srcDir.FullName+"\", "");

      #if ($target -match "netcore.+") { $os = "Windows" }

      if ($os -eq "Windows") { $os = "Windows_NT" }

      $ht = new-object System.Object
      $ht | Add-Member -type NoteProperty -name os -value $os
      $ht | Add-Member -type NoteProperty -name target -value $target
      $ht | Add-Member -type NoteProperty -name relPath -value $relPath
      $ht | Add-Member -type NoteProperty -name projName -value $projName
      $ht | Add-Member -type NoteProperty -name sortKey -value $($os+"-"+$target+"_"+$relPath)
      $pfcs += $ht;
    }
    $defaultConfig = GetDefaultConfiguration $pfcs $proj.FullName
    WriteDefaultConfiguration $defaultConfig $proj $pf
    $allConfigs += $pfcs;
  }

  return $allConfigs | sort target, os;
}

$srcDirs = dir .\src\*\src
$layoutDir = "D:\corefx-packages-backup\packages\Debug\layout\runtimes\win7-x64\lib\netcoreapp2.0"

foreach($srcDir in $srcDirs)
{
  $projName = $srcDir.Parent.Name
  $pjOrig = "$srcDir\project.json"
  $pj = "$layoutDir\$projName.deps"
  $plj = "$srcDir\project.lock.json"
  $proj = "$srcDir\$projName.csproj"
  Write-Host "Looking at $pj"
  if (Test-Path $pj)
  {
    $deps = new-object System.Collections.ArrayList
    $pjc = gc $pj;
    foreach ($line in $pjc)
    {
      if ($line -match "\`"`(?<dep>.*`)\`": \`"\d")
      {
        $item = $matches["dep"];
        if ($item -eq "Microsoft.TargetingPack.Private.CoreCLR")
        {
          $deps.Add("System.Private.CoreLib") | out-null
        }
        elseif ($item -eq "Microsoft.TargetingPack.NETFramework.v4.6")
        {
        }
        elseif ($item -eq "Microsoft.TargetingPack.NETFramework.v4.6.1")
        {
        }
        elseif ($item -eq "Microsoft.TargetingPack.NETFramework.v4.6.2")
        {
        }
        elseif ($item -eq "Microsoft.TargetingPack.Private.NETNative")
        {
        }
        elseif ($item -eq "Microsoft.NETCore.Platforms")
        {
        }
        else
        {
          $deps.Add($item) | out-null
        }
      }
    }

    $pjOrigC = gc $pjOrig;
    foreach ($line in $pjOrigC)
    {
      if ($line -match "\`"`(?<dep>.*`)\`": \`"\d")
      {
        $item = $matches["dep"];
        if ($item -eq "System.Diagnostics.Contracts")
        {
          $deps.Add($item) | out-null
        }
        elseif ($item -eq "System.Diagnostics.Tools")
        {
          $deps.Add($item) | out-null
        }
      }
    }

    $projc = gc $proj;
    $projc2 = $projc | % { if ($_ -match "None Include=`"project.json`"") { $($deps | % { "    <Reference Include=`"$_`" />" }) } else { $_ } }
    $projc2 | sc $proj

    del $pjOrig
    #del $plj
  }
}
