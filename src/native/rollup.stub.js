//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

import { promises as fs } from "fs";
import path from "path";

/**
 * This would be replace by real rollup with TypeScript compilation in next iteration
 */


const artifactsObjDir = "../../artifacts/obj";
const configuration = process.argv[2] || "Debug";
const productVersion = process.argv[3] || "10.0.0-dev";
const isContinuousIntegrationBuild = process.argv[4] === "true" ? true : false;

console.log(`Rollup configuration: Configuration=${configuration}, ProductVersion=${productVersion}, ContinuousIntegrationBuild=${isContinuousIntegrationBuild}`);

const copies = [
    ["./corehost/browserhost/loader/dotnet.d.ts",
        `${artifactsObjDir}/coreclr/browser.wasm.${configuration}/corehost/dotnet.d.ts`],
    ["./corehost/browserhost/loader/dotnet.js",
        `${artifactsObjDir}/coreclr/browser.wasm.${configuration}/corehost/dotnet.js`],
    ["./corehost/browserhost/libBrowserHost.js",
        `${artifactsObjDir}/coreclr/browser.wasm.${configuration}/corehost/libBrowserHost.js`],
    ["./libs/System.Native.Browser/libSystem.Native.Browser.js",
        `${artifactsObjDir}/native/browser-${configuration}-wasm/System.Native.Browser/libSystem.Native.Browser.js`],
    ["./libs/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.js",
        `${artifactsObjDir}/native/browser-${configuration}-wasm/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.js`],
    ["./libs/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js",
        `${artifactsObjDir}/native/browser-${configuration}-wasm/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js`]
];

const now = new Date();
for (const [src, dest] of copies) {
    const absoluteSrc = path.resolve(src);
    const absoluteDest = path.resolve(dest);
    const destDir = path.resolve(path.dirname(dest));
    console.log(`Copying ${absoluteSrc} to ${destDir}`);
    await fs.mkdir(destDir, { recursive: true });
    await fs.copyFile(absoluteSrc, absoluteDest);
    // TODO-WASM: make rollup incremental
    // await fs.utimes(absoluteDest, now, now);
}
await fs.writeFile(`${artifactsObjDir}/coreclr/browser.wasm.${configuration}/corehost/.rollup.stamp`, new Date().toISOString());
