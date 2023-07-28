// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetStartComSlotTests
    {
        public static IEnumerable<object[]> GetStartComSlot_TestData()
        {
            yield return new object[] { typeof(ComImportObject), -1 };
            yield return new object[] { typeof(SubComImportObject), -1 };
            yield return new object[] { typeof(InterfaceComImportObject), -1 };
            yield return new object[] { typeof(InterfaceOnComImportObject), 7 };
            yield return new object[] { typeof(IComImportObject), 7 };

            yield return new object[] { typeof(DualInterface), 7};
            yield return new object[] { typeof(IUnknownInterface), 3};
            yield return new object[] { typeof(IDispatchInterface), 7};
            yield return new object[] { typeof(DualComObject), 7};
            yield return new object[] { typeof(IUnknownComObject), 3};
            yield return new object[] { typeof(IDispatchComObject), 7};
            yield return new object[] { typeof(NonDualComObject), 7};
            yield return new object[] { typeof(AutoDispatchComObject), 7};
            yield return new object[] { typeof(AutoDualComObject), 7};
            yield return new object[] { typeof(NonDualComObjectEmpty), -1};
            yield return new object[] { typeof(AutoDispatchComObjectEmpty), -1};
            yield return new object[] { typeof(AutoDualComObjectEmpty), -1};

            yield return new object[] { typeof(ManagedInterfaceSupportIUnknown), 3 };
            yield return new object[] { typeof(ManagedInterfaceSupportIUnknownWithMethods), 3 };
            yield return new object[] { typeof(ManagedInterfaceSupportDualInterfaceWithMethods), 7 };
            yield return new object[] { typeof(ManagedInterfaceSupportIDispatch), 7 };
            yield return new object[] { typeof(ManagedInterfaceSupportIDispatchWithMethods), 7 };
            yield return new object[] { typeof(ManagedAutoDispatchClass), -1 };
            yield return new object[] { typeof(ManagedAutoDualClass), 7 };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetStartComSlot_TestData))]
        public void GetStartComSlot_Windows_ReturnsExpected(Type type, int expected)
        {
            Assert.Equal(expected, Marshal.GetStartComSlot(type));
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void GetStartComSlot_ManagedIInspectableObject_Fail()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetStartComSlot(typeof(IInspectableInterface)));
        }
    }
}
