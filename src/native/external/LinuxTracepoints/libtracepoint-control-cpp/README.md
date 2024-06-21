#  libtracepoint-control-cpp

- `TracepointSession.h` implements an event collection session that can
  collect tracepoint events and enumerate the events that the session has
  collected.
- `TracepointPath.h` has functions for finding the `/sys/kernel/tracing`
  mount point and reading `format` files.
- `TracepointName.h` represents a tracepoint name (system name + event
  name); for instance, `user_events/eventName`.
- `TracepointCache.h` implements a cache for tracking parsed `format` files
  and locating cached data by `TracepointName` or by `common_type` id.
