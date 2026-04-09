msbuild %~dp0..\NativeConsoleApp\NativeConsoleApp.vcxproj
msbuild %~dp0..\NativeLibrary\NativeLibrary.vcxproj
msbuild %~dp0..\SecondNativeLibrary\SecondNativeLibrary.vcxproj

copy %~dp0..\..\..\..\bin\AnyOS.AnyCPU.Debug\NativeConsoleApp\netstandard2.0\NativeConsoleApp.exe %~dp0NativeConsoleApp.exe
copy %~dp0..\..\..\..\bin\AnyOS.AnyCPU.Debug\NativeLibrary\netstandard2.0\NativeLibrary.dll %~dp0NativeLibrary.dll
copy %~dp0..\..\..\..\bin\AnyOS.AnyCPU.Debug\SecondNativeLibrary\netstandard2.0\SecondNativeLibrary.dll %~dp0SecondNativeLibrary.dll
