# libtracepoint

`tracepoint.h` defines a low-level C/C++ tracing interface. The
[default implementation](src/tracepoint.c)
writes data to the Linux
[user_events](https://docs.kernel.org/trace/user_events.html) facility.
The implementation of this interface can be replaced at link time to support
alternative scenarios, e.g. testing with a mock tracing implementation.

- [samples/tracepoint-sample.c](samples/tracepoint-sample.c) -
  demonstrates basic usage of the interface.
- [tracepoint.h](include/tracepoint/tracepoint.h) -
  interface functions.
- [tracepoint-state.h](include/tracepoint/tracepoint-state.h) -
  interface data types.
- [tracepoint.c](src/tracepoint.c) -
  default implementation.
- [tracepoint-provider.h](include/tracepoint/tracepoint-provider.h) -
  high-level C/C++ API for writing tracepoint events to any implementation
  of the tracepoint interface.

`tracepoint.h` is a low-level interface. Application developers are more likely
to use a higher-level library implemented on top of this interface, such as
`tracepoint-provider.h`.

This library includes a `tracepoint-register` tool that can be used to
pre-register a `user_event` tracepoint. This allows you to start collecting a
trace before the corresponding scenario starts running.

Alternative implementations of this interface are expected, e.g. a mock
tracing implementation could be used for testing. A developer would
select the alternative implementation by linking against a different
library.

An alternative implementation that writes directly to a file (bypassing the
kernel's event routing and filtering) could be implemented as follows:

- [../libeventheader-tracepoint/samples/tracepoint-file.cpp](../libeventheader-tracepoint/samples/tracepoint-file.cpp)
