[CmdletBinding(PositionalBinding=$false)]
Param(
    [string][Alias('c')]$configuration = "Debug",
    [switch] $pack,
    [switch][Alias('t')]$test,
    [switch] $rebuild,
    [switch] $clean,
    [switch] $llvm,
    [switch] $skipnative,
    [switch] $skipmscorlib,
    [switch] $ci,
    [switch][Alias('h')]$help,
    [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

. ../eng/common/pipeline-logging-functions.ps1

function Print-Usage() {
    Write-Host "Common settings:"
    Write-Host "  -configuration <value>  Build configuration: 'Debug' or 'Release' (short: -c)"
    Write-Host "  -help                   Print help and exit (short: -h)"
    Write-Host ""

    Write-Host "Actions:"
    Write-Host "  -pack                     Package build outputs into NuGet packages"
    Write-Host "  -test                     Run all unit tests in the solution (short: -t)"
    Write-Host "  -rebuild                  Clean only runtime build"
    Write-Host "  -clean                    Clean all and exit"
    Write-Host "  -llvm                     Enable LLVM support"
    Write-Host "  -skipnative               Do not build runtime"
    Write-Host "  -skipmscorlib             Do not build System.Private.CoreLib"
    Write-Host "  -ci                       Enable Azure DevOps telemetry decoration"

    Write-Host "Command line arguments not listed above are passed thru to msbuild."
    Write-Host "The above arguments can be shortened as much as to be unambiguous (e.g. -co for configuration, -t for test, etc.)."
}

try {
    if ($help) {
        Print-Usage
        exit 0
    }

    $temp_file = [IO.Path]::GetTempFileName()
    cmd.exe /c " ""$PSScriptRoot\..\msvc\setup-vs-msbuild-env.bat"" && set " > $temp_file
    Get-Content $temp_file | Foreach-Object {
        if ($_ -match "^(.*?)=(.*)$") {
            Set-Content "env:\$($matches[1])" $matches[2]
        }
    }
    Remove-Item $temp_file

    $enable_llvm="false"
    if ($llvm) {
        $enable_llvm="true"
    }

    # clean all
    if ($clean) {
        MSBuild "$PSScriptRoot\build.targets" `
        "/t:Clean" `
        "/p:Configuration=$configuration"

        if ($LastExitCode -ne 0) {
            Write-PipelineTelemetryError -Category "runtime" -Message "Error cleaning"
            exit 1
        }

        exit 0
    }

    # rebuild mono runtime
    if ($rebuild) {
        MSBuild "$PSScriptRoot\build.targets" `
        "/t:build-runtime" `
        "/p:Configuration=$configuration" `
        "/p:RuntimeBuildTarget=clean"

        if ($LastExitCode -ne 0) {
            Write-PipelineTelemetryError -Category "runtime" -Message "Error cleaning unmanaged runtime"
            exit 1
        }
    }

    # build mono runtime
    if (!$skipnative) {
        MSBuild "$PSScriptRoot\build.targets" `
        "/t:build-runtime" `
        "/p:Configuration=$configuration" `
        "/p:MONO_ENABLE_LLVM=$enable_llvm"

        if ($LastExitCode -ne 0) {
            Write-PipelineTelemetryError -Category "runtime" -Message "Error building unmanaged runtime"
            exit 1
        }
    }

    # build System.Private.CoreLib
    if (!$skipmscorlib) {
        MSBuild "$PSScriptRoot\build.targets" `
        "/t:bcl" `
        "/p:Configuration=$configuration"

        if ($LastExitCode -ne 0) {
            Write-PipelineTelemetryError -Category "bcl" -Message "Error building System.Private.CoreLib"
            exit 1
        }
    }

    if ($pack) {
        Write-PipelineTelemetryError -Category "nupkg" -Message "Error packing NuGet package (Not Implemented)"
        exit 1
    }

    # run all xunit tests
    if ($test) {
        MSBuild "$PSScriptRoot\build.targets" `
        "/t:update-tests-corefx" `
        "/p:Configuration=$configuration"

        if ($LastExitCode -ne 0) {
            Write-PipelineTelemetryError -Category "tests-download" -Message "Error downloading tests"
            exit 1
        }

        $timeout=-1
        if ($ci) {
            $timeout=600
        }

        MSBuild "$PSScriptRoot\build.targets" `
        "/t:run-tests-corefx" `
        "/p:Configuration=$configuration" `
        "/p:CoreFxTestTimeout=$timeout"

        if ($LastExitCode -ne 0) {
            Write-PipelineTelemetryError -Category "tests" -Message "Error running tests"
            exit 1
        }
    }
}
catch {
    Write-Host $_.ScriptStackTrace
    Write-PipelineTelemetryError -Category "build" -Message $_
    exit 1
}

exit 0
