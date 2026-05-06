// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.ComponentModel.DataAnnotations.Schema
{
    /// <summary>
    ///     Specifies the inverse of a navigation property that represents the other end of the same relationship.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class InversePropertyAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InversePropertyAttribute" /> class.
        /// </summary>
        /// <param name="property">The navigation property representing the other end of the same relationship.</param>
        public InversePropertyAttribute(string property)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(property);
            Property = property;
        }

        /// <summary>
        ///     The navigation property representing the other end of the same relationship.
        /// </summary>
        public string Property { get; }
    }
}
