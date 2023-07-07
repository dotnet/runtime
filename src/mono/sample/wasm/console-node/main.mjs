// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'

await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArguments("dotnet", "is", "great!")
    .run()