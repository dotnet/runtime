// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Fields
{
#pragma warning disable SA1649
#pragma warning disable SA1402
#pragma warning disable S1186
#pragma warning disable CA1822

    public class FirstModel
    {
        [Required]
        [MinLength(5)]
        public string P1 = string.Empty;

        [Microsoft.Extensions.Options.ValidateObjectMembers(typeof(SecondValidator))]
        public SecondModel? P2;

        [Microsoft.Extensions.Options.ValidateObjectMembers]
        public ThirdModel P3;
    }

    public class SecondModel
    {
        [Required]
        [MinLength(5)]
        public string P4 = string.Empty;
    }

    public struct ThirdModel
    {
        [Required]
        [MinLength(5)]
        public string P5 = string.Empty;

        public int P6 = default;

        public ThirdModel(object _)
        {
        }
    }

    [OptionsValidator]
    public partial struct FirstValidator : IValidateOptions<FirstModel>
    {
        public void Validate()
        {
        }

        public void Validate(int _)
        {
        }

        public void Validate(string? _)
        {
        }

        public void Validate(string? _0, object _1)
        {
        }
    }

    [OptionsValidator]
    public partial struct SecondValidator : IValidateOptions<SecondModel>
    {
    }
}
