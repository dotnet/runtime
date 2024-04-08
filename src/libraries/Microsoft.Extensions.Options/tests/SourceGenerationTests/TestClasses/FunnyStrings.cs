// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace FunnyStrings
{
#pragma warning disable SA1649
#pragma warning disable SA1402

    public class FirstModel
    {
        [RegularExpression("\"\r\n\\\\")]
        public string P1 { get; set; } = string.Empty;
    }

    [OptionsValidator]
    public partial struct FirstValidator : IValidateOptions<FirstModel>
    {
    }
}
