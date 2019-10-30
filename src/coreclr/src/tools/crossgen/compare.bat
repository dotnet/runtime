::mscorlib
::System
::System.Core
::System.Xml
::System.Configuration
::System.Drawing
::System.Data
::System.Windows.Forms
::System.Runtime.Remoting
::System.Serviceprocess
::System.Management
::Accessibility
::Microsoft.VisualBasic
::System.DirectoryServices
::System.Transactions
::System.Web.Services
::CustomMarshalers
::System.Configuration.Install
::System.Xaml
::WindowsBase
::System.Net.Http
::System.Xml.Linq
::System.Runtime.WindowsRuntime
::System.Runtime.WindowsRuntime.UI.Xaml
::System.Runtime.Serialization
::System.ServiceModel
::PresentationCore
::PresentationFramework
::System.EnterpriseServices
::System.Collections.Concurrent
::System.Collections
::System.ComponentModel.Annotations
::System.ComponentModel
::System.ComponentModel.EventBasedAsync
::System.Diagnostics.Contracts
::System.Diagnostics.Debug
::System.Diagnostics.Tools
::System.Diagnostics.Tracing
::System.Dynamic.Runtime
::System.Globalization
::System.IO
::System.Linq
::System.Linq.Expressions
::System.Linq.Parallel
::System.Linq.Queryable
::System.Net.Http.Rtc
::System.Net.NetworkInformation
::System.Net.Primitives
::System.Net.Requests
::System.ObjectModel
::System.Reflection
::System.Reflection.Emit
::System.Reflection.Emit.ILGeneration
::System.Reflection.Emit.Lightweight
::System.Reflection.Extensions
::System.Reflection.Primitives
::System.Resources.ResourceManager
::System.Runtime
::System.Runtime.Extensions
::System.Runtime.InteropServices
::System.Runtime.InteropServices.WindowsRuntime
::System.Runtime.Numerics
::System.Runtime.Serialization.Json
::System.Runtime.Serialization.Primitives
::System.Runtime.Serialization.Xml
::System.Security.Principal
::System.ServiceModel.Duplex
::System.ServiceModel.Http
::System.ServiceModel.NetTcp
::System.ServiceModel.Primitives
::System.ServiceModel.Security
::System.Text.Encoding
::System.Text.Encoding.Extensions
::System.Text.RegularExpressions
::System.Threading
::System.Threading.Tasks
::System.Threading.Tasks.Parallel
::System.Windows
::System.Xml.ReaderWriter
::System.Xml.XDocument
::System.Xml.XmlSerializer
::Windows.ApplicationModel
::Windows.Data
::Windows.Devices
::Windows.Foundation
::Windows.Globalization
::Windows.Graphics
::Windows.Management
::Windows.Media
::Windows.Networking
::Windows.Security
::Windows.Storage
::Windows.System
::Windows.UI
::Windows.UI.Xaml
::Windows.Web
::END_OF_LIST

@echo off

rem
rem This script compares ngen and crossgen output for framework assemblies
rem

SETLOCAL ENABLEDELAYEDEXPANSION

set BITNESS=
IF /I "%_BuildArch%" == "amd64" set BITNESS=64
set FRAMEWORKDIR=%SYSTEMROOT%\Microsoft.NET\Framework%BITNESS%\%COMPlus_Version%
IF "%BITNESS%" == "" set BITNESS=32

set NATIVEIMAGEPATH=%FRAMEWORKDIR%\assembly\NativeImages_%COMPlus_Version%_%BITNESS%

rem rmdir /S /Q %NATIVEIMAGEPATH%
rem %FRAMEWORKDIR%\ngen install mscorlib
rem %FRAMEWORKDIR%\ngen update

%FRAMEWORKDIR%\gacutil /if %_NTTREE%\System.Runtime.WindowsRuntime.dll
%FRAMEWORKDIR%\gacutil /if %_NTTREE%\System.Runtime.WindowsRuntime.UI.Xaml.dll

set ILIMAGEPATH=%_NTTREE%\il
rmdir /S /Q %ILIMAGEPATH%
if not exist %ILIMAGEPATH% mkdir %ILIMAGEPATH%

rem Collect all files from the GAC into ILIMAGEPATH directory to guaranteed that we get the exact same IL images
rem between ngen and crossgen. It is important on non-x86 builds because of non-x86 layouts pull files from x86 build.
forfiles /P %FRAMEWORKDIR%\assembly\GAC_%BITNESS% /M *.dll /S /C "cmd /c copy @path %ILIMAGEPATH%\@file > nul"
forfiles /P %FRAMEWORKDIR%\assembly\GAC_MSIL /M *.dll /S /C "cmd /c copy @path %ILIMAGEPATH%\@file > nul"
rem clr.dll and clrjit.dll are required for timestamps
copy %FRAMEWORKDIR%\clr.dll %ILIMAGEPATH%\clr.dll >nul
copy %FRAMEWORKDIR%\clrjit.dll %ILIMAGEPATH%\clrjit.dll >nul

set CROSSGENIMAGEPATH=%_NTTREE%\ni
rmdir /S /Q %CROSSGENIMAGEPATH%
if not exist %CROSSGENIMAGEPATH% mkdir %CROSSGENIMAGEPATH%

set WINMDPATH=%WINDIR%\System32\WinMetadata

set SELF=%~fd0
set FAILED=

for /f "eol=; usebackq tokens=1,2,3* delims=,:" %%I in ("%SELF%") DO (
  if "%%I"=="END_OF_LIST" goto LDone
  call :ProcessFile %%I
  if "!FAILED!"=="1" goto LFailed
)

:LDone
echo DONE
exit /B 0

:LFailed
echo FAILED
exit /B 1

:ProcessFile

set FILEPATH=
call :ProbeFile %ILIMAGEPATH%\%1.dll
call :ProbeFile %WINMDPATH%\%1.winmd

if "%FILEPATH%" == "" ( echo File not found: %1 & goto LError )

echo.
echo ========= COMPILE and COMPARE %1 ==========
echo ngen install /nodependencies %FILEPATH%
ngen install /nodependencies %FILEPATH%
echo.
echo %_NTTREE%\crossgen /platform_assemblies_paths %ILIMAGEPATH%;%CROSSGENIMAGEPATH% /Platform_Winmd_Paths %WINMDPATH% /in %FILEPATH% /out %CROSSGENIMAGEPATH%\%1.ni.dll
%_NTTREE%\crossgen /platform_assemblies_paths %ILIMAGEPATH%;%CROSSGENIMAGEPATH% /Platform_Winmd_Paths %WINMDPATH% /in %FILEPATH% /out %CROSSGENIMAGEPATH%\%1.ni.dll
IF NOT "%ERRORLEVEL%"=="0" set FAILED=1
echo.
forfiles /P %NATIVEIMAGEPATH% /M %1.ni.dll /S /C "cmd /c echo Compare: @path & fc /B @path %CROSSGENIMAGEPATH%\%1.ni.dll > %CROSSGENIMAGEPATH%\diff.txt & IF NOT ERRORLEVEL 1 del %CROSSGENIMAGEPATH%\diff.txt"
IF not exist %CROSSGENIMAGEPATH%\diff.txt goto LExit
echo ----- DIFFERENT -----
:LError
set FAILED=1
goto LExit

:ProbeFile
if NOT "%FILEPATH%" == "" goto LExit
if NOT exist "%1" goto LExit
set FILEPATH=%1
goto LExit

:LExit
