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
    ///         Provide a synchronous implementation of <see cref="IValidatableObject.Validate"/> when it can
    ///         evaluate the applicable rules, returning validation errors for rules that reject the object.
    ///         If a required rule applies but cannot be evaluated synchronously, throw
    ///         <see cref="InvalidOperationException"/> with a message directing callers to an asynchronous
    ///         validation entry point.
    ///     </para>
    ///     <para>
    ///         Returning no validation errors without evaluating a rule is appropriate only when the rule does not
    ///         apply. Do not return an empty result merely because a required asynchronous check applies but cannot
    ///         run synchronously. An empty result does not indicate pending validation or arrange a later asynchronous
    ///         invocation.
    ///     </para>
    ///     <para>
    ///         Do not implement <see cref="IValidatableObject.Validate"/> by blocking on asynchronous work or
    ///         asynchronously enumerated results.
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
        ///     <para>
        ///         This method must perform all applicable checks, including checks that can run synchronously.
        ///     </para>
        ///     <para>
        ///         The asynchronous <see cref="Validator"/> APIs do not automatically invoke
        ///         <see cref="IValidatableObject.Validate"/> before this method. Share common checks through a helper
        ///         when needed.
        ///     </para>
        /// </remarks>
        IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            CancellationToken cancellationToken = default);
    }
}
