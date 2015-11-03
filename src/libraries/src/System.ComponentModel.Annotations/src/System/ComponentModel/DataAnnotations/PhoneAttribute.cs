// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class PhoneAttribute : DataTypeAttribute
    {
        private const string _additionalPhoneNumberCharacters = "-.()";

        public PhoneAttribute()
            : base(DataType.PhoneNumber)
        {
            // Set DefaultErrorMessage not ErrorMessage, allowing user to set
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
            if (valueAsString == null)
            {
                return false;
            }

            valueAsString = valueAsString.Replace("+", string.Empty).TrimEnd();
            valueAsString = RemoveExtension(valueAsString);

            if (!valueAsString.Any(Char.IsDigit))
            {
                return false;
            }

            return valueAsString.All(c =>
                Char.IsDigit(c)
                || Char.IsWhiteSpace(c)
                || _additionalPhoneNumberCharacters.Contains(c));
        }

        private static string RemoveExtension(string potentialPhoneNumber)
        {
            var lastIndexOfExtension = potentialPhoneNumber
                .LastIndexOf("ext.", StringComparison.OrdinalIgnoreCase);
            if (lastIndexOfExtension >= 0)
            {
                var extension = potentialPhoneNumber.Substring(lastIndexOfExtension + 4);
                if (MatchesExtension(extension))
                {
                    return potentialPhoneNumber.Substring(0, lastIndexOfExtension);
                }
            }

            lastIndexOfExtension = potentialPhoneNumber
                .LastIndexOf("ext", StringComparison.OrdinalIgnoreCase);
            if (lastIndexOfExtension >= 0)
            {
                var extension = potentialPhoneNumber.Substring(lastIndexOfExtension + 3);
                if (MatchesExtension(extension))
                {
                    return potentialPhoneNumber.Substring(0, lastIndexOfExtension);
                }
            }

            lastIndexOfExtension = potentialPhoneNumber
                .LastIndexOf("x", StringComparison.OrdinalIgnoreCase);
            if (lastIndexOfExtension >= 0)
            {
                var extension = potentialPhoneNumber.Substring(lastIndexOfExtension + 1);
                if (MatchesExtension(extension))
                {
                    return potentialPhoneNumber.Substring(0, lastIndexOfExtension);
                }
            }

            return potentialPhoneNumber;
        }

        private static bool MatchesExtension(string potentialExtension)
        {
            potentialExtension = potentialExtension.TrimStart();
            if (potentialExtension.Length == 0)
            {
                return false;
            }

            return potentialExtension.All(c => Char.IsDigit(c));
        }
    }
}
