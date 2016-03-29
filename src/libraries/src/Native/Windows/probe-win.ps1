# This file probes for the prerequisites for the build system, and outputs commands for eval'ing
# from the cmd scripts to set variables (and exit on error)

function GetCMakeVersions
{
  $items = @()
  $items += @(Get-ChildItem hklm:\SOFTWARE\Wow6432Node\Kitware -ErrorAction SilentlyContinue)
  $items += @(Get-ChildItem hklm:\SOFTWARE\Kitware -ErrorAction SilentlyContinue)
  return $items | where { $_.PSChildName.StartsWith("CMake ") }
}

function GetCMakeInfo($regKey)
{
  # This no longer works for versions 3.5+
  try {
    $version = [System.Version] $regKey.PSChildName.Split(' ')[1]
  }
  catch {
    return $null
  }
  $cmakeDir = (Get-ItemProperty $regKey.PSPath).'(default)'
  $cmakePath = [System.IO.Path]::Combine($cmakeDir, "bin\cmake.exe")
  if (![System.IO.File]::Exists($cmakePath)) {
    return $null
  }
  return @{'version' = $version; 'path' = $cmakePath}
}

function LocateCMake
{
  $errorMsg = "CMake is a pre-requisite to build this repository but it was not found on the path. Please install CMake from http://www.cmake.org/download/ and ensure it is on your path."
  $inPathPath = (get-command cmake.exe -ErrorAction SilentlyContinue).Path
  if ($inPathPath -ne $null) {
    return $inPathPath
  }
  # Check the default installation directory
  $inDefaultDir = [System.IO.Path]::Combine(${Env:ProgramFiles(x86)}, "CMake\bin\cmake.exe")
  if ([System.IO.File]::Exists($inDefaultDir)) {
    return $inDefaultDir
  }
  # Let us hope that CMake keep using their current version scheme
  $validVersions = @()
  foreach ($regKey in GetCMakeVersions) {
    $info = GetCMakeInfo($regKey)
    if ($info -ne $null) { 
      $validVersions += @($info)
    }
  }
  $newestCMakePath = ($validVersions |
    Sort-Object -property @{Expression={$_.version}; Ascending=$false} |
    select -first 1).path
  if ($newestCMakePath -eq $null) {
    Throw $errorMsg
  }
  return $newestCMakePath
}

try {
  $cmakePath = LocateCMake
  [System.Console]::WriteLine("set CMakePath=" + $cmakePath)
}
catch {
  [System.Console]::Error.WriteLine($_.Exception.Message)
  [System.Console]::WriteLine("exit /b 1")
}
