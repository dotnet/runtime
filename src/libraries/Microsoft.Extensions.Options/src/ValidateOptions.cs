// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of <see cref="IValidateOptions{TOptions}"/>
    /// </summary>
    /// <typeparam name="TOptions">The instance being validated.</typeparam>
    public class ValidateOptions<TOptions> : IValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="validation"></param>
        /// <param name="failureMessage"></param>
        public ValidateOptions(string name, Func<TOptions, bool> validation, string failureMessage)
        {
            Name = name;
            Validation = validation;
            FailureMessage = failureMessage;
        }

        /// <summary>
        /// The options name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The validation action.
        /// </summary>
        public Func<TOptions, bool> Validation { get; }

        /// <summary>
        /// The error to return when validation fails.
        /// </summary>
        public string FailureMessage { get; }

        /// <summary>
        /// Validates a specific named options instance (or all when <paramref name="name"/> is null).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            // Null name is used to configure all named options.
            if (Name == null || name == Name)
            {
                if ((Validation?.Invoke(options)).Value)
                {
                    return ValidateOptionsResult.Success;
                }
                return ValidateOptionsResult.Fail(FailureMessage);
            }

            // Ignored if not validating this instance.
            return ValidateOptionsResult.Skip;
        }
    }
}
