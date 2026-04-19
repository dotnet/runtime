// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Collections.Tests
{
    /// <summary>
    /// Provides a base set of nongeneric operations that are used by all other testing interfaces.
    /// </summary>
    public abstract class TestBase
    {
        #region Helper Methods

        public static IEnumerable<object[]> ValidCollectionSizes()
        {
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { 75 };
        }

        public static IEnumerable<object[]> ValidPositiveCollectionSizes()
        {
            yield return new object[] { 1 };
            yield return new object[] { 75 };
        }

        /// <summary>
        /// MemberData to be passed to tests that take an IEnumerable{T}. This method returns every permutation of
        /// EnumerableType to test on (e.g. HashSet, Queue), and size of set to test with (e.g. 0, 1, etc.).
        /// </summary>
        public static IEnumerable<object[]> EnumerableTestData() =>
            ((IEnumerable<EnumerableType>)Enum.GetValues(typeof(EnumerableType))).SelectMany(GetEnumerableTestData);

        /// <summary>
        /// MemberData to be passed to tests that take an IEnumerable{T}. This method returns results for various
        /// sizes of sets to test with (e.g. 0, 1, etc.) but only for List.
        /// </summary>
        public static IEnumerable<object[]> ListTestData() =>
            GetEnumerableTestData(EnumerableType.List);

        public static IEnumerable<object[]> SetTestData() =>
            new[] { EnumerableType.HashSet, EnumerableType.List }.SelectMany(GetEnumerableTestData);

        protected static IEnumerable<object[]> GetEnumerableTestData(EnumerableType enumerableType)
        {
            foreach (object[] collectionSizeArray in ValidCollectionSizes())
            {
                int count = (int)collectionSizeArray[0];
                yield return new object[] { enumerableType, count, 0, 0, 0 };                       // Empty Enumerable
                yield return new object[] { enumerableType, count, count + 1, 0, 0 };               // Enumerable that is 1 larger

                if (count >= 1)
                {
                    yield return new object[] { enumerableType, count, count, 0, 0 };               // Enumerable of the same size
                    yield return new object[] { enumerableType, count, count - 1, 0, 0 };           // Enumerable that is 1 smaller
                    yield return new object[] { enumerableType, count, count, 1, 0 };               // Enumerable of the same size with 1 matching element
                    yield return new object[] { enumerableType, count, count + 1, 1, 0 };           // Enumerable that is 1 longer with 1 matching element
                    yield return new object[] { enumerableType, count, count, count, 0 };           // Enumerable with all elements matching
                    yield return new object[] { enumerableType, count, count + 1, count, 0 };       // Enumerable with all elements matching plus one extra
                }

                if (count >= 2)
                {
                    yield return new object[] { enumerableType, count, count - 1, 1, 0 };           // Enumerable that is 1 smaller with 1 matching element
                    yield return new object[] { enumerableType, count, count + 2, 2, 0 };           // Enumerable that is 2 longer with 2 matching element
                    yield return new object[] { enumerableType, count, count - 1, count - 1, 0 };   // Enumerable with all elements matching minus one
                    yield return new object[] { enumerableType, count, count, 2, 0 };               // Enumerable of the same size with 2 matching element
                    if ((enumerableType == EnumerableType.List || enumerableType == EnumerableType.Queue))
                        yield return new object[] { enumerableType, count, count, 0, 1 };           // Enumerable with 1 element duplicated
                }

                if (count >= 3)
                {
                    if ((enumerableType == EnumerableType.List || enumerableType == EnumerableType.Queue))
                        yield return new object[] { enumerableType, count, count, 0, 1 };           // Enumerable with all elements duplicated
                    yield return new object[] { enumerableType, count, count - 1, 2, 0 };           // Enumerable that is 1 smaller with 2 matching elements
                }
            }
        }

        public enum EnumerableType
        {
            HashSet,
            SortedSet,
            List,
            Queue,
            Lazy,
        };

        [Flags]
        public enum ModifyOperation
        {
            None = 0,
            Add = 1,
            Insert = 2,
            Overwrite = 4,
            Remove = 8,
            Clear = 16
        }

        #endregion
    }
}
