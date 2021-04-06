// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Specifies the minimum length of collection/string data allowed in a property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public class MinLengthAttribute : ValidationAttribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MinLengthAttribute" /> class.
        /// </summary>
        /// <param name="length">
        ///     The minimum allowable length of collection/string data.
        ///     Value must be greater than or equal to zero.
        /// </param>
        [RequiresUnreferencedCode(CountPropertyHelper.RequiresUnreferencedCodeMessage)]
        public MinLengthAttribute(int length)
            : base(SR.MinLengthAttribute_ValidationError)
        {
            Length = length;
        }

        /// <summary>
        ///     Gets the minimum allowable length of the collection/string data.
        /// </summary>
        public int Length { get; }

        /// <summary>
        ///     Determines whether a specified object is valid. (Overrides <see cref="ValidationAttribute.IsValid(object)" />)
        /// </summary>
        /// <remarks>
        ///     This method returns <c>true</c> if the <paramref name="value" /> is null.
        ///     It is assumed the <see cref="RequiredAttribute" /> is used if the value may not be null.
        /// </remarks>
        /// <param name="value">The object to validate.</param>
        /// <returns>
        ///     <c>true</c> if the value is null or greater than or equal to the specified minimum length, otherwise
        ///     <c>false</c>
        /// </returns>
        /// <exception cref="InvalidOperationException">Length is less than zero.</exception>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked with RequiresUnreferencedCode.")]
        public override bool IsValid(object? value)
        {
            // Check the lengths for legality
            EnsureLegalLengths();

            int length;
            // Automatically pass if value is null. RequiredAttribute should be used to assert a value is not null.
            if (value == null)
            {
                return true;
            }
            if (value is string str)
            {
                length = str.Length;
            }
            else if (CountPropertyHelper.TryGetCount(value, out var count))
            {
                length = count;
            }
            else
            {
                throw new InvalidCastException(SR.Format(SR.LengthAttribute_InvalidValueType, value.GetType()));
            }

            return length >= Length;
        }

        /// <summary>
        ///     Applies formatting to a specified error message. (Overrides <see cref="ValidationAttribute.FormatErrorMessage" />)
        /// </summary>
        /// <param name="name">The name to include in the formatted string.</param>
        /// <returns>A localized string to describe the minimum acceptable length.</returns>
        public override string FormatErrorMessage(string name) =>
            // An error occurred, so we know the value is less than the minimum
            string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, Length);

        /// <summary>
        ///     Checks that Length has a legal value.
        /// </summary>
        /// <exception cref="InvalidOperationException">Length is less than zero.</exception>
        private void EnsureLegalLengths()
        {
            if (Length < 0)
            {
                throw new InvalidOperationException(SR.MinLengthAttribute_InvalidMinLength);
            }
        }
    }
}
