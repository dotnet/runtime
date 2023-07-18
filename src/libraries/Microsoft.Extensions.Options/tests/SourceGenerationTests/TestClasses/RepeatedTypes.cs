// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace RepeatedTypes
{
#pragma warning disable SA1649
#pragma warning disable SA1402
#pragma warning disable CA1019

    public class FirstModel
    {
        [Required]
        [Microsoft.Extensions.Options.ValidateObjectMembers]
        public SecondModel? P1 { get; set; }

        [Required]
        [Microsoft.Extensions.Options.ValidateObjectMembers]
        public SecondModel? P2 { get; set; }

        [Required]
        [Microsoft.Extensions.Options.ValidateObjectMembers]
        public ThirdModel? P3 { get; set; }
    }

    public class SecondModel
    {
        [Required]
        [Microsoft.Extensions.Options.ValidateObjectMembers]
        public ThirdModel? P4 { get; set; }
    }

    public class ThirdModel
    {
        [Required]
        [MinLength(5)]
        public string? P5;
    }

    [OptionsValidator]
    public partial class FirstValidator : IValidateOptions<FirstModel>
    {
    }
}
