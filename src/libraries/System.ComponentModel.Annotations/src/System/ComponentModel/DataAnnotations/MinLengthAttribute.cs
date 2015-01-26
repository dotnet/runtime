// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Specifies the minimum length of array/string data allowed in a property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public class MinLengthAttribute : ValidationAttribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MinLengthAttribute" /> class.
        /// </summary>
        /// <param name="length">
        ///     The minimum allowable length of array/string data.
        ///     Value must be greater than or equal to zero.
        /// </param>
        public MinLengthAttribute(int length)
            : base(SR.MinLengthAttribute_ValidationError)
        {
            Length = length;
        }

        /// <summary>
        ///     Gets the minimum allowable length of the array/string data.
        /// </summary>
        public int Length { get; private set; }

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
        public override bool IsValid(object value)
        {
            // Check the lengths for legality
            EnsureLegalLengths();

            var length = 0;
            // Automatically pass if value is null. RequiredAttribute should be used to assert a value is not null.
            if (value == null)
            {
                return true;
            }
            var str = value as string;
            if (str != null)
            {
                length = str.Length;
            }
            else
            {
                // We expect a cast exception if a non-{string|array} property was passed in.
                length = ((Array)value).Length;
            }

            return length >= Length;
        }

        /// <summary>
        ///     Applies formatting to a specified error message. (Overrides <see cref="ValidationAttribute.FormatErrorMessage" />)
        /// </summary>
        /// <param name="name">The name to include in the formatted string.</param>
        /// <returns>A localized string to describe the minimum acceptable length.</returns>
        public override string FormatErrorMessage(string name)
        {
            // An error occurred, so we know the value is less than the minimum
            return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, Length);
        }

        /// <summary>
        ///     Checks that Length has a legal value.
        /// </summary>
        /// <exception cref="InvalidOperationException">Length is less than zero.</exception>
        private void EnsureLegalLengths()
        {
            if (Length < 0)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    SR.MinLengthAttribute_InvalidMinLength));
            }
        }
    }
}
