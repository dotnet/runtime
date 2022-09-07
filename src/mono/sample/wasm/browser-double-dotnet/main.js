// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet as dotnet1, exit } from './dotnet.js?1'
import { dotnet as dotnet2 } from './dotnet.js?2'

async function createDotnet(builder) {
    const { getAssemblyExports, getConfig } = await builder.create();

    const config = getConfig();
    const exports = await getAssemblyExports(config.mainAssemblyName);
    return exports.Sample.Test.Increment;
}

function bindIncrement(index, increment) {
    const out = document.getElementById("out" + index);
    document.getElementById("button" + index).addEventListener("click", e => out.innerHTML = increment());
}

try {
    const increment1 = await createDotnet(dotnet1);
    const increment2 = await createDotnet(dotnet2);

    bindIncrement("1", increment1);
    bindIncrement("2", increment2);
}
catch (err) {
    exit(2, err);
}