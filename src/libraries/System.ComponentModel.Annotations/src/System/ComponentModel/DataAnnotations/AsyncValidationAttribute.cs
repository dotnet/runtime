// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Base class for validation attributes that require asynchronous operations, such as database lookups or API calls.
    /// </summary>
    public abstract class AsyncValidationAttribute : ValidationAttribute
    {
        /// <summary>
        ///     Default constructor for any async validation attribute.
        /// </summary>
        protected AsyncValidationAttribute()
        {
        }

        /// <summary>
        ///     Constructor that accepts a fixed validation error message.
        /// </summary>
        /// <param name="errorMessage">A non-localized error message to use in <see cref="ValidationAttribute.ErrorMessageString" />.</param>
        protected AsyncValidationAttribute(string errorMessage)
            : base(errorMessage)
        {
        }

        /// <summary>
        ///     Allows for providing a resource accessor function that will be used by the <see cref="ValidationAttribute.ErrorMessageString" />
        ///     property to retrieve the error message.
        /// </summary>
        /// <param name="errorMessageAccessor">The <see cref="Func{T}" /> that will return an error message.</param>
        protected AsyncValidationAttribute(Func<string> errorMessageAccessor)
            : base(errorMessageAccessor)
        {
        }

        /// <summary>
        ///     Override of the base class <see cref="ValidationAttribute.IsValid(object?, ValidationContext)" /> method.
        ///     Subclasses must provide a synchronous validation implementation or throw an appropriate exception
        ///     to indicate that synchronous validation is not supported.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="validationContext">
        ///     A <see cref="ValidationContext" /> instance that provides context about the validation operation,
        ///     such as the object and member being validated. Provides access to services required to perform
        ///     validation using <see cref="IServiceProvider" />.
        /// </param>
        /// <returns>
        ///     <see cref="ValidationResult.Success" /> when validation is valid.
        ///     An instance of <see cref="ValidationResult" /> when validation is invalid.
        /// </returns>
        protected abstract override ValidationResult? IsValid(object? value, ValidationContext validationContext);

        /// <summary>
        ///     Override this method in subclasses to implement asynchronous validation logic.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="validationContext">
        ///     A <see cref="ValidationContext" /> instance that provides context about the validation operation,
        ///     such as the object and member being validated. Provides access to services required to perform
        ///     validation using <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        ///     A <see cref="Task{ValidationResult}" /> representing the asynchronous validation operation.
        ///     When validation is valid, the result is <see cref="ValidationResult.Success" />.
        ///     When validation is invalid, the result is an instance of <see cref="ValidationResult" />.
        /// </returns>
        protected abstract Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Sealed override of <see cref="ValidationAttribute.IsValid(object?)" /> that delegates to the
        ///     <see cref="ValidationContext" /> overload so that <see cref="AsyncValidationAttribute" /> implementations
        ///     only need to provide a single synchronous fallback via
        ///     <see cref="ValidationAttribute.IsValid(object?, ValidationContext)" />.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns>
        ///     <see langword="true" /> if the value is valid; otherwise, <see langword="false" />.
        /// </returns>
        public sealed override bool IsValid(object? value)
            => IsValid(value, null!) == ValidationResult.Success;

        /// <summary>
        ///     Tests whether the given <paramref name="value" /> is valid asynchronously with respect to the current
        ///     validation attribute without throwing a <see cref="ValidationException" />.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="validationContext">
        ///     A <see cref="ValidationContext" /> instance that provides context about the validation operation,
        ///     such as the object and member being validated. Provides access to services required to perform
        ///     validation using <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        ///     A <see cref="Task{ValidationResult}" /> representing the asynchronous validation operation.
        ///     When validation is valid, the result is <see cref="ValidationResult.Success" />.
        ///     When validation is invalid, the result is an instance of <see cref="ValidationResult" />.
        /// </returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
        /// <exception cref="ArgumentNullException">When <paramref name="validationContext" /> is null.</exception>
        public async Task<ValidationResult?> GetValidationResultAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(validationContext);

            ValidationResult? result = await IsValidAsync(value, validationContext, cancellationToken).ConfigureAwait(false);

            return EnsureValidationResultErrorMessage(result, validationContext);
        }

    }
}
