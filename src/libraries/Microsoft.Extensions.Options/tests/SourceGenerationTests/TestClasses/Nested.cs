// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// #if ROSLYN_4_0_OR_GREATER
// #if ROSLYN4_0_OR_GREATER

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Nested
{
#pragma warning disable SA1649
#pragma warning disable SA1402

    public static class Container1
    {
        public class FirstModel
        {
            [Required]
            [MinLength(5)]
            public string P1 { get; set; } = string.Empty;

            [Microsoft.Extensions.Options.ValidateObjectMembers(typeof(Container2.Container3.SecondValidator))]
            public SecondModel? P2 { get; set; }

            [Microsoft.Extensions.Options.ValidateObjectMembers]
            public ThirdModel P3 { get; set; }

            [Microsoft.Extensions.Options.ValidateObjectMembers(typeof(Container4.Container5.ThirdValidator))]
            public SecondModel? P4 { get; set; }
        }

        public class SecondModel
        {
            [Required]
            [MinLength(5)]
            public string P5 { get; set; } = string.Empty;
        }

        public struct ThirdModel
        {
            public ThirdModel(int _)
            {
            }

            [Required]
            [MinLength(5)]
            public string P6 { get; set; } = string.Empty;
        }
    }

    public static partial class Container2
    {
        public partial class Container3
        {
            public Container3(int _)
            {
                // nothing to do
            }

            [OptionsValidator]
            public partial struct FirstValidator : IValidateOptions<Container1.FirstModel>
            {
            }

            [OptionsValidator]
            public partial struct SecondValidator : IValidateOptions<Container1.SecondModel>
            {
            }
        }
    }

    public partial record class Container4
    {
        public partial record class Container5
        {
            public Container5(int _)
            {
                // nothing to do
            }

            [OptionsValidator]
            public partial struct ThirdValidator : IValidateOptions<Container1.SecondModel>
            {
            }
        }
    }

    public partial struct Container6
    {
        [OptionsValidator]
        public partial struct FourthValidator : IValidateOptions<Container1.SecondModel>
        {
        }
    }

    public partial record struct Container7
    {
        [OptionsValidator]
        public partial record struct FifthValidator : IValidateOptions<Container1.SecondModel>
        {
        }
    }
}

// #endif
