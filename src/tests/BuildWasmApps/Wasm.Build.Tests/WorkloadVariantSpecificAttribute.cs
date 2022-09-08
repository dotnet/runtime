// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

/// <summary>
/// Example usage:
///     [BuildAndRun(aot: true, parameters: new object[] { arg1, arg2 })]
///     public void Test(BuildArgs, arg1, arg2, RunHost, id)
/// </summary>
[TraitDiscoverer("Wasm.Build.Tests.WorkloadVariantSpecificDiscoverer", "Wasm.Build.Tests")]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class WorkloadVariantSpecificAttribute : Attribute, ITraitAttribute
{
    public WorkloadVariantSpecificAttribute(string propertyName)
    { }
}
