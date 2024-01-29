// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace MultiModelValidator
{
#pragma warning disable SA1649
#pragma warning disable SA1402

    public class FirstModel
    {
        [Required]
        [MinLength(5)]
        public string P1 { get; set; } = string.Empty;

        [Microsoft.Extensions.Options.ValidateObjectMembers(typeof(MultiValidator))]
        public SecondModel? P2 { get; set; }
    }

    public class SecondModel
    {
        [Required]
        [MinLength(5)]
        public string P3 { get; set; } = string.Empty;
    }

    [OptionsValidator]
    public partial struct MultiValidator : IValidateOptions<FirstModel>, IValidateOptions<SecondModel>
    {
    }
}
