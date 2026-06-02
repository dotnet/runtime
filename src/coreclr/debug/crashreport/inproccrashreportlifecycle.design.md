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

The lifecycle-managed root (`DOTNET_CrashReportRootPath`) is the **only** way to
enable JSON report files in the in-proc reporter. Without it the reporter still
runs, emitting compact console logs but writing no report file. The feature is
still in development, so it does not carry the out-of-proc `createdump`
`DbgMiniDumpName` template path; that subsystem is separate and untouched.

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
default of 32, temp-file-then-commit, bounded retention — but improve on the
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
| `DOTNET_CrashReportRootPath` | Activates file output. Absolute path to an existing directory under which the managed report directory is created. Must already be absolute — the runtime does **not** expand a leading `~` or environment variables. If it cannot be resolved or made writable, output is disabled and the failure is logged. |
| `DOTNET_CrashReportMaxFileCount` | Retention bound: the maximum number of completed reports to keep. Default **32**. Parsed with `strtol`; values that are non-numeric, have trailing junk, overflow, or fall outside `[1, INT32_MAX]` are rejected and fall back to the default (logged). |

On mobile the configured root is expected to live under the app's private
storage (for example `context.getFilesDir()` on Android CoreCLR app hosting, or
the app sandbox home on iOS), which makes the report directory per-app without the
lifecycle needing to encode the app identity in the path.

## On-disk layout and naming

```
<root>/.dotnet/crash-reports/report-<timestampNs>-<pid>.crashreport.json
```

- Reports are written directly under `<root>/.dotnet/crash-reports/`. There is no
  per-app subdirectory: on mobile the root is already app-private, so adding an
  app-name segment (and sanitizing it) bought nothing.
- `<timestampNs>` (nanoseconds from `clock_gettime(CLOCK_REALTIME)`) and `<pid>`
  order reports by intended crash time. Nanosecond resolution makes names unique
  without a suffix-retry loop — even back-to-back crashes in the same process get
  distinct names — so there is no `-<suffix>` component.
- The `.crashreport.json` extension is the canonical one already emitted by
  `createdump` and the existing reporter; external tooling recognizes it. The
  `report-` prefix is this feature's marker/parse anchor.
- In-progress reports use a `…/report-….crashreport.json.tmp` temp file.

## Two execution contexts

The class deliberately spans two contexts; every member documents which it obeys.

**Initialization path (`Initialize` and helpers).** Runs once at startup. May
allocate and call libc/filesystem APIs. Responsibilities:

1. Resolve/validate the root (require an absolute path), create
   `.dotnet/crash-reports` (each `mkdir` tolerates `EEXIST`; the final node is
   verified to be a directory).
2. Verify the directory permits create/rename/delete with a hidden probe file
   (`ProbeDirectoryWritable`), so the crash path never discovers an unusable
   directory. The probe paths live in local stack buffers because this runs only
   at init.
3. Scan the directory once (`PruneExistingReports`):
   - delete any leftover `.tmp` files unconditionally — a stray temp is from a
     previous, now-defunct run of this app (the directory is app-private and the
     writer renames its temp to the final name before returning), so no
     process-liveness probe is needed;
   - retain only the newest `maxFileCount` completed reports, unlinking older ones
     inline as it scans (a fixed `FileInfo[maxFileCount]` buffer allocated with
     `new(std::nothrow)`; OOM disables output gracefully);
   - if the directory is already at the bound, cache the single oldest retained
     report into `m_cachedOldestReport` so the crash path can unlink it before
     publishing a new one.

**Crash/signal path (`PrepareReportFile`, `FinishReportFile`, and helpers).**
Invoked from the signal handler; must be async-signal-safe and allocation-free:

- read a nanosecond timestamp via `clock_gettime(CLOCK_REALTIME)` (POSIX
  async-signal-safe; a failed read degrades to a zero timestamp rather than
  aborting the write) and build the temp + final paths into caller-provided fixed
  buffers (signal-safe integer formatting, no `snprintf`/`std::string`);
- unlink the cached over-retention report, if one was selected at init
  (`DeleteCachedReport`), so retention stays at the bound;
- `open` the temp with `O_WRONLY | O_CREAT | O_EXCL | O_CLOEXEC` (exclusive create
  so a collision never truncates an existing report; `O_CLOEXEC` avoids fd leaks);
- on success `rename` the temp to the final name. `rename` is a same-directory
  atomic commit and, unlike `link`, is permitted in the Android/iOS app-private
  storage sandboxes (hard links there are rejected with `EPERM`, which would
  silently lose every report). Deletion is *intentional only*: completed reports
  are removed by retention, never by the write path overwriting.

## Retention model

There is a single bounded retention mode: keep at most `maxFileCount` completed
reports. The configuration layer guarantees `maxFileCount >= 1`, so the scan does
not test sentinels per entry.

- At init, `PruneExistingReports` keeps the newest `min(total, maxFileCount)`
  completed reports and unlinks the rest. If the kept set is full
  (`keptCount == maxFileCount`), it caches the single oldest kept report.
- On a crash, the write path unlinks that cached oldest report (if any) before
  publishing the new one, so the directory stays at the bound. When the directory
  was below the bound, nothing is cached and the crash path performs no deletion —
  the new report simply fills an open slot.

This means the configured bound is enforced at startup regardless of whether a
crash follows: lowering `maxFileCount` between runs prunes the excess on the next
init, not only on the next crash. Directory enumeration and sorting happen at init
(not in the signal handler) because they are not signal-safe; only the single
precomputed unlink runs on the crash path.

## Key decisions and rationale

- **Lifecycle-managed root is the only file sink.** A managed directory gives a
  clear ownership boundary for retention; dropping the `DbgMiniDumpName` template
  path (which the in-proc reporter never relied on) removed template-expansion and
  hostname-caching logic the lifecycle does not need.
- **Opt-in via explicit root.** A default directory + automatic file creation
  would implicitly turn on persistent files; we avoid that.
- **Absolute root only, no `~`/`$HOME` expansion.** On mobile the integration
  layer supplies a concrete app-private path; doing shell-style expansion inside
  the runtime would add ambiguity for no real caller benefit.
- **Flattened layout (no per-app subdirectory).** The root is already app-private
  on mobile, so an app-name segment — and the path-sanitization it required —
  added complexity without isolating anything.
- **Nanosecond timestamp filenames over fixed slots + `mtime`, and no suffix
  retry.** Robust to copy/touch/restore that would scramble `mtime`; ordering
  comes from the name, and nanosecond resolution makes collisions vanishingly
  unlikely, so no disambiguating suffix or open-retry loop is needed.
- **Exclusive create (no `O_TRUNC`).** A timestamp/pid collision, stale file, or
  test mistake must never silently erase a completed report.
- **Init-time pruning + single cached oldest in fixed buffers.** Keeps the crash
  path signal-safe and free of malloc/heap state that may be corrupted at crash
  time, while still applying the bound eagerly at startup.
- **Same-directory `rename` commit (not `link` + `unlink`).** `rename` is POSIX
  async-signal-safe (per `signal-safety(7)`) and is the portable atomic-publish
  primitive. Hard links via `link` are rejected with `EPERM` in the Android and
  Apple mobile app-private storage sandboxes, which would silently lose every
  report — the precise failure this feature exists to prevent.
- **Unconditional stale-temp cleanup.** Because each app owns its report
  directory under private storage, a leftover `.tmp` can only come from a defunct
  run of the same app, so it is removed without a `kill(pid, 0)` liveness probe.

## Cross-process considerations

Retention is best-effort if several processes were ever to share one root: each
caches its own oldest-report deletion candidate at init, so the directory may
transiently hold more than `maxFileCount` reports. Strict global retention would
require cross-process locking, which is a non-goal here. In the intended mobile
deployment the root is app-private, so this is not a concern in practice.
