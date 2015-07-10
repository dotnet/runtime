// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoListSeparator
    {
        private int _MINI_STRING_LENGTH = 1;
        private int _MAX_STRING_LENGTH = 20;

        // PosTest1: Verify ListSeparator of en-US CultureInfo's TextInfo is  not empty
        [Fact]
        public void VerifyEnUSListSeperator()
        {
            CultureInfo ci = new CultureInfo("en-US");
            TextInfo textInfoUS = ci.TextInfo;
            Assert.NotEqual(String.Empty, textInfoUS.ListSeparator);
        }

        // PosTest2: Verify setting  ListSeparator
        [Fact]
        public void SetListSeperator()
        {
            CultureInfo ci = new CultureInfo("en-US");
            TextInfo textInfoUS = ci.TextInfo;
            string strListSeparator = TestLibrary.Generator.GetString(-55, false, _MINI_STRING_LENGTH, _MAX_STRING_LENGTH);
            textInfoUS.ListSeparator = strListSeparator;
            Assert.Equal(strListSeparator, textInfoUS.ListSeparator);
        }

        // NegTest1: Setting ListSeparator as a null reference
        [Fact]
        public void SetNullReference()
        {
            TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;
            string str = null;

            Assert.Throws<ArgumentNullException>(() =>
            {
                textInfoUS.ListSeparator = str;
            });
        }
    }
}

