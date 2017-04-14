@echo off

rmdir /s /q LinkBench

set ROOT=%cd%\LinkBench
mkdir LinkBench 2> nul
pushd %ROOT%

mkdir HelloWorld
cd HelloWorld
dotnet new console
if errorlevel 1 exit /b 1
cd ..

mkdir WebAPI
cd WebAPI
dotnet new webapi
if errorlevel 1 exit /b 1
cd ..

git clone https://github.com/aspnet/JitBench -b dev
if errorlevel 1 exit /b 1

git clone http://github.com/dotnet/corefx
if errorlevel 1 exit /b 1

git clone https://github.com/dotnet/roslyn.git
if errorlevel 1 exit /b 1

popd