// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
        private string? _asyncStatusMessage;
        private Func<string?>? _asyncStatusMessageResourceAccessor;
        private string? _asyncStatusMessageResourceName;
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        private Type? _asyncStatusMessageResourceType;

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
        ///     Gets the localized status message string that describes an asynchronous validation operation in progress.
        /// </summary>
        /// <value>
        ///     The localized status message, or <see langword="null" /> when no asynchronous status message is configured.
        /// </value>
        /// <exception cref="InvalidOperationException">The asynchronous status message resource configuration is invalid.</exception>
        protected string? AsyncStatusMessageString
        {
            get
            {
                SetupAsyncStatusMessageResourceAccessor();
                return _asyncStatusMessageResourceAccessor?.Invoke();
            }
        }

        /// <summary>
        ///     Gets or sets the explicit message that a user interface may show while asynchronous validation is in progress.
        /// </summary>
        /// <value>
        ///     A non-localized asynchronous status message template, or <see langword="null" /> if no status message is configured.
        ///     Use <see cref="AsyncStatusMessageResourceType" /> and <see cref="AsyncStatusMessageResourceName" /> for
        ///     localizable status messages.
        /// </value>
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
        public string? AsyncStatusMessage
        {
            get => _asyncStatusMessage;
            set
            {
                _asyncStatusMessage = value;
                _asyncStatusMessageResourceAccessor = null;
            }
        }

        /// <summary>
        ///     Gets or sets the resource name to use as the key for asynchronous status message lookups on the resource type.
        /// </summary>
        /// <value>
        ///     The name of the property within <see cref="AsyncStatusMessageResourceType" /> that provides a localized
        ///     asynchronous status message.
        /// </value>
        public string? AsyncStatusMessageResourceName
        {
            get => _asyncStatusMessageResourceName;
            set
            {
                _asyncStatusMessageResourceName = value;
                _asyncStatusMessageResourceAccessor = null;
            }
        }

        /// <summary>
        ///     Gets or sets the resource type to use for asynchronous status message lookups.
        /// </summary>
        /// <value>
        ///     The type containing the static string property named by <see cref="AsyncStatusMessageResourceName" />.
        /// </value>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public Type? AsyncStatusMessageResourceType
        {
            get => _asyncStatusMessageResourceType;
            set
            {
                _asyncStatusMessageResourceType = value;
                _asyncStatusMessageResourceAccessor = null;
            }
        }

        /// <summary>
        ///     Formats the message that a user interface may show while asynchronous validation is in progress.
        /// </summary>
        /// <remarks>
        ///     This method only formats metadata and does not start validation or represent validation state.
        ///     The status message is re-evaluated every time this method is called and the resolved template is passed to
        ///     <see cref="ValidationAttribute.FormatMessage" />.
        /// </remarks>
        /// <param name="name">The user-visible name to include in the formatted message.</param>
        /// <returns>
        ///     The formatted asynchronous status message, or <see langword="null" /> when no status message is configured.
        /// </returns>
        /// <exception cref="InvalidOperationException">The asynchronous status message resource configuration is invalid.</exception>
        /// <exception cref="FormatException">The asynchronous status message is not a valid composite format string.</exception>
        public virtual string? FormatAsyncStatusMessage(string name)
        {
            string? asyncStatusMessage = AsyncStatusMessageString;
            return asyncStatusMessage is null ? null : FormatMessage(asyncStatusMessage, name);
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
        /// <remarks>
        ///     Implementations must observe the supplied <paramref name="cancellationToken" /> and stop work promptly
        ///     when cancellation is requested. The validation infrastructure may cancel this token after a validation
        ///     failure to stop sibling validators and awaits all started validation tasks before returning. An
        ///     implementation that ignores cancellation can delay failure and short-circuiting.
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

        private void SetupAsyncStatusMessageResourceAccessor()
        {
            if (_asyncStatusMessageResourceAccessor is not null)
            {
                return;
            }

            if (string.IsNullOrEmpty(_asyncStatusMessage) &&
                string.IsNullOrEmpty(_asyncStatusMessageResourceName) &&
                _asyncStatusMessageResourceType is null)
            {
                return;
            }

            _asyncStatusMessageResourceAccessor = CreateMessageResourceAccessor(
                _asyncStatusMessage,
                _asyncStatusMessageResourceName,
                _asyncStatusMessageResourceType,
                SR.AsyncValidationAttribute_Cannot_Set_AsyncStatusMessage_And_Resource,
                SR.AsyncValidationAttribute_NeedBothAsyncStatusResourceTypeAndResourceName);
        }
    }
}
