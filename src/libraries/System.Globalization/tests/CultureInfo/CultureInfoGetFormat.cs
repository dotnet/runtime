// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoGetFormat
    {
        [Fact]
        public void PosTest1()
        {
            CultureInfo myCultureInfo = new CultureInfo("en-US");
            Assert.True(myCultureInfo is IFormatProvider);
            Assert.True(myCultureInfo.GetFormat(typeof(NumberFormatInfo)) is NumberFormatInfo);
        }

        [Fact]
        public void PosTest2()
        {
            CultureInfo myCultureInfo = new CultureInfo("en-US");
            Assert.True(myCultureInfo is IFormatProvider);
            Assert.True(myCultureInfo.GetFormat(typeof(DateTimeFormatInfo)) is DateTimeFormatInfo);
        }

        [Fact]
        public void PosTest3()
        {
            CultureInfo myCultureInfo = new CultureInfo("en-US");
            Assert.True(myCultureInfo is IFormatProvider);
            Assert.Null(myCultureInfo.GetFormat(typeof(string)));
        }

        [Fact]
        public void PosTest4()
        {
            TestClass myInstance = new TestClass();
            Assert.True(myInstance is IFormatProvider);
            Assert.True(myInstance.GetFormat(typeof(TestClass1)) is TestClass1);
        }
    }

    public class TestClass : ICustomFormatter, IFormatProvider
    {
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            return "SuccessFormat";
        }

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(TestClass1))
                return new TestClass1();
            else
                return null;
        }
    }
    public class TestClass1 : IFormatProvider
    {
        public object GetFormat(Type formatType)
        {
            return new TestClass1();
        }
    }
}
