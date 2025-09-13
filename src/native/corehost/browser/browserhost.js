// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ! this is temporary file until we setup proper build system for TypeScript

Module.preRun = () => {
    // copy all node/shell env variables to emscripten env
    for (const [key, value] of Object.entries(process.env)) {
        ENV[key] = value;
    }
    ENV["CWD"] = process.cwd();
};
