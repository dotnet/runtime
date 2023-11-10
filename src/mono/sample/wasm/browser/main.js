// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

setInterval(() => {
    console.log("UI thread is alive!");
}, 1000);

try {
    await dotnet
        .withElementOnExit()
        .create();

    await dotnet.run();
}
catch (err) {
    exit(2, err);
}