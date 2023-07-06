// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Marks a field or property to be enumerated, and each enumerated object to be validated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ValidateEnumeratedItemsAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateEnumeratedItemsAttribute"/> class.
        /// </summary>
        /// <remarks>
        /// Using this constructor for a field/property tells the code generator to
        /// generate validation for the individual members of the enumerable's type.
        /// </remarks>
        public ValidateEnumeratedItemsAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateEnumeratedItemsAttribute"/> class.
        /// </summary>
        /// <param name="validator">A type that implements <see cref="IValidateOptions{T}" /> for the enumerable's type.</param>
        /// <remarks>
        /// Using this constructor for a field/property tells the code generator to use the given type to validate
        /// the object held by the enumerable.
        /// </remarks>
        public ValidateEnumeratedItemsAttribute(Type validator)
        {
            Validator = validator;
        }

        /// <summary>
        /// Gets the type to use to validate the enumerable's objects.
        /// </summary>
        public Type? Validator { get; }
    }
}
