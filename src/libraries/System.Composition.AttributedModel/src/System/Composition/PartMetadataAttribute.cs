// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Composition
{
    /// <summary>
    ///     Specifies metadata for a type to be used as a part.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PartMetadataAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PartMetadataAttribute"/> with the 
        ///     specified name and metadata value.
        /// </summary>
        /// <param name="name">
        ///     A <see cref="string"/> containing the name of the metadata value; or 
        ///     <see langword="null"/> to use an empty string ("").
        /// </param>
        /// <param name="value">
        ///     An <see cref="object"/> containing the metadata value. This can be 
        ///     <see langword="null"/>.
        /// </param>
        public PartMetadataAttribute(string name, object value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

        /// <summary>
        ///     Gets the name of the metadata value.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> containing the name of the metadata value.
        /// </value>
        public string Name { get; }

        /// <summary>
        ///     Gets the metadata value.
        /// </summary>
        /// <value>
        ///     An <see cref="object"/> containing the metadata value.
        /// </value>
        public object Value { get; }
    }
}
