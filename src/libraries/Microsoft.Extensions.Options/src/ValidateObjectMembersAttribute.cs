// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Marks a field or property to be validated transitively.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ValidateObjectMembersAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateObjectMembersAttribute"/> class.
        /// </summary>
        /// <remarks>
        /// Using this constructor for a field/property tells the code generator to
        /// generate validation for the individual members of the field/property's type.
        /// </remarks>
        public ValidateObjectMembersAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateObjectMembersAttribute"/> class.
        /// </summary>
        /// <param name="validator">A type that implements <see cref="IValidateOptions{T}" /> for the field/property's type.</param>
        /// <remarks>
        /// Using this constructor for a field/property tells the code generator to use the given type to validate
        /// the object held by the field/property.
        /// </remarks>
        public ValidateObjectMembersAttribute(Type validator)
        {
            Validator = validator;
        }

        /// <summary>
        /// Gets the type to use to validate a field or property.
        /// </summary>
        public Type? Validator { get; }
    }
}
