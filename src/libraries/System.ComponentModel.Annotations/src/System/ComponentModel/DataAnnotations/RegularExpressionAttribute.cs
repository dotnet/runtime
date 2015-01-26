// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Regular expression validation attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public class RegularExpressionAttribute : ValidationAttribute
    {
        /// <summary>
        ///     Constructor that accepts the regular expression pattern
        /// </summary>
        /// <param name="pattern">The regular expression to use.  It cannot be null.</param>
        public RegularExpressionAttribute(string pattern)
            : base(() => SR.RegexAttribute_ValidationError)
        {
            Pattern = pattern;
        }

        /// <summary>
        ///     Gets the regular expression pattern to use
        /// </summary>
        public string Pattern { get; private set; }

        private Regex Regex { get; set; }

        /// <summary>
        ///     Override of <see cref="ValidationAttribute.IsValid(object)" />
        /// </summary>
        /// <remarks>
        ///     This override performs the specific regular expression matching of the given <paramref name="value" />
        /// </remarks>
        /// <param name="value">The value to test for validity.</param>
        /// <returns><c>true</c> if the given value matches the current regular expression pattern</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
        /// <exception cref="ArgumentException"> is thrown if the <see cref="Pattern" /> is not a valid regular expression.</exception>
        public override bool IsValid(object value)
        {
            SetupRegex();

            // Convert the value to a string
            string stringValue = Convert.ToString(value, CultureInfo.CurrentCulture);

            // Automatically pass if value is null or empty. RequiredAttribute should be used to assert a value is not empty.
            if (string.IsNullOrEmpty(stringValue))
            {
                return true;
            }

            var m = Regex.Match(stringValue);

            // We are looking for an exact match, not just a search hit. This matches what
            // the RegularExpressionValidator control does
            return (m.Success && m.Index == 0 && m.Length == stringValue.Length);
        }

        /// <summary>
        ///     Override of <see cref="ValidationAttribute.FormatErrorMessage" />
        /// </summary>
        /// <remarks>This override provide a formatted error message describing the pattern</remarks>
        /// <param name="name">The user-visible name to include in the formatted message.</param>
        /// <returns>The localized message to present to the user</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
        /// <exception cref="ArgumentException"> is thrown if the <see cref="Pattern" /> is not a valid regular expression.</exception>
        public override string FormatErrorMessage(string name)
        {
            SetupRegex();

            return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, Pattern);
        }


        /// <summary>
        ///     Sets up the <see cref="Regex" /> property from the <see cref="Pattern" /> property.
        /// </summary>
        /// <exception cref="ArgumentException"> is thrown if the current <see cref="Pattern" /> cannot be parsed</exception>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
        private void SetupRegex()
        {
            if (Regex == null)
            {
                if (string.IsNullOrEmpty(Pattern))
                {
                    throw new InvalidOperationException(
                        SR.RegularExpressionAttribute_Empty_Pattern);
                }
                Regex = new Regex(Pattern);
            }
        }
    }
}
