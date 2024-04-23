// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'

const { setModuleImports, getAssemblyExports, getConfig, INTERNAL } = await dotnet
    .withConsoleForwarding()
    .withElementOnExit()
    .withExitCodeLogging()
    .withExitOnUnhandledError()
    .withRuntimeOptions(['--interp-pgo-logging'])
    .withInterpreterPgo(true)
    .create();

setModuleImports('main.js', {
    window: {
        location: {
            href: () => globalThis.window.location.href
        }
    }
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

const query = new URLSearchParams(location.search);
const iterationCount = query.get('iterationCount') ?? 70;

console.log(`WASM debug level ${getConfig().debugLevel}`);
let text = '';
for (let i = 0; i < iterationCount; i++) { 
    text = exports.Interop.Greeting(); 
};
await INTERNAL.interp_pgo_save_data();

await dotnet.run();