// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetEndComSlotTests
    {
        public static IEnumerable<object[]> GetEndComSlot_TestData()
        {
            yield return new object[] { typeof(ComImportObject), -1 };
            yield return new object[] { typeof(SubComImportObject), -1 };
            yield return new object[] { typeof(InterfaceComImportObject), -1 };
            yield return new object[] { typeof(IComImportObject), 6 };
            yield return new object[] { typeof(ManagedInterfaceSupportIUnknown), 2 };
            yield return new object[] { typeof(ManagedInterfaceSupportIUnknownWithMethods), 4 };
            yield return new object[] { typeof(ManagedInterfaceSupportDualInterfaceWithMethods), 8 };
            yield return new object[] { typeof(ManagedInterfaceSupportIDispatch), 6 };
            yield return new object[] { typeof(ManagedInterfaceSupportIDispatchWithMethods), 8 };
            yield return new object[] { typeof(ManagedAutoDispatchClass), -1};
            yield return new object[] { typeof(ManagedAutoDualClass), 10 };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetEndComSlot_TestData))]
        public void GetEndComSlot_Windows_ReturnsExpected(Type type, int expected)
        {
            Assert.Equal(expected, Marshal.GetEndComSlot(type));
        }
    }
}
