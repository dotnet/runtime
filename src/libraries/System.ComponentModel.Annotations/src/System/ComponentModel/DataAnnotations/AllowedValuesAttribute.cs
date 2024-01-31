﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Specifies a list of values that should be allowed in a property.
    /// </summary>
    [CLSCompliant(false)]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public class AllowedValuesAttribute : ValidationAttribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AllowedValuesAttribute"/> class.
        /// </summary>
        /// <param name="values">
        ///     A list of values that the validated value should be equal to.
        /// </param>
        public AllowedValuesAttribute(params object?[] values)
        {
            ArgumentNullException.ThrowIfNull(values);
            Values = values;
            DefaultErrorMessage = SR.AllowedValuesAttribute_Invalid;
        }

        /// <summary>
        ///     Gets the list of values allowed by this attribute.
        /// </summary>
        public object?[] Values { get; }

        /// <summary>
        ///     Determines whether a specified object is valid. (Overrides <see cref="ValidationAttribute.IsValid(object)" />)
        /// </summary>
        /// <param name="value">The object to validate.</param>
        /// <returns>
        ///     <see langword="true" /> if any of the <see cref="Values"/> are equal to <paramref name="value"/>,
        ///     otherwise <see langword="false" />
        /// </returns>
        /// <remarks>
        ///     This method can return <see langword="true"/> if the <paramref name="value" /> is <see langword="null"/>,
        ///     provided that <see langword="null"/> is also specified in one of the <see cref="Values"/>.
        /// </remarks>
        public override bool IsValid(object? value)
        {
            foreach (object? allowed in Values)
            {
                if (allowed is null ? value is null : allowed.Equals(value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
