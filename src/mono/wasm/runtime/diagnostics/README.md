# Diagnostic Server and EventPipe

What's in here:

- `index.ts` toplevel APIs
- `browser/` APIs for the main thread. The main thread has 2 responsibilities:
  - control the overall diagnostic server `browser/controller.ts`
- `server_pthread/` A long-running worker that owns the WebSocket connections out of the browser to th ehost and that receives the session payloads from the streaming threads.  The server receives streaming EventPipe data from
EventPipe streaming threads (that are just ordinary C pthreads) through a shared memory queue and forwards the data to the WebSocket.  The server uses the [DS binary IPC protocol](https://github.com/dotnet/diagnostics/blob/main/documentation/design-docs/ipc-protocol.md) which repeatedly opens WebSockets to the host.
- `shared/` type definitions to be shared between the worker and browser main thread
- `mock/` a utility to fake WebSocket connectings by playing back a script.  Used for prototyping the diagnostic server without hooking up to a real WebSocket.
