// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    /// <summary>
    /// Determines how a given class is treated when it is (de)serialized.
    /// </summary>
    /// <remarks>
    /// Although bit flags are used, a given ConverterStrategy can only be one value.
    /// Bit flags are used to efficiently compare against more than one value.
    /// </remarks>
    internal enum ConverterStrategy : byte
    {
        /// <summary>
        /// Default value; not used by any converter.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Objects with properties.
        /// </summary>
        Object = 0x1,
        /// <summary>
        /// Simple values or user-provided custom converters.
        /// </summary>
        Value = 0x2,
        /// <summary>
        /// Enumerable collections except dictionaries.
        /// </summary>
        Enumerable = 0x8,
        /// <summary>
        /// Dictionary types.
        /// </summary>
        Dictionary = 0x10,
    }
}
