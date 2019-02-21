# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

# Usage:
#
#   . ParallelTestExecution.ps1
#   cd <testFolder> (e.g. testFolder = src\System.Net.Security\tests\FunctionalTests)
#   RunMultiple <n> <delayVarianceMilliseconds> [-UntilFailed] [-Tile:$false]
#
#   The above sequence will open up <n> windows running the test project present in the current folder.
#   If -UntilFailed is used, the tests will continuously loop until a failure is detected. 
#   Between loops, the execution will pause for <delayVarianceMilliseconds>. 

function BuildAndTestBinary
{
    $output = (msbuild /t:rebuild,test /p:OuterLoop=true)
    if ($lastexitcode -ne 0)
    {
        throw "Build/test failed."
    }

    return $output
}

function TileWindows
{
    $shell = New-Object -ComObject Shell.Application
    $shell.TileHorizontally()
}

function ParseTestExecutionCommand($msBuildOutput)
{
    $pathPattern = "*Executing in*"
    $commandPattern = "*call *"
    $runtimeFolderPattern = "*Using * as the test runtime folder.*"
        
    $cmdLine = ""
    $pathLine = ""
    $runtimeLine = ""

    foreach ($line in $msBuildOutput)
    {
        if ($line -like $pathPattern)
        {
            $pathLine = $line.Split()[4]
            $foundPath = $true
        }
        elseif ($line -like $commandPattern)
        {
            $foundcommandPattern = $true
            $cmdLine = $line
        }
        elseif ($line -like $runtimeFolderPattern)
        {
            $runtimeLine = $line
        }
    }

    if (-not $foundcommandPattern)
    {
        throw "Cannot parse MSBuild output: please ensure that the current folder contains a test."
    }

    $Global:TestPath = $pathLine.Trim()
    $Global:TestCommand = $cmdLine.Trim() -replace ("call ", "")
    
    $runtimePath = $runtimeLine.Split()[3]

    $Global:TestCommand = $Global:TestCommand -replace ("%RUNTIME_PATH%", $runtimePath);
    $Global:TestCommand = [System.Environment]::ExpandEnvironmentVariables($Global:TestCommand)
}

function Initialize
{
    Write-Host -NoNewline "Initializing tests . . . "

    try
    {
        $output = BuildAndTestBinary
        ParseTestExecutionCommand($output)

        Write-Host -ForegroundColor Green "OK"
    }
    catch
    {
        Write-Host -ForegroundColor Red "Failed"
        throw
    }
}

function RunOne($testPath, $testCommand)
{
    if (-not [string]::IsNullOrWhiteSpace($testPath))
    {
        $Global:TestPath = $testPath
    }

    if (-not [string]::IsNullOrWhiteSpace($testCommand))
    {
        $Global:TestCommand = $testCommand
    }

    if ($Global:TestCommand -eq $null)
    {
        throw "Run Initialize first or pass the test command line as a parameter."
    }

    Write-Host "Path: $($Global:TestPath) Command: $($Global:TestCommand)"

    Push-Location
    cd $Global:TestPath
    Invoke-Expression $Global:TestCommand
    if ($lastexitcode -ne 0)
    {
        throw "Test execution failed."
    }

    Pop-Location
}

function RunUntilFailed($testPath, $testCommand, $delayVarianceMilliseconds = 0)
{
    try
    {
        while($true)
        {
            RunOne $testPath $testCommand

            if ($delayVarianceMilliseconds -ne 0)
            {
                $sleepMilliseconds = Get-Random -Minimum 0 -Maximum $delayVarianceMilliseconds
                Write-Host -ForegroundColor Cyan "Sleeping $sleepMilliseconds"
                Start-Sleep -Milliseconds $sleepMilliseconds
            }
        }
    }
    catch
    {
        Write-Host -ForegroundColor Red "Test execution failed!"
        Read-Host "Press ENTER to continue..."
    }
}

function RunMultiple(
    [int]$n = 2, 
    [int]$RandomDelayVarianceMilliseconds = 0,
    [switch]$UntilFailed = $false,
    [switch]$Tile = $true)
{  
    if ($Global:TestCommand -eq $null)
    {
        Initialize
    }

    $script = $PSCommandPath
        
    if ($UntilFailed)
    {
        $executionMethod = "RunUntilFailed"
    }
    else
    {
        $executionMethod = "RunOne"
    }

    $cmdArguments = "-Command `"&{. $script; $executionMethod '$Global:TestPath' '$Global:TestCommand' $RandomDelayVarianceMilliseconds}`""

    $processes = @()

    for ($i=0; $i -lt $n; $i++)
    {
        $thisCmdArguments = $cmdArguments -replace ("testResults.xml", "testResults$i.xml")
        $process = Start-Process -PassThru powershell -ArgumentList $thisCmdArguments
        $processes += $process
    }

    $processesExited = $false
    while (-not ([console]::KeyAvailable -or $processesExited))
    {
        Clear-Host

        Write-Host -ForegroundColor Cyan "Active test processes:"
        Write-Host
        Write-Host "[Press any key to close.]"
        Write-Host
        $processes | Format-Table -Property Id, CPU, Handles, WS, ExitCode

        $processesExited = $true
        foreach($p in $processes)
        {
            if (-not $p.HasExited)
            {
                $processesExited = $false
            }
        }

        Start-Sleep -Milliseconds 1000
        if ($Tile)
        {
            TileWindows
        }
    }

    if (-not $processesExited)
    {
        Write-Host -ForegroundColor Cyan "Terminating all processes."
        foreach ($p in $processes)
        {
            if (-not $p.HasExited)
            {
                $p.Kill()
            }
        }
    }

    Write-Host "Done."
}
