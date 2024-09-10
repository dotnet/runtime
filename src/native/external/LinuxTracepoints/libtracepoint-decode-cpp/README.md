# libtracepoint-decode-cpp

C++ library for decoding tracepoints and perf.data files.
Works on Linux or Windows.

- **[PerfDataFile.h](include/tracepoint/PerfDataFile.h):**
  Splits a `perf.data` file into events.
- **[PerfEventInfo.h](include/tracepoint/PerfEventInfo.h):**
  Structures for sample and non-sample events.
- **[PerfEventMetadata.h](include/tracepoint/PerfEventMetadata.h):**
  Metadata parsing for ftrace-style tracepoint decoding information.
