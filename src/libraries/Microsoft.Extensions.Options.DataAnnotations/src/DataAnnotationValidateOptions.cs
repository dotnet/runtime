// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of <see cref="IValidateOptions{TOptions}"/> that uses DataAnnotation's <see cref="Validator"/> for validation.
    /// </summary>
    /// <typeparam name="TOptions">The instance being validated.</typeparam>
    public class DataAnnotationValidateOptions<TOptions> : IValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        public DataAnnotationValidateOptions(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The options name.
        /// </summary>
        public string Name { get; }

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
                var validationResults = new List<ValidationResult>();
                if (Validator.TryValidateObject(options,
                    new ValidationContext(options, serviceProvider: null, items: null), 
                    validationResults, 
                    validateAllProperties: true))
                {
                    return ValidateOptionsResult.Success;
                }

                var errors = new List<string>();
                foreach (var r in validationResults)
                {
                    errors.Add($"DataAnnotation validation failed for members: '{String.Join(",", r.MemberNames)}' with the error: '{r.ErrorMessage}'.");
                }
                return ValidateOptionsResult.Fail(errors);
            }

            // Ignored if not validating this instance.
            return ValidateOptionsResult.Skip;
        }
    }
}
