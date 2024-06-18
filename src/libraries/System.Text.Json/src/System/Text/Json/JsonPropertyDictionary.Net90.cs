// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.Json
{
    /// <summary>
    /// Defines an ordered dictionary for storing JSON property metadata.
    /// </summary>
    internal sealed class JsonPropertyDictionary<T>(StringComparer comparer, int capacity = 0)
        : OrderedDictionary<string, T>(capacity, comparer);
}
