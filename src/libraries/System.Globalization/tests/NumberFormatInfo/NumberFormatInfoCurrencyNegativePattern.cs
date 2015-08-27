// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyNegativePattern
    {
        // TestDefault: Verify default value of property CurrencyNegativePattern
        [Fact]
        public void TestDefault()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            int expected = nfi.CurrencyNegativePattern;
            Assert.Equal(0, expected);
        }

        // TestSetValue: Verify set value of property CurrencyNegativePattern
        [Fact]
        public void TestSetValue()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            for (int i = 0; i <= 15; i++)
            {
                nfi.CurrencyNegativePattern = i;
                Assert.Equal(i, nfi.CurrencyNegativePattern);
            }
        }

        // TestArgumentOutOfRangeException: ArgumentOutOfRangeException is thrown
        [Fact]
        public void TestArgumentOutOfRangeException()
        {
            VerificationHelper<ArgumentOutOfRangeException>(-1);
            VerificationHelper<ArgumentOutOfRangeException>(16);
        }

        // TestInvalidOperationException: InvalidOperationException is thrown
        [Fact]
        public void TestInvalidOperationException()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<InvalidOperationException>(() =>
            {
                nfiReadOnly.CurrencyNegativePattern = 1;
            });
        }

        // TestLocale0: Verify value of property CurrencyNegativePattern for specific locale
        [Fact]
        public void TestLocale0()
        {
            CultureInfo myTestCulture = new CultureInfo("en-US");
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.CurrencyNegativePattern; //0="($n)"

            // todo: determine if Windows version needs to support "accounting" currency explictly which contains parenthesis
            Assert.True(expected == 1 || expected == 0); //1=ICU, 0=Windows
        }

        // TestLocale1: Verify value of property CurrencyNegativePattern for specific locale
        [Fact]
        public void TestLocale1()
        {
            CultureInfo myTestCulture = new CultureInfo("en-CA");
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.CurrencyNegativePattern; // 1="-$n"
            Assert.Equal(1, expected);
        }

        // TestLocale3: Verify value of property CurrencyNegativePattern for specific locale
        [Fact]
        public void TestLocale3()
        {
            CultureInfo myTestCulture = new CultureInfo("fa-IR"); // RTL locale
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.CurrencyNegativePattern; // 3="$n-" 
            // todo: determine why some values are different
            Assert.True(expected == 1 || expected == 3);  //1=ICU, 3=Windows
        }

        // TestLocale4: Verify value of property CurrencyNegativePattern for specific locale
        [Fact]
        public void TestLocale4()
        {
            CultureInfo myTestCulture = new CultureInfo("fr-CD");
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.CurrencyNegativePattern; // 4="(n$)"

            // todo: determine if Windows version needs to support "accounting" currency explictly which contains parenthesis
            Assert.True(expected == 8 || expected == 4); //8=ICU, 4=Windows
        }

        // TestLocale8: Verify value of property CurrencyNegativePattern for specific locale
        [Fact]
        public void TestLocale8()
        {
            CultureInfo myTestCulture = new CultureInfo("bg-BG");
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.CurrencyNegativePattern; // 8="-n $"
            Assert.Equal(8, expected);
        }

        // TestLocale12: Verify value of property CurrencyNegativePattern for specific locale
        [Fact]
        public void TestLocale12()
        {
            CultureInfo myTestCulture = new CultureInfo("as");
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.CurrencyNegativePattern; // 12="$ -n"
            // todo: determine why some values are different
            Assert.True(expected == 9 || expected == 12);  //9=ICU, 12=Windows
        }

        // TestLocale14: Verify value of property CurrencyNegativePattern for specific locale
        [Fact]
        public void TestLocale14()
        {
            CultureInfo myTestCulture = new CultureInfo("es-BO");
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.CurrencyNegativePattern; //14="($ n)"
            // todo: determine if Windows version needs to support "accounting" currency explictly which contains parenthesis
            Assert.True(expected == 1 || expected == 14);  //1=ICU, 14=Windows
        }

        // TestLocale15: Verify value of property CurrencyNegativePattern for specific locale
        [Fact]
        public void TestLocale15()
        {
            CultureInfo myTestCulture = new CultureInfo("fr-CA");
            NumberFormatInfo nfi = myTestCulture.NumberFormat;
            int expected = nfi.CurrencyNegativePattern; //15="(n $)"
            // todo: determine if Windows version needs to support "accounting" currency explictly which contains parenthesis
            Assert.True(expected == 8 || expected == 15);  //8=ICU, 15=Windows
        }

        private void VerificationHelper<T>(int i) where T : Exception
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Throws<T>(() =>
            {
                nfi.CurrencyNegativePattern = i;
                int actual = nfi.CurrencyNegativePattern;
            });
        }
    }
}
