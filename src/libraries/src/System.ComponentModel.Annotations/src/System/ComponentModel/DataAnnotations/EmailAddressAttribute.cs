// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class EmailAddressAttribute : DataTypeAttribute
    {
        public EmailAddressAttribute()
            : base(DataType.EmailAddress)
        {
            // Set DefaultErrorMessage not ErrrorMessage, allowing user to set
            // ErrorMessageResourceType and ErrorMessageResourceName to use localized messages.
            DefaultErrorMessage = SR.EmailAddressAttribute_Invalid;
        }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return true;
            }

            var valueAsString = value as string;
            return (valueAsString != null
                && valueAsString.Count(c => c == '@') == 1
                && valueAsString[0] != '@'
                && valueAsString[valueAsString.Length-1] != '@');
        }
    }
}
