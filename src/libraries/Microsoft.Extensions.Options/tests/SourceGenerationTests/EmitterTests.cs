// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SourceGenerators.Tests;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Generators;
using Microsoft.Shared.Data.Validation;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class EmitterTests
{
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task TestEmitter()
    {
        var sources = new List<string>();
#pragma warning disable RS1035 // To allow using the File IO APIs inside the analyzer test
        foreach (var file in Directory.GetFiles("TestClasses"))
        {
#if NETCOREAPP3_1_OR_GREATER
            sources.Add("#define NETCOREAPP3_1_OR_GREATER\n" + File.ReadAllText(file));
#else
            sources.Add(File.ReadAllText(file));
#endif
        }

        var (d, r) = await RoslynTestUtils.RunGenerator(
            new OptionsValidatorGenerator(),
            new[]
            {
                Assembly.GetAssembly(typeof(RequiredAttribute))!,
                Assembly.GetAssembly(typeof(TimeSpanAttribute))!,
                Assembly.GetAssembly(typeof(OptionsValidatorAttribute))!,
                Assembly.GetAssembly(typeof(IValidateOptions<object>))!,
            },
            sources)
            .ConfigureAwait(false);

        Assert.Empty(d);
        _ = Assert.Single(r);

#if NETCOREAPP3_1_OR_GREATER
        string baseline = File.ReadAllText(@"Baselines/NetCoreApp/Validators.g.cs");
#else
        string baseline = File.ReadAllText(@"Baselines/NetFX/Validators.g.cs");
#endif

        string result = r[0].SourceText.ToString();
        Assert.Equal(baseline, result);
#pragma warning restore RS1035
    }
}
