// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// #if ROSLYN_4_0_OR_GREATER

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace RecordTypes
{
#pragma warning disable SA1649

    public record class FirstModel
    {
        [Required]
        [MinLength(5)]
        public string P1 { get; set; } = string.Empty;

        [Microsoft.Extensions.Options.ValidateObjectMembers(typeof(SecondValidator))]
        public SecondModel? P2 { get; set; }

        [Microsoft.Extensions.Options.ValidateObjectMembers(typeof(ThirdValidator))]
        public SecondModel P3 { get; set; } = new SecondModel();

        [Microsoft.Extensions.Options.ValidateObjectMembers]
        public ThirdModel P4 { get; set; }
    }

    public record class SecondModel
    {
        [Required]
        [MinLength(5)]
        public string P5 { get; set; } = string.Empty;
    }

    public record struct ThirdModel
    {
        [Required]
        [MinLength(5)]
        public string P6 { get; set; } = string.Empty;

        public ThirdModel(int _)
        {
        }

        public ThirdModel(object _)
        {
        }
    }

    [OptionsValidator]
    public partial record struct FirstValidator : IValidateOptions<FirstModel>
    {
    }

    [OptionsValidator]
    public partial record struct SecondValidator : IValidateOptions<SecondModel>
    {
    }

    [OptionsValidator]
    public partial record class ThirdValidator : IValidateOptions<SecondModel>
    {
    }
}

// #endif
