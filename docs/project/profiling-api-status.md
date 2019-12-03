# Status of CoreCLR Profiler APIs

The .NET Core project started with the codebase from the desktop CoreCLR/Silverlight so all the profiler APIs present there are also present in the code here. This is the status of our testing and porting efforts for these APIs.

## Platform test coverage

- Windows on x86/x64/arm32
- Linux on x86/x64/arm32
- OSX

## Known issues

### DoStackSnapshot

The implementation of this API was making some questionable assumptions about Windows OS API behavior in order to walk callstacks asynchronously. When operating in this async mode we aren't yet confident we can produce reasonable implementations for other platforms. Our understanding is that most users of this API are attempting to do sample based profiling. If so we think it may be easier to offer a runtime provided event stream of sample callstacks to accomplish the same scenario without needing the API, but we also haven't heard any demand for it. Feedback welcome!

### ReJIT on ARM

ReJIT feature is only available on x86/x64 for now.

### Profiler Attach/Detach

We only support launch at the moment, see https://github.com/dotnet/coreclr/issues/16796

### Any issues we missed?

Please let us know and we will get it addressed. Thanks!
