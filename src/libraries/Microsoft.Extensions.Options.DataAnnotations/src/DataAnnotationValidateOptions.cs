// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
        /// <param name="name">The name of the option.</param>
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
            if (Name != null && Name != name)
            {
                // Ignored if not validating this instance.
                return ValidateOptionsResult.Skip;
            }

            // Ensure options are provided to validate against
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var validationResults = new List<ValidationResult>();
            if (Validator.TryValidateObject(options, new ValidationContext(options), validationResults, validateAllProperties: true))
            {
                return ValidateOptionsResult.Success;
            }

            string typeName = options.GetType().Name;
            var errors = new List<string>();
            foreach (ValidationResult result in validationResults)
            {
                errors.Add($"DataAnnotation validation failed for '{typeName}' members: '{string.Join(",", result.MemberNames)}' with the error: '{result.ErrorMessage}'.");
            }

            return ValidateOptionsResult.Fail(errors);
        }
    }
}
