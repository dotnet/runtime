// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Generics
{
#pragma warning disable SA1649
#pragma warning disable SA1402

    public class FirstModel<T>
    {
        [Required]
        [MinLength(5)]
        public string P1 { get; set; } = string.Empty;

        public T? P2 { get; set; }

        [Microsoft.Extensions.Options.ValidateObjectMembers]
        public SecondModel? P3 { get; set; }
    }

    public class SecondModel
    {
        [Required]
        [MinLength(5)]
        public string P4 { get; set; } = string.Empty;
    }

    [OptionsValidator]
    public partial class FirstValidator<T> : IValidateOptions<FirstModel<T>>
    {
    }
}
