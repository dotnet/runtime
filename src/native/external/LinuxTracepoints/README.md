# Libraries for Linux Tracepoints and user_events

This repository contains C/C++ libraries for collecting and decoding
[Linux Tracepoint](https://www.kernel.org/doc/html/latest/trace/tracepoints.html)
events and for generating Tracepoint events from user mode using the
[user_events](https://docs.kernel.org/trace/user_events.html) facility.

Related repositories:

- [LinuxTracepoints-Net](https://github.com/microsoft/LinuxTracepoints-Net) -
  .NET libraries and tools for decoding perf.data files, including `eventheader`
  events.
- [LinuxTracepoints-Rust](https://github.com/microsoft/LinuxTracepoints-Rust) -
  Rust libraries for generating Tracepoint events from user mode using the
  [user_events](https://docs.kernel.org/trace/user_events.html) facility

## Overview

- [libtracepoint](libtracepoint) -
  low-level C/C++ tracing interface. Designed to support replacement at
  link-time if a different implementation is needed (e.g. for testing).

  - [tracepoint-provider.h](libtracepoint/include/tracepoint/tracepoint-provider.h) -
    a developer-friendly C/C++ API for writing tracepoint events to any
    implementation of the `tracepoint.h` interface.
  - [tracepoint.h](libtracepoint/include/tracepoint/tracepoint-provider.h) -
    low-level interface for writing tracepoint events.
  - [libtracepoint.a](libtracepoint/src/tracepoint.c) -
    default implementation that writes directly to the Linux `user_events` facility.

- [libtracepoint-control-cpp](libtracepoint-control-cpp) -
  C++ library for controlling a tracepoint event collection session.

  - `TracingSession.h` implements an event collection session that can
    collect tracepoint events and enumerate the events that the session has
    collected. Supports real-time and circular-buffer modes.
  - `TracingPath.h` has functions for finding the `/sys/kernel/tracing`
    mount point and reading `format` files.
  - `TracepointSpec.h` parses tracepoint event specifications for configuring
    a tracepoint collection session.
  - `TracingCache.h` implements a cache for tracking parsed `format` files
    based on system+name or by `common_type` id.

- [libtracepoint-decode-cpp](libtracepoint-decode-cpp) -
  C++ library for decoding tracepoints. Works on both Linux and Windows.

  - `PerfDataFile.h` defines the `PerfDataFile` class that decodes
    `perf.data` files.
  - `PerfEventInfo.h` defines the `PerfSampleEventInfo` and
    `PerfNonSampleEventInfo` structures for raw event information.
  - `PerfEventMetadata.h` defines classes for parsing ftrace event metadata
    information.

- [libeventheader-tracepoint](libeventheader-tracepoint) -
  `eventheader` envelope that supports extended attributes including severity
  level and optional field information (field types and field names).

  - [TraceLoggingProvider.h](libeventheader-tracepoint/include/eventheader/TraceLoggingProvider.h) -
    a developer-friendly C/C++ API for writing `eventheader`-encapsulated
    events to any implementation of the tracepoint interface.
  - [EventHeaderDynamic.h](libeventheader-tracepoint/include/eventheader/EventHeaderDynamic.h) -
    C++ API for writing runtime-defined `eventheader`-encapsulated events,
    intended for use as an implementation layer for a higher-level API like
    OpenTelemetry.

- [libeventheader-decode-cpp](libeventheader-decode-cpp) -
  C++ library for decoding events that use the `eventheader` envelope.
  - `EventEnumerator` class parses an event into fields.
  - `EventFormatter` class converts event data into a string.
  - `decode-perf` tool that decodes `perf.data` files to JSON.

## General Usage

- Configure a Linux system with the `user_events` feature enabled.

  - Supported on Linux kernel 6.4 and later.
  - Kernel must be built with `user_events` support (`CONFIG_USER_EVENTS=y`).
  - Must have either `tracefs` or `debugfs` mounted. For example, you might add
    the following line to your `/etc/fstab` file:
    `tracefs /sys/kernel/tracing tracefs defaults 0 0`
  - The user that will generate events must have `x` access to the `tracing`
    directory and `w` access to the `tracing/user_events_data` file. One
    possible implementation is to create a `tracers` group, then:
    - `chgrp tracers /sys/kernel/tracing`
    - `chgrp tracers /sys/kernel/tracing/user_events_data`
    - `chmod g+x /sys/kernel/tracing`
    - `chmod g+w /sys/kernel/tracing/user_events_data`

- Use one of the event generation APIs to write a program that generates events.

  - C/C++ programs can use
    [tracepoint-provider.h](libtracepoint/include/tracepoint/tracepoint-provider.h)
    to generate regular Linux Tracepoint events that are defined at compile-time.
    (Link with `libtracepoint`.)
  - C/C++ programs can use
    [TraceLoggingProvider.h](libeventheader-tracepoint/include/eventheader/TraceLoggingProvider.h)
    to generate eventheader-enabled Tracepoint events that are defined at
    compile-time. (Link with `libtracepoint` and `libeventheader-tracepoint`.)
  - C++ middle-layer APIs (e.g. an OpenTelemetry exporter) can use
    [EventHeaderDynamic.h](libeventheader-tracepoint/include/eventheader/EventHeaderDynamic.h)
    to generate eventheader-enabled Tracepoint events that are runtime-dynamic.
    (Link with `libtracepoint` and `libeventheader-tracepoint`.)
  - Rust programs can use
    [LinuxTracepoints-Rust](https://github.com/microsoft/LinuxTracepoints-Rust)
    to generate eventheader-enabled Tracepoint events.

- To collect events in a C++ program, use
  [libtracepoint-control-cpp](libtracepoint-control-cpp). Note that your
  program must run as a privileged user (`CAP_PERFMON` capability plus read access to
  `/sys/kernel/tracing/events`) because access to the event collection system is
  restricted by default.

- To collect events without writing C++ code, use the included
  [tracepoint-collect](libtracepoint-control-cpp/tools/tracepoint-collect.cpp) tool
  or the Linux [`perf`](https://www.man7.org/linux/man-pages/man1/perf.1.html) tool
  to collect events to a `perf.data` file, e.g.
  `tracepoint-collect -o File.perf user_events:MyEvent1 user_events:MyEvent2` or
  `perf record -o File.perf -k monotonic -e user_events:MyEvent1,user_events:MyEvent2`.
  Note that you must run the tool as a privileged user to collect events (`CAP_PERFMON`
  capability plus read access to `/sys/kernel/tracing/events`).

  - The `perf` tool binary is typically available as part of the `linux-perf`
    package (e.g. can be installed by `apt install linux-perf`). However, this
    package installs a `perf_VERSION` binary rather than a `perf` binary, so
    you will need to add an appropriate VERSION suffix to your `perf` commands
    or use a wrapper script.
  - To capture tracepoints using `perf`, you'll also need to install
    `libtraceevent`, e.g. `apt install libtraceevent1`.
  - The `linux-base` package installs a `perf` wrapper script that redirects to
    the version of `perf` that matches your current kernel (if present) so that
    you can run the appropriate version of `perf` without the VERSION suffix.
    This frequently doesn't work because the latest `perf` binary from `apt`
    doesn't always match the running kernel, so you may want to make your own
    wrapper script instead.
  - Note that for purposes of collecting events, it is usually not important
    for the version of the `perf` tool to match the kernel version, so it's
    ok to use e.g. `perf_5.10` even if you are running a newer kernel.

- Note that tracepoints must be registered before you can start collecting
  them. The `tracepoint-collect` tool has facilities to pre-register a user_events
  tracepoint. The `perf` command will report an error if the tracepoint is not yet
  registered.

  - You can usually register tracepoints by starting the program that generates
    them. Most programs will register all of their tracepoints when they start
    running. (They will usually unregister when they stop running.)
  - You can also use the
    [`tracepoint-register`](libtracepoint/tools/tracepoint-register.cpp)
    tool to pre-register an event so you can start collecting it before
    starting the program that generates it.
  - If writing your own event collection tool, you might do something similar
    in your tool to pre-register the events that you need to collect. For
    example, you might use the `PreregisterTracepoint` or
    `PreregisterEventHeaderTracepoint` methods of the `TracepointCache` class
    in [`libtracepoint=control`](libtracepoint-control-cpp).

- Use the [`decode-perf`](libeventheader-decode-cpp/tools/decode-perf.cpp)
  tool to decode the `perf.data` file to JSON text, or write your own decoding
  tool using [libtracepoint-decode-cpp](libtracepoint-decode-cpp) and
  `libeventheader-decode-cpp`.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit [https://cla.opensource.microsoft.com](https://cla.opensource.microsoft.com).

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
