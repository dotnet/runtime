# Diagnostic Server for .NET WebAssembly

The diagnostic server [IPC protocol](https://github.com/dotnet/diagnostics/blob/main/documentation/design-docs/ipc-protocol.md) can support "connect" mode and "listen" mode.  In listen mode the .NET runtime opens a socket and waits for connections.  This doesn't make a ton of sense in a websocket scenario (except maybe if the debugger is driving things?)

We will initially only support "connect" mode.  In connect mode the runtime must do the following in a loop:

    1. Open a socket to the server URL.
    2. Send an "advertise" request.
    3. Idle until the server responds with a command.
    4. Do two things:
        - Open a new socket send an advertise and go back to step 3.
        - Respond to the command on the existing socket and begin some kind of server action.
    5. If the remote end closes the socket, notify the runtime to stop the session.
    6. If the remote end closes the socket before the server sends a command, stop the server (?)

We will need a dedicated thread to handle the WebSocket connections.  This thread will need to be able to notify the runtime (or directly execute commands?)

## Implementation constraints

- The diagnostic worker needs to start before the runtime starts - we have to be able to accept connections in "wait to start" mode to do startup profiling.

- WebSocket JS objects are not transferable between WebWorkers.  So the diagnostic server worker needs to
forward streamed eventpipe data from the EventPipe session streaming thread to the WebSocket.

## Make the diagnostic Worker a pthread

ok, so if we make the diagnostic server a pthread, what would that look like:

Early during runtime startup if the appropriate bits are set, we will call into the runtime to make us a diagnostic pthread (which will use `emscripten_exit_with_live_runtime` to immediately return to JS and do everything else in an event-based way).

The problem is if the diagnostic URL has a "suspend" option, the Worker should wait in JS for a resume command and then post a message back to the main thread to resume.

One idea was to use a promise on the main thread to wait for the diagnostic server to signal us.  But that would be too early - before `mono_wasm_load_runtime` runs - and unfortunately the DS server needs to be able to create EventPipe session IDs before resuming the runtime.  If we could break up `mono_wasm_load_runtime` into several callees we could set up the runtime threading and minimal EventPipie initialization and then pause until the resume.

But instead right now we busy-wait in the main thread in `ds_server_wasm_pause_for_diagnostics_monitor`.  This at least processes the Emscripten dispatch queue (so other pthreads can do syscalls), but it hangs the browser UI.

## DS IPC stream

The native code for an EP session uses an `IpcStream` object to do the actual reading and writing which has a vtable of callbacks to provide the implementation.
We implement our own `WasmIpcStream` that has a 1-element single-writer single-reader queue so that synchronous writes from the eventpipe streaming threads wake the diagnostic server to pull the filled buffer
and send it over the websocket.
There's no particular reason why this has to be (1) synchronous, (2) 1-element.  Although that would make the implementation more complicated.  If there's a perf issue here we could look into something more sophisticated.
