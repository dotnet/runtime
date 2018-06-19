// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Validation attribute to assert a string property, field or parameter does not exceed a maximum length
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public class StringLengthAttribute : ValidationAttribute
    {
        /// <summary>
        ///     Constructor that accepts the maximum length of the string.
        /// </summary>
        /// <param name="maximumLength">The maximum length, inclusive.  It may not be negative.</param>
        public StringLengthAttribute(int maximumLength)
            : base(() => SR.StringLengthAttribute_ValidationError)
        {
            MaximumLength = maximumLength;
        }

        /// <summary>
        ///     Gets the maximum acceptable length of the string
        /// </summary>
        public int MaximumLength { get; }

        /// <summary>
        ///     Gets or sets the minimum acceptable length of the string
        /// </summary>
        public int MinimumLength { get; set; }

        /// <summary>
        ///     Override of <see cref="ValidationAttribute.IsValid(object)" />
        /// </summary>
        /// <remarks>
        ///     This method returns <c>true</c> if the <paramref name="value" /> is null.
        ///     It is assumed the <see cref="RequiredAttribute" /> is used if the value may not be null.
        /// </remarks>
        /// <param name="value">The value to test.</param>
        /// <returns><c>true</c> if the value is null or less than or equal to the set maximum length</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
        public override bool IsValid(object value)
        {
            // Check the lengths for legality
            EnsureLegalLengths();

            // Automatically pass if value is null. RequiredAttribute should be used to assert a value is not null.
            // We expect a cast exception if a non-string was passed in.
            if (value == null)
            {
                return true;
            }

            int length = ((string)value).Length;
            return length >= MinimumLength && length <= MaximumLength;
        }

        /// <summary>
        ///     Override of <see cref="ValidationAttribute.FormatErrorMessage" />
        /// </summary>
        /// <param name="name">The name to include in the formatted string</param>
        /// <returns>A localized string to describe the maximum acceptable length</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
        public override string FormatErrorMessage(string name)
        {
            EnsureLegalLengths();

            bool useErrorMessageWithMinimum = MinimumLength != 0 && !CustomErrorMessageSet;
            string errorMessage = useErrorMessageWithMinimum
                ? SR.StringLengthAttribute_ValidationErrorIncludingMinimum
                : ErrorMessageString;

            // it's ok to pass in the minLength even for the error message without a {2} param since string.Format will just
            // ignore extra arguments
            return string.Format(CultureInfo.CurrentCulture, errorMessage, name, MaximumLength, MinimumLength);
        }

        /// <summary>
        ///     Checks that MinimumLength and MaximumLength have legal values.  Throws InvalidOperationException if not.
        /// </summary>
        private void EnsureLegalLengths()
        {
            if (MaximumLength < 0)
            {
                throw new InvalidOperationException(SR.StringLengthAttribute_InvalidMaxLength);
            }

            if (MaximumLength < MinimumLength)
            {
                throw new InvalidOperationException(SR.Format(SR.RangeAttribute_MinGreaterThanMax, MaximumLength, MinimumLength));
            }
        }
    }
}
