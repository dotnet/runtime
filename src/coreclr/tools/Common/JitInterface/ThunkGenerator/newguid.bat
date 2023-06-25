pushd %~dp0
call ..\..\..\..\..\..\dotnet.cmd run -- NewJITEEVersion ..\..\..\..\inc\jiteeversionguid.h
popd
