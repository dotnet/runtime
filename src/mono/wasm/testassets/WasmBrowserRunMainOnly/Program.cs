using System;

TestOutput.WriteLine("Hello from WasmBrowserRunMainOnly!");

// TODO-WASM: CoreCLR currently doesn't exit from Main
Console.WriteLine("WASM EXIT 0");
return 0;