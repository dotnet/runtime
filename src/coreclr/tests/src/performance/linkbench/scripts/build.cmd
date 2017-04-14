@echo off

REM Usage: Build.cmd <LinkBench assets directory>
setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"

set ROOT=%cd%\LinkBench
set AssetDir=%1
set ExitCode=0
mkdir LinkBench 2> nul
pushd %ROOT%

echo Build ** HelloWorld **
cd %ROOT%\HelloWorld
dotnet restore -r win10-x64
dotnet publish -c release -r win10-x64
dotnet msbuild /t:Link /p:LinkerMode=sdk /p:RuntimeIdentifier=win10-x64 /v:n /p:Configuration=release
if errorlevel 1 set ExitCode=1 
echo -- Done -- 

echo Build ** WebAPI **
cd %ROOT%\WebAPI
dotnet restore -r win10-x64
dotnet publish -c release -r win10-x64
dotnet msbuild /t:Link /p:LinkerMode=sdk /p:RuntimeIdentifier=win10-x64 /v:n /p:Configuration=release
if errorlevel 1 set ExitCode=1 
echo -- Done -- 

echo Build ** MusicStore **
cd %ROOT%\JitBench\src\MusicStore
copy %AssetDir%\MusicStore\MusicStoreReflection.xml .
dotnet restore -r win10-x64 
dotnet publish -c release -r win10-x64
dotnet msbuild /t:Link /p:LinkerMode=sdk /p:RuntimeIdentifier=win10-x64 /v:n /p:LinkerRootFiles=MusicStoreReflection.xml /p:Configuration=release
if errorlevel 1 set ExitCode=1 
echo -- Done -- 

echo Build ** MusicStore Ready2Run **
cd %ROOT%\JitBench\src\MusicStore
powershell -noprofile -executionPolicy RemoteSigned -file Get-Crossgen.ps1
pushd  bin\release\netcoreapp2.0\win10-x64\
call :SetupR2R publish
if errorlevel 1 set ExitCode=1 
call :SetupR2R linked
if errorlevel 1 set ExitCode=1 
echo -- Done -- 

echo Build ** CoreFX **
cd %ROOT%\corefx
set BinPlaceILLinkTrimAssembly=true
call build.cmd -release
if errorlevel 1 set ExitCode=1 
echo -- Done -- 

echo Build ** Roslyn **
cd %ROOT%\roslyn
copy %AssetDir%\Roslyn\RoslynRoots.txt .
copy %AssetDir%\Roslyn\RoslynRoots.xml .
set RoslynRoot=%cd%
REM Build Roslyn
call restore.cmd
msbuild /m Roslyn.sln  /p:Configuration=Release
REM Fetch ILLink
mkdir illink
cd illink
copy %AssetDir%\Roslyn\illinkcsproj illink.csproj
dotnet restore illink.csproj -r win10-x64 --packages bin
cd ..
REM Create Linker Directory
cd Binaries\Release\Exes
mkdir Linked
cd CscCore
REM Copy Unmanaged Assets
FOR /F "delims=" %%I IN ('DIR /b *') DO (
    corflags %%I >nul 2> nul
    if errorlevel 1 copy %%I ..\Linked >nul
)
copy *.ni.dll ..\Linked
REM Run Linker
dotnet %RoslynRoot%\illink\bin\illink.tasks\0.1.0-preview\tools\illink.dll -t -c link -a @%RoslynRoot%\RoslynRoots.txt -x %RoslynRoot%\RoslynRoots.xml -l none -out ..\Linked
if errorlevel 1 set ExitCode=1 
echo -- Done -- 
popd

:Done
exit /b %ExitCode%

:SetupR2R
REM Create R2R directory and copy all contents from MSIL to R2R directory
mkdir %1_r2r
xcopy /E /Y /Q %1 %1_r2r
REM Generate Ready2Run images for all MSIL files by running crossgen
cd %1_r2r
copy ..\..\..\..\..\crossgen.exe 
FOR /F %%I IN ('dir /b *.dll ^| find /V /I ".ni.dll"  ^| find /V /I "System.Private.CoreLib" ^| find /V /I "mscorlib.dll"') DO (
    REM Don't crossgen Corlib, since the native image already exists.
    REM For all other MSIL files (corflags returns 0), run crossgen
    corflags %%I >nul 2>nul
    if not errorlevel 1 (
        crossgen /Platform_Assemblies_Paths . %%I >nul 2>nul
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
cd ..
exit /b 0
