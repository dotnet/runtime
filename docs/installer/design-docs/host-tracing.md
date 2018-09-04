# Host tracing

The various .NET Core host components provide detailed tracing of diagnostic information which can help solve issues around runtime, framework and assembly resolution and others.  

## Existing support
Currently (as of .NET Core 2.1) the host tracing is only written to the `stderr` output of the process. It can be turned on by setting `COREHOST_TRACE=1`.

## Proposed changes
For .NET Core 3 the proposal is to keep the existing behavior as described above and add two additional ways to capture the tracing:
* Redirect the trace to a file instead - this would be done by setting `COREHOST_TRACE=1` and also `COREHOST_TRACEFILE=<path>` in the environment for the process.
* For custom host support a way to capture the trace output in code. This will be done by adding `*_set_trace_listener` functions to `hostfxr` and `hostpolicy` which would let the custom host intercept all tracing.

## Components involved
Host tracing spans various components:
* `dotnet` or `apphost` executable
* `hostfxr` library
* `hostpolicy` library

If a custom host is used it will typically replace the executables and possible even some of the libraries. The custom host can use either `hostfx` or `hostpolicy` directly. So each of these components must support tracing on its own along with the configuration. In case several of these components are used together, they must cooperate and "share" the tracing setup.

Currently (as of .NET Core 2.1) all these components include the same code for tracing but act completely independently. VIa the fact that they share the same code, the end behavior is that they all write their trace output to the same location `stderr`. The only synchronization currently implemented is that each component fully flushes all internal tracing buffers before transferring control to a different component.

## New trace routing
By default tracing is disabled and is not routed anywhere. If it's enabled it is routed through an instance of `host_trace_listener` interface.
``` C++
struct host_trace_listener
{
    void trace_verbose(const pal::char_t *message);
    void trace_info(const pal::char_t *message);
    void trace_warning(const pal::char_t *message);
    void trace_error(const pal::char_t *message);
    void flush();
}
```

The `message` parameter is a standard `NUL` terminated string.

The host components provide two built in implementations of this interface:
* `stderr_trace_listener` which implements the .NET Core 2.1 behavior of routing tracing to the `stderr` output of the process. This is the default listener which is activated by setting `COREHOST_TRACE=1` environment variable.
* `file_trace_listener` which routes tracing into a file. This listener is activated by setting the `COREHOST_TRACE=1` and also providing the full path to the trace file in `COREHOST_TRACEFILE=<path>` environment variable.

Custom host can provide its own implementation of `host_trace_listener` by calling:
``` C++
void hostfxr_set_trace_listener(host_trace_listener* listener)
```
from the `hostfxr` component or
``` C++
void corehost_set_trace_listener(host_trace_listener* listener)
```
from the `hostpolicy` component.

The functions behave exactly the same in both components. The `listener` parameter can be:
* a pointer to an implementation of `host_trace_listener` which is then registered the only listener for all tracing.
* `NULL` value which unregisters any previously registered listener. After this call tracing is disabled.

Custom host can and should register the trace listener as the first thing it does with the respective host component to ensure that all tracing is routed to it.  

Only one trace listener can be registered at any given time.  

Registering custom trace listener or setting it to `NULL` overrides any tracing enabled by environment variables.

The `hostfxr` component will propagate the trace listener to the `hostpolicy` component before it calls into it. So custom host only needs to register its trace listener with the `hostfxr` component and not both. The propagation of the trace listener is only done for the duration necessary after which it will be unregistered again. So custom host might need to register its own listener if it makes calls directly to `hostpolicy` on top of the calls to `hostfxr`.

## Future investments
### Trace content
Currently the host components tend to trace a lot. The trace contains lot of interesting information but it's done in a very verbose way which is sometimes hard to navigate. Future investment should look at the common scenarios which are using the host tracing and optimize the trace output for those scenarios. This doesn't necessarily mean decrease the amount of tracing, but possibly introduce "summary sections" which would describe the end result decisions for certain scenarios.

### Interaction with other diagnostics in the .NET Core
The host tracing covers several areas which are interesting for diagnosing common failure patterns:
* Finding and starting the runtime
* Resolving frameworks
* Resolving assemblies (and native libraries)

Especially resolution of assemblies is tightly coupled with assembly binder behavior at runtime. As of now there's no correlation or cooperation between the host tracing and the runtime diagnostics (exceptions, logging) in the assembly binder. Future improvements might investigate ways to introduce some amount of cooperation to make the various diagnostic techniques easier to use together.