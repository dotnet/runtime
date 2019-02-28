$tasksFolder="~\.nuget\packages\illink.tasks"
If (Test-Path $tasksFolder) {
  Remove-Item -r $tasksFolder
}

$dotNetTool = Join-Path $PSScriptRoot "..\..\eng\dotnet.ps1"
# create integration packages
& $dotNetTool restore (Join-Path $PSScriptRoot ".." ".." "illink.sln")
& $dotNetTool pack (Join-Path $PSScriptRoot ".." ".." "illink.sln")
