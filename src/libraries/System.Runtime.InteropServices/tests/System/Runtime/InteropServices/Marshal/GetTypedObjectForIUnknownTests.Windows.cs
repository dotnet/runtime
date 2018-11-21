// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetTypedObjectForIUnknownTests
    {
        public static IEnumerable<object> GetTypedObjectForIUnknown_ComObject_TestData()
        {
            yield return new object[] { new ComImportObject(), typeof(ComImportObject) };
            yield return new object[] { new ComImportObject(), typeof(ComImportObject).BaseType };
            yield return new object[] { new ComImportObject(), typeof(object) };

            yield return new object[] { new DualComObject(), typeof(DualComObject) };
            yield return new object[] { new DualComObject(), typeof(DualInterface) };
            yield return new object[] { new IUnknownComObject(), typeof(IUnknownComObject) };
            yield return new object[] { new IUnknownComObject(), typeof(IUnknownInterface) };
            yield return new object[] { new IDispatchComObject(), typeof(IDispatchComObject) };
            yield return new object[] { new IDispatchComObject(), typeof(IDispatchInterface) };
            yield return new object[] { new IInspectableComObject(), typeof(IInspectableComObject) };
            yield return new object[] { new IInspectableComObject(), typeof(IInspectableInterface) };

            yield return new object[] { new NonDualComObject(), typeof(NonDualComObject) };
            yield return new object[] { new AutoDispatchComObject(), typeof(AutoDispatchComObject) };
            yield return new object[] { new AutoDualComObject(), typeof(AutoDualComObject) };

            yield return new object[] { new NonDualComObjectEmpty(), typeof(NonDualComObjectEmpty) };
            yield return new object[] { new AutoDispatchComObjectEmpty(), typeof(AutoDispatchComObjectEmpty) };
            yield return new object[] { new AutoDualComObjectEmpty(), typeof(AutoDualComObjectEmpty) };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(GetTypedObjectForIUnknown_ComObject_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void GetTypedObjectForIUnknown_ComObject_ReturnsExpected(object o, Type type)
        {
            GetTypedObjectForIUnknown_ValidPointer_ReturnsExpected(o, type);
        }

        public static IEnumerable<object[]> GetTypedObjectForIUnknownTypeUncastableComObject_TestData()
        {
            yield return new object[] { new ComImportObject(), typeof(DualComObject) };
            yield return new object[] { new IInspectableComObject(), typeof(IUnknownInterface) };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(GetTypedObjectForIUnknownTypeUncastableComObject_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void GetTypedObjectForIUnknown_UncastableComObject_ThrowsInvalidCastException(object o, Type type)
        {
            GetTypedObjectForIUnknown_UncastableObject_ThrowsInvalidCastException(o, type);
        }
    }
}
