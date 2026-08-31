// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Provides a way for an object to be validated asynchronously.
    /// </summary>
    /// <remarks>
    ///     When an object implements <see cref="IAsyncValidatableObject"/>, the asynchronous
    ///     <see cref="Validator"/> APIs (such as
    ///     <see cref="Validator.TryValidateObjectAsync(object, ValidationContext, System.Collections.Generic.ICollection{ValidationResult}?, System.Threading.CancellationToken)"/>)
    ///     invoke only <see cref="ValidateAsync"/>; <see cref="IValidatableObject.Validate"/>
    ///     is not called on the async path. The synchronous <see cref="Validator"/>
    ///     APIs continue to invoke <see cref="IValidatableObject.Validate"/>.
    ///     <para>
    ///         Implementors should provide a synchronous fallback in
    ///         <see cref="IValidatableObject.Validate"/> for compatibility with callers that
    ///         do not use the async APIs, or throw <see cref="InvalidOperationException"/>
    ///         if no synchronous implementation is feasible.
    ///     </para>
    /// </remarks>
    public interface IAsyncValidatableObject : IValidatableObject
    {
        /// <summary>
        ///     Determines whether the specified object is valid asynchronously, yielding
        ///     validation results as each check completes.
        /// </summary>
        /// <param name="validationContext">
        ///     A <see cref="ValidationContext" /> instance that provides context about the validation operation,
        ///     such as the object and member being validated.
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        ///     An <see cref="IAsyncEnumerable{T}" /> that yields <see cref="ValidationResult" /> instances
        ///     as each validation check completes.
        /// </returns>
        /// <remarks>
        ///     Implementors should also provide a synchronous implementation of
        ///     <see cref="IValidatableObject.Validate"/> for compatibility with callers that
        ///     do not use the async APIs, or throw <see cref="InvalidOperationException"/>
        ///     if no synchronous implementation is feasible.
        /// </remarks>
        IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            CancellationToken cancellationToken = default);
    }
}
