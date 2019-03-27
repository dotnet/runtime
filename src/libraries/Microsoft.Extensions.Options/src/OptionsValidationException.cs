// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public OptionsValidationException(string optionsName, Type optionsType, IEnumerable<string> failureMessages)
        {
            Failures = failureMessages ?? new List<string>();
            OptionsType = optionsType ?? throw new ArgumentNullException(nameof(optionsType));
            OptionsName = optionsName ?? throw new ArgumentNullException(nameof(optionsName));
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
    }
}