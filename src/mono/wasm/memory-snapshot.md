# Memory snapshot of the Mono runtime #

We take snapshot of WASM memory after first cold start at run time​.
We store it on the client side in the browser cache.
For subsequent start of the application with the same configuration and same assets, we could use the snapshot.
Instead of downloading all the assets and doing the runtime startup again.
Such subsequent start is significantly faster.

### Implementation details

- the consistency of inputs (configuration and assets) with the snapshot is done by calculating SHA256 of the inputs.
    - the DLLs and other downloaded assets each have SHA256 which is used to validate 'integrity' by the browser.
    - the mono-config has field `assetsHash` which is summary SHA256 of all the assets.
    - the configuration could be changed programmatically and se we calculate the hash at the runtime, just before taking snapshot.
- the snapshot is taken just after we initialize the Mono runtime
    - before cwraps are initialized (they would be initialized again on subsequent start)
    - after loading all the DLLs into memory.
    - after loading ICU and timezone data
    - after applying environment variables and other runtime options.
    - before any worker threads init
    - before any JavaScript interop initialization
    - before any Managed code initialization
    - therefore we do not expect to store any application state in the snapshot.

### How to opt out
 - you can use new API `withMemoryCache(false)` to opt out from the feature.

