# Host tracing

The various .NET Core host components provide detailed tracing of diagnostic information which can help solve issues around runtime, framework and assembly resolution and others.

## Existing support
Currently (as of .NET Core 2.1) the host tracing is only written to the `stderr` output of the process. It can be turned on by setting `COREHOST_TRACE=1`.

## Proposed changes
For .NET Core 3 the proposal is to keep the existing behavior as described above and add two additional ways to capture the tracing:
* Redirect the trace to a file (always appends) - this would be done by setting `COREHOST_TRACE=1` and also `COREHOST_TRACEFILE=<path>` in the environment for the process.
* Add a knob to control trace verbosity `COREHOST_TRACE_VERBOSITY`.  If unset tracing will be exhaustive.  When set in the range 1-4 the tracing verbosity will increase with an increase in the value of `COREHOST_TRACE_VERBOSITY`.
  * `COREHOST_TRACE_VERBOSITY=1 will show errors
  * `COREHOST_TRACE_VERBOSITY=2 will show errors and warnings
  * `COREHOST_TRACE_VERBOSITY=3 will show errors, warnings, and info
  * `COREHOST_TRACE_VERBOSITY=4 will show errors, warnings, info, and verbose.
* For custom host support a way to capture the trace output in code. This will be done by adding `*_set_trace_listener` functions to `hostfxr` and `hostpolicy` which would let the custom host intercept all tracing.

This is directly related to #4455.
Also solution for #4112 might eventually use these mechanisms.

## Components involved
Host tracing spans various components:
* `dotnet` or `apphost` executable
* `hostfxr` library
* `hostpolicy` library

If a custom host is used it will typically replace the executables and possible even some of the libraries. The custom host can use either `hostfx` or `hostpolicy` directly. So each of these components must support tracing on its own along with the configuration. In case several of these components are used together, they must cooperate and "share" the tracing setup.

Currently (as of .NET Core 2.1) all these components include the same code for tracing but act completely independently. The fact that all the components share the same code produces the end behavior that the components all write their trace output to the same location (that is `stderr`). The only synchronization currently implemented is that each component fully flushes all internal tracing buffers before transferring control to a different component.

## New trace routing
The host components implement two routes for tracing:
* Tracing to `stderr` (this is the .NET Core 2.1 behavior). This default route is activated by setting the `COREHOST_TRACE=1` environment variable.
* Tracing to a file (new in .NET Core 3). This route is activated by setting the `COREHOST_TRACE=1` and also providing the full path to the trace file in `COREHOST_TRACEFILE=<path>` environment variable. This overrides the `stderr` route, so if the file tracing is enabled, traces will not be written to `stderr`.  Instead traces will be appended to the specified file.

Custom host can enable a third route which passes tracing into a registered trace listener. The custom host does this by implementating `host_trace_listener` interface and registering it through:
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

Registering custom trace listener or setting it to `NULL` doesn't override the tracing enabled by environment variables. If a trace listener is registered and the `COREHOST_TRACE=1` is set as well, the traces will be routed to both the `stderr` as well as the registered listener.

The `hostfxr` component will propagate the trace listener to the `hostpolicy` component before it calls into it. So custom host only needs to register its trace listener with the `hostfxr` component and not both. The propagation of the trace listener is only done for the duration necessary after which it will be unregistered again. So custom host might need to register its own listener if it makes calls directly to `hostpolicy` on top of the calls to `hostfxr`.
In case of new (.NET Core 3) `hostfxr` component which would call into an old (.NET Core 2.1) `hostpolicy` component, the `hostfxr` will not perform the propagation in any way since the older `hostpolicy` doesn't support this mechanism.

The trace listener interface looks like this:
``` C++
struct host_trace_listener
{
    void trace_verbose(const pal::char_t *message, const pal::char_t *activityId);
    void trace_info(const pal::char_t *message, const pal::char_t *activityId);
    void trace_warning(const pal::char_t *message, const pal::char_t *activityId);
    void trace_error(const pal::char_t *message, const pal::char_t *activityId);
    void flush();
}
```

The `message` parameter is a standard `NUL` terminated string and it's the message to trace with the respective verbosity level.
The `activityId` parameter is a standard `NUL` terminated string. It's used to correlate traces for a given binding event. The content of the string is not yet defined, but the trace listeners should consider it opaque. Trace listeners should include this string in the trace of the message in some form. The parameter may be `NULL` in which case the trace doesn't really belong to any specific binding event.

Methods on the trace listener interface can be called from any thread in the app, and should be able to handle multiple calls at the same time from different threads.

## Future investments
### Trace content
Currently the host components tend to trace a lot. The trace contains lot of interesting information but it's done in a very verbose way which is sometimes hard to navigate. Future investment should look at the common scenarios which are using the host tracing and optimize the trace output for those scenarios. This doesn't necessarily mean decrease the amount of tracing, but possibly introduce "summary sections" which would describe the end result decisions for certain scenarios.
It would also be good to review the usage of verbose versus info tracing and make it consistent.

### Interaction with other diagnostics in the .NET Core
The host tracing covers several areas which are interesting for diagnosing common failure patterns:
* Finding and starting the runtime
* Resolving frameworks
* Resolving assemblies (and native libraries)

Especially resolution of assemblies is tightly coupled with assembly binder behavior at runtime. As of now there's no correlation or cooperation between the host tracing and the runtime diagnostics (exceptions, logging) in the assembly binder. Future improvements might investigate ways to introduce some amount of cooperation to make the various diagnostic techniques easier to use together.
