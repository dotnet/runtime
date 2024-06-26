// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './dotnet.js'

await dotnet
    .withApplicationArguments("A", "B", "C")
    .withMainAssembly("NativeAOT")
    .create();

await dotnet.run();
