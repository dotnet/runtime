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
        // typeof(object)
        Unknown = 0x1,
        // POCO or rich data type
        Object = 0x2,
        // Value or object with a converter.
        Value = 0x4,
        // IEnumerable
        Enumerable = 0x8,
        // IDictionary
        Dictionary = 0x10,
    }
}
