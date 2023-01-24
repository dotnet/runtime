// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetIDispatchForObjectTests
    {
        public static IEnumerable<object[]> GetIDispatchForObject_ComObject_TestData()
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
        [MemberData(nameof(GetIDispatchForObject_ComObject_TestData))]
        public void GetIDispatchForObject_DispatchObject_Success(object obj)
        {
            IntPtr ptr = Marshal.GetIDispatchForObject(obj);
            try
            {
                Assert.NotEqual(IntPtr.Zero, ptr);
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void GetIDispatchForObject_ManagedIInspectableObject_Fail()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetIDispatchForObject(new IInspectableManagedObject()));
        }
    }
}
