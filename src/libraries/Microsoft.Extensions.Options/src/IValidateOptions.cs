// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Interface used to validate options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to validate.</typeparam>
    public interface IValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Validates a specific named options instance (or all when name is null).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        ValidateOptionsResult Validate(string name, TOptions options);
    }
}