// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace Test
{
    public class Co6057ToDateTime_all
    {
        [Fact]
        public static void runTest()
        {
            ///////////////////////////////////////////// DateTime Convert.ToDateTime( String )

            //[] ToDateTime(String) - Vanilla Cases ("12/31/1999 11:59:59 PM","0100/01/01 12:00:00 AM","1492/02/29 12:00:00 AM")

            // Setup String Test
            {
                DateTime[] expectedValues = { new DateTime(1999, 12, 31, 23, 59, 59), new DateTime(100, 1, 1, 0, 0, 0), new DateTime(2216, 2, 29, 0, 0, 0), new DateTime(1, 1, 1, 0, 0, 0), };
                // Some calendars have very restricted date ranges
                var dateTimeFormat = CultureInfo.CurrentCulture.DateTimeFormat;
                if (expectedValues[1] < dateTimeFormat.Calendar.MinSupportedDateTime)
                    expectedValues[1] = dateTimeFormat.Calendar.MinSupportedDateTime.AddYears(100);
                if (expectedValues[2] > dateTimeFormat.Calendar.MaxSupportedDateTime)
                    expectedValues[2] = dateTimeFormat.Calendar.MaxSupportedDateTime.Date;
                if (expectedValues[3] < dateTimeFormat.Calendar.MinSupportedDateTime)
                    expectedValues[3] = dateTimeFormat.Calendar.MinSupportedDateTime;
                String[] testValues = new String[4];
                for (int i = 0; i < expectedValues.Length; i++)
                {
                    testValues[i] = expectedValues[i].ToString(dateTimeFormat.LongDatePattern, dateTimeFormat);
                }
                testValues[0] = expectedValues[0].ToString();

                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    DateTime result = Convert.ToDateTime(testValues[i]);
                    if (!result.Equals(expectedValues[i]))
                    {
                        //Logger.LogInformation("{0} - {1} - {2}", result.Ticks, expectedValues[i].Ticks, testValues[i]);
                        Assert.True(false, " Expected = '" + expectedValues[i] + "' ... Received = '" + result + "'.");
                    }
                }
            }

            ///////////////////////////////////////////// []DateTime Convert.ToDateTime( String, IFormatPRovider )

            // Setup String Test
            {
                String[] testValues = { "12/31/1999 11:59:59 PM", "0100/01/01 12:00:00 AM", "1492/02/29 12:00:00 AM", "0001/01/01 12:00:00 AM", };
                DateTime[] expectedValues = { new DateTime(1999, 12, 31, 23, 59, 59), new DateTime(100, 1, 1, 0, 0, 0), new DateTime(1492, 2, 29, 0, 0, 0), new DateTime(1, 1, 1, 0, 0, 0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    DateTime result = Convert.ToDateTime(testValues[i], new DateTimeFormatInfo());
                    if (!result.Equals(expectedValues[i]))
                    {
                        Assert.True(false, " Expected = '" + expectedValues[i] + "' ... Received = '" + result + "'.");
                    }
                }
            }

            ///////////////////////////////////////////// DateTime Convert.ToDateTime( Object )

            //[] ToDateTime( Object ) - obj = null
            {
                DateTime bTest = Convert.ToDateTime((Object)null);
                Assert.Equal(DateTime.MinValue, bTest);
            }

            //[] ToDateTime( Object, IFP ) - obj = null
            {
                DateTime bTest = Convert.ToDateTime((Object)null, CultureInfo.CurrentCulture);
                Assert.Equal(DateTime.MinValue, bTest);
            }
            ///////////////////////////////////////////// []DateTime Convert.ToDateTime( DateTime )

            // Setup String Test
            {
                DateTime[] expectedValues = { new DateTime(1999, 12, 31, 23, 59, 59), new DateTime(100, 1, 1, 0, 0, 0), new DateTime(1492, 2, 29, 0, 0, 0), new DateTime(1, 1, 1, 0, 0, 0), };
                // Vanila Test Cases
                for (int i = 0; i < expectedValues.Length; i++)
                {
                    DateTime result = Convert.ToDateTime(expectedValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            {
                //[] ToDateTime(String) - Cases (null, "null")
                DateTime result = Convert.ToDateTime(null);
                Assert.Equal(DateTime.MinValue, result);

                // Exception Test Case
                //[] ToDateTime(String) - Exception Cases (null, "null")
                result = Convert.ToDateTime(null, new DateTimeFormatInfo());
                Assert.Equal(DateTime.MinValue, result);
            }
        }

        [Fact]
        public static void runTest_Negative()
        {
            // Exception Test Case
            Assert.Throws<FormatException>(() => { DateTime result = Convert.ToDateTime("null"); });

            // NDPWhidbey #5722 regression case
            // This was throwing IndexOutOfRangeException
            String foo = "20-5-14T00:00:00";
            Assert.Throws<FormatException>(() => { DateTime dt = Convert.ToDateTime(foo); });

            // Exception Test Case
            Assert.Throws<FormatException>(() => { DateTime result = Convert.ToDateTime("null", new DateTimeFormatInfo()); });

            //[] ToDateTime( Object ) - Exception Case (Object that does not implement IConvertible) 
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime(new Object()); });

            ///////////////////////////////////////////// [] DateTime Convert.ToDateTime( Object, IFormatProvider )
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime(new Object(), new DateTimeFormatInfo()); });

            ///////////////////////////////////////////// [] DateTime Convert.ToDateTime( Boolean )
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime(false); });

            ///////////////////////////////////////////// [] DateTime Convert.ToDateTime( Char )
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime('a'); });

            ///////////////////////////////////////////// [] DateTime Convert.ToDateTime( Int16 )
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime((short)5); });

            ///////////////////////////////////////////// [] DateTime Convert.ToDateTime( Int32 )
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime(6); });

            ///////////////////////////////////////////// [] DateTime Convert.ToDateTime( Int64 )
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime((long)5); });

            ///////////////////////////////////////////// [] DateTime Convert.ToDateTime( UInt16 )
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime((ushort)5); });

            ///////////////////////////////////////////// [] DateTime Convert.ToDateTime( UInt32 )
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime((uint)5); });

            ///////////////////////////////////////////// [] DateTime Convert.ToDateTime( UInt64 )
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime((ulong)5); });

            ///////////////////////////////////////////// [] DateTime Convert.ToDateTime( Single )
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime(1.0f); });

            ///////////////////////////////////////////// [] DateTime Convert.ToDateTime( Double )
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime(1.1); });

            ///////////////////////////////////////////// [] DateTime Convert.ToDateTime( Decimal )
            Assert.Throws<InvalidCastException>(() => { DateTime bTest = Convert.ToDateTime(1.0m); });
        }
    }
}
