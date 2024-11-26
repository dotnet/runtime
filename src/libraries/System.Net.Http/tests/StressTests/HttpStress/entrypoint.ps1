New-Item -Path $env:DUMPS_SHARE_MOUNT_ROOT/$env:STRESS_ROLE -ItemType Directory -ErrorAction SilentlyContinue

# Enable dump collection
$env:DOTNET_DbgEnableMiniDump = '1'
$env:DOTNET_DbgMiniDumpType = "MiniDumpWithFullMemory"
$env:DOTNET_DbgMiniDumpName = "$env:DUMPS_SHARE_MOUNT_ROOT/$env:STRESS_ROLE/coredump.%p.%t"

gi "C:/live-runtime-artifacts/testhost/net$ENV:VERSION-windows-$ENV:CONFIGURATION-x64/shared/Microsoft.AspNetCore.App/*/" | % {
    cp $PSScriptRoot/Microsoft.AspNetCore.Server.Kestrel.Core.* $_
}

& "C:/live-runtime-artifacts/testhost/net$env:VERSION-windows-$env:CONFIGURATION-x64/dotnet" exec --roll-forward Major ./bin/$env:CONFIGURATION/net$env:VERSION/HttpStress.dll $env:STRESS_ARGS.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)

$ExitCode = $LASTEXITCODE

if ($ExitCode -ne 0) {
    Write-Host "HttpStress failed, copying artifacts for investigation"

    # Copy runtime if it's not already there
    if ($env:DUMPS_SHARE_MOUNT_ROOT -and !(Test-Path -Path $env:DUMPS_SHARE_MOUNT_ROOT/net$env:VERSION-windows-$env:CONFIGURATION-x64/ -ErrorAction SilentlyContinue)) {
        Copy-Item -Recurse C:/live-runtime-artifacts/testhost/net$env:VERSION-windows-$env:CONFIGURATION-x64/ $env:DUMPS_SHARE_MOUNT_ROOT/
    }
}

exit $ExitCode
