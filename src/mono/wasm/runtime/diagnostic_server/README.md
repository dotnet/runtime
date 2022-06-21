# Diagnostic Server and EventPipe

What's in here:

- `browser/` APIs for the main thread. The main thread has 2 responsibilities:
  - control the overall diagnostic server `browser/controller.ts`
  - establish communication channels between EventPipe session streaming threads and the diagnostic server pthread
- `server_pthread/` A long-running worker that owns the WebSocket connections out of the browser and that receives the session payloads from the streaming threads.
- `pthread/` (**TODO* decide if this is necessary) APIs for normal pthreads that need to do things to diagnostics
- `shared/` type definitions to be shared between the worker and browser main thread
