Debugging CoreFX on Windows
==========================

You can Debug .NET Core via Visual Studio or WinDBG.

For Visual Studio debugging, follow the instructions at [Debugging tests in Visual Studio](https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md) to run and debug tests.
For bugs that cannot be reproduced within Visual Studio (certain low-probability race conditions, SafeHandle life-time problems, etc) you will need to use WinDBG.

## Required Software

WinDBG free download:

* [WDK and WinDBG downloads](https://msdn.microsoft.com/en-us/windows/hardware/hh852365.aspx)

Note: You can select the Standalone Debugging Tools for Windows or download only WinDBG via the WDK.

## Prerequisites to Debugging with WinDBG

1. Build the entire repository. This ensures that all packages are downloaded and that you have up-to-date symbols.

2. Find the path where the debugging engine (SOS) is located.

Assuming that your repo is at `C:\corefx` this should be located at:

```
C:\corefx\packages\runtime.win7-x64.Microsoft.NETCore.Runtime.CoreCLR\<version>\tools\sos.dll   
```

3. Install WinDBG as post-mortem debugger
As Administrator:

```
windbg -I
```

You may need to do this for both x64 and x86 versions. 
Any application that crashes should now automatically start a WinDBG session.

## Debugging tests
To run a single test from command line:

* Locate the test binary folder based on the CSPROJ name. 

For example: `src\System.Net.Sockets\tests\Functional\System.Net.Sockets.Tests.csproj` will build and output binaries at  `bin\tests\Windows_NT.AnyCPU.Debug\System.Net.Sockets.Tests\dnxcore50`.
 
* Execute the test

Assuming that your repo is at `C:\corefx`:

```
cd C:\corefx\bin\tests\Windows_NT.AnyCPU.Debug\System.Net.Sockets.Tests\dnxcore50
C:\corefx\bin\tests\Windows_NT.AnyCPU.Debug\System.Net.Sockets.Tests\dnxcore50\CoreRun.exe xunit.console.netcore.exe System.Net.Sockets.Tests.dll -xml testResults.xml -notrait category=nonwindowstests -notrait category=OuterLoop -notrait category=failing
```

* If the test crashes or encounteres a `Debugger.Launch()` method call, WinDBG will automatically start and attach to the `CoreRun.exe` process 

The following commands will properly configure the debugging extension and fix symbol and source-code references:

```
.symfix
.srcfix
.reload
!load C:\corefx\packages\runtime.win7-x64.Microsoft.NETCore.Runtime.CoreCLR\<version>\tools\sos
```

_Important_: Pass in the correct path to your SOS extension discovered during the Prerequisites, step 2.

Documentation on how to use the SOS extension is available on [MSDN](https://msdn.microsoft.com/en-us/library/bb190764\(v=vs.110\).aspx).

For quick reference, type the following in WinDBG:

```
0:000> !sos.help
```

## Traces

In Windows, EventSource generated traces are collected via ETW using PerfView.

### Using PerfView

1. Install [PerfView](http://www.microsoft.com/en-us/download/details.aspx?id=28567)
2. Run PerfView as Administrator
3. Disable all other collection parameters
4. Add Additional Providers (see below - Important: keep the "*" wildcard before the names.)

### Built-in EventSource tracing

The following EventSources are built-in CoreFX. The ones that are not marked as [__TestCode__] or [__FEATURE_TRACING__] can be enabled in production scenarios for log collection. 

#### Global
* `*System.Diagnostics.Eventing.FrameworkEventSource`: Global EventSource used by multiple namespaces.

#### System.Collections
* `*System.Collections.Concurrent.ConcurrentCollectionsEventSource`: [__FEATURE_TRACING__] Provides an event source for tracing Coordination Data Structure collection information.

#### System.Linq
* `*System.Linq.Parallel.PlinqEventSource`: Provides an event source for tracing PLINQ information.

#### System.Net namespaces

* `*Microsoft-System-Net-Debug`: Highly verbose, low-level debugging traces in production code.
* `*Microsoft-System-Net-TestLogging`: [__TestCode__] Test-code tracing (I/O async completions, performance test reporting).

#### System.Threading
* `*System.Threading.SynchronizationEventSource`: Provides an event source for tracing Coordination Data Structure synchronization information.
* `*System.Threading.Tasks.TplEventSource`: Provides an event source for tracing TPL information.
* `*System.Threading.Tasks.Parallel.EventSource`: Provides an event source for tracing TPL information.
* `*System.Threading.Tasks.Dataflow.DataflowEventSource`: [__FEATURE_TRACING__] Provides an event source for tracing Dataflow information. 

## Notes 
* You can find the test invocation command-line by looking at the logs generated after the `msbuild /t:rebuild,test` within the test folder.
