// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class QueryInterfaceTests
    {
        public const string IID_IDISPATCH = "00020400-0000-0000-C000-000000000046";
        public const string IID_IINSPECTABLE = "AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90";

        public static IEnumerable<object[]> QueryInterface_ValidComObjectInterface_TestData()
        {
            yield return new object[] { new ComImportObject(), IID_IUNKNOWN };
            yield return new object[] { new ComImportObject(), IID_IDISPATCH };

            yield return new object[] { new DualComObject(), IID_IUNKNOWN };
            yield return new object[] { new DualComObject(), IID_IDISPATCH };
            yield return new object[] { new IUnknownComObject(), IID_IUNKNOWN };
            yield return new object[] { new IUnknownComObject(), IID_IDISPATCH };
            yield return new object[] { new IDispatchComObject(), IID_IUNKNOWN };
            yield return new object[] { new IDispatchComObject(), IID_IDISPATCH };

            yield return new object[] { new NonDualComObject(), IID_IUNKNOWN };
            yield return new object[] { new NonDualComObject(), IID_IDISPATCH };
            yield return new object[] { new AutoDispatchComObject(), IID_IUNKNOWN };
            yield return new object[] { new AutoDispatchComObject(), IID_IDISPATCH };
            yield return new object[] { new AutoDualComObject(), IID_IUNKNOWN };
            yield return new object[] { new AutoDualComObject(), IID_IDISPATCH };

            yield return new object[] { new NonDualComObjectEmpty(), IID_IUNKNOWN };
            yield return new object[] { new NonDualComObjectEmpty(), IID_IDISPATCH };
            yield return new object[] { new AutoDispatchComObjectEmpty(), IID_IUNKNOWN };
            yield return new object[] { new AutoDispatchComObjectEmpty(), IID_IDISPATCH };
            yield return new object[] { new AutoDualComObjectEmpty(), IID_IUNKNOWN };
            yield return new object[] { new AutoDualComObjectEmpty(), IID_IDISPATCH };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(QueryInterface_ValidComObjectInterface_TestData))]
        public void QueryInterface_ValidComObjectInterface_Success(object o, string iidString)
        {
            IntPtr ptr = Marshal.GetIUnknownForObject(o);
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

        public static IEnumerable<object[]> QueryInterface_NoSuchComObjectInterface_TestData()
        {
            const string IID_CUSTOMINTERFACE = "ED53244F-0514-49D1-9A85-E14B33E71CDF";

            yield return new object[] { new ComImportObject(), IID_IINSPECTABLE };
            yield return new object[] { new ComImportObject(), IID_CUSTOMINTERFACE };

            yield return new object[] { new DualComObject(), IID_IINSPECTABLE };
            yield return new object[] { new DualComObject(), IID_CUSTOMINTERFACE };
            yield return new object[] { new IUnknownComObject(), IID_IINSPECTABLE };
            yield return new object[] { new IUnknownComObject(), IID_CUSTOMINTERFACE };
            yield return new object[] { new IDispatchComObject(), IID_IINSPECTABLE };
            yield return new object[] { new IDispatchComObject(), IID_CUSTOMINTERFACE };
            yield return new object[] { new IInspectableComObject(), IID_IINSPECTABLE };
            yield return new object[] { new IInspectableComObject(), IID_CUSTOMINTERFACE };

            yield return new object[] { new NonDualComObject(), IID_IINSPECTABLE };
            yield return new object[] { new NonDualComObject(), IID_CUSTOMINTERFACE };
            yield return new object[] { new AutoDispatchComObject(), IID_IINSPECTABLE };
            yield return new object[] { new AutoDispatchComObject(), IID_CUSTOMINTERFACE };
            yield return new object[] { new AutoDualComObject(), IID_IINSPECTABLE };
            yield return new object[] { new AutoDualComObject(), IID_CUSTOMINTERFACE };

            yield return new object[] { new NonDualComObjectEmpty(), IID_IINSPECTABLE };
            yield return new object[] { new NonDualComObjectEmpty(), IID_CUSTOMINTERFACE };
            yield return new object[] { new AutoDispatchComObjectEmpty(), IID_IINSPECTABLE };
            yield return new object[] { new AutoDispatchComObjectEmpty(), IID_CUSTOMINTERFACE };
            yield return new object[] { new AutoDualComObjectEmpty(), IID_IINSPECTABLE };
            yield return new object[] { new AutoDualComObjectEmpty(), IID_CUSTOMINTERFACE };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(QueryInterface_NoSuchComObjectInterface_TestData))]
        public void QueryInterface_NoSuchComObjectInterface_Success(object o, string iidString)
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
    }
}
