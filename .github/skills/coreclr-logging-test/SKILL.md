---
name: coreclr-logging-test
description: >
  Run any coreclr or libraries test with CoreCLR diagnostic logging enabled,
  producing log files for analysis. Use when asked to "run a test with CLR
  logging", "enable CoreCLR logs", "capture runtime logs", "debug with LOG
  facility", or "get diagnostic output from the runtime". Helps diagnose
  runtime issues by enabling the built-in CLR logging infrastructure.
---

# Running Tests with CoreCLR Logging Enabled

Run any test (coreclr or libraries) with the CoreCLR diagnostic logging subsystem enabled, producing text log files for analysis.

> 🚨 **IMPORTANT**: CoreCLR logging is only available in **Debug** or **Checked** builds of the runtime. It is compiled out of Release builds. You must use a Debug or Checked CoreCLR (`-rc debug` or `-rc checked`). If the user has not already built a Debug/Checked runtime, build one first.

## Background

The CoreCLR runtime has a built-in logging infrastructure controlled by environment variables. In code, the `LOG((facility, level, fmt, ...))` and `LOG2((facility2, level, fmt, ...))` macros generate log output. These are defined in `src/coreclr/inc/log.h` and the facilities are defined in `src/coreclr/inc/loglf.h`.

## Step 1: Determine What to Log

Ask the user (if not already specified) which subsystems they want to log. The logging is controlled by two bitmask environment variables:

### LogFacility (DOTNET_LogFacility) — Primary Facilities

These are hex bitmask values that can be OR'd together:

| Facility | Hex Value | Description |
|---|---|---|
| `LF_GC` | `0x00000001` | Garbage collector |
| `LF_GCINFO` | `0x00000002` | GC info encoding |
| `LF_STUBS` | `0x00000004` | Stub code generation |
| `LF_JIT` | `0x00000008` | JIT compiler |
| `LF_LOADER` | `0x00000010` | Assembly/module loader |
| `LF_METADATA` | `0x00000020` | Metadata operations |
| `LF_SYNC` | `0x00000040` | Synchronization primitives |
| `LF_EEMEM` | `0x00000080` | Execution engine memory |
| `LF_GCALLOC` | `0x00000100` | GC allocation tracking |
| `LF_CORDB` | `0x00000200` | Debugger (managed debugging) |
| `LF_CLASSLOADER` | `0x00000400` | Class/type loader |
| `LF_CORPROF` | `0x00000800` | Profiling API |
| `LF_DIAGNOSTICS_PORT` | `0x00001000` | Diagnostics port |
| `LF_DBGALLOC` | `0x00002000` | Debug allocator |
| `LF_EH` | `0x00004000` | Exception handling |
| `LF_ENC` | `0x00008000` | Edit and Continue |
| `LF_ASSERT` | `0x00010000` | Assertions |
| `LF_VERIFIER` | `0x00020000` | IL verifier |
| `LF_THREADPOOL` | `0x00040000` | Thread pool |
| `LF_GCROOTS` | `0x00080000` | GC root tracking |
| `LF_INTEROP` | `0x00100000` | Interop / P/Invoke |
| `LF_MARSHALER` | `0x00200000` | Data marshalling |
| `LF_TIEREDCOMPILATION` | `0x00400000` | Tiered compilation |
| `LF_ZAP` | `0x00800000` | Native images (R2R) |
| `LF_STARTUP` | `0x01000000` | Startup / shutdown |
| `LF_APPDOMAIN` | `0x02000000` | AppDomain |
| `LF_CODESHARING` | `0x04000000` | Code sharing |
| `LF_STORE` | `0x08000000` | Storage |
| `LF_SECURITY` | `0x10000000` | Security |
| `LF_LOCKS` | `0x20000000` | Lock operations |
| `LF_BCL` | `0x40000000` | Base Class Library |
| `LF_ALWAYS` | `0x80000000` | Always log (level-gated only) |

To log everything, use `0xFFFFFFFF`.

### LogFacility2 (DOTNET_LogFacility2) — Extended Facilities

| Facility | Hex Value | Description |
|---|---|---|
| `LF2_MULTICOREJIT` | `0x00000001` | Multicore JIT |
| `LF2_INTERPRETER` | `0x00000002` | Interpreter |

### LogLevel (DOTNET_LogLevel) — Verbosity

| Level | Value | Expected Volume |
|---|---|---|
| `LL_ALWAYS` | `0` | Essential messages only |
| `LL_FATALERROR` | `1` | Fatal errors |
| `LL_ERROR` | `2` | Errors |
| `LL_WARNING` | `3` | Warnings |
| `LL_INFO10` | `4` | ~10 messages per run |
| `LL_INFO100` | `5` | ~100 messages per run |
| `LL_INFO1000` | `6` | ~1,000 messages per run |
| `LL_INFO10000` | `7` | ~10,000 messages per run |
| `LL_INFO100000` | `8` | ~100,000 messages per run |
| `LL_INFO1000000` | `9` | ~1,000,000 messages per run |
| `LL_EVERYTHING` | `10` | All messages |

**Default recommendation**: Start with level `6` (LL_INFO1000) for a manageable amount of output. Use `10` only if you need exhaustive detail and are prepared for very large log files.

## Step 2: Prepare the Log Output Directory

Create a directory to hold the log output files:

```powershell
$logDir = Join-Path (Get-Location) "clr-logs"
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
```

## Step 3: Set Environment Variables and Run the Test

Set the following environment variables before running the test. All logging knobs use the `DOTNET_` prefix.

### Required Variables

| Variable | Description |
|---|---|
| `DOTNET_LogEnable` | Set to `1` to enable logging |
| `DOTNET_LogFacility` | Hex bitmask of primary facilities to log |
| `DOTNET_LogLevel` | Verbosity level (0–10) |

### Output Control Variables

| Variable | Description |
|---|---|
| `DOTNET_LogToFile` | Set to `1` to write logs to a file |
| `DOTNET_LogFile` | Path to the log file (e.g., `C:\clr-logs\clr.log`) |
| `DOTNET_LogWithPid` | Set to `1` to append PID to filename (useful for multi-process) |
| `DOTNET_LogFlushFile` | Set to `1` to flush on each write (slower but crash-safe) |
| `DOTNET_LogToConsole` | Set to `1` to also write logs to console |
| `DOTNET_LogFileAppend` | Set to `1` to append to existing log file instead of overwriting |

### Optional Extended Facilities

| Variable | Description |
|---|---|
| `DOTNET_LogFacility2` | Hex bitmask for extended facilities (LF2_*) |

### Running a Libraries Test

```powershell
$logDir = Join-Path (Get-Location) "clr-logs"
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

$env:DOTNET_LogEnable = "1"
$env:DOTNET_LogFacility = "0x00000008"   # LF_JIT — adjust as needed
$env:DOTNET_LogLevel = "6"               # LL_INFO1000
$env:DOTNET_LogToFile = "1"
$env:DOTNET_LogFile = Join-Path $logDir "clr.log"
$env:DOTNET_LogWithPid = "1"
$env:DOTNET_LogFlushFile = "1"

# Run the test (example — adapt the path to the specific library)
dotnet build /t:Test src\libraries\<LibraryName>\tests\<LibraryName>.Tests.csproj

# Clean up environment variables after the test
Remove-Item Env:\DOTNET_LogEnable
Remove-Item Env:\DOTNET_LogFacility
Remove-Item Env:\DOTNET_LogLevel
Remove-Item Env:\DOTNET_LogToFile
Remove-Item Env:\DOTNET_LogFile
Remove-Item Env:\DOTNET_LogWithPid
Remove-Item Env:\DOTNET_LogFlushFile
```

### Running a CoreCLR Test

For CoreCLR tests, run the test executable directly with the environment variables set:

```powershell
$logDir = Join-Path (Get-Location) "clr-logs"
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

$env:DOTNET_LogEnable = "1"
$env:DOTNET_LogFacility = "0x00000008"   # LF_JIT — adjust as needed
$env:DOTNET_LogLevel = "6"               # LL_INFO1000
$env:DOTNET_LogToFile = "1"
$env:DOTNET_LogFile = Join-Path $logDir "clr.log"
$env:DOTNET_LogWithPid = "1"
$env:DOTNET_LogFlushFile = "1"

# Build the test if needed
src\tests\build.cmd x64 checked tree JIT\Regression\JitBlue\Runtime_99391

# Run it via the generated runner script or directly
& "artifacts\tests\coreclr\windows.x64.Checked\JIT\Regression\JitBlue\Runtime_99391\Runtime_99391.cmd"

# Clean up environment variables after the test
Remove-Item Env:\DOTNET_LogEnable
Remove-Item Env:\DOTNET_LogFacility
Remove-Item Env:\DOTNET_LogLevel
Remove-Item Env:\DOTNET_LogToFile
Remove-Item Env:\DOTNET_LogFile
Remove-Item Env:\DOTNET_LogWithPid
Remove-Item Env:\DOTNET_LogFlushFile
```

## Step 4: Examine the Log Output

After the test completes, log files will be in the `$logDir` directory. When `DOTNET_LogWithPid=1`, each process produces a separate log file with the PID appended to the filename (e.g., `clr.log.1234`).

```powershell
# List log files
Get-ChildItem $logDir

# View the tail of a log file
Get-Content (Get-ChildItem $logDir -Filter "clr.log*" | Select-Object -First 1).FullName -Tail 100

# Search for specific content
Select-String -Path "$logDir\clr.log*" -Pattern "keyword"
```

## Common Facility Combinations

Here are useful pre-built facility masks for common debugging scenarios:

| Scenario | LogFacility | LogFacility2 | Description |
|---|---|---|---|
| JIT debugging | `0x00000008` | — | JIT compiler only |
| GC investigation | `0x00080103` | — | GC + GCINFO + GCALLOC + GCROOTS |
| Class loading | `0x00000410` | — | LOADER + CLASSLOADER |
| Exception handling | `0x00004000` | — | EH only |
| Interop / marshalling | `0x00300000` | — | INTEROP + MARSHALER |
| Tiered compilation | `0x00400008` | — | TIEREDCOMPILATION + JIT |
| Startup issues | `0x01000000` | — | STARTUP |
| Thread pool | `0x00040000` | — | THREADPOOL |
| Everything | `0xFFFFFFFF` | `0xFFFFFFFF` | All facilities (very verbose!) |

## Step 5: Clean Up

After analysis, remove the log directory to avoid consuming disk space:

```powershell
Remove-Item -Recurse -Force $logDir
```

## Tips

- **Start narrow**: Begin with a specific facility and low log level, then widen if needed. Logging everything at level 10 can produce gigabytes of output.
- **Use `LogFlushFile=1`**: If the runtime crashes during the test, unflushed logs are lost. Enable flush for crash investigations.
- **Use `LogWithPid=1`**: Always recommended. Tests may spawn child processes, and separate files per process make analysis easier.
- **Checked builds are preferred**: They include both logging and runtime assertions, giving the most diagnostic information without the full performance cost of Debug builds.
- **Libraries tests need a Checked runtime**: Build with `build.cmd clr -rc checked` (or `build.sh clr -rc checked`) so the runtime used to execute library tests has logging compiled in.
- **Log file location**: Use an absolute path for `DOTNET_LogFile` to ensure logs land in a predictable location regardless of working directory changes during test execution.
