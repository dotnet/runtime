// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TestIFormatProviderClass : IFormatProvider
    {
        public object GetFormat(Type formatType)
        {
            return this;
        }
    }

    public class TestIFormatProviderClass2 : IFormatProvider
    {
        public object GetFormat(Type formatType)
        {
            return new DateTimeFormatInfo();
        }
    }

    public class DateTimeFormatInfoGetInstance
    {
        public static IEnumerable<object[]> GetInstance_NotNull_TestData()
        {
            yield return new object[] { new DateTimeFormatInfo() };
            yield return new object[] { new CultureInfo("en-US") };
            yield return new object[] { new TestIFormatProviderClass2() };
        }

        [Theory]
        [MemberData(nameof(GetInstance_NotNull_TestData))]
        public void GetInstance_NotNull(IFormatProvider provider)
        {
            Assert.NotNull(DateTimeFormatInfo.GetInstance(provider));
        }

        [Fact]
        public void GetInstance_ExpectedCurrent()
        {
            Assert.Same(DateTimeFormatInfo.CurrentInfo, DateTimeFormatInfo.GetInstance(null));
            Assert.Same(DateTimeFormatInfo.CurrentInfo, DateTimeFormatInfo.GetInstance(new TestIFormatProviderClass()));
        }
    }
}
