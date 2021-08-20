// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyModel
{
    using System;
    using System.Collections.Generic;

    internal static class LibraryCollectionExtensions

    {
        public static Dictionary<string, T> LibraryCollectionToDictionary<T>(this IReadOnlyList<T> collection) where T : Library
        {
            var dictionary = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in collection)
            {
                if (dictionary.ContainsKey(element.Name))
                {
                    throw new ArgumentException(SR.Format(SR.DuplicateItem, element.Name, collection.GetType().ToString()));
                }

                dictionary.Add(element.Name, element);
            }

            return dictionary;
        }
    }
}
