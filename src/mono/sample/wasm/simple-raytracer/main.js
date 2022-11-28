// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './dotnet.js'

function renderCanvas(rgbaView) {
    const canvas = document.getElementById("out");
    const ctx = canvas.getContext('2d');
    const clamped = new Uint8ClampedArray(rgbaView.slice());
    const image = new ImageData(clamped, 640, 480);
    ctx.putImageData(image, 0, 0);
    rgbaView.dispose();
    canvas.style = "";
}

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet.create();
setModuleImports("main.js", { renderCanvas });
const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
globalThis.onClick = exports.Program.OnClick;

await dotnet
    .withRuntimeOptions(["--jiterpreter-stats-enabled"])
    .run();
const btnRender = document.getElementById("btnRender");
btnRender.disabled = false;
