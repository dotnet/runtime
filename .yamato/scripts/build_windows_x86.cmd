# build solution
dotnet build unity\managed.sln -c Release
cd unity\unitygc
cmake . -A Win32
cmake --build . --config Release
cd ../../
# build subset library and core clr
build.cmd -subset clr+libs -a x86 -c release -ci
copy unity\unitygc\Release\unitygc.dll artifacts\bin\microsoft.netcore.app.runtime.win-x86\Release\runtimes\win-x86\native
copy artifacts\bin\unity-embed-host\Release\net6.0\unity-embed-host.dll artifacts\bin\microsoft.netcore.app.runtime.win-x86\Release\runtimes\win-x86\lib\net7.0
copy artifacts\bin\unity-embed-host\Release\net6.0\unity-embed-host.pdb artifacts\bin\microsoft.netcore.app.runtime.win-x86\Release\runtimes\win-x86\lib\net7.0
powershell .yamato\scripts\download_7z.ps1
copy LICENSE.md artifacts\bin\microsoft.netcore.app.runtime.win-x86\Release\runtimes\win-x86
artifacts\7za-win-x64\7za.exe a artifacts\unity\%ARTIFACT_FILENAME% .\artifacts\bin\microsoft.netcore.app.runtime.win-x86\Release\runtimes\win-x86\*
