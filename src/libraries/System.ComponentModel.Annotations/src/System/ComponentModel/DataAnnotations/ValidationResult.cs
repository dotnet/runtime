// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Container class for the results of a validation request.
    ///     <para>
    ///         Use the static <see cref="ValidationResult.Success" /> to represent successful validation.
    ///     </para>
    /// </summary>
    /// <seealso cref="ValidationAttribute.GetValidationResult" />
    public class ValidationResult
    {
        #region Member Fields

        /// <summary>
        ///     Gets a <see cref="ValidationResult" /> that indicates Success.
        /// </summary>
        /// <remarks>
        ///     The <c>null</c> value is used to indicate success.  Consumers of <see cref="ValidationResult" />s
        ///     should compare the values to <see cref="ValidationResult.Success" /> rather than checking for null.
        /// </remarks>
        public static readonly ValidationResult? Success;

        #endregion

        #region All Constructors

        /// <summary>
        ///     Constructor that accepts an error message.  This error message would override any error message
        ///     provided on the <see cref="ValidationAttribute" />.
        /// </summary>
        /// <param name="errorMessage">
        ///     The user-visible error message.  If null, <see cref="ValidationAttribute.GetValidationResult" />
        ///     will use <see cref="ValidationAttribute.FormatErrorMessage" /> for its error message.
        /// </param>
        public ValidationResult(string? errorMessage)
            : this(errorMessage, null)
        {
        }

        /// <summary>
        ///     Constructor that accepts an error message as well as a list of member names involved in the validation.
        ///     This error message would override any error message provided on the <see cref="ValidationAttribute" />.
        /// </summary>
        /// <param name="errorMessage">
        ///     The user-visible error message.  If null, <see cref="ValidationAttribute.GetValidationResult" />
        ///     will use <see cref="ValidationAttribute.FormatErrorMessage" /> for its error message.
        /// </param>
        /// <param name="memberNames">
        ///     The list of member names affected by this result.
        ///     This list of member names is meant to be used by presentation layers to indicate which fields are in error.
        /// </param>
        public ValidationResult(string? errorMessage, IEnumerable<string>? memberNames)
        {
            ErrorMessage = errorMessage;
            MemberNames = memberNames ?? Array.Empty<string>();
        }

        /// <summary>
        ///     Constructor that creates a copy of an existing ValidationResult.
        /// </summary>
        /// <param name="validationResult">The validation result.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="validationResult" /> is null.</exception>
        protected ValidationResult(ValidationResult validationResult)
        {
            ArgumentNullException.ThrowIfNull(validationResult);

            ErrorMessage = validationResult.ErrorMessage;
            MemberNames = validationResult.MemberNames;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the collection of member names affected by this result.  The collection may be empty but will never be null.
        /// </summary>
        public IEnumerable<string> MemberNames { get; }

        /// <summary>
        ///     Gets the error message for this result.  It may be null.
        /// </summary>
        public string? ErrorMessage { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Override the string representation of this instance, returning
        ///     the <see cref="ErrorMessage" /> if not <c>null</c>, otherwise
        ///     the base <see cref="object.ToString" /> result.
        /// </summary>
        /// <remarks>
        ///     If the <see cref="ErrorMessage" /> is empty, it will still qualify
        ///     as being specified, and therefore returned from <see cref="ToString" />.
        /// </remarks>
        /// <returns>
        ///     The <see cref="ErrorMessage" /> property value if specified,
        ///     otherwise, the base <see cref="object.ToString" /> result.
        /// </returns>
        public override string ToString() => ErrorMessage ?? base.ToString()!;
        #endregion Methods
    }
}
