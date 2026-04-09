// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace RandomMembers
{
#pragma warning disable SA1649
#pragma warning disable SA1402
#pragma warning disable CA1822

    public class FirstModel
    {
        [Required]
        [MinLength(5)]
        public string? P1 { get; set; }

        public void Foo()
        {
            throw new NotSupportedException();
        }

        public class Nested
        {
        }
    }

    [OptionsValidator]
    public partial class FirstValidator : IValidateOptions<FirstModel>
    {
    }
}
