rem build solution
cmd /c dotnet build unity\managed.sln -c Release
cd unity\unitygc
cmake .
cmake --build . --config Release
cd ../../
rem build subset library and core clr
build.cmd -subset clr+libs -a x64 -c release -ci
copy unity\unitygc\Release\unitygc.dll artifacts\bin\microsoft.netcore.app.runtime.win-x64\Release\runtimes\win-x64\native
copy artifacts\bin\unity-embed-host\Release\net6.0\unity-embed-host.dll artifacts\bin\microsoft.netcore.app.runtime.win-x64\Release\runtimes\win-x64\lib\net7.0
copy artifacts\bin\unity-embed-host\Release\net6.0\unity-embed-host.pdb artifacts\bin\microsoft.netcore.app.runtime.win-x64\Release\runtimes\win-x64\lib\net7.0
