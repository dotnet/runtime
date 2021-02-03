// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System;

namespace Microsoft.Extensions.Options.Tests
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class DepValidatorAttribute
        : ValidationAttribute
    {
        public string Target { get; set; }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            object instance = validationContext.ObjectInstance;
            Type type = instance.GetType();
            var dep1 = type.GetProperty("Dep1")?.GetValue(instance);
            var dep2 = type.GetProperty(Target)?.GetValue(instance);
            if (dep1 == dep2)
            {
                return ValidationResult.Success;
            }

            return new ValidationResult("Dep1 != " + Target, new[] { "Dep1", Target });
        }
    }
}
