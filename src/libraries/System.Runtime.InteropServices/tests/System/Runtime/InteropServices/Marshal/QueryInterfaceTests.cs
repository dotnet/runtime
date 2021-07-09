// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class QueryInterfaceTests
    {
        public const int E_NOINTERFACE = unchecked((int)0x80004002);
        public const string IID_IUNKNOWN = "00000000-0000-0000-C000-000000000046";
        public const string IID_IDISPATCH = "00020400-0000-0000-C000-000000000046";

        public static IEnumerable<object[]> QueryInterface_ValidInterface_TestData()
        {
            yield return new object[] { new object(), IID_IUNKNOWN };
            yield return new object[] { new object(), IID_IDISPATCH };

            yield return new object[] { 10, IID_IUNKNOWN };
            if (!PlatformDetection.IsNetCore)
            {
                yield return new object[] { 10, IID_IDISPATCH };
            }

            yield return new object[] { "string", IID_IUNKNOWN };
            if (!PlatformDetection.IsNetCore)
            {
                yield return new object[] { "string", IID_IDISPATCH };
            }

            yield return new object[] { new NonGenericClass(), IID_IUNKNOWN };
            if (!PlatformDetection.IsNetCore)
            {
                yield return new object[] { new NonGenericClass(), IID_IDISPATCH };
            }
            yield return new object[] { new GenericClass<string>(), IID_IUNKNOWN };

            yield return new object[] { new NonGenericStruct(), IID_IUNKNOWN };
            if (!PlatformDetection.IsNetCore)
            {
                yield return new object[] { new NonGenericStruct(), IID_IDISPATCH };
            }
            yield return new object[] { new GenericStruct<string>(), IID_IUNKNOWN };

            yield return new object[] { Int32Enum.Value1, IID_IUNKNOWN };
            if (!PlatformDetection.IsNetCore)
            {
                yield return new object[] { Int32Enum.Value1, IID_IDISPATCH };
            }

            yield return new object[] { new int[] { 10 }, IID_IUNKNOWN };
            yield return new object[] { new int[][] { new int[] { 10 } }, IID_IUNKNOWN };
            yield return new object[] { new int[,] { { 10 } }, IID_IUNKNOWN };

            MethodInfo method = typeof(GetObjectForIUnknownTests).GetMethod(nameof(NonGenericMethod), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate d = method.CreateDelegate(typeof(NonGenericDelegate));
            yield return new object[] { d, IID_IUNKNOWN };
            yield return new object[] { d, IID_IDISPATCH };

            yield return new object[] { new KeyValuePair<string, int>("key", 10), IID_IUNKNOWN };
        }

        [Theory]
        [MemberData(nameof(QueryInterface_ValidInterface_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void QueryInterface_ValidInterface_Success(object o, string guid)
        {
            IntPtr ptr = Marshal.GetIUnknownForObject(o);
            try
            {
                Guid iidString = new Guid(guid);
                Assert.Equal(0, Marshal.QueryInterface(ptr, ref iidString, out IntPtr ppv));
                Assert.NotEqual(IntPtr.Zero, ppv);
                try
                {
                    Assert.Equal(new Guid(guid), iidString);
                }
                finally
                {
                    Marshal.Release(ppv);
                }
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        public static IEnumerable<object[]> QueryInterface_NoSuchInterface_TestData()
        {
            yield return new object[] { new object(), Guid.Empty.ToString() };
            yield return new object[] { new object(), "927971f5-0939-11d1-8be1-00c04fd8d503" };

            yield return new object[] { new int[] { 10 }, IID_IDISPATCH };
            yield return new object[] { new int[][] { new int[] { 10 } }, IID_IDISPATCH };
            yield return new object[] { new int[,] { { 10 } }, IID_IDISPATCH };

            yield return new object[] { new GenericClass<string>(), IID_IDISPATCH };
            yield return new object[] { new Dictionary<string, int>(), IID_IDISPATCH };
            yield return new object[] { new GenericStruct<string>(), IID_IDISPATCH };
            yield return new object[] { new KeyValuePair<string, int>(), IID_IDISPATCH };
        }

        [Theory]
        [MemberData(nameof(QueryInterface_NoSuchInterface_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void QueryInterface_NoSuchInterface_Success(object o, string iidString)
        {
            IntPtr ptr = Marshal.GetIUnknownForObject(o);
            try
            {
                Guid iid = new Guid(iidString);
                Assert.Equal(E_NOINTERFACE, Marshal.QueryInterface(ptr, ref iid, out IntPtr ppv));
                Assert.Equal(IntPtr.Zero, ppv);
                Assert.Equal(new Guid(iidString), iid);
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        [Fact]
        public void QueryInterface_ZeroPointer_ThrowsArgumentNullException()
        {
            Guid iid = Guid.Empty;
            AssertExtensions.Throws<ArgumentNullException>("pUnk", () => Marshal.QueryInterface(IntPtr.Zero, ref iid, out IntPtr ppv));
        }

        private static void NonGenericMethod(int i) { }
        public delegate void NonGenericDelegate(int i);

        public enum Int32Enum : int { Value1, Value2 }
    }
}
