@rem Licensed to the .NET Foundation under one or more agreements.
@rem The .NET Foundation licenses this file to you under the MIT license.
@rem See the LICENSE file in the project root for more information.

@echo off

set CORE_ROOT=%CD%\bin\tests\Windows_NT.%1.%2\Tests\Core_Root
set FRAMEWORK_DIR=%CD%\bin\tests\Windows_NT.%1.%2\GC\Stress\Framework\ReliabilityFramework
powershell "%CORE_ROOT%\CoreRun.exe %FRAMEWORK_DIR%\ReliabilityFramework.exe %FRAMEWORK_DIR%\testmix_gc.config | tee stdout.txt"

