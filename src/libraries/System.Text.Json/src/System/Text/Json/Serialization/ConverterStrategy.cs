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
        // Default - no class type.
        None = 0x0,
        // JsonObjectConverter<> - objects with properties.
        Object = 0x1,
        // JsonConverter<> - simple values.
        Value = 0x2,
        // JsonIEnumerableConverter<> - all enumerable collections except dictionaries.
        Enumerable = 0x8,
        // JsonDictionaryConverter<,> - dictionary types.
        Dictionary = 0x10,
    }
}
