# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

# Usage:
#
#   . ParallelTestExecution.ps1
#   cd <testFolder> (e.g. testFolder = src\System.Net.Security\tests\FunctionalTests)
#   RunMultiple <n> <delayVarianceMilliseconds> [-UntilFailed]
#
#   The above sequence will open up <n> windows running the test project present in the current folder.
#   If -UntilFailed is used, the tests will continuously loop until a failure is detected. 
#   Between loops, the execution will pause for <delayVarianceMilliseconds>. 

function BuildAndTestBinary
{
    $output = (msbuild /t:rebuild,test)
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

function CurrentPath
{
    return (Get-Item -Path ".\" -Verbose).FullName
}

function ParseCurrentPath
{
    $p = CurrentPath
    
    $test_found = $false
    $contract_found = $false
    $root_found = $false
    
    while ((-not $root_found) -and ($p -ne ""))
    {
      $leaf = Split-Path $p -Leaf
      
      if (Test-Path (Join-Path $p 'build.cmd'))
      {
          $Global:RootPath = $p
          $root_found = $true
      }
      
      if ($test_found -and (-not $contract_found))
      {
          $Global:ContractName = $leaf
          $contract_found = $true
      }
      
      if ($leaf -eq "tests")
      {
          $test_found = $true
      }
      
      $p = Split-Path $p
    }
    
    if (-not $test_found)
    {
       throw "This folder doesn't appear to be part of a test (looking for ...\contract\tests\...)." 
    }
}

function ParseTestExecutionCommand($msBuildOutput)
{
    $foundTestExecution = $false
    $cmdLine = ""

    foreach ($line in $msBuildOutput)
    {
        if ($foundTestExecution -eq $true)
        {
            $cmdLine = $line
            break
        }
        
        if ($line.Contains("RunTestsForProject:"))
        {
            $foundTestExecution = $true
        }
    }

    if (-not $foundTestExecution)
    {
        throw "Cannot parse MSBuild output: please ensure that the current folder contains a test."
    }

    $Global:TestCommand = $cmdLine.Trim()
}

function ParseTestFolder($testExecutionCmdLine)
{
    $coreRunPath = $testExecutionCmdLine.Split()[0]
    return Split-Path $coreRunPath
}

function Initialize
{
    ParseCurrentPath

    Write-Host -NoNewline "Initializing tests for $($Global:ContractName) . . . "

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

function RunOne($testCommand)
{
    if ($testCommand -ne "")
    {
        $Global:TestCommand = $testCommand
    }

    if ($Global:TestCommand -eq $null)
    {
        throw "Run Initialize first or pass the test command line as a parameter."
    }

    Write-Host $Global:TestCommand
    $path = ParseTestFolder($Global:TestCommand)
    Write-Host "$path"

    Push-Location
    cd $path
    Invoke-Expression $Global:TestCommand
    if ($lastexitcode -ne 0)
    {
        throw "Test execution failed."
    }

    Pop-Location
}

function RunUntilFailed($testCommand, $delayVarianceMilliseconds = 0)
{

    try
    {
        while($true)
        {
            RunOne $testCommand

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
    [switch]$UntilFailed = $false)
{  
    if ($Global:TestCommand -eq $null)
    {
        Initialize
    }

    $script = $PSCommandPath
    $testCommand = $Global:TestCommand
    
    if ($untilFailed)
    {
        $executionMethod = "RunUntilFailed"
    }
    else
    {
        $executionMethod = "RunOne"
    }

    $cmdArguments = "-Command `"&{. $script; $executionMethod '$testCommand' $RandomDelayVarianceMilliseconds}`""

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
        TileWindows
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
