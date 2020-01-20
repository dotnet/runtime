# Introduction

This is a small netcore app to convert a corefx test assembly into a console app which runs the tests without any xunit framework code.

# Usage

## Project Runner Generator

```
dotnet run ../output ~/git/corefx/ ~/git/corefx/artifacts/bin/System.Runtime.Tests/netcoreapp-Unix-Debug/System.Runtime.Tests.dll -notrait category=nonosxtests -notrait category=failing -notrait category=Outerloop -stoponfail
```

There is support for response files, i.e.
```
dotnet run ../output ~/git/corefx/ ~/git/corefx/artifacts/bin/System.Runtime.Tests/netcoreapp-Unix-Debug/System.Runtime.Tests.dll @excludes.rsp
```


## Tests Runner Build

Go to for example `../output` from the example above and do

```
dotnet build
```

## Tests Execution

This assumes you have sucesfully ran HelloWorld sample before

```
../../sample/dotnet  --fx-version "3.0.0-preview-27408-5" bin/Debug/netcoreapp3.0/{name-of-the-test-dll}.dll
```

The test dll will usually be in the format `System.Runtime.Tests-runner.dll`


# Notes

- If xunit can't load a trait discoverer assembly, it silently ignores the error.
- The RemoteTestExecutor code used by corefx only seems to work if
the app is executed from the binary dir using dotnet ./<dllname>.
If ran using dotnet run, it seems to invoke itself instead of
RemoteExecutorConsoleApp.exe.
If ran using dotnet bin/.../<dllname>, it fails with:
No executable found matching command "dotnet-<dir>/RemoteExecutorConsoleApp.exe"
