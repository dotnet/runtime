Write-Host 'Killing running build processes ...'
foreach ($processName in @('msbuild', 'dotnet', 'vbcscompiler')) {
  Get-Process -Name $processName -ErrorAction SilentlyContinue | Stop-Process
}
