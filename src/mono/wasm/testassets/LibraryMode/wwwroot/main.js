// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'

const { getAssemblyExports, getConfig } = await dotnet.create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
var code = exports.LibraryMode.Test.MyExport();
console.log(`WASM EXIT ${code}`);