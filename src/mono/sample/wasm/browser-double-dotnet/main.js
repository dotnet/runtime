// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet as dotnet1, exit } from './dotnet.js?1'
import { dotnet as dotnet2 } from './dotnet.js?2'

async function bindIncrement(index, runtime) {
    const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
    const increment = exports.Sample.Test.Increment;

    const out = document.getElementById("out" + index);
    document.getElementById("button" + index).addEventListener("click", () => out.innerHTML = increment());
}

try {
    const runtime1 = await dotnet1.create();
    const runtime2 = await dotnet2.create();

    await bindIncrement("1", runtime1);
    await bindIncrement("2", runtime2);
}
catch (err) {
    exit(2, err);
}