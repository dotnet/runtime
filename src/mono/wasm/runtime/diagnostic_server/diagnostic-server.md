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

- The diagnostic worker needs to be able to send commands to the runtime.  Or to directlly start sessions.
- The runtime needs to be able to send events from (some? all?) thread to the server.
- The diagnostic worker needs to be able to notify the runtime when the connection is closed. Or to directly stop sessions.

## Make the diagnostic Worker a pthread

ok, so if we make the diagnostic server a pthread, what would that look like:

Early during runtime startup if the appropriate bits are set, we will call into the runtime to make us a diagnostic pthread (which will use `emscripten_exit_with_live_runtime` to immediately return to JS and do everything else in an event-based way).

The problem is if the diagnostic URL has a "suspend" option, the Worker should wait in JS for a resume command and then post a message back to the main thread to resume.

So we need to create a promise in the main thread that becomes resolved when we receive some kind of notification from the worker.  We could use the `Atomics.waitAsync` to make a promise on the main thread that will resolve when the memory changes.

## DS IPC stream

the native code for an EP session uses an `IpcStream` object to do the actual reading and writing which has a vtable of callbacks to provide the implementation.
We can use the `ds-ipc-pal-socket.c` implementation as a guide - essentially there's an `IpcStream` subclass that
has a custom vtable that has pointers to the functions to call.

Once we're sending the actual payloads, we can wrap up the bytes in a JS buffer and pass it over to websocket.

For this to work well we probably need the diagnostic Worker to be a pthread?

It would be nice if we didn't have to do our own message queue.

## Make our own MessagePort

the emscripten onmessage handler in the worker errors on unknown messages.  the emscripten onmessage handler in the main thread ignores unknown messages.

So when mono starts a thread it can send over a port to the main thread.

then the main thread can talk to the worker thread.

There's a complication here that we need to be careful because emscripten reuses workers for pthreads.  but if we only ever talk over the dedicated channel, it's probably good.

Emscripten `pthread_t` is a pointer. hope...fully... they're not reused.

Basically make `mono_thread_create_internal` run some JS that

## TODO

- [browser] in dotnet preInit read the config options. extract websocket URL and whether to suspend.
- [browser] call down to C to start diagnostics
- [browser] create a pthread and have it call the JS diagnostic server start and then set it to not die after thread main returns. return pthread id to JS.
- [server_worker] start listening on the URL.
- [browser] if suspending, listen for a continue event from the JS and await for that to resolve.
- [server_worker] when there's a session start event, if we were suspended, add a pending session, and send a continue event.
- [server_worker] wait for a "runtime started" event?
- [server_worker] when there's a new session start event, call down to C to start and EP session.  **FIXME** need a port at this point -
- [browser] in early startup callback, start any pending EP sessions (and create ports for them to the diagnostic server)
- [session_streamer] call out to JS to post messages to the diagnostic server.
- [browser] in C, fire events, which will wake up the session streamer
- [session_streamer] post more messages

So the tricky bit is that for startup sessions and for "course of running" sessions, we need the browser thread to originate the message port transfer.  (hopefully queuing work on the main thread is good enough?)

Also the streamer thread probably needs to do a bunch of preliminary work in asynchronous JS before it can begin serving sessions.
