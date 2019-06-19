// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    /// Determines how a given class is treated when it is (de)serialized.
    /// </summary>
    internal enum ClassType
    {
        // typeof(object)
        Unknown = 0,
        // POCO or rich data type
        Object = 1,
        // Value or object with a converter.
        Value = 2,
        // IEnumerable
        Enumerable = 3,
        // IDictionary
        Dictionary = 4,
        // Is deserialized by passing a IDictionary to its constructor
        // i.e. immutable dictionaries, Hashtable, SortedList,
        IDictionaryConstructible = 5,
    }
}
