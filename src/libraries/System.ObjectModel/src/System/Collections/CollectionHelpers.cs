// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections
{
    internal static class CollectionHelpers
    {
        internal static void ValidateCopyToArguments(int sourceCount, Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            }

            if (index < 0 || index > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - index < sourceCount)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }
        }

        internal static void CopyTo<T>(ICollection<T> collection, Array array, int index)
        {
            ValidateCopyToArguments(collection.Count, array, index);

            if (collection is ICollection nonGenericCollection)
            {
                // Easy out if the ICollection<T> implements the non-generic ICollection
                nonGenericCollection.CopyTo(array, index);
            }
            else if (array is T[] items)
            {
                collection.CopyTo(items, index);
            }
            else
            {
                // We can't cast array of value type to object[], so we don't support widening of primitive types here.
                if (array is not object?[] objects)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }

                try
                {
                    foreach (T item in collection)
                    {
                        objects[index++] = item;
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
            }
        }
    }
}
