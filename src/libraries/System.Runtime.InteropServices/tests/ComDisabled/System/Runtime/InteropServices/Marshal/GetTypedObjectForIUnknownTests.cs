// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetTypedObjectForIUnknownTests
    {
        public static IEnumerable<object> GetTypedObjectForIUnknown_RoundtrippableType_TestData()
        {
            yield return new object();
            yield return 10;
            yield return "string";

            yield return new NonGenericClass();
            yield return new NonGenericStruct();
            yield return Int32Enum.Value1;

            MethodInfo method = typeof(GetTypedObjectForIUnknownTests).GetMethod(nameof(NonGenericMethod), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate d = method.CreateDelegate(typeof(NonGenericDelegate));
            yield return d;
        }

        public static IEnumerable<object[]> GetTypedObjectForIUnknown_TestData()
        {
            foreach (object o in GetTypedObjectForIUnknown_RoundtrippableType_TestData())
            {
                yield return new object[] { o};
                yield return new object[] { o};
                yield return new object[] { o};
            }

            yield return new object[] { new ClassWithInterface()};
            yield return new object[] { new StructWithInterface()};

            yield return new object[] { new GenericClass<string>()};
            yield return new object[] { new Dictionary<string, int>()};
            yield return new object[] { new GenericStruct<string>()};
            yield return new object[] { new GenericStruct<string>()};

            yield return new object[] { new int[] { 10 }};
            yield return new object[] { new int[] { 10 }};

            yield return new object[] { new int[][] { new int[] { 10 } }};
            yield return new object[] { new int[][] { new int[] { 10 } }};

            yield return new object[] { new int[,] { { 10 } }};
            yield return new object[] { new int[,] { { 10 } }};

            yield return new object[] { new KeyValuePair<string, int>("key", 10)};
            yield return new object[] { new KeyValuePair<string, int>("key", 10)};
        }

        [Theory]
        [MemberData(nameof(GetTypedObjectForIUnknown_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypedObjectForIUnknown_ValidPointer_ReturnsExpected(object o)
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetIUnknownForObject(o));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetTypedObjectForIUnknown_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetTypedObjectForIUnknown(IntPtr.Zero, typeof(int)));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypedObjectForIUnknown_ZeroUnknown_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("pUnk", () => Marshal.GetTypedObjectForIUnknown(IntPtr.Zero, typeof(int)));
        }

        public class ClassWithInterface : INonGenericInterface { }
        public struct StructWithInterface : INonGenericInterface { }

        private static void NonGenericMethod(int i) { }
        public delegate void NonGenericDelegate(int i);

        public enum Int32Enum : int { Value1, Value2 }
    }
}
