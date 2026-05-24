// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'

await dotnet
    .withApplicationArguments("start")
    .withDiagnosticTracing(true)
    .withConfig({ forwardConsole: true, appendElementOnExit: true, logExitCode: true, exitOnUnhandledError: true })
    .create();

await dotnet.runMainAndExit();
