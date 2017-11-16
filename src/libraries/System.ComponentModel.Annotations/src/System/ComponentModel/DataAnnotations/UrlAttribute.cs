// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class UrlAttribute : DataTypeAttribute
    {
        public UrlAttribute()
            : base(DataType.Url)
        {
            // Set DefaultErrorMessage not ErrorMessage, allowing user to set
            // ErrorMessageResourceType and ErrorMessageResourceName to use localized messages.
            DefaultErrorMessage = SR.UrlAttribute_Invalid;
        }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return true;
            }

            return value is string valueAsString &&
                (valueAsString.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || valueAsString.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || valueAsString.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase));
        }
    }
}
