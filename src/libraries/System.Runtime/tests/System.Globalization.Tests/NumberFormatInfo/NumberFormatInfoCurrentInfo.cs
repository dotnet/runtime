// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [Theory]
        [MemberData(nameof(CurrentInfo_CustomCulture_TestData))]
        public void CurrentInfo_CustomCulture(CultureInfo newCurrentCulture)
        {
            using (new ThreadCultureChange(newCurrentCulture))
            {
                Assert.Same(newCurrentCulture.NumberFormat, NumberFormatInfo.CurrentInfo);
            }
        }

        [Fact]
        public void CurrentInfo_Subclass_OverridesGetFormat()
        {
            using (new ThreadCultureChange(new CultureInfoSubclassOverridesGetFormat("en-US")))
            {
                Assert.Same(CultureInfoSubclassOverridesGetFormat.CustomFormat, NumberFormatInfo.CurrentInfo);
            }
        }

        [Fact]
        public void CurrentInfo_Subclass_OverridesNumberFormat()
        {
            using (new ThreadCultureChange(new CultureInfoSubclassOverridesNumberFormat("en-US")))
            {
                Assert.Same(CultureInfoSubclassOverridesNumberFormat.CustomFormat, NumberFormatInfo.CurrentInfo);
            }
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
