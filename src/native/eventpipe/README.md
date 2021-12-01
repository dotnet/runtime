# EventPipe and DiagnosticServer

If there is information you wish was here and it isn't, please add it :)

## Overview

EventPipe is a cross-platform eventing library written in C with significant inspiration from ETW
on Windows. Previously when .NET primarily ran on Windows we relied solely on ETW, but now that
we run on multiple platforms we wanted to have cross platform logging supported directly in the
runtime. For more info see [the docs](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe).

DiagnosticServer is a simple RPC mechanism that allows external tools to communicate with the
runtime over named pipes and sockets. External tools send the runtime commands which the runtime
executes and responds to. Creating EventPipe logging sessions are one of the commands supported
over the RPC channel but there are also others entirely unrelated to EventPipe functionality.

## EventPipe concepts

EventPipe collects structured log messages from instrumented code, potentially buffers it, and then
emits it to a logging sink, typically a serialized file on disk, named pipe or socket. The schema for
each logged message is defined in an EventPipeEvent. EventPipeProviders act as namespaces for
EventPipeEvents. In order to receive log messages a telemetry consumer creates an EventPipeSession
which records the configuration about which EventPipeEvents should be enabled, which logging
sink should be used, and manages storage for intermediate storage.

A simple workflow looks like this:
1. Managed code creates an EventSource. The implementation of EventSource automatically creates
EventPipeProvider and EventPipeEvent objects that correspond to the EventSource object and the
individual logging methods on the EventSource. At this point nobody is listening so invoking
EventPipe WriteEvent() APIs don't record any data.

2. At some point a user uses dotnet-trace to send an IPC command to the runtime that creates
a new EventPipeSession. This session enables some providers specifying a level and keywords
which determine the events to enable. The session creates an EventPipeBufferManager to manage
the in-memory log buffering and starts a thread that will dequeue messages from the buffer
and serialize them to an outbound stream.

3. Now when EventSource APIs are called, WriteEvent() serializes a buffer of data and saves it
in the session's buffer. Asynchronously a session specific thread dequeues it, formats it, and
writes it out to the IPC stream.

4. dotnet-trace listening at the other end of the IPC stream receives the log messages and
serializes them to disk. Later the user takes that file to Visual Studio or PerfView to visualize
the contents.


## Guide to the code

EventPipe was initially written in C++ for coreclr and then translated to C for use by both Mono and
CoreCLR runtimes. The code depends solely on the C runtime and a limited set of runtime specific
implementations for basic datatypes, locks, threads, etc. The set of functionality each runtime is
expected to provide is defined in ep-rt-* files (ep=EventPipe and rt=Runtime). Each runtime then
needs to compile a separate lib that implements this ABI and link it together. For example CoreCLR's
implementation is in ../../coreclr/vm/eventing/eventpipe and mono's implementation is in
../../mono/mono/eventpipe. Files starting with ep-* are the runtime neutral portions.

Files starting with ds-* are the runtime neutral implementation of DiagnosticServer. ds-rt-* files
are the runtime specific dependencies, following the same pattern used by EventPipe.

### Getters and Setters

The code uses macros to define getter and setter functions in ep-getter-setter.h. It maps like this:
```
typename_get_fieldname(instance) { return instance->fieldname; }
typename_get_fieldname_ref(instance) { return &(instance->fieldname); }
typename_get_fieldname_cref(const instance) { return &(instance->fieldname); }
typename_set_fieldname(instance, value) { instance->fieldname = value); }
```


### Datatypes

Many templated datatypes are defined implicitly using macros such as EP_RT_DEFINE_ARRAY().
These macros define a set of functions following a specific naming pattern and like all *rt*
functionality the expectation is that each runtime will implement it. These indirections make the
code a little harder to follow but you can decode it if you understand the mapping. The source is
always definitive but at this time here is mapping:

````
                 CoreCLR                                               Mono
ARRAY            CQuickArrayList                                       GArray
LIST             SList                                                 GList
QUEUE            SList                                                 GQueue
HASH_MAP         SHash<NoRemoveSHashTraits<MapSHashTraits<T1,T2>>>     GHashTable
HASH_MAP_REMOVE  SHash<MapSHashTraits<T1,T2>>                          GHashTable
````

Given some method such as ep_rt_thread_session_state_array_init() you can't find it directly in the
source because it is constructed by macros, however we can still track it down if we need to:
1. The naming convention is always ep_rt_datatype_func. Extract just the type
   "thread_session_state_array" and do a text search for it in the runtime specific source.
   You should find:
```
EP_RT_DEFINE_ARRAY (thread_session_state_array, ep_rt_thread_session_state_array_t, ep_rt_thread_session_state_array_iterator_t, EventPipeThreadSessionState *)
EP_RT_DEFINE_LOCAL_ARRAY (thread_session_state_array, ep_rt_thread_session_state_array_t, ep_rt_thread_session_state_array_iterator_t, EventPipeThreadSessionState *)
```
2. EP_RT_DEFINE_ARRAY and EP_RT_DEFINE_LOCAL_ARRAY are defined:
```
#define EP_RT_DEFINE_ARRAY(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_ARRAY_PREFIX(ep, array_name, array_type, iterator_type, item_type)
#define EP_RT_DEFINE_LOCAL_ARRAY(array_name, array_type, iterator_type, item_type) \
	EP_RT_DEFINE_LOCAL_ARRAY_PREFIX(ep, array_name, array_type, iterator_type, item_type)
```
3. Searching for these ARRAY_PREFIX and LOCAL_ARRAY_PREFIX macros we find that LOCAL_ARRAY_PREFIX defined the
init method (where the method name is constructed by the EP_RT_BUILD_TYPE_FUNC_NAME macro)
```
#define EP_RT_DEFINE_LOCAL_ARRAY_PREFIX(prefix_name, array_name, array_type, iterator_type, item_type) \
	static inline void EP_RT_BUILD_TYPE_FUNC_NAME(prefix_name, array_name, init) (array_type *ep_array) { \
		STATIC_CONTRACT_NOTHROW; \
	} \
```
In this case the init() method did nothing other than provide a placeholder for the static contract.
