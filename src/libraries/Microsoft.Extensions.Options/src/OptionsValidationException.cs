// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Represents the exception that's thrown when options validation fails.
    /// </summary>
    public class OptionsValidationException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="OptionsValidationException"/>.
        /// </summary>
        /// <param name="optionsName">The name of the options instance that failed.</param>
        /// <param name="optionsType">The options type that failed.</param>
        /// <param name="failureMessages">The validation failure messages.</param>
        public OptionsValidationException(string optionsName, Type optionsType, IEnumerable<string>? failureMessages)
        {
            ArgumentNullException.ThrowIfNull(optionsName);
            ArgumentNullException.ThrowIfNull(optionsType);

            Failures = failureMessages ?? new List<string>();
            OptionsType = optionsType;
            OptionsName = optionsName;
        }

        /// <summary>
        /// Gets the name of the options instance that failed.
        /// </summary>
        public string OptionsName { get; }

        /// <summary>
        /// Gets the type of the options that failed.
        /// </summary>
        public Type OptionsType { get; }

        /// <summary>
        /// Gets the validation failures.
        /// </summary>
        public IEnumerable<string> Failures { get; }

        /// <summary>
        /// Gets a semicolon-separated list of the <see cref="Failures"/>.
        /// </summary>
        public override string Message => string.Join("; ", Failures);
    }
}
