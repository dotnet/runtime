// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

TestOutput.WriteLine("Hello from WasmBrowserRunMainOnly!");

// TODO-WASM: CoreCLR currently doesn't exit from Main
Console.WriteLine("WASM EXIT 0");
return 0;
