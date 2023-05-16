// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Validation attribute to indicate that a property, field or parameter is required.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public class RequiredAttribute : ValidationAttribute
    {
        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <remarks>
        ///     This constructor selects a reasonable default error message for
        ///     <see cref="ValidationAttribute.FormatErrorMessage" />
        /// </remarks>
        public RequiredAttribute()
            : base(() => SR.RequiredAttribute_ValidationError)
        {
        }

        /// <summary>
        ///     Gets or sets a flag indicating whether the attribute should allow empty strings.
        /// </summary>
        public bool AllowEmptyStrings { get; set; }

        /// <summary>
        ///     Gets or sets a flag indicating whether the attribute should also disallow non-null default values.
        /// </summary>
        public bool DisallowAllDefaultValues { get; set; }

        /// <summary>
        ///     Override of <see cref="ValidationAttribute.IsValid(object)" />
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>
        ///     Returns <see langword="false" /> if the <paramref name="value" /> is null or an empty string.
        ///     If <see cref="AllowEmptyStrings" /> then <see langword="true" /> is returned for empty strings.
        ///     If <see cref="DisallowAllDefaultValues"/> then <see langword="false" /> is returned for values
        ///     that are equal to the <see langword="default" /> of the declared type.
        /// </returns>
        public override bool IsValid(object? value)
            => IsValidCore(value, validationContext: null);

        /// <inheritdoc />
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            return IsValidCore(value, validationContext)
                ? ValidationResult.Success
                : CreateFailedValidationResult(validationContext);
        }

        private bool IsValidCore(object? value, ValidationContext? validationContext)
        {
            if (value is null)
            {
                return false;
            }

            if (DisallowAllDefaultValues)
            {
                // To determine the default value of non-nullable types we need the declaring type of the value.
                // This is the property type in a validation context falling back to the runtime type for root values.
                Type declaringType = validationContext?.MemberType ?? value.GetType();
                if (GetDefaultValueForNonNullableValueType(declaringType) is object defaultValue)
                {
                    return !defaultValue.Equals(value);
                }
            }

            // only check string length if empty strings are not allowed
            return AllowEmptyStrings || value is not string stringValue || !string.IsNullOrWhiteSpace(stringValue);
        }


        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "GetUninitializedObject is only called struct types. You can always create an instance of a struct.")]
        private object? GetDefaultValueForNonNullableValueType(Type type)
        {
            object? defaultValue = _defaultValueCache;

            if (defaultValue != null && defaultValue.GetType() == type)
            {
                Debug.Assert(type.IsValueType && Nullable.GetUnderlyingType(type) is null);
            }
            else if (type.IsValueType && Nullable.GetUnderlyingType(type) is null)
            {
                _defaultValueCache = defaultValue = RuntimeHelpers.GetUninitializedObject(type);
            }
            else
            {
                defaultValue = null;
            }

            return defaultValue;
        }

        private object? _defaultValueCache;
    }
}
