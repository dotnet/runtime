# This file probes for the prerequisites for the build system, and outputs commands for eval'ing
# from the cmd scripts to set variables (and exit on error)

function GetCMakeVersions
{
  $items = @()
  $items += @(Get-ChildItem hklm:\SOFTWARE\Wow6432Node\Kitware -ErrorAction SilentlyContinue)
  $items += @(Get-ChildItem hklm:\SOFTWARE\Kitware -ErrorAction SilentlyContinue)
  return $items | where { $_.PSChildName.StartsWith("CMake") }
}

function GetCMakeInfo($regKey)
{
  try {
    $version = [System.Version] $regKey.PSChildName.Split(' ')[1]
  }
  catch {
    return $null
  }
  $itemProperty = Get-ItemProperty $regKey.PSPath;
  if (Get-Member -inputobject $itemProperty -name "InstallDir" -Membertype Properties) {
    $cmakeDir = $itemProperty.InstallDir
  }
  else {
    $cmakeDir = $itemProperty.'(default)'
  }
  $cmakePath = [System.IO.Path]::Combine($cmakeDir, "artifacts\cmake.exe")
  if (![System.IO.File]::Exists($cmakePath)) {
    return $null
  }
  return @{'version' = $version; 'path' = $cmakePath}
}

function LocateCMake
{
  $errorMsg = "CMake is a pre-requisite to build this repository but it was not found on the path. Please install CMake from https://cmake.org/download/ and ensure it is on your path."
  $inPathPath = (get-command cmake.exe -ErrorAction SilentlyContinue)
  if ($inPathPath -ne $null) {
    # Resolve the first version of CMake if multiple commands are found
    if ($inPathPath.Length -gt 1) {
      return $inPathPath[0].Path
    }
    return $inPathPath.Path
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
  $version = [Version]$(& $cmakePath --version | Select-String -Pattern '\d+\.\d+\.\d+' | %{$_.Matches.Value})

  if ($version -lt [Version]"3.14.0") {
      Throw "This repository requires CMake 3.14. The newest version of CMake installed is $version. Please install CMake 3.14 or newer from https://cmake.org/download/ and ensure it is on your path."
  }

  [System.Console]::WriteLine("set CMakePath=" + $cmakePath)

}
catch {
  [System.Console]::Error.WriteLine($_.Exception.Message)
  [System.Console]::WriteLine("exit /b 1")
}
