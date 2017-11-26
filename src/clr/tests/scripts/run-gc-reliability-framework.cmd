@rem Licensed to the .NET Foundation under one or more agreements.
@rem The .NET Foundation licenses this file to you under the MIT license.
@rem See the LICENSE file in the project root for more information.

@echo off

set CORE_ROOT=%CD%\bin\tests\Windows_NT.%1.%2\Tests\Core_Root
set FRAMEWORK_DIR=%CD%\bin\tests\Windows_NT.%1.%2\GC\Stress\Framework\ReliabilityFramework
powershell -NoProfile "%CORE_ROOT%\CoreRun.exe %FRAMEWORK_DIR%\ReliabilityFramework.exe %FRAMEWORK_DIR%\testmix_gc.config"
if %ERRORLEVEL% == 100 (
    @rem The ReliabilityFramework returns 100 on success and 99 on failure
    echo ReliabilityFramework successful
    exit /b 0
) else if %ERRORLEVEL% == 99 (
    echo ReliabilityFramework test failed, some tests failed
    exit /b 1
) else (
    @rem The ReliabilityFramework returns -1 when something is wrong with the
    @rem run configuration. It should be obvious from standard out why this happened.
    echo ReliabilityFramework returned a strange exit code %ERRORLEVEL%, perhaps some config is wrong?
    exit /b 1
)

