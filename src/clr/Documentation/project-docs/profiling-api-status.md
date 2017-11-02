# Status of CoreCLR Profiler APIs

The .NET Core project started with the codebase from the desktop CoreCLR/Silverlight so all the profiler APIs present there are also present in the code here. This is the status of our testing and porting efforts for these APIs.

## Platform test coverage

Windows and Linux x86/x64 have been covered. Arm32 is in progress at this time, OSX coming soon (please let us know if you have a pressing issue)

## Known issues

### DoStackSnapshot

The implementation of this API was making some questionable assumptions about Windows OS API behavior in order to walk callstacks asynchronously. When operating in this async mode we aren't yet confident we can produce reasonable implementations for other platforms. Our understanding is that most users of this API are attempting to do sample based profiling. If so we think it may be easier to offer a runtime provided event stream of sample callstacks to accomplish the same scenario without needing the API, but we also haven't heard any demand for it. Feedback welcome!

### Profiler does not disable Concurrent GC

See github issue [#13153](https://github.com/dotnet/coreclr/issues/13153) for more details.

### Any issues we missed?

Please let us know and we will get it addressed. Thanks!