[CmdletBinding(PositionalBinding=$false)]
Param(
    [string]$fxversion,
    [int]$timeout = -1,
    [string]$corefxtests = "*",
    [switch][Alias('h')]$help
)

function Print-Usage() {
    Write-Host "Common settings:"
    Write-Host "  -fxversion              Version passed to dotnet.exe"
    Write-Host "  -timeout                Individual test timeout (default -1)"
    Write-Host "  -corefxtests            CoreFx test to run, all or individual (defaul *)"
    Write-Host "  -help                   Print help and exit (short: -h)"
    Write-Host ""

    Write-Host "The above arguments can be shortened as much as to be unambiguous."
}

function RunCoreFxTest($_netcore_version, $_timeout, $_testname, $_counter, $_tests_count) {

    Write-Host ""
    Write-Host "***************** $($_testname) $($_counter) / $($_tests_count) *********************"
    Copy-Item -Path "$PSScriptRoot\corefx\restore\corefx-restore.deps.json" -Destination "$PSScriptRoot\corefx\tests\extracted\$_testname\$_testname.deps.json" -Force
    Copy-Item -Path "$PSScriptRoot\corefx\restore\corefx-restore.runtimeconfig.dev.json" -Destination "$PSScriptRoot\corefx\tests\extracted\$_testname\$_testname.runtimeconfig.dev.json" -Force
    Copy-Item -Path "$PSScriptRoot\corefx\restore\corefx-restore.dll" -Destination "$PSScriptRoot\corefx\tests\extracted\$_testname\corefx-restore.dll" -Force
    (Get-Content "$PSScriptRoot\corefx\tests\extracted\$_testname\$_testname.runtimeconfig.json").replace("""5.0.0""", """$_netcore_version""") | Set-Content "$PSScriptRoot\corefx\tests\extracted\$_testname\$_testname.runtimeconfig.json"

    if ((Test-Path "$PSScriptRoot\corefx\tests\TestResult-$_testname.html" -PathType Leaf)) {
        Remove-Item -Path "$PSScriptRoot\corefx\tests\TestResult-$_testname.html" -Force
    }

    if ((Test-Path "$PSScriptRoot\corefx\tests\TestResult-$_testname-netcore-xunit.xml" -PathType Leaf)) {
        Remove-Item -Path "$PSScriptRoot\corefx\tests\TestResult-$_testname-netcore-xunit.xml" -Force
    }

    $_process_path = "$PSScriptRoot\dotnet.exe"
    $_argument_list = "exec"
    $_argument_list = $_argument_list + " --runtimeconfig $_testname.runtimeconfig.json"
    $_argument_list = $_argument_list + " --additional-deps $_testname.deps.json"
    $_argument_list = $_argument_list + " --fx-version ""$_netcore_version"""
    $_argument_list = $_argument_list + " xunit.console.dll $_testname.dll"
    $_argument_list = $_argument_list + " -html ""$PSScriptRoot\corefx\tests\TestResult-$_testname.html"""
    $_argument_list = $_argument_list + " -xml ""$PSScriptRoot\corefx\tests\TestResult-$_testname-netcore-xunit.xml"""
    $_argument_list = $_argument_list + " -notrait category=nonwindowstests ""@$PSScriptRoot\CoreFX.issues_windows.rsp"" ""@$PSScriptRoot\CoreFX.issues.rsp"""

    $_process = Start-Process -filePath $_process_path -ArgumentList $_argument_list -workingdirectory "$PSScriptRoot\corefx\tests\extracted\$_testname" -PassThru -NoNewWindow
    if ($_timeout -eq -1) {
        Wait-Process -InputObject $_process
    } else {
        Wait-Process -InputObject $_process -Timeout $_timeout -ErrorAction SilentlyContinue -ErrorVariable _process_error

        if ($_process_error) {
            $_process | kill
            Write-Host "Error running $_testname, timeout"
            return 1
        }
    }

    $_process_exit = $_process.ExitCode
    if ($_process_exit -ne 0) {
        Write-Host "Error running $_testname, ExitCode=$($_process_exit)"
        return $_process_exit
    }

    # Check for xunit XML file, if missing, treat as failure.
    if (-Not (Test-Path "$PSScriptRoot\corefx\tests\TestResult-$_testname-netcore-xunit.xml" -PathType Leaf)) {
        Write-Host "Error running $_testname, missing xunit XML file"
        return 1
    }

    return 0
}

$exit_code = 0
if ($help) {
    Print-Usage
    exit $exit_code
}

if ($corefxtests -eq "*") {
    if ((Test-Path "$PSScriptRoot\.failures" -PathType Leaf)) {
        Remove-Item -Path "$PSScriptRoot\.failures" -Force
    }

    $tests = Get-ChildItem "$PSScriptRoot\corefx\tests\extracted\"
    $tests_count = $tests.Count
    $counter = 0
    for ($i=0; $i -lt $tests_count; $i++) {
        $counter=$counter + 1
        $testname=$tests[$i].Name

        $result = RunCoreFxTest $fxversion $timeout $testname $counter $tests_count
        if ($result -ne 0) {
            Add-Content -Path "$PSScriptRoot\.failures" -Value "$testname"
            $exit_code = 1
        }
    }
    if (Get-Command "python.exe" -ErrorAction SilentlyContinue) {
        python.exe "$PSScriptRoot\xunit-summary.py" "$PSScriptRoot\corefx\tests"
    } else {
        Write-Host "Couldn't locate python.exe, skiping xunit-summary.py"
    }
    if ((Test-Path "$PSScriptRoot\.failures" -PathType Leaf)) {
        Write-Host ""
        Write-Host "Failures in test suites:"
        type "$PSScriptRoot\.failures"
        Write-Host ""
        $exit_code = 1
    }
} else {
    if (-Not (Test-Path "$PSScriptRoot\corefx\tests\extracted\$corefxtests" -PathType Container)) {
        Write-Host "Uknown test: $corefxtests"
        $exit_code = 1
    } else {
      $exit_code = RunCoreFxTest $fxversion $timeout $corefxtests 1 1
    }
}

exit $exit_code