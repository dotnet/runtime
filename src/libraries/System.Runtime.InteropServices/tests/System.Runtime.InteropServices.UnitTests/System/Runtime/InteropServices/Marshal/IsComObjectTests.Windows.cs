// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class IsComObjectTests
    {
        public static IEnumerable<object[]> IsComObject_Windows_TestData()
        {
            yield return new object[] { new ComImportObject(), true };
            yield return new object[] { new ComImportObject[0], false };
            yield return new object[] { new SubComImportObject(), true };
            yield return new object[] { new GenericSubComImportObject<string>(), true };
            yield return new object[] { new InterfaceOnComImportObject(), true };
            yield return new object[] { new InterfaceComImportObject(), false };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(IsComObject_Windows_TestData))]
        public void IsComObject_Windows_ReturnsExpected(object value, bool expected)
        {
            Assert.Equal(expected, Marshal.IsComObject(value));
        }
    }
}
