# Introduction

This is a small netcore app to convert a corefx test assembly into a console app which runs the tests without any xunit framework code.

# Usage

```
dotnet run ../runner.cs ~/git/corefx/ ~/git/corefx//artifacts/bin/System.Runtime.Tests/netcoreapp-Unix-Debug/System.Runtime.Tests.dll -notrait category=nonosxtests -notrait category=failing -notrait category=Outerloop -stoponfail
```

# Notes

- If xunit can't laod a trait discoverer assembly, it silently ignores the error.
- The RemoteTestExecutor code used by corefx only seems to work if
the app is executed from the binary dir using dotnet ./<dllname>.
If ran using dotnet run, it seems to invoke itself instead of
RemoteExecutorConsoleApp.exe.
If ran using dotnet bin/.../<dllname>, it fails with:
No executable found matching command "dotnet-<dir>/RemoteExecutorConsoleApp.exe"
