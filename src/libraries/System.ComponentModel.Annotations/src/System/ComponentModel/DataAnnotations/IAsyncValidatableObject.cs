// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Provides a way for an object to be validated asynchronously.
    ///     Inherits from <see cref="IValidatableObject"/>. Implementors must provide both
    ///     <see cref="IValidatableObject.Validate"/> and <see cref="ValidateAsync"/>.
    /// </summary>
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
        IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            CancellationToken cancellationToken = default);
    }
}
