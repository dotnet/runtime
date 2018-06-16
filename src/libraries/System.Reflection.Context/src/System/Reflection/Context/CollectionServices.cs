// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Reflection.Context
{
    internal static class CollectionServices
    {
        public static T[] Empty<T>()
        {
            return Array.Empty<T>();
        }

        public static bool CompareArrays<T>(T[] left, T[] right)
        {
            if (left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (!left[i].Equals(right[i]))
                    return false;
            }

            return true;
        }

        public static int GetArrayHashCode<T>(T[] array)
        {
            int hashcode = 0;
            foreach (T t in array)
                hashcode ^= t.GetHashCode();

            return hashcode;
        }

        public static object[] ConvertListToArray(List<object> list, Type arrayType)
        {
            // Mimic the behavior of GetCustomAttributes in runtime reflection.
            if (arrayType.HasElementType || arrayType.IsValueType || arrayType.ContainsGenericParameters)
                return list.ToArray();
            
            // Converts results to typed array.
            Array typedArray = Array.CreateInstance(arrayType, list.Count);

            list.CopyTo((object[])typedArray);

            return (object[])typedArray;
        }

        public static object[] IEnumerableToArray(IEnumerable<object> enumerable, Type arrayType)
        {
            List<object> list = new List<object>(enumerable);

            return ConvertListToArray(list, arrayType);
        }
    }
}
