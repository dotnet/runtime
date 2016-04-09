// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrentInfo : RemoteExecutorTestBase
    {
        public static IEnumerable<object[]> CurrentInfo_CustomCulture_TestData()
        {
            yield return new object[] { new CultureInfo("en") };
            yield return new object[] { new CultureInfo("en-US") };
            yield return new object[] { CultureInfo.InvariantCulture };
        }

        [Theory]
        [MemberData(nameof(CurrentInfo_CustomCulture_TestData))]
        public void CurrentInfo_CustomCulture(CultureInfo newCurrentCulture)
        {
            RemoteInvoke((cultureName) =>
            {
                if (cultureName.Equals("EmptyString"))
                    cultureName = string.Empty;
                CultureInfo newCulture = new CultureInfo(cultureName);
                CultureInfo.CurrentCulture = newCulture;
                Assert.Same(newCulture.NumberFormat, NumberFormatInfo.CurrentInfo);
                return SuccessExitCode;
            }, newCurrentCulture.Name.Length > 0 ? newCurrentCulture.Name : "EmptyString").Dispose();
        }

        [Fact]
        public void CurrentInfo_Subclass_OverridesGetFormat()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfoSubclassOverridesGetFormat("en-US");
                Assert.Same(CultureInfoSubclassOverridesGetFormat.CustomFormat, NumberFormatInfo.CurrentInfo);
                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void CurrentInfo_Subclass_OverridesNumberFormat()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfoSubclassOverridesNumberFormat("en-US");
                Assert.Same(CultureInfoSubclassOverridesNumberFormat.CustomFormat, NumberFormatInfo.CurrentInfo);
                return SuccessExitCode;
            }).Dispose();
        }

        private class CultureInfoSubclassOverridesGetFormat : CultureInfo
        {
            public CultureInfoSubclassOverridesGetFormat(string name): base(name) { }

            public static NumberFormatInfo CustomFormat { get; } = new CultureInfo("fr-FR").NumberFormat;

            public override object GetFormat(Type formatType) => CustomFormat;
        }

        private class CultureInfoSubclassOverridesNumberFormat : CultureInfo
        {
            public CultureInfoSubclassOverridesNumberFormat(string name): base(name) { }

            public static NumberFormatInfo CustomFormat { get; } = new CultureInfo("fr-FR").NumberFormat;

            public override NumberFormatInfo NumberFormat
            {
                get { return CustomFormat; }
                set { }
            }
        }
    }
}
