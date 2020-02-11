// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    /// Determines how a given class is treated when it is (de)serialized.
    /// </summary>
    /// <remarks>
    /// Although bit flags are used, a given ClassType can only be one value.
    /// Bit flags are used to efficiently compare against more than one value.
    /// </remarks>
    internal enum ClassType : byte
    {
        // JsonObjectConverter<> - objects with properties.
        Object = 0x1,
        // JsonConverter<> - simple values.
        Value = 0x2,
        // JsonValueConverter<> - simple values that need to re-enter the serializer such as KeyValuePair<TKey, TValue>.
        NewValue = 0x4,
        // JsonIEnumerbleConverter<> - all enumerable collections except dictionaries.
        Enumerable = 0x8,
        // JsonDictionaryConverter<,> - dictionary types.
        Dictionary = 0x10,
        // Invalid (not used directly for serialization)
        Invalid = 0x20
    }
}
