$tasksFolder="~\.nuget\packages\illink.tasks"
If (Test-Path $tasksFolder) {
  Remove-Item -r $tasksFolder
}

$dotNetTool = Join-Path $PSScriptRoot "..\corebuild\dotnet.ps1"
# create integration packages
& $dotNetTool restore (Join-Path $PSScriptRoot "linker.sln")
& $dotNetTool pack (Join-Path $PSScriptRoot "linker.sln")
