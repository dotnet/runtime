
gi "C:/live-runtime-artifacts/testhost/net$ENV:VERSION-windows-$ENV:CONFIGURATION-x64/shared/Microsoft.AspNetCore.App/*/" | % {
    cp "C:/live-runtime-artifacts/System.IO.Pipelines/$ENV:CONFIGURATION/net$ENV:VERSION/System.IO.Pipelines.dll" $_
    cp $PSScriptRoot/Microsoft.AspNetCore.Server.Kestrel.Core.* $_
}

& "C:/live-runtime-artifacts/testhost/net$env:VERSION-windows-$env:CONFIGURATION-x64/dotnet.exe" exec --roll-forward Major ./bin/$env:CONFIGURATION/net$env:VERSION/HttpStress.dll $env:HTTPSTRESS_ARGS.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)

if ($LASTEXITCODE -ne 0) {
    cp "C:/live-runtime-artifacts/testhost/net$env:VERSION-windows-$env:CONFIGURATION-x64/" $env:DUMPS_SHARE_MOUNT_ROOT
    exit 1;
}
