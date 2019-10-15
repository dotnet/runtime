// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrentInfo
    {
        public static IEnumerable<object[]> CurrentInfo_CustomCulture_TestData()
        {
            yield return new object[] { CultureInfo.GetCultureInfo("en") };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US") };
            yield return new object[] { CultureInfo.InvariantCulture };
        }

        [ActiveIssue(33904, TargetFrameworkMonikers.Uap)]
        [Theory]
        [MemberData(nameof(CurrentInfo_CustomCulture_TestData))]
        public void CurrentInfo_CustomCulture(CultureInfo newCurrentCulture)
        {
            RemoteExecutor.Invoke((cultureName) =>
            {
                CultureInfo newCulture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentCulture = newCulture;
                Assert.Same(newCulture.NumberFormat, NumberFormatInfo.CurrentInfo);
                return RemoteExecutor.SuccessExitCode;
            }, newCurrentCulture.Name).Dispose();
        }

        [Fact]
        public void CurrentInfo_Subclass_OverridesGetFormat()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfoSubclassOverridesGetFormat("en-US");
                Assert.Same(CultureInfoSubclassOverridesGetFormat.CustomFormat, NumberFormatInfo.CurrentInfo);
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void CurrentInfo_Subclass_OverridesNumberFormat()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfoSubclassOverridesNumberFormat("en-US");
                Assert.Same(CultureInfoSubclassOverridesNumberFormat.CustomFormat, NumberFormatInfo.CurrentInfo);
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        private class CultureInfoSubclassOverridesGetFormat : CultureInfo
        {
            public CultureInfoSubclassOverridesGetFormat(string name): base(name) { }

            public static NumberFormatInfo CustomFormat { get; } = CultureInfo.GetCultureInfo("fr-FR").NumberFormat;

            public override object GetFormat(Type formatType) => CustomFormat;
        }

        private class CultureInfoSubclassOverridesNumberFormat : CultureInfo
        {
            public CultureInfoSubclassOverridesNumberFormat(string name): base(name) { }

            public static NumberFormatInfo CustomFormat { get; } = CultureInfo.GetCultureInfo("fr-FR").NumberFormat;

            public override NumberFormatInfo NumberFormat
            {
                get { return CustomFormat; }
                set { }
            }
        }
    }
}
