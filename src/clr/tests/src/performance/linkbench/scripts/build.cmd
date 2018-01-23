setlocal ENABLEDELAYEDEXPANSION
@echo on

REM Usage: Build.cmd <LinkBench assets directory>
setlocal

set AssetDir=%1
set ExitCode=0
pushd %LinkBenchRoot%

where.exe /Q CorFlags.exe || (echo [Error] CorFlags.exe is not on the environment & exit /b 1)

if defined __test_HelloWorld call :HelloWorld
if defined __test_WebAPI call :WebAPI
if defined __test_MusicStore call :MusicStore
if defined __test_MusicStore_R2R call :MusicStore_R2R 
if defined __test_CoreFx call :CoreFx
if defined __test_Roslyn call :Roslyn

popd
exit /b %ExitCode%

:HelloWorld
echo Build ** HelloWorld **
pushd %LinkBenchRoot%\HelloWorld
call %__dotnet% restore -r win10-x64
call %__dotnet% publish -c release -r win10-x64 /p:LinkDuringPublish=false --output bin\release\netcoreapp2.0\win10-x64\Unlinked
if errorlevel 1 set ExitCode=1&&echo HelloWorld: publish failed
call %__dotnet% publish -c release -r win10-x64 --output bin\release\netcoreapp2.0\win10-x64\Linked
if errorlevel 1 set ExitCode=1&&echo HelloWorld: publish-illink failed
popd
exit /b

:WebAPI
echo Build ** WebAPI **
pushd %LinkBenchRoot%\WebAPI
call %__dotnet% restore -r win10-x64
call %__dotnet% publish -c release -r win10-x64 /p:LinkDuringPublish=false --output bin\release\netcoreapp2.0\win10-x64\unlinked
if errorlevel 1 set ExitCode=1&&echo WebAPI: publish failed
call %__dotnet% publish -c release -r win10-x64 --output bin\release\netcoreapp2.0\win10-x64\linked
if errorlevel 1 set ExitCode=1&&echo WebAPI: publish failed
popd
exit /b

:MusicStore
echo Build ** MusicStore **
pushd %LinkBenchRoot%\JitBench\src\MusicStore
copy %AssetDir%\MusicStore\MusicStoreReflection.xml .
call %__dotnet% restore -r win10-x64 
call %__dotnet% publish -c release -r win10-x64 /p:LinkerRootDescriptors=MusicStoreReflection.xml /p:LinkDuringPublish=false --output bin\release\netcoreapp2.0\win10-x64\unlinked
if errorlevel 1 set ExitCode=1&&echo MusicStore: publish failed
call %__dotnet% publish -c release -r win10-x64 /p:LinkerRootDescriptors=MusicStoreReflection.xml --output bin\release\netcoreapp2.0\win10-x64\linked
if errorlevel 1 set ExitCode=1&&echo MusicStore: publish-illink failed 
popd
exit /b

:MusicStore_R2R
REM Since the musicstore benchmark has a workaround to use an old framework (to get non-crossgen'd packages), 
REM we need to run crossgen on these assemblies manually for now. 
REM Even once we have the linker running on R2R assemblies and remove this workaround, 
REM we'll need a way to get the pre-crossgen assemblies for the size comparison. 
REM We need to use it to crossgen the linked assemblies for the size comparison, 
REM since the linker targets don't yet include a way to crossgen the linked assemblies.
echo Build ** MusicStore Ready2Run **
pushd %LinkBenchRoot%\JitBench\src\MusicStore
copy %AssetDir%\MusicStore\Get-Crossgen.ps1
powershell -noprofile -executionPolicy RemoteSigned -file Get-Crossgen.ps1
pushd  bin\release\netcoreapp2.0\win10-x64\
mkdir R2R 
call :SetupR2R unlinked
if errorlevel 1 set ExitCode=1&&echo MusicStore R2R: setup-unlinked failed 
call :SetupR2R linked
if errorlevel 1 set ExitCode=1&&echo MusicStore R2R: setup-linked failed
popd
exit /b

:SetupR2R
REM Create R2R directory and copy all contents from MSIL to R2R directory
mkdir R2R\%1
xcopy /E /Y /Q %1 R2R\%1
REM Generate Ready2Run images for all MSIL files by running crossgen
pushd R2R\%1
copy ..\..\..\..\..\..\crossgen.exe
FOR /F %%I IN ('dir /b *.dll ^| find /V /I ".ni.dll"  ^| find /V /I "System.Private.CoreLib" ^| find /V /I "mscorlib.dll"') DO (
    REM Don't crossgen Corlib, since the native image already exists.
    REM For all other MSIL files (corflags returns 0), run crossgen
    CorFlags.exe %%I 
    if not errorlevel 1 (
        crossgen.exe /Platform_Assemblies_Paths . %%I 
        if errorlevel 1 (
            exit /b 1
        )
    )
)
del crossgen.exe

REM Remove the original MSIL files, rename the Ready2Run files .ni.dll --> .dll
FOR /F "delims=" %%I IN ('dir /b *.dll') DO (
    if exist %%~nI.ni.dll (
        del %%I 
        ren %%~nI.ni.dll %%I
    )
)
popd
exit /b 0

:CoreFx
echo Build ** CoreFX **
pushd %LinkBenchRoot%\corefx
set BinPlaceILLinkTrimAssembly=true
call build.cmd -release
if errorlevel 1 set ExitCode=1&&echo CoreFx: build failed 
popd
exit /b

:Roslyn
echo Build ** Roslyn **
pushd %LinkBenchRoot%\roslyn\
call restore.cmd
cd src\Compilers\CSharp\csc
call %__dotnet2% restore -r win10-x64
call %__dotnet2% publish -c release -r win10-x64 -f netcoreapp2.0 /p:LinkDuringPublish=false --output ..\..\..\..\Binaries\release\Exes\csc\netcoreapp2.0\win10-x64\Unlinked
if errorlevel 1 set ExitCode=1&&echo Roslyn: publish failed
call %__dotnet2% publish -c release -r win10-x64 -f netcoreapp2.0 --output ..\..\..\..\Binaries\release\Exes\csc\netcoreapp2.0\win10-x64\Linked
if errorlevel 1 set ExitCode=1&&echo Roslyn: publish-iLLink failed
popd
exit /b
