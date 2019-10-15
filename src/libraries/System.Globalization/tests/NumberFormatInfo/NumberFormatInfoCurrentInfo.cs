// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Tests;
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
            RemoteExecutorForUap.Invoke((cultureName) =>
            {
                var newCulture = CultureInfo.GetCultureInfo(cultureName);
                using (new ThreadCultureChange(newCulture))
                {
                    Assert.Same(newCulture.NumberFormat, NumberFormatInfo.CurrentInfo);
                }
            }, newCurrentCulture.Name).Dispose();
        }

        [Fact]
        public void CurrentInfo_Subclass_OverridesGetFormat()
        {
            RemoteExecutorForUap.Invoke(() =>
            {
                using (new ThreadCultureChange(new CultureInfoSubclassOverridesGetFormat("en-US")))
                {
                    Assert.Same(CultureInfoSubclassOverridesGetFormat.CustomFormat, NumberFormatInfo.CurrentInfo);
                }
            }).Dispose();
        }

        [Fact]
        public void CurrentInfo_Subclass_OverridesNumberFormat()
        {
            RemoteExecutorForUap.Invoke(() =>
            {
                using (new ThreadCultureChange(new CultureInfoSubclassOverridesNumberFormat("en-US")))
                {
                    Assert.Same(CultureInfoSubclassOverridesNumberFormat.CustomFormat, NumberFormatInfo.CurrentInfo);
                }
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
