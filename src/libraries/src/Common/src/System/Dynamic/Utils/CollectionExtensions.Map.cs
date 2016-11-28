// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Dynamic.Utils
{
    internal static partial class CollectionExtensions
    {
        // Name needs to be different so it doesn't conflict with Enumerable.Select
        public static U[] Map<T, U>(this T[] array, Func<T, U> select)
        {
            int count = array.Length;

            U[] result = new U[count];
            
            for (int i = 0; i < count; i++)
            {
                result[i] = select(array[i]);
            }

            return result;
        }
    }
}
