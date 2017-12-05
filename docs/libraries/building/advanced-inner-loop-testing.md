# Advanced scenario - Build and run application code with csc/vbc and CoreRun

 __Don't consider using this tutorial for anything else than inner-loop testing of corefx/coreclr binaries. Prefer using the official .NET Core SDK: https://docs.microsoft.com/en-us/dotnet/core/tools/?tabs=netcore2x__

This tutorial describes how to build and run application code that targets self-compiled .NET Core binaries without using Visual Studio,  the .NET Core SDK Host (`dotnet.exe`) or a project file (e.g. `csproj`). Follow these steps to quickly validate changes you've made in the product e.g. by running benchmarks or tests on it.

If you are on Windows you currently need to use the `Developer Command Prompt for VS 2017` to build corefx/coreclr! For the sake of completeness, we have placed our repositories under `d:\git\`.

## Compile corefx with self-compiled coreclr binaries
If you've made changes to coreclr make sure to also build it and pass its binaries to corefx.
```
coreclr\build -release
corefx\build -release -- /p:CoreCLROverridePath=d:\git\coreclr\bin\Product\Windows_NT.x64.Release\
```

## Compile corefx with pre-compiled coreclr binaries
If you haven't made any changes to coreclr you're fine with just building corefx. This automatically picks pre-compiled coreclr binaries from MyGet.
```
corefx\build -release
```

## Create and prepare your application
We will build a sample application which outputs `Hello World!` to the console.

1. Create an application directory (in our example under `d:\git\`):
```
mkdir core-demo
cd core-demo
```
2. Save the following C# code to a file called `Program.cs` into your application folder:
```csharp
using System;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Hello World!");
    }
}
```

3. Copy the just built corefx assemblies into your application directory. When using Visual Studio or the .NET Core SDK Host (`dotnet.exe`) you usually compile against *reference assemblies*. For simplicity we compile against the same assembly set that we use during run time.
```
xcopy ..\corefx\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9 runtime /e /i /y /s
```

You don't need all the assemblies that are built by corefx but copying the entire directory makes it easier if you want to reference additional ones. At a minimum, this app will need the following assemblies to run:

- CoreClr assemblies: `clrjit.dll`, `CoreRun.exe`, `coreclr.dll`, `System.Private.CoreLib.dll`. For more information about the CoreClr parts, visit [Using your build](https://github.com/dotnet/coreclr/blob/master/Documentation/workflow/UsingYourBuild.md)
- CoreFx assemblies: `System.Runtime.dll`, `System.Runtime.Extensions.dll`, `System.Runtime.InteropServices.dll`, `System.Text.Encoding.Extensions.dll`, `System.Threading.dll`

## Compile your application
Use the C# Compiler (`csc`) to compile your C# code (`Program.cs`) against the copied assemblies. For our Hello World example we need to compile our application code against `System.Private.Corelib.dll`, `System.Runtime.dll`, `System.Runtime.Extensions.dll` and `System.Console.dll`. As described above these assemblies have dependencies on two other assemblies: `System.Text.Encoding.Extensions.dll` and `System.Threading.dll`.
```
.\runtime\corerun ..\corefx\tools\csc.dll /noconfig /r:runtime\System.Private.Corelib.dll /r:runtime\System.Runtime.dll /r:runtime\System.Runtime.Extensions.dll /r:runtime\System.Console.dll /out:runtime\Program.dll Program.cs
```

If you want to compile Visual Basic code simply replace `csc.dll` with `vbc.dll`.

## Run your application
`Corerun.exe` is part of the coreclr binaries and is best described as the host of your .NET Core application. Find more information at [Using CoreRun](https://github.com/dotnet/coreclr/blob/master/Documentation/workflow/UsingCoreRun.md).
```
cd runtime
.\corerun Program.dll
```

> Hello World!
