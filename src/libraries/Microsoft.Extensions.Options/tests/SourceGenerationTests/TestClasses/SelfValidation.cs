// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace SelfValidation
{
#pragma warning disable SA1649

    public class FirstModel : IValidatableObject
    {
        [Required]
        public string P1 { get; set; } = string.Empty;

        [Required]
        [ValidateObjectMembers]
        public SecondModel P2 { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (P1.Length < 5)
            {
                return new[] { new ValidationResult("P1 is not long enough") };
            }

            return Array.Empty<ValidationResult>();
        }
    }

    public class SecondModel : IValidatableObject
    {
        [Required]
        public string P3 { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (P3.Length < 5)
            {
                return new[] { new ValidationResult("P3 is not long enough") };
            }

            return Array.Empty<ValidationResult>();
        }
    }

    [OptionsValidator]
    public partial struct FirstValidator : IValidateOptions<FirstModel>
    {
    }

    // SelfValidateOptions is self validate class as it implements IValidatableObject and contains no properties have ValidationAttribute
    // Source generator should generate valid code for this class
    [OptionsValidator]
    public partial class SelfValidateOptions : IValidateOptions<SelfValidateOptions>, IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            return new[] { new ValidationResult($"Display: {context.DisplayName}, Member: {context.MemberName}") };
        }
    }
}
