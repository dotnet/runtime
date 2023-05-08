// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './dotnet.js'

await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArguments(...arguments)
    .run()