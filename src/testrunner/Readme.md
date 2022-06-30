# Build

## Windows
From current folder
```bash
.\..\..\.dotnet\dotnet.exe build .\Microsoft.NET.Test.Runner
```
To build inside VS(fix the path if needed)
```powershell
$env:PATH="C:\git\runtime\.dotnet;"+$env:Path
```
To test with System.Runtime.Tests.dll
```bash
cd src\libraries\System.Runtime\tests
del  .\..\..\..\..\artifacts\bin\System.Runtime.Tests\Debug\net7.0-windows\*.*
del C:\Users\...user...\.nuget\packages\microsoft.net.test.runner\*.*
 .\..\..\..\..\.dotnet\dotnet.exe build .\System.Runtime.Tests.csproj /p:SelfContainedTestRunner=true
 .\..\..\..\..\artifacts\bin\System.Runtime.Tests\Debug\net7.0-windows\System.Runtime.Tests.exe

 .\..\..\..\..\.dotnet\dotnet.exe publish .\System.Runtime.Tests.csproj /p:SelfContainedTestRunner=true --framework net7.0-windows
  .\..\..\..\..\.dotnet\dotnet.exe publish .\System.Runtime.Tests.csproj /p:SelfContainedTestRunner=true --framework net7.0-windows --self-contained true
```
