// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options.Generators;
using SourceGenerators.Tests;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public partial class ParserTests
{
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task CircularTypeReferencesInEnumeration()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                [ValidateEnumeratedItems]
                public FirstModel[]? P1 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }
        ");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.CircularTypeReferences.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NotValidatorInEnumeration()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [ValidateEnumeratedItems(typeof(SecondValidator)]
                public SecondModel[]? P1;
            }

            public class SecondModel
            {
                [Required]
                public string? P2;
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            public partial class SecondValidator
            {
            }
        ");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.DoesntImplementIValidateOptions.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NullValidatorInEnumeration()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [ValidateEnumeratedItems(null!)]
                public SecondModel[]? P1;
            }

            public class SecondModel
            {
                [Required]
                public string? P2;
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            [OptionsValidator]
            public partial class SecondValidator : IValidateOptions<SecondModel>
            {
            }
        ");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.NullValidatorType.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NoSimpleValidatorConstructorInEnumeration()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public string? P1;

                [ValidateEnumeratedItems(typeof(SecondValidator)]
                public SecondModel[]? P2;
            }

            public class SecondModel
            {
                [Required]
                public string? P3;
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            [OptionsValidator]
            public partial class SecondValidator : IValidateOptions<SecondModel>
            {
                public SecondValidator(int _)
                {
                }
            }
        ");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ValidatorsNeedSimpleConstructor.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task CantValidateOpenGenericMembersInEnumeration()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel<T>
            {
                [Required]
                [ValidateEnumeratedItems]
                public T[]? P1;

                [ValidateEnumeratedItems]
                [Required]
                public T[]? P2;

                [ValidateEnumeratedItems]
                [Required]
                public System.Collections.Generic.IList<T> P3 = null!;
            }

            [OptionsValidator]
            public partial class FirstValidator<T> : IValidateOptions<FirstModel<T>>
            {
            }
        ");

        Assert.Equal(3, d.Count);
        Assert.Equal(DiagDescriptors.CantUseWithGenericTypes.Id, d[0].Id);
        Assert.Equal(DiagDescriptors.CantUseWithGenericTypes.Id, d[1].Id);
        Assert.Equal(DiagDescriptors.CantUseWithGenericTypes.Id, d[2].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ClosedGenericsInEnumeration()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel<T>
            {
                [ValidateEnumeratedItems]
                [Required]
                public T[]? P1;

                [ValidateEnumeratedItems]
                [Required]
                public int[]? P2;

                [ValidateEnumeratedItems]
                [Required]
                public System.Collections.Generic.IList<T>? P3;
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel<string>>
            {
            }
        ");

        Assert.Equal(3, d.Count);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, d[0].Id);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, d[1].Id);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, d[2].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NotEnumerable()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                [ValidateEnumeratedItems]
                public int P1;
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }
        ");

        Assert.Equal(1, d.Count);
        Assert.Equal(DiagDescriptors.NotEnumerableType.Id, d[0].Id);
    }
}
