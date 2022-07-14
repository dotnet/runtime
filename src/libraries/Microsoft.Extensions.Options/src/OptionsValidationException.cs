// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Thrown when options validation fails.
    /// </summary>
    public class OptionsValidationException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="optionsName">The name of the options instance that failed.</param>
        /// <param name="optionsType">The options type that failed.</param>
        /// <param name="failureMessages">The validation failure messages.</param>
        public OptionsValidationException(string optionsName, Type optionsType, IEnumerable<string>? failureMessages)
        {
            ThrowHelper.ThrowIfNull(optionsName);
            ThrowHelper.ThrowIfNull(optionsType);

            Failures = failureMessages ?? new List<string>();
            OptionsType = optionsType;
            OptionsName = optionsName;
        }

        /// <summary>
        /// The name of the options instance that failed.
        /// </summary>
        public string OptionsName { get; }

        /// <summary>
        /// The type of the options that failed.
        /// </summary>
        public Type OptionsType { get; }

        /// <summary>
        /// The validation failures.
        /// </summary>
        public IEnumerable<string> Failures { get; }

        /// <summary>
        /// The message is a semicolon separated list of the <see cref="Failures"/>.
        /// </summary>
        public override string Message => string.Join("; ", Failures);
    }
}
