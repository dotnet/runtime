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

        public static IEnumerable<object[]> QueryInterface_ValidInterface_TestData()
        {
            yield return new object[] { new object(), IID_IUNKNOWN };
            yield return new object[] { new object(), ComWrappersImpl.IID_TestQueryInterface };
        }

        [Theory]
        [MemberData(nameof(QueryInterface_ValidInterface_TestData))]
        [SkipOnMono("ComWrappers are not supported on Mono")]
        public void QueryInterface_ValidInterface_Success(object o, string iidString)
        {
            var cw = new ComWrappersImpl();
            IntPtr ptr = cw.GetOrCreateComInterfaceForObject(o, CreateComInterfaceFlags.None);
            try
            {
                Guid guid = new Guid(iidString);
                Assert.Equal(0, Marshal.QueryInterface(ptr, ref guid, out IntPtr ppv));
                Assert.NotEqual(IntPtr.Zero, ppv);
                try
                {
                    Assert.Equal(new Guid(iidString), guid);
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
        }

        [Theory]
        [MemberData(nameof(QueryInterface_NoSuchInterface_TestData))]
        [SkipOnMono("ComWrappers are not supported on Mono")]
        public void QueryInterface_NoSuchInterface_Success(object o, string iidString)
        {
            var cw = new ComWrappersImpl();
            IntPtr ptr = cw.GetOrCreateComInterfaceForObject(o, CreateComInterfaceFlags.None);
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
    }
}
