// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class CopyToArrayIntNameValueCollectionTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;
            NameValueCollection nvc;

            // simple string values
            string[] values =
            {
                "",
                " ",
                "a",
                "aA",
                "text",
                "     SPaces",
                "1",
                "$%^#",
                "2222222222222222222222222",
                System.DateTime.Today.ToString(),
                Int32.MaxValue.ToString()
            };

            // keys for simple string values
            string[] keys =
            {
                "zero",
                "oNe",
                " ",
                "",
                "aa",
                "1",
                System.DateTime.Today.ToString(),
                "$%^#",
                Int32.MaxValue.ToString(),
                "     spaces",
                "2222222222222222222222222"
            };

            string[] destination;
            int cnt = 0;            // Count 

            // initialize IntStrings
            intl = new IntlStrings();


            // [] NameValueCollection is constructed as expected
            //-----------------------------------------------------------------

            nvc = new NameValueCollection();

            // [] CopyTo() empty collection into empty array
            //
            destination = new string[] { };
            try
            {
                nvc.CopyTo(destination, -1);
                Assert.False(true, "Error, no exception");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Error, unexpected exception: {0}", e.ToString()));
            }

            try
            {
                nvc.CopyTo(destination, 0);
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Error, unexpected exception: {0}", e.ToString()));
            }

            try
            {
                nvc.CopyTo(destination, 1);
                Assert.False(true, "Error, no exception");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Error, unexpected exception: {0}", e.ToString()));
            }

            // [] CopyTo() empty collection into filled array
            //
            destination = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                destination[i] = values[i];
            }
            nvc.CopyTo(destination, 0);
            if (destination.Length != values.Length)
            {
                Assert.False(true, "Error, altered array after copying empty collection");
            }
            if (destination.Length == values.Length)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (String.Compare(destination[i], values[i]) != 0)
                    {
                        Assert.False(true, string.Format("Error, altered item {0} after copying empty collection", i));
                    }
                }
            }

            //
            // [] CopyTo(array, 0) collection with simple strings


            cnt = nvc.Count;
            int len = values.Length;
            for (int i = 0; i < len; i++)
            {
                nvc.Add(keys[i], values[i]);
            }
            if (nvc.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, values.Length));
            }

            destination = new string[len];
            nvc.CopyTo(destination, 0);
            //
            // order of items is the same as order it was in collection
            //
            for (int i = 0; i < len; i++)
            {
                // verify that collection is copied correctly
                //

                if (String.Compare(nvc[i], destination[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, destination[i], nvc[i]));
                }
            }


            // [] CopyTo(array, middle_index) collection with simple strings
            //

            nvc.Clear();

            for (int i = 0; i < len; i++)
            {
                nvc.Add(keys[i], values[i]);
            }
            if (nvc.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, values.Length));
            }

            destination = new string[len * 2];
            nvc.CopyTo(destination, len);

            //
            // order of items is the same as they wer in collection
            //
            for (int i = 0; i < len; i++)
            {
                // verify that collection is copied correctly
                //
                if (String.Compare(nvc[i], destination[i + len]) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, destination[i + len], nvc[i]));
                }
            }

            //
            // Intl strings
            // [] CopyTo(array, 0) collection with Intl strings
            //

            string[] intlValues = new string[len * 2];

            // fill array with unique strings
            //
            for (int i = 0; i < len * 2; i++)
            {
                string val = intl.GetRandomString(MAX_LEN);
                while (Array.IndexOf(intlValues, val) != -1)
                    val = intl.GetRandomString(MAX_LEN);
                intlValues[i] = val;
            }

            Boolean caseInsensitive = false;
            for (int i = 0; i < len * 2; i++)
            {
                if (intlValues[i].Length != 0 && intlValues[i].ToLowerInvariant() == intlValues[i].ToUpperInvariant())
                    caseInsensitive = true;
            }


            nvc.Clear();
            for (int i = 0; i < len; i++)
            {
                nvc.Add(intlValues[i + len], intlValues[i]);
            }
            if (nvc.Count != (len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, len));
            }

            destination = new string[len];
            nvc.CopyTo(destination, 0);
            //
            // order of items is the same as they wer in collection
            //
            for (int i = 0; i < len; i++)
            {
                // verify that collection is copied correctly
                //
                if (String.Compare(nvc[i], destination[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, destination[i], nvc[i]));
                }
            }

            //
            // Intl strings
            // [] CopyTo(array, middle_index) collection with Intl strings
            //


            destination = new string[len * 2];
            nvc.CopyTo(destination, len);

            //
            // order of items is the same as they were in collection
            //
            for (int i = 0; i < len; i++)
            {
                // verify that collection is copied correctly
                //
                if (String.Compare(nvc[i], destination[i + len]) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, destination[i + len], nvc[i]));
                }
            }


            //
            // [] Case sensitivity
            //

            string[] intlValuesLower = new string[len * 2];

            // fill array with unique strings
            //
            for (int i = 0; i < len * 2; i++)
            {
                intlValues[i] = intlValues[i].ToUpperInvariant();
            }

            for (int i = 0; i < len * 2; i++)
            {
                intlValuesLower[i] = intlValues[i].ToLowerInvariant();
            }

            nvc.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                nvc.Add(intlValues[i + len], intlValues[i]);     // adding uppercase strings
            }

            destination = new string[len];
            nvc.CopyTo(destination, 0);

            //
            // order of items is the same as they were in collection
            //
            for (int i = 0; i < len; i++)
            {
                // verify that collection is copied correctly
                //
                if (String.Compare(nvc[i], destination[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, destination[i], nvc[i]));
                }

                if (!caseInsensitive && Array.IndexOf(intlValuesLower, destination[i]) != -1)
                {
                    Assert.False(true, string.Format("Error, copied lowercase string"));
                }
            }


            //
            //   [] CopyTo(null, int)
            //
            destination = null;
            Assert.Throws<ArgumentNullException>(() => { nvc.CopyTo(destination, 0); });

            //
            //   [] CopyTo(string[], -1)
            //
            cnt = nvc.Count;

            destination = new string[] { };
            Assert.Throws<ArgumentOutOfRangeException>(() => { nvc.CopyTo(destination, -1); });

            //
            //   [] CopyTo(Array, upperBound+1)
            //
            if (nvc.Count < 1)
            {
                for (int i = 0; i < len; i++)
                {
                    nvc.Add(keys[i], values[i]);
                }
            }

            destination = new string[len];
            Assert.Throws<ArgumentException>(() => { nvc.CopyTo(destination, len); });

            //
            //   [] CopyTo(Array, upperBound+2)
            //
            Assert.Throws<ArgumentException>(() => { nvc.CopyTo(destination, len + 1); });

            //
            //   [] CopyTo(Array, not_enough_space)
            //
            Assert.Throws<ArgumentException>(() => { nvc.CopyTo(destination, len / 2); });

            //
            //   [] CopyTo(multidim_Array, 0)
            //

            Array dest = new string[len, len];
            Assert.Throws<ArgumentException>(() => { nvc.CopyTo(dest, 0); });


            // [] CopyTo(array, 0) collection with multiple items with the same key
            //

            nvc.Clear();
            len = values.Length;
            string k = "keykey";
            string exp = "";
            for (int i = 0; i < len; i++)
            {
                nvc.Add(k, "Value" + i);
                if (i < len - 1)
                    exp += "Value" + i + ",";
                else
                    exp += "Value" + i;
            }
            if (nvc.Count != 1)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, 1));
            }

            destination = new string[1];
            nvc.CopyTo(destination, 0);
            // verify that collection is copied correctly
            //

            if (String.Compare(nvc[0], destination[0]) != 0)
            {
                Assert.False(true, string.Format("Error, copied \"{0}\" instead of \"{1}\"", destination[0], nvc[0]));
            }
            if (String.Compare(exp, destination[0]) != 0)
            {
                Assert.False(true, string.Format("Error, copied string is not the same as expected: {0}", destination[0]));
            }

            //
            //  [] CopyTo(wrong_type, 0)
            //

            dest = new DictionaryEntry[len];

            Assert.Throws<InvalidCastException>(() => { nvc.CopyTo(dest, 0); });
        }
    }
}
