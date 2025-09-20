//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

import { promises as fs } from "fs";
import path from "path";
import { artifactsObjDir, configuration } from "./rollup.config.defines.js"

/**
 * This would be replace by real rollup with TypeScript compilation in next iteration
 */

const copies = [
    ["./corehost/browserhost/loader/dotnet.d.ts",
        `${artifactsObjDir}/coreclr/browser.wasm.${configuration}/corehost/dotnet.d.ts`],
    ["./corehost/browserhost/loader/dotnet.js",
        `${artifactsObjDir}/coreclr/browser.wasm.${configuration}/corehost/dotnet.js`],
    ["./libs/System.Native.Browser/libSystem.Native.Browser.js",
        `${artifactsObjDir}/native/browser-${configuration}-wasm/System.Native.Browser/libSystem.Native.Browser.js`],
    ["./libs/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.js",
        `${artifactsObjDir}/native/browser-${configuration}-wasm/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.js`],
    ["./libs/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js",
        `${artifactsObjDir}/native/browser-${configuration}-wasm/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js`]
];

for (const [src, dest] of copies) {
    const destDir = path.dirname(dest);
    await fs.mkdir(destDir, { recursive: true });
    await fs.copyFile(src, dest);
}
