// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'

await dotnet.create();

try {
    await dotnet.run();
    console.log("WASM EXIT 0");
} catch (err) {
    console.error(err);
    console.log("WASM EXIT 1");
}