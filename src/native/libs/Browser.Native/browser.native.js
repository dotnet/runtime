// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

function captureEmscriptenInternals() {
    // WASMTODO capture emscripten internals and propagate to dotnet.runtime.js
}

const DotnetSupportLib = {
    $DOTNET: { captureEmscriptenInternals },
    "$DOTNET__postset": `DOTNET.captureEmscriptenInternals();`,
    BrowserNative_RandomBytes: function (bufferPtr, bufferLength) {
        // WASMTODO implementation
        return -1;
    }
}
autoAddDeps(DotnetSupportLib, "$DOTNET");
addToLibrary(DotnetSupportLib);
