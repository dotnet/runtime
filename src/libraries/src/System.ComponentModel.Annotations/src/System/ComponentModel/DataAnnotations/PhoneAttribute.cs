// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class PhoneAttribute : DataTypeAttribute
    {
        // see unit tests for examples
        private static readonly Regex _regex =
            new Regex(
                @"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$",
                RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public PhoneAttribute()
            : base(DataType.PhoneNumber)
        {
            // Set DefaultErrorMessage, allowing user to set
            // ErrorMessageResourceType and ErrorMessageResourceName to use localized messages.
            DefaultErrorMessage = SR.PhoneAttribute_Invalid;
        }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return true;
            }

            var valueAsString = value as string;
            return valueAsString != null && _regex.Match(valueAsString).Length > 0;
        }
    }
}
