// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            if (!(value is string valueAsString))
            {
                return false;
            }

            // only return true if there is only 1 '@' character
            // and it is neither the first nor the last character
            bool found = false;
            for (int i = 0; i < valueAsString.Length; i++)
            {
                if (valueAsString[i] == '@')
                {
                    if (found || i == 0 || i == valueAsString.Length - 1)
                    {
                        return false;
                    }
                    found = true;
                }
            }

            return found;
        }
    }
}
