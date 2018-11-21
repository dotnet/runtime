// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class QueryInterfaceTests
    {
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
            yield return new object[] { new IInspectableComObject(), IID_IUNKNOWN };
            yield return new object[] { new IInspectableComObject(), IID_IDISPATCH };

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

            yield return new object[] { new IUnknownComObject(), IID_IUNKNOWN };
            yield return new object[] { new IUnknownComObject(), IID_IDISPATCH };
        }
    
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(QueryInterface_ValidComObjectInterface_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void QueryInterface_ValidComObjectInterface_Success(object o, string iidString)
        {
            QueryInterface_ValidInterface_Success(o, iidString);
        }

        public static IEnumerable<object[]> QueryInterface_NoSuchComObjectInterface_TestData()
        {
            const string IID_CUSTOMINTERFACE = "927971f5-0939-11d1-8be1-00c04fd8d503";

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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(QueryInterface_NoSuchComObjectInterface_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void QueryInterface_NoSuchComObjectInterface_Success(object o, string iidString)
        {
            QueryInterface_NoSuchInterface_Success(o, iidString);
        }
    }
}
