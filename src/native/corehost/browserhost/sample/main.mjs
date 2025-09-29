// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './dotnet.js'
import { config } from './dotnet.boot.js'


async function downloadConfig(url) {
    if (url.endsWith(".json")) {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`Failed to download config from ${url}: ${response.status} ${response.statusText}`);
        }
        const newConfig = await response.json();
        mergeLoaderConfig(newConfig);
    } else if (url.endsWith(".js") || url.endsWith(".mjs")) {
    }
}


try {
    await dotnet
        .withConfig(config)
        .run();
}
catch (err) {
    console.error(err);
    exit(2, err);
}
