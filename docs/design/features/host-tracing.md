# Host tracing

The various .NET host components provide detailed tracing of diagnostic information which can help solve issues around runtime, framework and assembly resolution.

## Components involved

Host tracing spans various components:

* `dotnet` or `apphost` executable - uses `hostfxr`
* `nethost` library - no dependencies
* `hostfxr` library - uses `hostpolicy`
* `hostpolicy` library - no host dependencies (uses the runtime only)
* custom host - uses `hostfxr` and possibly `nethost`

The trace settings must be propagated across the component boundaries, such that all components have the same external behavior.

All these components (with the exception to custom host) include the same code for tracing but act mostly independently. The fact that all the components share the same code produces the end behavior that the components all write their trace output to the same location given the same environment. The only synchronization currently implemented is that each component fully flushes all internal tracing buffers before transferring control to a different component.

## Trace routing

Tracing is enabled by setting `COREHOST_TRACE=1` env. variable.

In .NET Core 2.1 and below, the host tracing is only written to the `stderr` output of the process.

Starting with .NET Core 3, tracing can be redirected and its verbosity controlled:

* Redirect the trace to a file (always appends) - enable tracing via `COREHOST_TRACE=1` and also set `COREHOST_TRACEFILE=<path>` in the environment for the process. The `<path>` is resolved against current directory and the file is opened for text append write. The directory for the file must exist, the file itself will be created if it doesn't exist.
* Control trace verbosity via `COREHOST_TRACE_VERBOSITY` env. variable.  If not set, tracing contains the maximum level of detail.  When set in the range 1-4, the tracing verbosity increases with an increase in the value of `COREHOST_TRACE_VERBOSITY`.
  * `COREHOST_TRACE_VERBOSITY=1` shows errors
  * `COREHOST_TRACE_VERBOSITY=2` shows errors and warnings
  * `COREHOST_TRACE_VERBOSITY=3` shows errors, warnings, and info
  * `COREHOST_TRACE_VERBOSITY=4` shows errors, warnings, info, and verbose. (currently the default and maximum level of detail)

## Error routing

The host components implement two routes for outputting errors:

* Errors go to `stderr`. This is the default route and is used by both the `dotnet` and `apphost` as well.
* Starting with .NET Core 3, a custom host can redirect errors to a registered error writer by implementing `hostfxr_error_writer_fn` callback and passing it to:

``` C++
void hostfxr_set_error_writer(hostfxr_error_writer_fn error_writer)
```

`hostpolicy` also exposes `corehost_set_error_writer(corehost_error_writer_fn error_writer)` which is used by the `hostfxr` to propagate the custom error writer to the `hostpolicy` when called.

The functions behave exactly the same in both components. The `error_writer` parameter can be:

* a function pointer which is then registered as the current error writer. Errors are only written to the registered error writer and will not be outputted to `stderr`.
* `NULL` value which unregisters the previously registered error writer. After this errors go to `stderr` again.

Custom host can and should set the error writer as the first thing it does with the respective host component to ensure that all errors are routed to it.

`hostfxr_set_error_writer` sets the error writer only for the current thread (the setting is thread local). The custom host must set it separately (and potentially with different callbacks) on each thread where wants to use `hostfxr` functions and with error redirection.

Only one error writer can be registered on a given thread at any given time.

All errors are also written to a trace output if one is enabled (via `COREHOST_TRACE=1`) regardless of which error routing is used.

The `hostfxr` component propagates the error writer to the `hostpolicy` component before it calls into it, so a custom host only needs to register its error writer with the `hostfxr` component. The propagation of the error writer is only done for the duration necessary, after which it will be unregistered again.
In case of a .NET Core 3+ `hostfxr` which would call into an old (.NET Core 2.1) `hostpolicy` component, the `hostfxr` will not perform the propagation in any way since the older `hostpolicy` doesn't support this mechanism.

The error writer callback is declared as:

```cpp
typedef void (__cdecl *error_writer_fn)(const pal::char_t* message);
```

The `message` parameter is a standard `NULL` terminated string. The memory for it is owned by the caller (so some of the hosting components, it may not be just `hostfxr`) and it's only valid for the duration of the call to the error writer callback.

## Implementation notes

Several components disable error writing to `stderr`. Note that enabling tracing via `COREHOST_TRACE=1` still works and without additional env. variables will output tracing, which includes all errors, into `stderr`.

### `nethost`

The `nethost` library intentionally disables error writing to `stderr` but also doesn't provide a way to register error writer. It is possible to add support for registering custom error writer in the future.

### `apphost`

`apphost` on Windows uses the `hostfxr_set_error_writer` to redirect error writing. The callback writes to `stderr` immediately and also caches the errors into a buffer.

In all cases the buffered error is written into Windows Event Log.

If it's a GUI app, the buffered error is used to show a user friendly dialog box.

### `ijwhost`

`ijwhost` intentionally disables error writing to `stderr`.

### `comhost`

`comhost` redirects error writing to a custom callback which buffers the errors. It will use the buffered error to set error info via `IErrorInfo` interface.

## Future investments

### Trace content

Currently the host components tend to trace a lot. The trace contains lot of interesting information but it's done in a very verbose way which is sometimes hard to navigate. Future investment should look at the common scenarios which are using the host tracing and optimize the trace output for those scenarios. This doesn't necessarily mean decrease the amount of tracing, but possibly introduce "summary sections" which would describe the end result decisions for certain scenarios.
It would also be good to review the usage of verbose versus info tracing and make it consistent.

### Interaction with other diagnostics in the .NET Core

The host tracing covers several areas which are interesting for diagnosing common failure patterns:

* Finding and starting the runtime
* Resolving frameworks
* Resolving assemblies (and native libraries)

Especially resolution of assemblies is tightly coupled with assembly binder behavior at runtime. As of now there's no correlation or cooperation between the host tracing and the runtime diagnostics (tracing, exceptions, logging) in the assembly binder. Future improvements might investigate ways to introduce some amount of cooperation to make the various diagnostic techniques easier to use together.
