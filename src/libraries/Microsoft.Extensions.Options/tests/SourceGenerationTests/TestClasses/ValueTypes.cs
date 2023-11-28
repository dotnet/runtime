// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace ValueTypes
{
#pragma warning disable SA1649

    public class FirstModel
    {
        [Required]
        [MinLength(5)]
        public string P1 { get; set; } = string.Empty;

        [ValidateObjectMembers]
        public SecondModel? P2 { get; set; }

        [ValidateObjectMembers]
        public SecondModel P3 { get; set; }

        [ValidateObjectMembers]
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1125:Use shorthand for nullable types", Justification = "Testing System>Nullable<T>")]
        public Nullable<SecondModel> P4 { get; set; }
    }

    public struct SecondModel
    {
        [Required]
        [MinLength(5)]
        public string P4 { get; set; } = string.Empty;

        public SecondModel(object _)
        {
        }
    }

    [OptionsValidator]
    public partial struct FirstValidator : IValidateOptions<FirstModel>
    {
    }
}
