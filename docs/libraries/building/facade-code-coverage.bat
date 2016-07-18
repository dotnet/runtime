@echo off
:: Example settings for System.Runtime
SET project=System.Runtime
SET msbuildargs=/T:Build
SET testsubdir=AnyOS.AnyCPU.Debug
SET filter="+[*]* -[*.Tests]* -[*]System.Collections.* -[*]System.Diagnostics.* -[*]System.Globalization.* -[*]System.IO.* -[*]System.Reflection.* -[*]System.Resources.* -[*]System.Runtime.* -[*]System.Security.* -[*]System.StubHelpers.* -[*]System.Threading.* -[*]Microsoft.* -[*]Windows.* -[*]System.App* -[*]System.Text.Decoder* -[*]System.Text.Encoder* -[*]System.Text.*Encoding -[*]System.Text.Internal* -[xunit.*]*"

:: Update this when OpenCover or ReportGenerator are updated
SET opencoverversion=4.6.519
SET reportgeneratorversion=2.4.3

:: Assumes that the corefx and coreclr repo folders are in the same parent folder
SET root=C:\Users\Hugh\Documents\Github

SET corefx=%root%\corefx
SET coreclr=%root%\coreclr

SET packages=%corefx%\packages
SET opencover=%packages%\OpenCover\%opencoverversion%\tools\OpenCover.Console.exe
SET reportgenerator=%packages%\ReportGenerator\%reportgeneratorversion%\tools\ReportGenerator.exe

SET targetdir=%corefx%\bin\tests\%testsubdir%\%project%.Tests\netcoreapp1.0

SET resultsfile=testresults.xml
SET coveragefile=coverage.xml

SET coveragedir=coverage

SET originalfolder=%cd%
SET sourcefolder=%corefx%\src\%project%\tests

SET coreclrbuild=%coreclr%\bin\Product\Windows_NT.x64.Debug
SET coreclrbuild=%coreclr%\bin\Product\Windows_NT.x64.Release

:: Build the library
cd %sourcefolder%
msbuild %msbuildargs%
cd %originalfolder%

:: Delete old files (see #8381 for why)
del %targetdir%\mscorlib.dll
del %targetdir%\mscorlib.ni.dll
del %targetdir%\System.Private.CoreLib.dll
del %targetdir%\System.Private.CoreLib.ni.dll
del %targetdir%\coreclr.dll
del %targetdir%\CoreRun.exe
del %targetdir%\CoreConsole.exe
del %targetdir%\clretwrc.dll
del %targetdir%\clrjit.dll
del %targetdir%\dbgshim.dll
del %targetdir%\mscordaccore.dll
del %targetdir%\mscordbi.dll
del %targetdir%\mscorrc.debug.dll
del %targetdir%\mscorrc.dll
del %targetdir%\sos.dll

:: Copy over our local build files
For %%a in (
%coreclrbuild%\mscorlib.dll
%coreclrbuild%\PDB\mscorlib.pdb
%coreclrbuild%\System.Private.CoreLib.dll
%coreclrbuild%\PDB\System.Private.CoreLib.pdb
%coreclrbuild%\coreclr.dll
%coreclrbuild%\PDB\coreclr.pdb
%coreclrbuild%\CoreRun.exe
%coreclrbuild%\CoreConsole.exe
%coreclrbuild%\clretwrc.dll
%coreclrbuild%\clrjit.dll
%coreclrbuild%\dbgshim.dll
%coreclrbuild%\mscordaccore.dll
%coreclrbuild%\mscordbi.dll
%coreclrbuild%\mscorrc.debug.dll
%coreclrbuild%\mscorrc.dll
%coreclrbuild%\sos.dll
) do copy /b/v/y "%%~a" "%targetdir%\"

:: Now, run the actual tests and generate a coverage report
SET corerunargs=%targetdir%\xunit.console.netcore.exe %project%.Tests.dll -xml %resultsfile% -notrait category=OuterLoop -notrait category=failing -notrait category=nonwindowstests

%opencover% -oldStyle -filter:%filter% -excludebyfile:"*\Common\src\System\SR.*" -nodefaultfilters -excludebyattribute:*.ExcludeFromCodeCoverage* -skipautoprops -hideskipped:All -threshold:1 -returntargetcode -register:user -targetdir:%targetdir% -target:CoreRun.exe -output:%coveragefile% -targetargs:"%corerunargs%"

%reportgenerator% -targetdir:%coveragedir% -reporttypes:Html;Badges -reports:%coveragefile% -verbosity:Error