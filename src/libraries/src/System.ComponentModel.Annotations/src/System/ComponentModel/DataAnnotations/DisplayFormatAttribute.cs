// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Allows overriding various display-related options for a given field. The options have the same meaning as in
    ///     BoundField.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DisplayFormatAttribute : Attribute
    {
        /// <summary>
        ///     Default constructor
        /// </summary>
        public DisplayFormatAttribute()
        {
            ConvertEmptyStringToNull = true; // default to true to match behavior in related components

            HtmlEncode = true; // default to true to match behavior in related components
        }

        /// <summary>
        ///     Gets or sets the format string
        /// </summary>
        public string DataFormatString { get; set; }

        /// <summary>
        ///     Gets or sets the string to display when the value is null
        /// </summary>
        public string NullDisplayText { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether empty strings should be set to null
        /// </summary>
        public bool ConvertEmptyStringToNull { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the format string should be used in edit mode
        /// </summary>
        public bool ApplyFormatInEditMode { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the field should be html encoded
        /// </summary>
        public bool HtmlEncode { get; set; }
    }
}
