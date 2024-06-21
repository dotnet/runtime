// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'

const { setModuleImports, getAssemblyExports, getConfig, runMain } = await dotnet
    .withApplicationArguments("start")
    .create();

setModuleImports('main.js', {
    dom: {
        setInnerText: (selector, time) => document.querySelector(selector).innerText = time
    }
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

document.getElementById('reset').addEventListener('click', e => {
    exports.StopwatchSample.Reset();
    e.preventDefault();
});

const pauseButton = document.getElementById('pause');
pauseButton.addEventListener('click', e => {
    const isRunning = exports.StopwatchSample.Toggle();
    pauseButton.innerText = isRunning ? 'Pause' : 'Start';
    e.preventDefault();
});

// run the C# Main() method and keep the runtime process running and executing further API calls
await runMain();