// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Lxr.Harness.Core;
using Lxr.Harness.Scenarios;

// The process under test for every scenario except aspnet-request-load. It carries no ASP.NET Core
// framework reference, which is what lets it run on the corerun host against a locally built runtime
// and therefore, later, against the LXR arm.
return WorkerEntryPoint.Run(args, static id => ScenarioRegistry.TryCreate(id, out IScenario scenario) ? scenario : null);
