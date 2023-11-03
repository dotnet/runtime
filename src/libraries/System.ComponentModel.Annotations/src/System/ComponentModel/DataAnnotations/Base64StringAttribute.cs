// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Specifies that a data field value is a well-formed Base64 string.
    /// </summary>
    /// <remarks>
    ///     Recognition of valid Base64 is delegated to the <see cref="Convert"/> class,
    ///     using the <see cref="Convert.TryFromBase64String(string, Span{byte}, out int)"/> method.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class Base64StringAttribute : ValidationAttribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Base64StringAttribute"/> class.
        /// </summary>
        public Base64StringAttribute()
        {
            // Set DefaultErrorMessage not ErrorMessage, allowing user to set
            // ErrorMessageResourceType and ErrorMessageResourceName to use localized messages.
            DefaultErrorMessage = SR.Base64StringAttribute_Invalid;
        }

        /// <summary>
        ///     Determines whether a specified object is valid. (Overrides <see cref="ValidationAttribute.IsValid(object)" />)
        /// </summary>
        /// <param name="value">The object to validate.</param>
        /// <returns>
        ///     <see langword="true" /> if <paramref name="value"/> is <see langword="null"/> or is a valid Base64 string,
        ///     otherwise <see langword="false" />
        /// </returns>
        public override bool IsValid(object? value)
        {
            if (value is null)
            {
                return true;
            }

            if (value is not string valueAsString)
            {
                return false;
            }

            return Base64.IsValid(valueAsString);
        }
    }
}
