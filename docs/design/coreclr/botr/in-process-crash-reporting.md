# Introduction #

The .NET runtime can generate a compact crash report from inside a crashing process. The in-process crash reporter is intended for environments where launching and attaching an external dump-generation process is unavailable, restricted, or unnecessarily expensive. This is particularly important for mobile applications, but the reporter is supported by CoreCLR on Unix platforms that provide the required native fatal-signal process model.

The reporter complements the out-of-process _createdump_ utility described in [Cross-platform Minidumps](xplat-minidump-generation.md). It does not generate a memory dump. Instead, it records the most useful information that can be collected safely from the crashing process:

- the process and runtime version;
- the fatal signal and the native register context at the crash site;
- managed threads and their stack frames, when the runtime can safely walk them;
- the managed exception on the crashing thread, when available; and
- module identities needed to interpret managed frames.

The report is emitted as a compact platform log and can also be written as a JSON file. Report generation is best-effort: preserving process termination and avoiding another hang or fault take precedence over producing a complete report.

# Design #

An in-process crash reporter operates under different constraints than _createdump_. The external utility can attach to a stopped target, load the Data Access Component (DAC), allocate memory, and use general-purpose operating system and C++ runtime services. The in-process reporter runs while handling a fatal signal, when the process may have corrupted state, may hold runtime or allocator locks, and may have exhausted its stack.

When crash reporting is enabled, the design divides the work into two phases:

1. During normal runtime startup, after configuration has enabled the reporter, it allocates its fixed state, captures process and platform information, and initializes optional services such as file lifecycle management and the watchdog.
2. If a terminal fatal signal subsequently reaches PAL while the reporter is enabled, the reporter uses only preallocated buffers and operations selected for that execution context. It streams output directly to the platform log and, when configured, to a temporary report file.

The reporter is implemented under `src/coreclr/debug/crashreport`. VM-specific thread enumeration and managed stack walking are provided by `src/coreclr/vm/crashreportstackwalker.cpp`. This separation keeps the signal-safe formatting and output machinery independent of CoreCLR's internal thread and code-management types.

## Activation and signal handling ##

The reporter is enabled with `DOTNET_EnableCrashReport=1`. It registers a callback with the Platform Abstraction Layer (PAL), which invokes the callback from terminal fatal-signal paths such as `SIGSEGV`, `SIGBUS`, `SIGFPE`, `SIGILL`, `SIGABRT`, and `SIGTRAP`.

The runtime coordinates concurrent crash-reporting attempts so that only one report is generated from the shared process state at a time.

CoreCLR chooses the crash-reporting mechanism as follows:

- On desktop Unix platforms, `DOTNET_DbgEnableMiniDump=1` uses _createdump_ for dump and crash-report generation.
- On desktop Unix platforms without dump generation enabled, `DOTNET_EnableCrashReport=1` enables the in-process reporter.
- On mobile platforms, `DOTNET_EnableCrashReport=1` enables the in-process reporter because _createdump_ is not available.

The in-process reporter is not currently enabled on Windows, Browser, or WASI. Windows uses its existing dump and Windows Error Reporting mechanisms, while Browser and WASI do not provide the required native fatal-signal process model.

## Signal chaining ##

Applications, native hosts, and platforms can install signal handlers before CoreCLR starts. By default, PAL invokes a previously registered non-default handler before generating the crash report. If that handler terminates the process instead of returning, CoreCLR never gets an opportunity to produce the report.

Android is an important example. Android installs its own crash handler, which is preserved as a chained signal handler when CoreCLR installs its handlers. The Android handler terminates the process after handling the crash, so invoking it first would prevent the in-process reporter from running.

Setting `DOTNET_CrashReportBeforeSignalChaining=1` changes the order so PAL generates the crash report before invoking the previous signal handler. The same behavior can be selected through the runtime configuration property `System.Runtime.CrashReportBeforeSignalChaining`:

```json
{
  "runtimeOptions": {
    "configProperties": {
      "System.Runtime.CrashReportBeforeSignalChaining": true
    }
  }
}
```

The environment-style CLR configuration value takes precedence when both forms are specified. This setting changes only when crash diagnostics run relative to a previously registered handler; it does not enable the reporter by itself.

## Signal safety ##

Initialization performs work that is unsuitable for a signal handler. This includes allocating reporter state, resolving the process name, reading Apple platform information with `sysctl`, creating the watchdog thread, and scanning the report directory. Values needed during a crash are cached in fixed-size buffers.

The crash path does not allocate memory or load modules. Formatting uses bounded, fixed-size buffers and streams chunks through small JSON and console writers. File output uses operations such as `open`, `write`, `close`, `unlink`, and same-directory `rename`. The signal dispatcher preserves `errno` across report generation.

The reporter deliberately tolerates missing information. Strings may be truncated to their fixed bounds, frames that cannot be classified remain native frames, and a failure to suspend the runtime limits collection to the crashing thread. An output failure stops or discards the affected output rather than attempting complex recovery from the crash path.

## Thread enumeration and stack walking ##

The crashing managed thread is emitted first so that the most important diagnostic information remains present if collection later becomes incomplete. The reporter captures its managed exception type and HRESULT before attempting to suspend the runtime.

Walking the remaining managed threads requires a completed runtime suspension. The reporter can either create its own suspension or reuse an existing one when the crashing thread owns it. This includes applicable Workstation and Server GC scenarios; a participating Server GC worker can also reuse the completed suspension. If no stable suspension can be used safely, the reporter includes only the information it can collect from the crashing thread.

Each thread entry identifies whether it is managed and whether it is the crashing thread. The crashing thread begins with a native crash-site frame constructed from the signal's saved register context. Managed frames can include the method name, metadata token, IL offset, native offset, module name, module timestamp and size, and module MVID when those values are available.

The compact log uses a bounded module table so frames can refer to modules by a short numeric index. If the table is full or a module cannot be resolved, the frame includes the available module identity inline instead. The JSON output records the corresponding frame data directly.

## Stack overflow ##

A stack overflow cannot rely on an ordinary stack walk from the crashing thread's exhausted stack. CoreCLR already constructs a compressed stack trace while handling a stack overflow. The in-process reporter snapshots that trace into preallocated storage before the process begins handling the fatal signal.

When the crashing thread and the saved stack-overflow trace match, the reporter emits the captured managed frames and identifies the exception as `System.StackOverflowException`. Repeated frame sequences retain their repeat metadata in the JSON report. The snapshot is bounded; if it is unavailable or truncated, the report records the limitation rather than attempting an unsafe walk.

# Output #

The reporter has two output forms generated from the same collected data.

## Compact log ##

For reports triggered by a fatal signal, the compact log is always attempted. On Android, each logical line is written to logcat with the `DOTNET_CRASH` tag. On other supported platforms it is written to the runtime's standard error logging sink.

The log begins with the report protocol version, runtime build, architecture, process name, process ID, and fatal signal. It then lists thread blocks and stack frames, followed by the module table used by managed frames. The number of frames written per thread can be bounded so a badly corrupted or unusually large stack does not produce unbounded log output.

## JSON report ##

The JSON output uses a createdump-shaped payload so crash-processing systems can consume a familiar structure. The document contains a `payload` object with configuration, process, thread, exception, and frame information, plus a `parameters` object containing the fatal signal and available platform information. The schema carries an explicit protocol version so readers can distinguish future revisions.

For ordinary stack walks, the JSON report records every frame returned by the stack walker. `DOTNET_CrashReportFrameLimitPerThread` limits only the compact log because the JSON file is the more complete postmortem artifact and is streamed directly to disk. Stack-overflow reports are an exception: their preallocated trace snapshot is limited to 128 entries, and the JSON records when frames were truncated.

JSON file output is enabled when `DOTNET_CrashReportRootPath` specifies an existing absolute directory. The reporter creates the following private subdirectories below that root:

```text
<root>/.dotnet/crash-reports/
```

Completed reports use names of the following form:

```text
report-<timestamp-in-nanoseconds>-<pid>.crashreport.json
```

The report is streamed to a file with a `.tmp` suffix. After the JSON document has been completed and the file has been closed successfully, a same-directory rename publishes the final report atomically. A failed or incomplete report is removed instead of being exposed as a completed report.

# Report lifecycle #

Report directory setup and retention pruning run during normal startup, not during a crash. Startup removes stale temporary files and retains only the newest configured number of completed reports. If the directory is already at the retention limit, the oldest report path is cached so the crash path can remove it before publishing the next report without rescanning the directory.

The reporter never creates the configured root itself. The root must already exist, be an absolute path, and be writable. If lifecycle initialization fails, JSON file output is disabled, but compact log output remains available.

# Watchdog #

Walking damaged process state can hang. Unless the watchdog is disabled through configuration, the reporter creates a detached watchdog thread and a non-blocking pipe during startup. The watchdog blocks the fatal signals handled by the runtime so a process-directed fatal signal is not delivered to the watchdog instead of an application thread.

Some platforms already provide watchdogs that monitor application responsiveness, such as whether the main thread continues to pump messages. Those watchdogs do not necessarily detect a crash reporter that hangs on another thread while the monitored thread remains responsive. The in-process crash reporter therefore uses its own watchdog, which monitors report generation regardless of which runtime thread encountered the crash.

The crash path sends a start notification before collection and a finish notification when collection ends. If the finish notification does not arrive within the configured timeout, the watchdog aborts the process. The watchdog is best-effort: if its communication channel fails, it exits and leaves termination to the platform's normal fatal-signal handling.

# Configuration/Policy #

The following settings control the in-process reporter:

| Setting | Meaning |
|---------|---------|
| `DOTNET_EnableCrashReport` | Enables crash reporting. When _createdump_ owns the crash path, it generates both its configured dump and crash report. Otherwise this enables the in-process reporter. |
| `DOTNET_CrashReportRootPath` | Existing absolute directory under which lifecycle-managed JSON reports are stored. If unset, only the compact log is emitted. |
| `DOTNET_CrashReportMaxFileCount` | Maximum number of completed JSON reports to retain. The default is 32 and the value must be positive. |
| `DOTNET_CrashReportTimeoutSeconds` | Watchdog timeout in seconds. The default is 30; `0` disables the watchdog. |
| `DOTNET_CrashReportFrameLimitPerThread` | Maximum number of stack frames per thread in the compact log. The default is 32; `0` disables the limit. JSON frame collection is not limited by this setting. |
| `DOTNET_CrashReportBeforeSignalChaining` | When set to `1`, generates crash diagnostics before invoking a previously registered native signal handler. The default is to invoke the previous handler first. |
| `System.Runtime.CrashReportBeforeSignalChaining` | Runtime configuration equivalent of `DOTNET_CrashReportBeforeSignalChaining`, specified as a Boolean in `runtimeconfig.json`. |

Crash reports contain process, module, exception, and stack information and should be handled as diagnostic data. Applications are responsible for choosing an appropriately protected root directory and for collecting, uploading, or deleting reports according to their privacy and retention requirements.

# Limitations #

The in-process reporter trades completeness for availability on a damaged process path:

- it is not a replacement for a memory dump and cannot support arbitrary postmortem memory inspection;
- native frame symbolication is not performed in the crashing process;
- only managed threads known to CoreCLR are enumerated;
- other managed threads are omitted when the runtime cannot be suspended safely;
- severe memory corruption can prevent stack walking or output; and
- report generation after a fatal signal remains best-effort and may be terminated by the watchdog.

When the environment permits launching and attaching _createdump_, a dump remains the more complete diagnostic artifact. The in-process report is aimed at reliably preserving a useful crash summary when _createdump_ is unavailable or has not been enabled.
