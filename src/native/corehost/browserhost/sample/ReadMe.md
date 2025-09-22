# Demo files

This is simple demo customer code that uses dotnet.js

Easiest way is to test it is:
- copy this and other assemblies into `artifacts/bin/coreclr/browser.wasm.Debug/corehost`
- edit `dotnet.boot.js` to match your assemblies
- run `dotnet-serve --directory artifacts/bin/coreclr/browser.wasm.Debug/corehost` and point your browser
- or run `node ./main.mjs` in that directory.