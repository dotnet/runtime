# In-proc crash report lifecycle — design notes

Design rationale for `InProcCrashReportLifecycle` (`inproccrashreportlifecycle.h` /
`inproccrashreportlifecycle.cpp`). It records *why* the lifecycle behaves the way
it does, derived from an investigation of how Android and iOS manage their own
crash artifacts and a brainstorming/grill-me design session. It is documentation,
not a spec — the code is the source of truth.

## Problem

The in-proc crash reporter (`InProcCrashReporter`) emits a createdump-shaped
`*.crashreport.json` payload from the crash signal handler. On mobile, disk space
is constrained, so crash reports that pile up without bound can bloat the device —
yet there was no durable, app-local place to put them and no mechanism to cap how
many are kept.

The reporter could write a file only when `DOTNET_DbgMiniDumpName` was set (writing
`<expanded-DbgMiniDumpName>.crashreport.json`), emitting compact logs otherwise.
That path is relatively new and carries no notion of a managed location or of
retaining/pruning past reports; it stays supported here, but it is recent enough
that we could drop it if we chose to.

## Background: how the platforms manage crash artifacts

**Android (tombstones).** The OS keeps a *bounded rotating set* of tombstone
files. `tombstoned` writes completed dumps to `/data/tombstones/tombstone_00`,
`tombstone_01`, … The default maximum is **32**
(`tombstoned.max_tombstone_count`). It picks a missing slot first, otherwise the
oldest slot, then advances modulo the max. Dumps are first written to anonymous
temp files and only **linked** into the final `tombstone_NN` path once the dumper
reports completion, so a failed/timed-out dump never leaves a named partial
tombstone. Because filenames are fixed slots, age is tracked by `mtime`. The
breadcrumb tying a crash to its file is the logcat line
`Tombstone written to: /data/tombstones/tombstone_06`. (See AOSP `debuggerd`/
`tombstoned` and the Android debugging docs for the authoritative mechanics.)

**iOS / Apple.** Crash reports are OS-managed diagnostic logs (`.ips`/`.crash`),
not a public circular tombstone directory. The system generates a report after
the app is allowed to finish crashing (an attached Xcode debugger intercepts the
crash until you detach). Reports live in the device's Analytics Data and are
surfaced/transferred via Xcode/Console, TestFlight, and the App Store. Retention
and pruning are Apple/OS-managed; there is **no** app-controlled, Android-style
fixed-slot rotation contract. (See Apple's developer documentation on acquiring
crash reports and diagnostic logs.)

**Takeaways that shaped this design.** Borrow Android's robust mechanics —
default of 32, temp-file-then-link commit, bounded retention — but improve on the
fixed-slot model by encoding the crash time in the filename (so ordering does not
depend on fragile `mtime`), and accept that on iOS the runtime, not the OS, owns
this app-local directory.

## Goals and non-goals

- **Goal:** a durable, bounded, app-local directory of completed crash reports,
  written safely from the crash signal handler.
- **Goal:** opt-in. No file output unless explicitly configured, so we never
  silently start persisting files to a device.
- **Goal:** fail loud and disabled on misconfiguration rather than guessing.
- **Non-goal:** cross-process coordination/locking. Retention is best-effort when
  multiple processes share a root (see *Cross-process*).
- **Non-goal:** replacing the OS crash facilities; this is the runtime's own JSON
  report alongside them.

## Configuration

| Variable | Meaning |
| --- | --- |
| `DOTNET_CrashReportRootPath` | Activates file output. Root under which the managed directory is created. Supports a leading `~` or `$HOME` shorthand expanded to `getenv("HOME")`. Must resolve to an existing, absolute directory or output is disabled. |
| `DOTNET_CrashReportMaxFileCount` | Retention bound. Default **32**; **-1** unlimited; **0** cleanup-only. Parsed as a *signed* value (the usual DWORD config helper is unsigned); out-of-range values fall back to the default. |

When `CrashReportRootPath` is unset, the legacy `DbgMiniDumpName +
.crashreport.json` behavior is unchanged — the root path supersedes the legacy
path only when set.

## On-disk layout and naming

```
<root>/.dotnet/crash-reports/<appName>/report-<timestamp>-<pid>[-<suffix>].crashreport.json
```

- `<appName>` is the sanitized process name (unsafe path characters replaced with
  `_`, falling back to `unknown`) — isolates retention per app under a shared root.
- `<timestamp>` (epoch seconds) and `<pid>` order reports by intended crash time;
  the optional `-<suffix>` disambiguates same-second/pid collisions.
- The `.crashreport.json` extension is the canonical one already emitted by
  `createdump` and the existing reporter; external tooling recognizes it. The
  `report-` prefix is this feature's marker/parse anchor.
- In-progress reports use a `…/report-….crashreport.json.tmp` temp file.

On mobile, `HOME` is app-scoped (`context.getFilesDir()` on Android CoreCLR app
hosting; the app sandbox home on iOS), so `$HOME/.dotnet/crash-reports` is
per-app, not shared across apps.

## Two execution contexts

The class deliberately spans two contexts; every member documents which it obeys.

**Initialization path (`Initialize` and helpers).** Runs once at startup. May
allocate and call libc/filesystem APIs. Responsibilities:

1. Resolve/validate the root, create `.dotnet/crash-reports/<appName>` (each
   `mkdir` tolerates `EEXIST`; the final node is verified to be a directory).
2. Verify writability with a create+close+unlink probe file — so the crash path
   never discovers an unwritable directory.
3. Scan the directory (capped at a bounded entry count to avoid startup hangs):
   - delete stale `.tmp` files whose owning pid is no longer alive
     (`kill(pid,0)`), so we never unlink another live process's active temp;
   - in cleanup-only mode, delete every completed report and leave output
     disabled;
   - otherwise collect completed reports, sort oldest-first, and **precompute**
     the over-retention deletion candidates into fixed buffers.

**Crash/signal path (`PrepareReportFile`, `FinishReportFile`, and helpers).**
Invoked from the signal handler; must be async-signal-safe and allocation-free:

- build the temp + final paths into caller-provided fixed buffers (signal-safe
  integer formatting, no `snprintf`/`std::string`);
- `open` the temp with `O_WRONLY | O_CREAT | O_EXCL | O_CLOEXEC` (exclusive create
  so a collision never truncates an existing report; `O_CLOEXEC` avoids fd leaks);
- on success `link` the temp to the final name then `unlink` the temp; the old
  precomputed candidates are unlinked separately. Deletion is *intentional only*:
  completed reports are removed by retention, never by the write path overwriting.

Because the final report is a *new* file (not a replacement), there can briefly be
`N+1` completed reports between link and candidate deletion.

## Retention model

`GetRetentionMode` maps `maxFileCount` to one mode once, so the scan does not
re-test sentinels per entry:

- **Unlimited (`-1`):** keep every completed report; still clean stale temps.
- **CleanupOnly (`0`):** delete all completed reports + stale temps, then disable
  new writes for the process lifetime.
- **Bounded (`>0`):** keep at most `maxFileCount`; precompute deletions beyond
  `maxFileCount - 1`, reserving one slot for the imminent report.

Candidates are precomputed at init (not enumerated in the signal handler) because
directory enumeration and sorting are not signal-safe.

## Key decisions and rationale

- **Dedicated app-local directory instead of inferring from `DbgMiniDumpName`.**
  A managed directory gives a clear ownership boundary for retention.
- **Opt-in via explicit root.** A default directory + automatic file creation
  would implicitly turn on persistent files; we avoid that.
- **Timestamp filenames over Android-style fixed slots + `mtime`.** Robust to
  copy/touch/restore that would scramble `mtime`; ordering comes from the name.
- **Exclusive create (no `O_TRUNC`).** A PID/timestamp collision, stale file, or
  test mistake must never silently erase a completed report.
- **Init-time candidate caching in fixed buffers.** Keeps the crash path
  signal-safe and free of malloc/heap state that may be corrupted at crash time.
- **`link` + `unlink` commit (not `rename`).** Both are POSIX async-signal-safe;
  `rename` is not formally on the AS-safe list across bionic/Darwin.
- **pid in temp name + liveness-gated stale cleanup.** Lets multiple processes
  share a root without deleting each other's in-flight temp files.

## Cross-process considerations

Retention is best-effort when several processes share one root: each caches its
own delete candidates at init, so the directory may transiently hold more than
`maxFileCount` reports. The app-name subdirectory and pid-tagged temps prevent the
dangerous cases (deleting a live writer's temp, or one app pruning another's
reports). Strict global retention would require cross-process locking, which is a
non-goal here.

