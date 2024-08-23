// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.DependencyModel;

namespace System.Collections.Generic
{
    public static partial class CollectionExtensions
    {
        internal static Dictionary<string, T> LibraryCollectionToDictionary<T>(this IReadOnlyList<T> collection) where T : Library
        {
            // On .NET Core, when a duplicate key is added to a Dictionary, the exception message contains
            // the duplicate key value, so just use ToDictionary.
            return collection.ToDictionary(l => l.Name, StringComparer.OrdinalIgnoreCase);
        }
    }
}
