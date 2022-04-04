// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var validationResults = new List<ValidationResult>();

            var errors = new List<string>();

            bool success = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, validateAllProperties: true);

            if (!success)
            {
                string typeName = options.GetType().Name;
                foreach (ValidationResult result in validationResults)
                {
                    errors.Add(
                        $"DataAnnotation validation failed for '{typeName}' members: '{string.Join(",", result.MemberNames)}' with the error: '{result.ErrorMessage}'.");
                }
            }

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(options);

            foreach (PropertyDescriptor eachProperty in properties)
            {
                if (AnyChildPropertyHasValidationAttributes(eachProperty))
                {
                    object? value = eachProperty.GetValue(options);

                    // if the user wanted this particular property to be null, then
                    // they would annotate it with `Required`.
                    if (value == null) continue;

                    var result = CreateAndRunValidator(value);

                    if (result.Failed)
                    {
                        success = false;
                        errors.AddRange(result.Failures.ToList());
                    }
                }
            }

            return success ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);

            static bool AnyChildPropertyHasValidationAttributes(PropertyDescriptor eachProperty)
            {
                var childProperties = eachProperty.GetChildProperties();

                foreach (PropertyDescriptor childProperty in childProperties)
                {
                    AttributeCollection attributes = childProperty.Attributes;

                    foreach (Attribute attribute in attributes)
                    {
                        if (attribute is ValidationAttribute)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            static ValidateOptionsResult CreateAndRunValidator(object value)
            {
                Type validatorType = typeof(DataAnnotationValidateOptions<>).MakeGenericType(value.GetType());
                object validator = Activator.CreateInstance(validatorType, args: Options.DefaultName)!;

                MethodInfo validateMethod = validator.GetType().GetMethod("Validate")!;

                ValidateOptionsResult result = (ValidateOptionsResult)validateMethod.Invoke(validator, new[] { Options.DefaultName, value })!;

                return result;
            }
        }
    }
}
