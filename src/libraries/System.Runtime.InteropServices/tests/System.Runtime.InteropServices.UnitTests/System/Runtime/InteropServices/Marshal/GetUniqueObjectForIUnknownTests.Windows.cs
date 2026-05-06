// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetUniqueObjectForIUnknownTests
    {
        public static IEnumerable<object[]> GetUniqueObjectForIUnknown_ComObject_TestData()
        {
            yield return new object[] { new ComImportObject() };

            yield return new object[] { new DualComObject() };
            yield return new object[] { new IUnknownComObject() };
            yield return new object[] { new IDispatchComObject() };
            yield return new object[] { new IInspectableComObject() };

            yield return new object[] { new NonDualComObject() };
            yield return new object[] { new AutoDispatchComObject() };
            yield return new object[] { new AutoDualComObject() };

            yield return new object[] { new NonDualComObjectEmpty() };
            yield return new object[] { new AutoDispatchComObjectEmpty() };
            yield return new object[] { new AutoDualComObjectEmpty() };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetUniqueObjectForIUnknown_ComObject_TestData))]
        public void GetUniqueObjectForIUnknown_ComObject_ReturnsExpected(object o)
        {
            IntPtr ptr = Marshal.GetIUnknownForObject(o);
            try
            {
                Assert.NotEqual(IntPtr.Zero, ptr);

                object uniqueObject = Marshal.GetUniqueObjectForIUnknown(ptr);
                Assert.NotNull(uniqueObject);
                Assert.NotEqual(o, uniqueObject);
                Assert.NotEqual(o, Marshal.GetUniqueObjectForIUnknown(ptr));
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }
    }
}
