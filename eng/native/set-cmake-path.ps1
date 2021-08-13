# This script locates the CMake executable for the build system and outputs either the "set CMakePath=..."
# command (if CMake is found) or the "exit /b 1" command (if not found) for evaluating from batch files.

Set-StrictMode -Version 3

function LocateCMake {
  # Find the first cmake.exe on the PATH
  $cmakeApp = (Get-Command cmake.exe -ErrorAction SilentlyContinue)
  if ($cmakeApp -ne $null) {
    return $cmakeApp.Path
  }

  # Find cmake.exe using the registry
  $cmakeRegKey = Get-ItemProperty HKLM:\SOFTWARE\Kitware\CMake -Name InstallDir -ErrorAction SilentlyContinue
  if ($cmakeRegKey -eq $null) {
    $cmakeRegKey = Get-ItemProperty HKLM:\SOFTWARE\Wow6432Node\Kitware\CMake -Name InstallDir -ErrorAction SilentlyContinue
  }

  if ($cmakeRegKey -ne $null) {
    $cmakePath = $cmakeRegKey.InstallDir + "bin\cmake.exe"
    if (Test-Path $cmakePath -PathType Leaf) {
      return $cmakePath
    }
  }

  return $null
}

try {
  $cmakePath = LocateCMake

  if ($cmakePath -eq $null) {
    throw "CMake is a pre-requisite to build this repository but it was not found on the PATH or in the registry. Please install CMake from https://cmake.org/download/."
  }

  $version = [Version]$(& $cmakePath --version | Select-String -Pattern '\d+\.\d+\.\d+' | %{$_.Matches.Value})

  if ($version -lt [Version]"3.16.4") {
    throw "CMake 3.16.4 or newer is required for building this repository. The newest version of CMake installed is $version. Please install CMake 3.16.4 or newer from https://cmake.org/download/."
  }

  [System.Console]::WriteLine("set CMakePath=" + $cmakePath)

}
catch {
  [System.Console]::Error.WriteLine($_.Exception.Message)
  [System.Console]::WriteLine("exit /b 1")
}
