REM execute build.cmd
call build.cmd
if errorlevel 1 exit /b 1
REM the coreclr package should have been installed.  This is temporarily set to this location
set CORE_ROOT=%CD%\tests\src\packages\Microsoft.DotNet.TestHost.1.0.1-prerelease\lib\testhost
cd tests
call runtest.cmd
if errorlevel 1 exit /b 1