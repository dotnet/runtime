// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Tests
{
    /// <summary>
    /// Non-generic static class holding MemberData providers that would otherwise
    /// be on generic test classes. The xunit v3 AOT source generator cannot emit
    /// valid code for MemberData on generic classes (open type parameters in non-generic context).
    /// </summary>
    public static class CollectionTestData
    {
        public static IEnumerable<object[]> ValidCollectionSizes_GreaterThanOne()
        {
            yield return new object[] { 2 };
            yield return new object[] { 20 };
        }

        public static IEnumerable<object[]> EnsureCapacity_LargeCapacity_Throws_MemberData()
        {
            yield return new object[] { 5, Array.MaxLength + 1 };
            yield return new object[] { 1, int.MaxValue };
        }

        public static IEnumerable<object[]> Stack_Generic_EnsureCapacity_LargeCapacityRequested_Throws_MemberData()
        {
            yield return new object[] { Array.MaxLength + 1 };
            yield return new object[] { int.MaxValue };
        }

        public static IEnumerable<object[]> Queue_Generic_EnsureCapacity_LargeCapacityRequested_Throws_MemberData()
        {
            yield return new object[] { Array.MaxLength + 1 };
            yield return new object[] { int.MaxValue };
        }

        public static IEnumerable<object[]> UnionWith_HashSet_TestData()
        {
            foreach (int count in new[] { 0, 1, 75 })
            {
                foreach (bool destinationEmpty in new[] { true, false })
                {
                    foreach (bool sourceSparseFilled in new[] { true, false })
                    {
                        yield return new object[] { count, destinationEmpty, sourceSparseFilled };
                    }
                }
            }
        }

        public static IEnumerable<object[]> Dictionary_GetAlternateLookup_OperationsMatchUnderlyingDictionary_MemberData()
        {
            yield return new object[] { EqualityComparer<string>.Default };
            yield return new object[] { StringComparer.Ordinal };
            yield return new object[] { StringComparer.OrdinalIgnoreCase };
            yield return new object[] { StringComparer.InvariantCulture };
            yield return new object[] { StringComparer.InvariantCultureIgnoreCase };
            yield return new object[] { StringComparer.CurrentCulture };
            yield return new object[] { StringComparer.CurrentCultureIgnoreCase };
        }

        public enum IndexOfMethod
        {
            IndexOf_T,
            IndexOf_T_int,
            IndexOf_T_int_int,
            LastIndexOf_T,
            LastIndexOf_T_int,
            LastIndexOf_T_int_int,
        }

        public static IEnumerable<object[]> IndexOfTestData()
        {
            foreach (object[] sizes in TestBase.ValidCollectionSizes())
            {
                int count = (int)sizes[0];
                yield return new object[] { IndexOfMethod.IndexOf_T, count, true };
                yield return new object[] { IndexOfMethod.LastIndexOf_T, count, false };

                if (count > 0) // 0 is an invalid index for IndexOf when the count is 0.
                {
                    yield return new object[] { IndexOfMethod.IndexOf_T_int, count, true };
                    yield return new object[] { IndexOfMethod.LastIndexOf_T_int, count, false };
                    yield return new object[] { IndexOfMethod.IndexOf_T_int_int, count, true };
                    yield return new object[] { IndexOfMethod.LastIndexOf_T_int_int, count, false };
                }
            }
        }
    }
}
