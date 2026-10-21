// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Lxr.Harness.Core;
using Lxr.Harness.Worker.AspNet;

// A second worker executable exists solely so the ASP.NET Core framework reference does not reach the
// other nine scenarios. See Lxr.Harness.Worker.AspNet.csproj for why that matters.
return WorkerEntryPoint.Run(args, static id =>
    id == "aspnet-request-load" ? new AspNetRequestLoadScenario() : null);
