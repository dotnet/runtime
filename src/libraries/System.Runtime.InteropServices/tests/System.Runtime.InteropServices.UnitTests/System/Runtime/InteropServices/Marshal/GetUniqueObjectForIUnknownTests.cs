// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetUniqueObjectForIUnknownTests
    {
        public static IEnumerable<object[]> GetUniqueObjectForIUnknown_Valid_TestData()
        {
            yield return new object[] { new object() };
            yield return new object[] { 10 };
            yield return new object[] { "string" };

            yield return new object[] { new NonGenericClass() };
            yield return new object[] { new GenericClass<string>() };
            yield return new object[] { new Dictionary<string, int>() };
            yield return new object[] { new NonGenericStruct() };
            yield return new object[] { new GenericStruct<string>() };
            yield return new object[] { Int32Enum.Value1 };

            yield return new object[] { new int[] { 10 } };
            yield return new object[] { new int[][] { new int[] { 10 } } };
            yield return new object[] { new int[,] { { 10 } } };

            MethodInfo method = typeof(GetUniqueObjectForIUnknownTests).GetMethod(nameof(NonGenericMethod), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate d = method.CreateDelegate(typeof(NonGenericDelegate));
            yield return new object[] { d };

            yield return new object[] { new KeyValuePair<string, int>("key", 10) };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetUniqueObjectForIUnknown_Valid_TestData))]
        public void GetUniqueObjectForIUnknown_ValidPointer_ReturnsExpected(object o)
        {
            IntPtr ptr = Marshal.GetIUnknownForObject(o);
            try
            {
                Assert.NotEqual(IntPtr.Zero, ptr);
                Assert.Equal(o, Marshal.GetUniqueObjectForIUnknown(ptr));
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetUniqueObjectForIUnknown_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetUniqueObjectForIUnknown(IntPtr.Zero));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void GetUniqueObjectForIUnknown_NullPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("unknown", () => Marshal.GetUniqueObjectForIUnknown(IntPtr.Zero));
        }

        private static void NonGenericMethod(int i) { }
        public delegate void NonGenericDelegate(int i);

        public enum Int32Enum : int { Value1, Value2 }
    }
}
