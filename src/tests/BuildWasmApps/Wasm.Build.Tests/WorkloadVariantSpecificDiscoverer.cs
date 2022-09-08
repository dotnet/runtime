// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Sdk;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

/// <summary>
/// This class discovers all of the tests and test classes that have
/// applied the PlatformSpecific attribute
/// </summary>
public class WorkloadVariantSpecificDiscoverer : ITraitDiscoverer
{
    /// <summary>
    /// Gets the trait values from the Category attribute.
    /// </summary>
    /// <param name="traitAttribute">The trait attribute containing the trait values.</param>
    /// <returns>The trait values.</returns>
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        string conditionMemberName = (string)traitAttribute.GetConstructorArguments().First();
        // System.Console.WriteLine ($"vv: {variant}");
        // List<string> categories = new();
        // foreach (var v in Enum.GetValues<WorkloadSetupVariant>())
        // {
        //     if (variant.HasFlag(v))
        //     {
        //         Console.WriteLine ($"\t{v}");
        //         categories.Add(v.ToString());
        //     }
        // }

        return new[] {
            new KeyValuePair<string, string>("category", "none"),
            new KeyValuePair<string, string>("category", "net7")
        };
    }
}
