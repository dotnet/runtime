// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace CustomAttr
{
#pragma warning disable SA1649
#pragma warning disable SA1402
#pragma warning disable CA1019
#pragma warning disable IDE0052

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class CustomAttribute : ValidationAttribute
    {
        private readonly char _ch;
        private readonly bool _caseSensitive;
        private readonly string? _extra;

        public CustomAttribute(char ch, bool caseSensitive, string? extra)
        {
            _ch = ch;
            _caseSensitive = caseSensitive;
            _extra = extra;
        }

        protected override ValidationResult IsValid(object? value, ValidationContext? validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success!;
            }

            if (_caseSensitive)
            {
                if ((char)value != _ch)
                {
                    return new ValidationResult($"{validationContext?.MemberName} didn't match");
                }
            }
            else
            {
                if (char.ToUpperInvariant((char)value) != char.ToUpperInvariant(_ch))
                {
                    return new ValidationResult($"{validationContext?.MemberName} didn't match");
                }
            }

            return ValidationResult.Success!;
        }
    }

    public class FirstModel
    {
        [Custom('A', true, null)]
        public char P1 { get; set; }

        [Custom('A', false, "X")]
        public char P2 { get; set; }
    }

    [OptionsValidator]
    public partial class FirstValidator : IValidateOptions<FirstModel>
    {
    }
}
