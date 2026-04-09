// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Specifies the minimum and maximum length of collection/string data allowed in a property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class LengthAttribute : ValidationAttribute
    {
        [RequiresUnreferencedCode(CountPropertyHelper.RequiresUnreferencedCodeMessage)]
        public LengthAttribute(int minimumLength, int maximumLength)
            : base(SR.LengthAttribute_ValidationError)
        {
            MinimumLength = minimumLength;
            MaximumLength = maximumLength;
        }

        /// <summary>
        ///     Gets the minimum allowable length of the collection/string data.
        /// </summary>
        public int MinimumLength { get; }

        /// <summary>
        ///     Gets the maximum allowable length of the collection/string data.
        /// </summary>
        public int MaximumLength { get; }

        /// <summary>
        ///     Determines whether a specified object is valid. (Overrides <see cref="ValidationAttribute.IsValid(object)" />)
        /// </summary>
        /// <remarks>
        ///     This method returns <c>true</c> if the <paramref name="value" /> is null.
        ///     It is assumed the <see cref="RequiredAttribute" /> is used if the value may not be null.
        /// </remarks>
        /// <param name="value">The object to validate.</param>
        /// <returns>
        ///     <c>true</c> if the value is null or its length is between the specified minimum length and maximum length, otherwise
        ///     <c>false</c>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     <see cref="MinimumLength"/> is less than zero or <see cref="MaximumLength"/> is less than <see cref="MinimumLength"/>.
        /// </exception>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked with RequiresUnreferencedCode.")]
        public override bool IsValid(object? value)
        {
            // Check the lengths for legality
            EnsureLegalLengths();

            int length;
            // Automatically pass if value is null. RequiredAttribute should be used to assert a value is not null.
            if (value is null)
            {
                return true;
            }

            if (value is string str)
            {
                length = str.Length;
            }
            else if (!CountPropertyHelper.TryGetCount(value, out length))
            {
                throw new InvalidCastException(SR.Format(SR.LengthAttribute_InvalidValueType, value.GetType()));
            }

            return (uint)(length - MinimumLength) <= (uint)(MaximumLength - MinimumLength);
        }

        /// <summary>
        ///     Applies formatting to a specified error message. (Overrides <see cref="ValidationAttribute.FormatErrorMessage" />)
        /// </summary>
        /// <param name="name">The name to include in the formatted string.</param>
        /// <returns>A localized string to describe the minimum acceptable length.</returns>
        public override string FormatErrorMessage(string name) =>
            // An error occurred, so we know the value is less than the minimum
            string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, MinimumLength, MaximumLength);

        /// <summary>
        ///     Checks that Length has a legal value.
        /// </summary>
        /// <exception cref="InvalidOperationException">Length is less than zero.</exception>
        private void EnsureLegalLengths()
        {
            if (MinimumLength < 0)
            {
                throw new InvalidOperationException(SR.LengthAttribute_InvalidMinLength);
            }

            if (MaximumLength < MinimumLength)
            {
                throw new InvalidOperationException(SR.LengthAttribute_InvalidMaxLength);
            }
        }
    }
}
