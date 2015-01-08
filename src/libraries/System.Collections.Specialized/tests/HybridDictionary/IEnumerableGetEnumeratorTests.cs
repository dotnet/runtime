// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class IEnumerableGetEnumeratorTests
    {
        [Fact]
        public void Test01()
        {
            HybridDictionary hd;
            IEnumerator en;

            const int BIG_LENGTH = 100;

            // simple string values
            string[] valuesShort =
            {
                "",
                " ",
                "$%^#",
                System.DateTime.Today.ToString(),
                Int32.MaxValue.ToString()
            };

            // keys for simple string values
            string[] keysShort =
            {
                Int32.MaxValue.ToString(),
                " ",
                System.DateTime.Today.ToString(),
                "",
                "$%^#"
            };

            string[] valuesLong = new string[BIG_LENGTH];
            string[] keysLong = new string[BIG_LENGTH];

            for (int i = 0; i < BIG_LENGTH; i++)
            {
                valuesLong[i] = "Item" + i;
                keysLong[i] = "keY" + i;
            }

            // [] HybridDictionary GetEnumerator()
            //-----------------------------------------------------------------

            hd = new HybridDictionary();

            //  [] Enumerator for empty dictionary
            //
            en = ((IEnumerable)hd).GetEnumerator();
            string type = en.GetType().ToString();
            if (type.IndexOf("Enumerator", 0) == 0)
            {
                Assert.False(true, string.Format("Error, type is not Enumerator"));
            }

            //  ---------------------------------------------------------
            //   [] Enumerator for Filled dictionary  - list
            //  ---------------------------------------------------------
            //
            for (int i = 0; i < valuesShort.Length; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }

            en = ((IEnumerable)hd).GetEnumerator();
            type = en.GetType().ToString();
            if (type.IndexOf("Enumerator", 0) == 0)
            {
                Assert.False(true, string.Format("Error, type is not Enumerator"));
            }

            //  ---------------------------------------------------------
            //   [] Enumerator for Filled dictionary  - hashtable
            //  ---------------------------------------------------------
            //
            for (int i = 0; i < valuesLong.Length; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }

            en = ((IEnumerable)hd).GetEnumerator();
            type = en.GetType().ToString();
            if (type.IndexOf("Enumerator", 0) == 0)
            {
                Assert.False(true, string.Format("Error, type is not Enumerator"));
            }

            //  ---------------------------------------------------------
            //   [] Enumerator for empty case-insensitive dictionary
            //  ---------------------------------------------------------
            //
            hd = new HybridDictionary(true);

            en = ((IEnumerable)hd).GetEnumerator();
            type = en.GetType().ToString();
            if (type.IndexOf("Enumerator", 0) == 0)
            {
                Assert.False(true, string.Format("Error, type is not Enumerator"));
            }
        }
    }
}
