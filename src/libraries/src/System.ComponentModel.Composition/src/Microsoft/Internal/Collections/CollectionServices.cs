// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Internal.Collections
{
    internal static partial class CollectionServices
    {
        private static readonly Type StringType = typeof(string);
        private static readonly Type IEnumerableType = typeof(IEnumerable);
        private static readonly Type IEnumerableOfTType = typeof(IEnumerable<>);
        private static readonly Type ICollectionOfTType = typeof(ICollection<>);

        public static bool IsEnumerableOfT(Type type)
        {
            if (type.IsGenericType)
            {
                Type genericType = type.GetGenericTypeDefinition().UnderlyingSystemType;

                if (genericType == IEnumerableOfTType)
                {
                    return true;
                }
            }
            return false;
        }

        public static Type GetEnumerableElementType(Type type)
        {
            if (type.UnderlyingSystemType == StringType || !IEnumerableType.IsAssignableFrom(type))
            {
                return null;
            }

            Type closedType;
            if (ReflectionServices.TryGetGenericInterfaceType(type, IEnumerableOfTType, out closedType))
            {
                return closedType.GetGenericArguments()[0];
            }

            return null;
        }

        public static Type GetCollectionElementType(Type type)
        {
            Type closedType;
            if (ReflectionServices.TryGetGenericInterfaceType(type, ICollectionOfTType, out closedType))
            {
                return closedType.GetGenericArguments()[0];
            }

            return null;
        }

        public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> source)
        {
            Assumes.NotNull(source);

            return new ReadOnlyCollection<T>(source.AsArray());
        }

        public static IEnumerable<T> ConcatAllowingNull<T>(this IEnumerable<T> source, IEnumerable<T> second)
        {
            if (second == null || !second.FastAny())
            {
                return source;
            }

            if (source == null || !source.FastAny())
            {
                return second;
            }

            return source.Concat(second);
        }

        public static ICollection<T> ConcatAllowingNull<T>(this ICollection<T> source, ICollection<T> second)
        {
            if (second == null || (second.Count == 0))
            {
                return source;
            }

            if (source == null || (source.Count == 0))
            {
                return second;
            }

            List<T> result = new List<T>(source);
            result.AddRange(second);

            return result;
        }

        public static List<T> FastAppendToListAllowNulls<T>(this List<T> source, IEnumerable<T> second)
        {
            if (second == null)
            {
                return source;
            }

            // if there's nothing in the source, return the second
            if ((source == null) || (source.Count == 0))
            {
                return second.AsList();
            }

            // if the second is List<T>, and contains very few elements there's no need for AddRange
            List<T> secondAsList = second as List<T>;
            if (secondAsList != null)
            {
                if (secondAsList.Count == 0)
                {
                    return source;
                }
                else if (secondAsList.Count == 1)
                {
                    source.Add(secondAsList[0]);
                    return source;
                }
            }

            // last resort - nothing is null, need to append
            source.AddRange(second);
            return source;

        }

        private static List<T> FastAppendToListAllowNulls<T>(this List<T> source, T value)
        {
            if (source == null)
            {
                source = new List<T>();
            }
            source.Add(value);

            return source;
        }

        public static List<T> FastAppendToListAllowNulls<T>(
                        this List<T> source, T value,
                        IEnumerable<T> second)
        {
            if (second == null)
            {
                source = source.FastAppendToListAllowNulls(value);
            }
            else
            {
                source = source.FastAppendToListAllowNulls(second);
            }
            return source;
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T t in source)
            {
                action.Invoke(t);
            }
        }

        public static EnumerableCardinality GetCardinality<T>(this IEnumerable<T> source)
        {
            Assumes.NotNull(source);

            // Cast to ICollection instead of ICollection<T> for performance reasons.
            ICollection collection = source as ICollection;
            if (collection != null)
            {
                switch (collection.Count)
                {
                    case 0:
                        return EnumerableCardinality.Zero;
                    case 1:
                        return EnumerableCardinality.One;
                    default:
                        return EnumerableCardinality.TwoOrMore;
                }
            }

            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    return EnumerableCardinality.Zero;
                }

                if (!enumerator.MoveNext())
                {
                    return EnumerableCardinality.One;
                }

                return EnumerableCardinality.TwoOrMore;
            }
        }

        public static bool FastAny<T>(this IEnumerable<T> source)
        {
            // Enumerable.Any<T> underneath doesn't cast to ICollection, 
            // like it does with many of the other LINQ methods.
            // Below is significantly (4x) when mainly working with ICollection
            // sources and a little slower if working with mainly IEnumerable<T> 
            // sources.

            // Cast to ICollection instead of ICollection<T> for performance reasons.
            ICollection collection = source as ICollection;
            if (collection != null)
            {
                return collection.Count > 0;
            }

            return source.Any();
        }

        public static Stack<T> Copy<T>(this Stack<T> stack)
        {
            Assumes.NotNull(stack);

            // Stack<T>.GetEnumerator walks from top to bottom 
            // of the stack, whereas Stack<T>(IEnumerable<T>) 
            // pushes to bottom from top, so we need to reverse 
            // the stack to get them in the right order.
            return new Stack<T>(stack.Reverse());
        }

        public static T[] AsArray<T>(this IEnumerable<T> enumerable)
        {
            T[] array = enumerable as T[];

            if (array != null)
            {
                return array;
            }

            return enumerable.ToArray();
        }

        public static List<T> AsList<T>(this IEnumerable<T> enumerable)
        {
            List<T> list = enumerable as List<T>;

            if (list != null)
            {
                return list;
            }

            return enumerable.ToList();
        }

        public static bool IsArrayEqual<T>(this T[] thisArray, T[] thatArray)
        {
            if (thisArray.Length != thatArray.Length)
            {
                return false;
            }

            for (int i = 0; i < thisArray.Length; i++)
            {
                if (!thisArray[i].Equals(thatArray[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsCollectionEqual<T>(this IList<T> thisList, IList<T> thatList)
        {
            if (thisList.Count != thatList.Count)
            {
                return false;
            }

            for (int i = 0; i < thisList.Count; i++)
            {
                if (!thisList[i].Equals(thatList[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
