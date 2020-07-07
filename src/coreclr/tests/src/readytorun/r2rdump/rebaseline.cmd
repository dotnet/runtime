set CurrentDir=%~dp0
set ProjectDir=%CurrentDir%..\..\..\..\
set RepoRoot=%ProjectDir%..\..\

"%RepoRoot%dotnet.cmd" build /p:TargetArchitecture=x64 /p:TargetOS=Windows_NT /p:Configuration=Checked %ProjectDir%src\tools\r2rdump\R2RDump.csproj
"%RepoRoot%dotnet.cmd" build /p:TargetArchitecture=x64 /p:TargetOS=Windows_NT /p:Configuration=Debug %ProjectDir%src\tools\r2rdump\R2RDump.csproj
"%RepoRoot%dotnet.cmd" build /p:TargetArchitecture=x86 /p:TargetOS=Windows_NT /p:Configuration=Release %ProjectDir%src\tools\r2rdump\R2RDump.csproj
"%RepoRoot%dotnet.cmd" build /p:TargetArchitecture=x86 /p:TargetOS=Windows_NT /p:Configuration=Debug %ProjectDir%src\tools\r2rdump\R2RDump.csproj

set tests=HelloWorld GcInfoTransitions GenericFunctions MultipleRuntimeFunctions

(for %%a in (%tests%) do (
    "%RepoRoot%dotnet.cmd" build /p:TargetArchitecture=x64 /p:TargetOS=Windows_NT /p:Configuration=Checked "%ProjectDir%tests\src\readytorun\r2rdump\files\%%a.csproj"
    %ProjectDir%artifacts\tests\Windows_NT.x64.Checked\Tests\Core_Root\crossgen /readytorun /platform_assemblies_paths %ProjectDir%artifacts\tests\Windows_NT.x64.Checked\Tests\Core_Root /out %%a.ni.dll %ProjectDir%artifacts\tests\Windows_NT.x64.Checked\readytorun\r2rdump\files\%%a\%%a.dll
    "%RepoRoot%dotnet.cmd" %ProjectDir%artifacts\bin\coreclr\Windows_NT.x64.Checked\netcoreapp2.0\R2RDump.dll --in %%a.ni.dll --out %ProjectDir%tests\src\readytorun\r2rdump\files\Windows_NT.x64.Checked\%%a.xml -x -v --ignoreSensitive
))

(for %%a in (%tests%) do (
    "%RepoRoot%dotnet.cmd" build /p:TargetArchitecture=x64 /p:TargetOS=Windows_NT /p:Configuration=Debug "%ProjectDir%tests\src\readytorun\r2rdump\files\%%a.csproj"
    %ProjectDir%artifacts\tests\Windows_NT.x64.Debug\Tests\Core_Root\crossgen /readytorun /platform_assemblies_paths %ProjectDir%artifacts\tests\Windows_NT.x64.Debug\Tests\Core_Root /out %%a.ni.dll %ProjectDir%artifacts\tests\Windows_NT.x64.Debug\readytorun\r2rdump\files\%%a\%%a.dll
    "%RepoRoot%dotnet.cmd" %ProjectDir%artifacts\bin\coreclr\Windows_NT.x64.Debug\netcoreapp2.0\R2RDump.dll --in %%a.ni.dll --out %ProjectDir%tests\src\readytorun\r2rdump\files\Windows_NT.x64.Debug\%%a.xml -x -v --ignoreSensitive
))

(for %%a in (%tests%) do (
    "%RepoRoot%dotnet.cmd" build /p:TargetArchitecture=x86 /p:TargetOS=Windows_NT /p:Configuration=Release "%ProjectDir%tests\src\readytorun\r2rdump\files\%%a.csproj"
    %ProjectDir%artifacts\tests\Windows_NT.x86.Release\Tests\Core_Root\crossgen /readytorun /platform_assemblies_paths %ProjectDir%artifacts\tests\Windows_NT.x86.Release\Tests\Core_Root /out %%a.ni.dll %ProjectDir%artifacts\tests\Windows_NT.x86.Release\readytorun\r2rdump\files\%%a\%%a.dll
    "%RepoRoot%dotnet.cmd" %ProjectDir%artifacts\bin\coreclr\Windows_NT.x86.Release\netcoreapp2.0\R2RDump.dll --in %%a.ni.dll --out %ProjectDir%tests\src\readytorun\r2rdump\files\Windows_NT.x86.Release\%%a.xml -x -v --ignoreSensitive
))

(for %%a in (%tests%) do (
    "%RepoRoot%dotnet.cmd" build /p:TargetArchitecture=x86 /p:TargetOS=Windows_NT /p:Configuration=Debug "%ProjectDir%tests\src\readytorun\r2rdump\files\%%a.csproj"
    %ProjectDir%artifacts\tests\Windows_NT.x86.Debug\Tests\Core_Root\crossgen /readytorun /platform_assemblies_paths %ProjectDir%artifacts\tests\Windows_NT.x86.Debug\Tests\Core_Root /out %%a.ni.dll %ProjectDir%artifacts\tests\Windows_NT.x86.Debug\readytorun\r2rdump\files\%%a\%%a.dll
    "%RepoRoot%dotnet.cmd" %ProjectDir%artifacts\bin\coreclr\Windows_NT.x86.Debug\netcoreapp2.0\R2RDump.dll --in %%a.ni.dll --out %ProjectDir%tests\src\readytorun\r2rdump\files\Windows_NT.x86.Debug\%%a.xml -x -v --ignoreSensitive
))

COPY /Y %ProjectDir%tests\src\readytorun\r2rdump\files\Windows_NT.x64.Checked\*.xml %ProjectDir%tests\src\readytorun\r2rdump\files\Windows_NT.x64.Release\
COPY /Y %ProjectDir%tests\src\readytorun\r2rdump\files\Windows_NT.x86.Release\*.xml %ProjectDir%tests\src\readytorun\r2rdump\files\Windows_NT.x86.Checked\
