# Diagnostic Server and EventPipe

What's in here:

- `index.ts` toplevel APIs
- `browser/` APIs for the main thread. The main thread has 2 responsibilities:
  - control the overall diagnostic server `browser/controller.ts`
- `server_pthread/` A long-running worker that owns the WebSocket connections out of the browser to the host and that receives the session payloads from the streaming threads.
  The server receives streaming EventPipe data from EventPipe streaming threads (that are just ordinary C pthreads) through a shared memory queue and forwards the data to the WebSocket.
  The server uses the [DS binary IPC protocol](https://github.com/dotnet/diagnostics/blob/main/documentation/design-docs/ipc-protocol.md) which repeatedly opens WebSockets to the host.
- `shared/` type definitions to be shared between the worker and browser main thread
- `mock/` a utility to fake WebSocket connections by playing back a script.  Used for prototyping the diagnostic server without hooking up to a real WebSocket.

## Mocking diagnostics clients

In `diagnostics/mock` we provide a framework for mocking a connection between the diagnostic server and a diagnostic client.
Instead of creating tests around a real WebSocket connection to `dotnet-dsrouter` and a tool such as `dotnet-trace collect`, we
can simulate a connection by playing back a script.  The script represents the commands and responses of a client such as `dotnet-trace` that is connected to `dotnet-dsrouter`.

Build the runtime with `/p:MonoDiagnosticsMock=true`.

To use mocking:

1. create a `mock.js` file in your project,

2. add it using `<WasmExtraFilesToDeploy Include="mock.js" />` to your `.csproj`

3. configure the diagnostics server with a `mock:relative_url_of/mock.js`

   ```xml
          <WasmExtraConfig Include="diagnosticOptions" Value='
    {
      "server": { "suspend": false, "connectUrl": "mock:./mock.js" }
    }' />
   ```

4. The file `mock.js` should be an ES6 module with a default export like this:

   ```js
      function script (env) {
        return [
            async (conn) => { /* script for 1st call to "WebSocket.open" */ },
            async (conn) => { /* script for 2nd call to "WebSocket.open" */ },
            /* etc */
        ]
      }
    export default script;
   ```

### Mock environment

The mock environment parameter `env` (of type `MockEnvironment` defined in [./mock/index.ts](./mock/index.ts)) provides
access to utility functions useful for creating mock connection scripts.

It includes:

- `createPromiseController` - this is defined in [../promise-controller.ts](../promise-controller.ts).

### Mock connection

The mock script should return an array of functions `async (connection) => { ... }` where each function defines the interaction with one open WebSocket connection. Each function should return `Promise<void>`.

The connection object (of type `MockScriptConnection` defined in [./mock/index.ts](./mock/index.ts) has the following methods:

- `waitForSend (filter: (data: string | ArrayBuffer) => boolean): Promise<void>` or `waitForSend<T>(filter: (data: string | ArrayBuffer) => boolean, extract: (data: string | ArrayBuffer) => T): Promise<T>`.  Waits until the diagnostic server sends a single message with data that is accepted by `filter` (note the mocking doesn't support aggregating multiple partial replies).  If the `filter` returns a falsy value, the mock script will throw an error.  If the `filter` returns a truthy value and there is an `extract` argument given, the data will be passed to `extract` and the returned promise will be resolved with that value.  (This is useful for returning EventPipe session IDs, for example).

- `reply(data: string | ArrayBuffer): void` sends a reply back to the diagnostic server.  This can be anything, but should usually be a diagnostic server IPC protocol command

### Mock example

See [browser-eventpipe](../../../sample/wasm/browser-eventpipe/)
