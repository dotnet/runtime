// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
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
        // PosTest1: Call GetInstance to get an DateTimeFormatInfo instance when provider is an CultureInfo instance
        [Fact]
        public void PosTest1()
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.GetInstance(new CultureInfo("en-us"));
            Assert.NotNull(info);
        }

        // PosTest2: Call GetInstance to get an DateTimeFormatInfo instance when provider is null reference
        [Fact]
        public void PosTest2()
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.GetInstance(null);
            Assert.Equal(DateTimeFormatInfo.CurrentInfo, info);
        }

        // PosTest3: Call GetInstance to get an DateTimeFormatInfo instance when provider is a DateTimeFormatInfo instance
        [Fact]
        public void PosTest3()
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.GetInstance(new DateTimeFormatInfo());
            Assert.NotNull(info);
        }

        // PosTest4: Call GetInstance to get an DateTimeFormatInfo instance when provider.GetFormat method supports a DateTimeFormatInfo instance
        [Fact]
        public void PosTest4()
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.GetInstance(new TestIFormatProviderClass2());
            Assert.NotNull(info);
        }

        // PosTest5: Call GetInstance to get an DateTimeFormatInfo instance when provider.GetFormat method does not support a DateTimeFormatInfo instance
        [Fact]
        public void PosTest5()
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.GetInstance(new TestIFormatProviderClass());
            Assert.Equal(DateTimeFormatInfo.CurrentInfo, info);
        }
    }
}
