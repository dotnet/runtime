// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices
{
    public partial class MarshalTests
    {
        public static IEnumerable<object[]> IsComImport_Windows_ReturnsExpected()
        {
            yield return new object[] { new ComImportObject(), true };
            yield return new object[] { new SubComImportObject(), true };
            yield return new object[] { new InterfaceAndComImportObject(), true };
            yield return new object[] { new InterfaceComImportObject(), false };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(IsComImport_Windows_ReturnsExpected))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void IsComObject_Windows_ReturnsExpected(object value, bool expected)
        {
            Assert.Equal(expected, Marshal.IsComObject(value));
        }
    }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    public interface IComImportObject { }

    public class InterfaceComImportObject : IComImportObject { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    public class InterfaceAndComImportObject : IComImportObject { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    public class ComImportObject { }

    public class SubComImportObject : ComImportObject { }
}
