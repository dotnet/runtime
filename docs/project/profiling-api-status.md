# Status of CoreCLR Profiler APIs

Below is a table of the version of CoreCLR that profiler support and testing was completed. Profiling may work prior to these versions, but there may be bugs and missing features.

|       | Windows | Linux | OSX |
| ----- | ------- | ----- | --- |
| x64   | 2.1     | 2.1   | 3.1 |
| x86   | 2.1     | N/A   | N/A |
| arm32 | 3.1     | 3.1   | N/A |
| arm64 | 3.1     | 3.1   | TBA |

## Known issues

### DoStackSnapshot

The implementation of this API was making some questionable assumptions about Windows OS API behavior in order to walk callstacks asynchronously. When operating in this async mode we aren't yet confident we can produce reasonable implementations for other platforms. Our understanding is that most users of this API are attempting to do sample based profiling. If so we think it may be easier to offer a runtime provided event stream of sample callstacks to accomplish the same scenario without needing the API, but we also haven't heard any demand for it. Feedback welcome!

### Any issues we missed?

Please let us know and we will get it addressed. Thanks!
