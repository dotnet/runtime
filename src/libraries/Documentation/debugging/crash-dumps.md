Crash dumps can be useful for analyzing and debugging intermittent or hard-to-reproduce bugs. In all of our CI test runs and official build test runs, we use a utility called "Dumpling" to collect and archive crash dumps that are created during test execution. These crash dumps are archived on on the [Dumpling web portal](https://dumpling.azurewebsites.net/), which has download links, as well as auxiliary triage information gathered during crash dump collection.

When a crash is encountered in a test run (and crash dump collection is enabled), the following information will be printed to the log:

```
  Process is terminating due to StackOverflowException.
  processing dump file C:\Users\DOTNET~1\AppData\Local\Temp\CoreRunCrashDumps\dotnet.exe.13228.dmp
  creating dumpling dump 37ad6dce8b9d7f29def35b1ae1c9a3d4e3fc03bf
  uploading artifact 37ad6dce8b9d7f29def35b1ae1c9a3d4e3fc03bf dotnet.exe.13228.dmp
```

The crash dump can then be located via this unique identifier from the Dumpling portal. In the example above, the identifier is "37ad6dce8b9d7f29def35b1ae1c9a3d4e3fc03bf".

Note that, while Dumpling archives the crash dumps for a long time, the Jenkins CI logs (containing the test info and the crash dump identifier) are not persisted for more than a few days.

Debugging crash dumps is a fairly involved process. Windows crash dumps are well-supported using existing and documented tools like Visual Studio, WinDBG, and SOS. The instructions for Unix platforms are complicated, and are documented [here](https://github.com/dotnet/corefx/blob/master/Documentation/debugging/unix-instructions.md#debugging-core-dumps-with-lldb).
