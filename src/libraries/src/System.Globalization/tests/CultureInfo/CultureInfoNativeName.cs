// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoNativeName
    {
        public static IEnumerable<object[]> NativeName_TestData()
        {
            yield return new object[] { CultureInfo.CurrentCulture.Name, CultureInfo.CurrentCulture.NativeName };
            yield return new object[] { "en-US", "English (United States)" };
            yield return new object[] { "en-CA", "English (Canada)" };
        }

        [Theory]
        [MemberData(nameof(NativeName_TestData))]
        public void NativeName(string name, string expected)
        {
            CultureInfo myTestCulture = new CultureInfo(name);
            Assert.Equal(expected, myTestCulture.NativeName);
        }
    }
}
