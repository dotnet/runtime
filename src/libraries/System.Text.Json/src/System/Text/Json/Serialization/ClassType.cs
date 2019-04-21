// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Determines how a given class is treated when it is (de)serialized.
    /// </summary>
    internal enum ClassType
    {
        Unknown = 0,        // typeof(object)
        Object = 1,         // POCO or rich data type
        Value = 2,          // Data type with single value
        Enumerable = 3,     // IEnumerable
        Dictionary = 4,     // IDictionary
    }
}
