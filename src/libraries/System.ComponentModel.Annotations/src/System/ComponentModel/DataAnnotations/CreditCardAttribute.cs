// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class CreditCardAttribute : DataTypeAttribute
    {
        public CreditCardAttribute()
            : base(DataType.CreditCard)
        {
            // Set DefaultErrorMessage, allowing user to set
            // ErrorMessageResourceType and ErrorMessageResourceName to use localized messages.
            DefaultErrorMessage = SR.CreditCardAttribute_Invalid;
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
            {
                return true;
            }

            if (!(value is string ccValue))
            {
                return false;
            }
            ccValue = ccValue.Replace("-", string.Empty);
            ccValue = ccValue.Replace(" ", string.Empty);

            var checksum = 0;
            var evenDigit = false;

            // Note: string.Reverse() does not exist for WinPhone
            for (var i = ccValue.Length - 1; i >= 0; i--)
            {
                char digit = ccValue[i];
                if (!char.IsAsciiDigit(digit))
                {
                    return false;
                }

                var digitValue = (digit - '0') * (evenDigit ? 2 : 1);
                evenDigit = !evenDigit;

                while (digitValue > 0)
                {
                    checksum += digitValue % 10;
                    digitValue /= 10;
                }
            }

            return (checksum % 10) == 0;
        }
    }
}
