// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoClone
    {
        public static IEnumerable<object[]> Clone_TestData()
        {
            yield return new object[] { new CultureInfo(CultureInfo.InvariantCulture.Name) };
            yield return new object[] { CultureInfo.InvariantCulture };
            yield return new object[] { new CultureInfo("fr-FR") };
            yield return new object[] { new CultureInfo("en") };
        }

        [Theory]
        [MemberData(nameof(Clone_TestData))]
        public void Clone(CultureInfo culture)
        {
            CultureInfo clone = (CultureInfo)culture.Clone();
            Assert.Equal(culture, clone);
            Assert.NotSame(clone, culture);
        }
    }
}
