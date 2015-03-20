// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Dynamic.Utils
{
    internal static partial class CollectionExtensions
    {
        public static T[] Copy<T>(this T[] array)
        {
            T[] copy = new T[array.Length];
            Array.Copy(array, copy, array.Length);
            return copy;
        }
    }
}
