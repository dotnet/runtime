# Host tracing

The various .NET Core host components provide detailed tracing of diagnostic information which can help solve issues around runtime, framework and assembly resolution and others.  
In .NET Core 2.1 and below the tracing was only routed to stderr of the process which makes it hard to consume for custom hosts and in some cases even for direct command line usage. Below improvements are proposed for .NET Core 3.

## Components involved
Host tracing spans various components:
* `dotnet` or `apphost` executable
* `hostfxr` library
* `hostpolicy` library

If a custom host is used it will typically replace the executables and possible even some of the libraries. The custom host can use either `hostfx` or `hostpolicy` directly. So each of these components must support tracing on its own along with the configuration. In case several of these components are used together, they must cooperate and "share" the tracing setup.

## Trace routing
By default tracing is disabled and is not routed anywhere. If it's enabled it is routed through an instance of `host_trace_listener` interface.
```
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

Custom host can provide its own implementating of `host_trace_listener` by calling:
* `void hostfxr_set_trace_listener(host_trace_listener* listener)` from the `hostfxr` component.
* `void corehost_set_trace_listener(host_trace_listener* listener)` from the `hostpolicy` component.  

The functions behave exactly the same in both components. The `listener` parameter can be:
* a pointer to an implementation of `host_trace_listener` which is then registered the only listener for all tracing.
* `NULL` value which unregisters any previously registered listener. After this call tracing is disabled.

Custom host can and should register the trace listener as the first thing it does with the respective host component to ensure that all tracing is routed to it.  

Only one trace listener can be registered at any given time.  

Registering custom trace listener or setting it to `NULL` overrides any tracing enabled by environment variables.

The `hostfxr` component will propagate the trace listener to the `hostpolicy` component before it calls into it. So custom host only needs to register its trace listener with the `hostfxr` component and not both. The propagation of the trace listener is only done for the duration necessary after which it will be unregistered again. So custom host might need to register its own listener if it makes calls directly to `hostpolicy` on top of the calls to `hostfxr`.