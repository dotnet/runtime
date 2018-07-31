set CurrentDir=%~dp0
set ProjectDir=%CurrentDir%..\..\..\..\

"%ProjectDir%Tools\dotnetcli\dotnet.exe" build /p:__BuildArch=x64 /p:__BuildOS=Windows_NT /p:__BuildType=Checked %ProjectDir%src\tools\r2rdump\R2RDump.csproj
"%ProjectDir%Tools\dotnetcli\dotnet.exe" build /p:__BuildArch=x86 /p:__BuildOS=Windows_NT /p:__BuildType=Release %ProjectDir%src\tools\r2rdump\R2RDump.csproj

set tests=HelloWorld GcInfoTransitions GenericFunctions MultipleRuntimeFunctions

(for %%a in (%tests%) do (
    "%ProjectDir%Tools\dotnetcli\dotnet.exe" build /p:__BuildArch=x64 /p:__BuildOS=Windows_NT /p:__BuildType=Checked "%ProjectDir%tests\src\readytorun\r2rdump\files\%%a.csproj"
    %ProjectDir%bin\tests\Windows_NT.x64.Checked\Tests\Core_Root\crossgen /readytorun /platform_assemblies_paths %ProjectDir%bin\tests\Windows_NT.x64.Checked\Tests\Core_Root /out %%a.ni.dll %ProjectDir%bin\tests\Windows_NT.x64.Checked\readytorun\r2rdump\files\%%a\%%a.dll
    "%ProjectDir%Tools\dotnetcli\dotnet.exe" %ProjectDir%bin\Product\Windows_NT.x64.Checked\netcoreapp2.0\R2RDump.dll --in %%a.ni.dll --out %ProjectDir%tests\src\readytorun\r2rdump\files\Windows_NT.x64.Checked\%%a.xml -x -v
))

(for %%a in (%tests%) do (
    "%ProjectDir%Tools\dotnetcli\dotnet.exe" build /p:__BuildArch=x86 /p:__BuildOS=Windows_NT /p:__BuildType=Release "%ProjectDir%tests\src\readytorun\r2rdump\files\%%a.csproj"
    %ProjectDir%bin\tests\Windows_NT.x86.Release\Tests\Core_Root\crossgen /readytorun /platform_assemblies_paths %ProjectDir%bin\tests\Windows_NT.x86.Release\Tests\Core_Root /out %%a.ni.dll %ProjectDir%bin\tests\Windows_NT.x86.Release\readytorun\r2rdump\files\%%a\%%a.dll
    "%ProjectDir%Tools\dotnetcli\dotnet.exe" %ProjectDir%bin\Product\Windows_NT.x86.Release\netcoreapp2.0\R2RDump.dll --in %%a.ni.dll --out %ProjectDir%tests\src\readytorun\r2rdump\files\Windows_NT.x86.Release\%%a.xml -x -v
))

COPY /Y %ProjectDir%tests\src\readytorun\r2rdump\files\Windows_NT.x86.Release\*.xml %ProjectDir%tests\src\readytorun\r2rdump\files\Windows_NT.x86.Checked
