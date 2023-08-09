// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of <see cref="IValidateOptions{TOptions}"/> that uses DataAnnotation's <see cref="Validator"/> for validation.
    /// </summary>
    /// <typeparam name="TOptions">The instance being validated.</typeparam>
    public class DataAnnotationValidateOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TOptions>
        : IValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The name of the option.</param>
        [RequiresUnreferencedCode("The implementation of Validate method on this type will walk through all properties of the passed in options object, and its type cannot be " +
            "statically analyzed so its members may be trimmed.")]
        public DataAnnotationValidateOptions(string? name)
        {
            Name = name;
        }

        /// <summary>
        /// The options name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Validates a specific named options instance (or all when <paramref name="name"/> is null).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Suppressing the warnings on this method since the constructor of the type is annotated as RequiresUnreferencedCode.")]
        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            // Null name is used to configure all named options.
            if (Name != null && Name != name)
            {
                // Ignored if not validating this instance.
                return ValidateOptionsResult.Skip;
            }

            // Ensure options are provided to validate against
            ThrowHelper.ThrowIfNull(options);

            var validationResults = new List<ValidationResult>();
            HashSet<object>? visited = null;
            List<string>? errors = null;

            if (TryValidateOptions(options, options.GetType().Name, validationResults, ref errors, ref visited))
            {
                return ValidateOptionsResult.Success;
            }

            Debug.Assert(errors is not null && errors.Count > 0);

            return ValidateOptionsResult.Fail(errors);
        }

        [RequiresUnreferencedCode("This method on this type will walk through all properties of the passed in options object, and its type cannot be " +
            "statically analyzed so its members may be trimmed.")]
        private static bool TryValidateOptions(object options, string qualifiedName, List<ValidationResult> results, ref List<string>? errors, ref HashSet<object>? visited)
        {
            if (visited is not null && visited.Contains(options))
            {
                return true;
            }

            results?.Clear();

            bool res = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
            if (!res)
            {
                errors ??= new List<string>();

                foreach (ValidationResult result in results!)
                {
                    errors.Add($"DataAnnotation validation failed for '{qualifiedName}' members: '{string.Join(",", result.MemberNames)}' with the error: '{result.ErrorMessage}'.");
                }
            }

            foreach (PropertyInfo propertyInfo in options.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (propertyInfo.GetMethod is null)
                {
                    continue;
                }

                object? value = propertyInfo!.GetValue(options);

                if (value is null)
                {
                    continue;
                }

                if (propertyInfo.GetCustomAttribute<ValidateObjectMembersAttribute>() is not null)
                {
                    visited ??= new HashSet<object>(new ReferenceComparer());
                    visited.Add(options);

                    results ??= new List<ValidationResult>();
                    res = TryValidateOptions(value, $"{qualifiedName}.{propertyInfo.Name}", results, ref errors, ref visited) && res;
                }
                else if (propertyInfo.GetCustomAttribute<ValidateEnumeratedItemsAttribute>() is not null &&
                        value is IEnumerable enumerable)
                {
                    visited ??= new HashSet<object>(new ReferenceComparer());
                    visited.Add(options);
                    results ??= new List<ValidationResult>();

                    int index = 0;
                    foreach (object item in enumerable)
                    {
                        res = TryValidateOptions(item, $"{qualifiedName}.{propertyInfo.Name}[{index++}]", results, ref errors, ref visited) && res;
                    }
                }
            }

            return res;
        }

        private readonly struct ReferenceComparer : IEqualityComparer<object>
        {
            public ReferenceComparer() { }
            bool IEqualityComparer<object>.Equals(object? x, object? y) => object.ReferenceEquals(x, y);
            int IEqualityComparer<object>.GetHashCode(object obj) => obj is null ? 0 : obj.GetHashCode();
        }
    }
}
