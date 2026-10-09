// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Base class for validation attributes that require asynchronous operations, such as database lookups or API calls.
    ///     Derived implementations must be thread-safe, as instances of this attribute may be invoked concurrently from multiple threads.
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
        /// <remarks>
        ///     <para>
        ///         Synchronous validation consumers invoke this method. Provide a synchronous implementation when it
        ///         can evaluate the applicable rule, and return a validation error when the rule rejects the value.
        ///         If a required rule applies but cannot be evaluated synchronously, throw
        ///         <see cref="InvalidOperationException" /> with a message directing callers to an asynchronous
        ///         validation entry point. An unsupported invocation does not establish that the value is invalid.
        ///     </para>
        ///     <para>
        ///         Return <see cref="ValidationResult.Success" /> without evaluating the rule only when the rule does
        ///         not apply. Do not return success merely because a required asynchronous check applies but cannot run
        ///         synchronously. Success does not indicate pending validation or arrange a later asynchronous invocation.
        ///     </para>
        ///     <para>
        ///         Do not implement this method by blocking on asynchronous work through
        ///         <c>Task&lt;TResult&gt;.Result</c>, <c>Task.Wait()</c>, or
        ///         <c>GetAwaiter().GetResult()</c>. Wrapping the asynchronous operation in
        ///         <c>Task.Run</c> does not make a blocking wait appropriate.
        ///     </para>
        /// </remarks>
        /// <param name="value">The value to validate.</param>
        /// <param name="validationContext">
        ///     A <see cref="ValidationContext" /> instance that provides context about the validation operation,
        ///     such as the object and member being validated. Provides access to services required to perform
        ///     validation using <see cref="IServiceProvider" />. This value can be <see langword="null" /> when the
        ///     contextless <see cref="ValidationAttribute.IsValid(object?)" /> overload invokes this method.
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
        /// <remarks>
        ///     <para>
        ///         This method must perform all applicable checks, including checks that can run synchronously.
        ///         The synchronous <see cref="IsValid(object?, ValidationContext)" /> implementation is not
        ///         automatically invoked before this method. Share common checks through a helper when needed.
        ///     </para>
        ///     <para>
        ///         Implementations must observe the supplied <paramref name="cancellationToken" /> and stop work
        ///         promptly when cancellation is requested. The validation infrastructure may cancel this token after
        ///         a validation failure to stop sibling validators and awaits all started validation tasks before
        ///         returning. An implementation that ignores cancellation can delay failure and short-circuiting.
        ///     </para>
        /// </remarks>
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
        /// <remarks>
        ///     <para>
        ///         The underlying <see cref="IsValidAsync(object, ValidationContext, CancellationToken)" /> implementation
        ///         must observe the supplied <paramref name="cancellationToken" /> and stop work promptly when cancellation
        ///         is requested. The validation infrastructure awaits all started validation tasks before returning, so an
        ///         implementation that ignores cancellation can delay failure and short-circuiting.
        ///     </para>
        ///     <para>
        ///         Callers that need to bound validation time should pass a token configured to cancel after a timeout,
        ///         such as one from a <see cref="CancellationTokenSource" /> configured with
        ///         <see cref="CancellationTokenSource.CancelAfter(TimeSpan)" />.
        ///     </para>
        /// </remarks>
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
