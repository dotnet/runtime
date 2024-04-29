& "C:/live-runtime-artifacts/testhost/net$env:VERSION-windows-$env:CONFIGURATION-x64/dotnet.exe" exec --roll-forward Major ./bin/$env:CONFIGURATION/net$env:VERSION/HttpStress.dll $env:HTTPSTRESS_ARGS.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)

if ($LASTEXITCODE -ne 0) {
    cp "C:/live-runtime-artifacts/testhost/net$env:VERSION-windows-$env:CONFIGURATION-x64/" $env:DUMPS_SHARE_MOUNT_ROOT
}
