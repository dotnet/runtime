# LinuxTracepoints Change Log

## v1.3.4 (TBD)

- libtracepoint-control: New `tracepoint-collect` tool that records tracepoint
  events into a perf.data file.
- libtracepoint-control: TracepointSession SavePerfDataFile adds a
  `PERF_RECORD_FINISHED_INIT` record to the generated perf.data file.
- libeventheader: tool `eventheader-register` deleted. Instead, use
  `tracepoint-register` from libtracepoint.

## v1.3.3 (2024-04-15)

- BUG FIX: EADDRINUSE returned during TraceLoggingRegister on newer kernels.
  The "name already in use" detection splits on whitespace, while all other
  processing splits on semicolon. Fix by adding space after each semicolon
  in `EVENTHEADER_COMMAND_TYPES`.
- libtracepoint-decode: In pipe mode, load event names at FinishedInit instead
  of HeaderLastFeature since not all traces emit HeaderLastFeature.
- libtracepoint-decode: Recognize files from LP32 systems as 32-bit.
- libtracepoint: new tool `tracepoint-register` for pre-registering
  tracepoints.
- libeventheader: existing tool `eventheader-register` is deprecated in
  favor of `tracepoint-register`.
- libeventheader-decode-dotnet: Moved to separate repository
  [LinuxTracepoints-Net](https://github.com/microsoft/LinuxTracepoints-Net).

## v1.3.2 (2024-02-27)

- Bug fix: Open `user_events_data` for `O_WRONLY` instead of `O_RDWR`.

## v1.3.1 (2024-01-11)

- `TracepointSession` supports per-CPU buffer sizes (including 0) to allow
  memory usage optimization when trace producers are known to be bound to
  specific CPUs.
- `TracepointSession` uses `PERF_ATTR_SIZE_VER3` for the size of
  `perf_event_attr` to minimize the chance of incompatibilities.

## v1.3.0 (2023-11-27)

- **Breaking changes** to `PerfDataFile`:
  - `dataFile.AttrCount()` method replaced by `EventDescCount()` method.
  - `dataFile.Attr(index)` method replaced by `EventDesc(index)` method.
    The returned `PerfEventDesc` object contains an `attr` pointer.
  - `dataFile.EventDescById(id)` method replaced by `FindEventDescById(id)`.
- **Breaking changes** to `PerfSampleEventInfo`:
  - `eventInfo.session` field renamed to `session_info`.
  - `eventInfo.attr` field replaced by `Attr()` method.
  - `eventInfo.name` field replaced by `Name()` method.
  - `eventInfo.sample_type` field replaced by `SampleType()` method.
  - `eventInfo.raw_meta` field replaced by `Metadata()` method.
- **Breaking changes** to `TracepointSession`:
  - `session.EnableTracePoint(...)` method renamed to `EnableTracepoint(...)`.
  - `session.DisableTracePoint(...)` method renamed to `DisableTracepoint(...)`.
- `EventFormatter` formats timestamps as date-time if clock information is
  available in the event metadata. If clock information is not present, it
  continues to format timestamps as seconds.
- `TracepointSession` provides `SavePerfDataFile(filename)` method to save
  the current contents of the session buffers into a `perf.data` file.
- `TracepointSession` now includes ID in default sample type.
- `TracepointSession` records clock information from the session.
- `TracepointSession` provides access to information about the tracepoints
   that have been added to the session (metadata, status, statistics).
- `PerfDataFile` decodes clock information from perf.data files if present.
- `PerfDataFile` provides access to more metadata via `PerfEventDesc` struct.
- `PerfDataFile` provides `EventDataSize` for determining the size of an event.
- New `PerfDataFileWriter` class for generating `perf.data` files.
- Changed procedure for locating the `user_events_data` file.
  - Old: parse `/proc/mounts` to determine the `tracefs` or `debugfs` mount
    point, then use that as the root for the `user_events_data` path.
  - New: try `/sys/kernel/tracing/user_events_data`; if that doesn't exist,
    parse `/proc/mounts` to find the `tracefs` or `debugfs` mount point.
  - Rationale: Probe an absolute path so that containers don't have to
    create a fake `/proc/mounts` and for efficiency in the common case.

## v1.2.1 (2023-07-24)

- Prefer `user_events_data` from `tracefs` over `user_events_data` from
  `debugfs`.

## v1.2 (2023-06-27)

- Added "Preregister" methods to the `TracepointCache` class so that a
  controller can pre-register events that it wants to collect.
- If no consumers have enabled a tracepoint, the kernel now returns `EBADF`.
  The provider APIs have been updated to be consistent with the new behavior.

## v1.1 (2023-06-20)

- Add namespaces to the C++ APIs.
- Move non-eventheader logic from eventheader-decode to new tracepoint-decode
  library.
- Add new libtracepoint-control library.
