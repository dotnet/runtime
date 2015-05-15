// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace System.Reflection.Tests
{
    public class CultureNameTests
    {
        [Fact, ActiveIssue(846, PlatformID.AnyUnix)]
        public void SettingNullCultureNameSucceeds()
        {
            var an = new AssemblyName("Test, Culture=en-US");
            Assert.Equal("en-US", an.CultureName);

            an.CultureName = null;

            Assert.Equal(null, an.CultureName);
            AssertAssemblyNamesAreEqual(new AssemblyName("Test"), an);
        }

        [Fact, ActiveIssue(846, PlatformID.AnyUnix)]
        public void SettingEmptyCultureNameSucceeds()
        {
            var an = new AssemblyName("Test, Culture=en-US");
            Assert.Equal("en-US", an.CultureName);

            an.CultureName = String.Empty;

            Assert.Equal(String.Empty, an.CultureName);
            AssertAssemblyNamesAreEqual(new AssemblyName("Test, Culture=neutral"), an);
        }

        [Fact, ActiveIssue(846, PlatformID.AnyUnix)]
        public void SettingValidCultureNameSucceeds()
        {
            var an = new AssemblyName("Test");
            Assert.Equal(null, an.CultureName);
            an.CultureName = "en-US";

            Assert.Equal("en-US", an.CultureName);
            AssertAssemblyNamesAreEqual(new AssemblyName("Test, Culture=en-US"), an);
        }

        [Fact, ActiveIssue(846, PlatformID.AnyUnix)]
        public void SettingCultureNameIsCaseInsensitive()
        {
            var an = new AssemblyName("Test");
            Assert.Equal(null, an.CultureName);

            an.CultureName = "En-Us";

            Assert.Equal("en-US", an.CultureName);
            AssertAssemblyNamesAreEqual(new AssemblyName("Test, Culture=en-US"), an);
        }

        [Fact, ActiveIssue(846, PlatformID.AnyUnix)]
        public void SettingInvalidCultureNameThrowsCultureNotFound()
        {
            var an = new AssemblyName("Test");
            Assert.Throws<CultureNotFoundException>(() => an.CultureName = "NotAValidCulture");
            Assert.Throws<CultureNotFoundException>(() => new AssemblyName("Test, Culture=NotAValidCulture"));
        }

        private void AssertAssemblyNamesAreEqual(AssemblyName expected, AssemblyName actual)
        {
            Assert.Equal(expected.FullName, actual.FullName);
        }
    }
}