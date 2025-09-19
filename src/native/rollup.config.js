//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

import { promises as fs } from "fs";
import { artifactsObjDir, configuration } from "./rollup.config.defines.js"

/**
 * This would be replace by real rollup with TypeScript compilation in next iteration
 */

await fs.copyFile("./corehost/browserhost/loader/dotnet.d.ts",
    artifactsObjDir + `/coreclr/browser.wasm.${configuration}/corehost/dotnet.d.ts`);
await fs.copyFile("./corehost/browserhost/loader/dotnet.js",
    artifactsObjDir + `/coreclr/browser.wasm.${configuration}/corehost/dotnet.js`);
await fs.copyFile("./libs/System.Native.Browser/libSystem.Native.Browser.js",
    artifactsObjDir + `/native/browser-${configuration}-wasm/System.Native.Browser/libSystem.Native.Browser.js`);
await fs.copyFile("./libs/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.js",
    artifactsObjDir + `/native/browser-${configuration}-wasm/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.js`);
await fs.copyFile("./libs/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js",
    artifactsObjDir + `/native/browser-${configuration}-wasm/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js`);
