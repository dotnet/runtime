// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class PhoneAttribute : DataTypeAttribute
    {
        private const string AdditionalPhoneNumberCharacters = "-.()";
        private const string ExtensionAbbreviationExtDot = "ext.";
        private const string ExtensionAbbreviationExt = "ext";
        private const string ExtensionAbbreviationX = "x";

        public PhoneAttribute()
            : base(DataType.PhoneNumber)
        {
            // Set DefaultErrorMessage not ErrorMessage, allowing user to set
            // ErrorMessageResourceType and ErrorMessageResourceName to use localized messages.
            DefaultErrorMessage = SR.PhoneAttribute_Invalid;
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
            {
                return true;
            }

            if (!(value is string valueAsString))
            {
                return false;
            }

            ReadOnlySpan<char> valueSpan = valueAsString.Replace("+", string.Empty).AsSpan().TrimEnd();
            valueSpan = RemoveExtension(valueSpan);

            bool digitFound = false;
            foreach (char c in valueSpan)
            {
                if (char.IsDigit(c))
                {
                    digitFound = true;
                    break;
                }
            }

            if (!digitFound)
            {
                return false;
            }

            foreach (char c in valueSpan)
            {
                if (!(char.IsDigit(c)
                    || char.IsWhiteSpace(c)
                    || AdditionalPhoneNumberCharacters.IndexOf(c) != -1))
                {
                    return false;
                }
            }

            return true;
        }

        private static ReadOnlySpan<char> RemoveExtension(ReadOnlySpan<char> potentialPhoneNumber)
        {
            int lastIndexOfExtension = potentialPhoneNumber
                .LastIndexOf(ExtensionAbbreviationExtDot, StringComparison.OrdinalIgnoreCase);
            if (lastIndexOfExtension >= 0)
            {
                ReadOnlySpan<char> extension = potentialPhoneNumber.Slice(
                    lastIndexOfExtension + ExtensionAbbreviationExtDot.Length);
                if (MatchesExtension(extension))
                {
                    return potentialPhoneNumber.Slice(0, lastIndexOfExtension);
                }
            }

            lastIndexOfExtension = potentialPhoneNumber
                .LastIndexOf(ExtensionAbbreviationExt, StringComparison.OrdinalIgnoreCase);
            if (lastIndexOfExtension >= 0)
            {
                ReadOnlySpan<char> extension = potentialPhoneNumber.Slice(
                    lastIndexOfExtension + ExtensionAbbreviationExt.Length);
                if (MatchesExtension(extension))
                {
                    return potentialPhoneNumber.Slice(0, lastIndexOfExtension);
                }
            }

            lastIndexOfExtension = potentialPhoneNumber
                .LastIndexOf(ExtensionAbbreviationX, StringComparison.OrdinalIgnoreCase);
            if (lastIndexOfExtension >= 0)
            {
                ReadOnlySpan<char> extension = potentialPhoneNumber.Slice(
                    lastIndexOfExtension + ExtensionAbbreviationX.Length);
                if (MatchesExtension(extension))
                {
                    return potentialPhoneNumber.Slice(0, lastIndexOfExtension);
                }
            }

            return potentialPhoneNumber;
        }

        private static bool MatchesExtension(ReadOnlySpan<char> potentialExtension)
        {
            potentialExtension = potentialExtension.TrimStart();
            if (potentialExtension.Length == 0)
            {
                return false;
            }

            foreach (char c in potentialExtension)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
