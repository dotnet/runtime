// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoEquals
    {
        private int _MINI_STRING_LENGTH = 8;
        private int _MAX_STRING_LENGTH = 256;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: Verify same culture TextInfo equals original TextInfo
        [Fact]
        public void TestSameCultureTextInfo()
        {
            CultureInfo ci = new CultureInfo("en-US");
            CultureInfo ci2 = new CultureInfo("en-US");
            object textInfo = ci2.TextInfo;
            Assert.True(ci.TextInfo.Equals(textInfo));
        }

        // PosTest2: Verify the TextInfo is not same CultureInfo's
        [Fact]
        public void TestDiffCultureTextInfo()
        {
            TextInfo textInfoFrance = new CultureInfo("fr-FR").TextInfo;
            TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;

            Assert.False(textInfoFrance.Equals((object)textInfoUS));
        }

        // PosTest3: Verify the TextInfo not equal a null reference
        [Fact]
        public void TestNullReference()
        {
            TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;

            Assert.False(textInfoUS.Equals(null));
        }

        // PosTest4: Verify the TextInfo not equal another type object
        [Fact]
        public void TestDiffTypeObject()
        {
            TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;
            object obj = (object)(new MyClass());
            Assert.False(textInfoUS.Equals(obj));
        }

        // PosTest5: Verify the TextInfo not equal a int object
        [Fact]
        public void TestIntObject()
        {
            TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;
            int i = _generator.GetInt32(-55);
            object intObject = i as object;
            Assert.False(textInfoUS.Equals(intObject));
        }

        // PosTest6: Verify the TextInfo not equal a string object
        [Fact]
        public void TestStringObject()
        {
            TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;
            String str = _generator.GetString(-55, false, _MINI_STRING_LENGTH, _MAX_STRING_LENGTH);
            object strObject = str as object;

            Assert.False(textInfoUS.Equals(strObject));
        }

        #region Customer Class
        public class MyClass
        {
        }
        #endregion
    }
}

