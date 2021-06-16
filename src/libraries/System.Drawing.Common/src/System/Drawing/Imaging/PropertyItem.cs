// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Imaging
{
    // sdkinc\imaging.h
    /// <summary>
    /// Encapsulates a metadata property to be included in an image file.
    /// </summary>
    public sealed class PropertyItem
    {
        internal PropertyItem()
        {
        }

        /// <summary>
        /// Represents the ID of the property.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Represents the length of the property.
        /// </summary>
        public int Len { get; set; }

        /// <summary>
        /// Represents the type of the property.
        /// </summary>
        public short Type { get; set; }

        /// <summary>
        /// Contains the property value.
        /// </summary>
        public byte[]? Value { get; set; }
    }
}
