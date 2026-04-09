// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyModel;

namespace System.Collections.Generic
{
    public static partial class CollectionExtensions
    {
        /// <summary>
        /// Creates a Dictionary from List keyed off of the Library's name.
        /// </summary>
        /// <remarks>
        /// On .NET Framework, when a duplicate key is added to a Dictionary, the exception message doesn't
        /// list the duplicate element name. This method ensures the duplicate element name is logged in the exception message.
        /// </remarks>
        internal static Dictionary<string, T> LibraryCollectionToDictionary<T>(this IReadOnlyList<T> collection) where T : Library
        {
            var dictionary = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

            foreach (T element in collection)
            {
                try
                {
                    dictionary.Add(element.Name, element);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException(SR.Format(SR.DuplicateItem, element.Name));
                }
            }

            return dictionary;
        }
    }
}
