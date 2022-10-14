// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Globalization.Tests
{
    public class IcuAppLocalTests
    {
        private static bool SupportIcuLocals => PlatformDetection.IsNotMobile && !PlatformDetection.Is32BitProcess;

        [ConditionalFact(nameof(SupportIcuLocals))]
        public void TestIcuAppLocal()
        {
            Type? interopGlobalization = Type.GetType("Interop+Globalization, System.Private.CoreLib");
            Assert.NotNull(interopGlobalization);

            MethodInfo? methodInfo = interopGlobalization.GetMethod("GetICUVersion", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(methodInfo);

            // Assert the ICU version 0x44020009 is 68.2.0.9
            Assert.Equal(0x44020009, (int)methodInfo.Invoke(null, null));

            // Now call globalization API to ensure the binding working without any problem.
            Assert.Equal(-1, CultureInfo.GetCultureInfo("en-US").CompareInfo.Compare("sample\u0000", "Sample\u0000", CompareOptions.IgnoreSymbols));
        }
    }
}
