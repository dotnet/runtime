# Memory snapshot of the Mono runtime

We take snapshot of WASM memory after first cold start at run time​.
We store it on the client side in the browser cache.
For subsequent runs with the same configuration and same assets, we use the snapshot
instead of downloading everything again and doing the runtime startup again.
These subsequent starts are significantly faster.

### Implementation details

- the consistency of inputs (configuration and assets) with the snapshot is done by calculating SHA256 of the inputs.
    - the DLLs and other downloaded assets each have SHA256 which is used to validate 'integrity' by the browser.
    - the mono-config has field `assetsHash` which is summary SHA256 of all the assets.
    - the configuration could be changed programmatically and so we calculate the hash at the runtime, just before taking snapshot.
- the snapshot is taken just after we initialize the Mono runtime.
    - before cwraps are initialized (they would be initialized again on subsequent start).
    - after loading all the DLLs into memory.
    - after loading ICU and timezone data.
    - after applying environment variables and other runtime options.
    - before any worker threads initialization.
    - before any JavaScript interop initialization.
    - before any Managed code executes.
    - therefore we do not expect to store any application state in the snapshot.

### How to opt out
You can turn this feature of by calling `withStartupMemoryCache (false)` on [dotnet API](https://github.com/dotnet/runtime/blob/main/src/mono/wasm/runtime/dotnet.d.ts).

