// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Exception used for validation using <see cref="ValidationAttribute" />.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class ValidationException : Exception
    {
        private ValidationResult? _validationResult;

        /// <summary>
        ///     Constructor that accepts a structured <see cref="ValidationResult" /> describing the problem.
        /// </summary>
        /// <param name="validationResult">The value describing the validation error</param>
        /// <param name="validatingAttribute">The attribute that triggered this exception</param>
        /// <param name="value">The value that caused the validating attribute to trigger the exception</param>
        public ValidationException(ValidationResult validationResult, ValidationAttribute? validatingAttribute,
            object? value)
            : this(validationResult.ErrorMessage, validatingAttribute, value)
        {
            _validationResult = validationResult;
        }

        /// <summary>
        ///     Constructor that accepts an error message, the failing attribute, and the invalid value.
        /// </summary>
        /// <param name="errorMessage">The localized error message</param>
        /// <param name="validatingAttribute">The attribute that triggered this exception</param>
        /// <param name="value">The value that caused the validating attribute to trigger the exception</param>
        public ValidationException(string? errorMessage, ValidationAttribute? validatingAttribute, object? value)
            : base(errorMessage)
        {
            Value = value;
            ValidationAttribute = validatingAttribute;
        }

        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <remarks>
        ///     To include the failing validation attribute and invalid value in the exception, use
        ///     <see cref="ValidationException(string, ValidationAttribute, object)" />.
        /// </remarks>
        public ValidationException()
        {
        }

        /// <summary>
        ///     Constructor that accepts only a localized message
        /// </summary>
        /// <remarks>
        ///     To include the failing validation attribute and invalid value in the exception, use the
        ///     <see cref="ValidationException(string, ValidationAttribute, object)" /> overload.
        ///     That overload preserves the attribute in <see cref="ValidationAttribute" />
        ///     and the value in <see cref="Value" />.
        /// </remarks>
        /// <param name="message">The localized message</param>
        public ValidationException(string? message)
            : base(message)
        {
        }

        /// <summary>
        ///     Constructor that accepts a localized message and an inner exception
        /// </summary>
        /// <remarks>
        ///     Use this overload to preserve an exception that caused the validation failure.
        ///     To include the failing validation attribute and invalid value instead, use
        ///     <see cref="ValidationException(string, ValidationAttribute, object)" />.
        /// </remarks>
        /// <param name="message">The localized error message</param>
        /// <param name="innerException">inner exception</param>
        public ValidationException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Constructor that takes a SerializationInfo.
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected ValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        ///     Gets the <see>ValidationAttribute</see> instance that triggered this exception.
        /// </summary>
        public ValidationAttribute? ValidationAttribute { get; }

        /// <summary>
        ///     Gets the <see cref="ValidationResult" /> instance that describes the validation error.
        /// </summary>
        /// <value>
        ///     This property will never be null.
        /// </value>
        public ValidationResult ValidationResult =>
            _validationResult ??= new ValidationResult(Message);

        /// <summary>
        ///     Gets the value that caused the validating attribute to trigger the exception
        /// </summary>
        public object? Value { get; }
    }
}
