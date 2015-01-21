// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CtorIntBoolTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            HybridDictionary hd;

            const int BIG_LENGTH = 100;

            string[] valuesLong = new string[BIG_LENGTH];
            string[] keysLong = new string[BIG_LENGTH];
            int len;

            for (int i = 0; i < BIG_LENGTH; i++)
            {
                valuesLong[i] = "Item" + i;
                keysLong[i] = "keY" + i;
            }

            // [] HybridDictionary is constructed as expected
            //-----------------------------------------------------------------

            // [] Capacity 0, Case-sensitive ctor

            hd = new HybridDictionary(0, false);

            if (hd == null)
            {
                Assert.False(true, string.Format("Error, dictionary is null after default ctor"));
            }

            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} after default ctor", hd.Count));
            }

            if (hd["key"] != null)
            {
                Assert.False(true, string.Format("Error, Item(some_key) returned non-null after default ctor"));
            }

            //
            // [] Add(string, string)
            //
            // should be able to add keys that differ only in casing
            hd.Add("Name", "Value");
            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", hd.Count));
            }
            if (String.Compare(hd["Name"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value"));
            }
            hd.Add("NaMe", "Value");
            if (hd.Count != 2)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 2", hd.Count));
            }
            if (String.Compare(hd["NaMe"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value"));
            }
            // by default should be case sensitive
            if (hd["NAME"] != null)
            {
                Assert.False(true, string.Format("Error, Item() returned non-null value for uppercase key"));
            }

            //
            // [] Clear() short dictionary
            //
            hd.Clear();
            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", hd.Count));
            }
            if (hd["Name"] != null)
            {
                Assert.False(true, string.Format("Error, Item() returned non-null value after Clear()"));
            }

            //
            // [] numerous Add(string, string)
            //
            len = valuesLong.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", hd.Count, len));
            }
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(hd[keysLong[i]].ToString(), valuesLong[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, Item() returned unexpected value", i));
                }
                if (hd[keysLong[i].ToUpper()] != null)
                {
                    Assert.False(true, string.Format("Error, Item() returned non-null for uppercase key", i));
                }
            }

            //
            // [] Clear() long dictionary
            //
            hd.Clear();
            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", hd.Count));
            }

            //
            // [] few elements not overriding Equals()
            //
            hd.Clear();
            Hashtable[] lbls = new Hashtable[2];
            ArrayList[] bs = new ArrayList[2];
            lbls[0] = new Hashtable();
            lbls[1] = new Hashtable();
            bs[0] = new ArrayList();
            bs[1] = new ArrayList();
            hd.Add(lbls[0], bs[0]);
            hd.Add(lbls[1], bs[1]);
            if (hd.Count != 2)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 2", hd.Count));
            }
            if (!hd.Contains(lbls[0]))
            {
                Assert.False(true, string.Format("Error, doesn't contain 1st special item"));
            }
            if (!hd.Contains(lbls[1]))
            {
                Assert.False(true, string.Format("Error, doesn't contain 2nd special item"));
            }
            if (hd.Values.Count != 2)
            {
                Assert.False(true, string.Format("Error, Values.Count returned {0} instead of 2", hd.Values.Count));
            }

            hd.Remove(lbls[1]);
            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, failed to remove item"));
            }
            if (hd.Contains(lbls[1]))
            {
                Assert.False(true, string.Format("Error, failed to remove special item"));
            }

            //
            // [] many elements not overriding Equals()
            //
            hd.Clear();
            lbls = new Hashtable[BIG_LENGTH];
            bs = new ArrayList[BIG_LENGTH];
            for (int i = 0; i < BIG_LENGTH; i++)
            {
                lbls[i] = new Hashtable();
                bs[i] = new ArrayList();
                hd.Add(lbls[i], bs[i]);
            }
            if (hd.Count != BIG_LENGTH)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", hd.Count, BIG_LENGTH));
            }
            for (int i = 0; i < BIG_LENGTH; i++)
            {
                if (!hd.Contains(lbls[0]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain 1st special item"));
                }
            }
            if (hd.Values.Count != BIG_LENGTH)
            {
                Assert.False(true, string.Format("Error, Values.Count returned {0} instead of {1}", hd.Values.Count, BIG_LENGTH));
            }

            hd.Remove(lbls[0]);
            if (hd.Count != BIG_LENGTH - 1)
            {
                Assert.False(true, string.Format("Error, failed to remove item"));
            }
            if (hd.Contains(lbls[0]))
            {
                Assert.False(true, string.Format("Error, failed to remove special item"));
            }

            // ----------------------------------------------------------------
            //-----------------------------------------------------------------

            // [] Capacity 10 - Case-sensitive

            hd = new HybridDictionary(10, false);

            if (hd == null)
            {
                Assert.False(true, string.Format("Error, dictionary is null after default ctor"));
            }

            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} after default ctor", hd.Count));
            }

            if (hd["key"] != null)
            {
                Assert.False(true, string.Format("Error, Item(some_key) returned non-null after default ctor"));
            }

            //
            // [] Add(string, string)
            //
            // should be able to add keys that differ only in casing
            hd.Add("Name", "Value");
            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", hd.Count));
            }
            if (String.Compare(hd["Name"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value"));
            }
            hd.Add("NaMe", "Value");
            if (hd.Count != 2)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 2", hd.Count));
            }
            if (String.Compare(hd["NaMe"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value"));
            }
            // by default should be case sensitive
            if (hd["NAME"] != null)
            {
                Assert.False(true, string.Format("Error, Item() returned non-null value for uppercase key"));
            }

            //
            // [] Clear() short dictionary
            //
            hd.Clear();
            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", hd.Count));
            }
            if (hd["Name"] != null)
            {
                Assert.False(true, string.Format("Error, Item() returned non-null value after Clear()"));
            }

            //
            // [] numerous Add(string, string)
            //
            len = valuesLong.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", hd.Count, len));
            }
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(hd[keysLong[i]].ToString(), valuesLong[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, Item() returned unexpected value", i));
                }
                if (hd[keysLong[i].ToUpper()] != null)
                {
                    Assert.False(true, string.Format("Error, Item() returned non-null for uppercase key", i));
                }
            }

            //
            // [] Clear() long dictionary
            //
            hd.Clear();
            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", hd.Count));
            }

            //
            // [] few elements not overriding Equals()
            //
            hd.Clear();
            lbls = new Hashtable[2];
            bs = new ArrayList[2];
            lbls[0] = new Hashtable();
            lbls[1] = new Hashtable();
            bs[0] = new ArrayList();
            bs[1] = new ArrayList();
            hd.Add(lbls[0], bs[0]);
            hd.Add(lbls[1], bs[1]);
            if (hd.Count != 2)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 2", hd.Count));
            }
            if (!hd.Contains(lbls[0]))
            {
                Assert.False(true, string.Format("Error, doesn't contain 1st special item"));
            }
            if (!hd.Contains(lbls[1]))
            {
                Assert.False(true, string.Format("Error, doesn't contain 2nd special item"));
            }
            if (hd.Values.Count != 2)
            {
                Assert.False(true, string.Format("Error, Values.Count returned {0} instead of 2", hd.Values.Count));
            }

            hd.Remove(lbls[1]);
            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, failed to remove item"));
            }
            if (hd.Contains(lbls[1]))
            {
                Assert.False(true, string.Format("Error, failed to remove special item"));
            }

            //
            // [] many elements not overriding Equals()
            //
            hd.Clear();
            lbls = new Hashtable[BIG_LENGTH];
            bs = new ArrayList[BIG_LENGTH];
            for (int i = 0; i < BIG_LENGTH; i++)
            {
                lbls[i] = new Hashtable();
                bs[i] = new ArrayList();
                hd.Add(lbls[i], bs[i]);
            }
            if (hd.Count != BIG_LENGTH)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", hd.Count, BIG_LENGTH));
            }
            for (int i = 0; i < BIG_LENGTH; i++)
            {
                if (!hd.Contains(lbls[0]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain 1st special item"));
                }
            }
            if (hd.Values.Count != BIG_LENGTH)
            {
                Assert.False(true, string.Format("Error, Values.Count returned {0} instead of {1}", hd.Values.Count, BIG_LENGTH));
            }

            hd.Remove(lbls[0]);
            if (hd.Count != BIG_LENGTH - 1)
            {
                Assert.False(true, string.Format("Error, failed to remove item"));
            }
            if (hd.Contains(lbls[0]))
            {
                Assert.False(true, string.Format("Error, failed to remove special item"));
            }

            // ---------------------------------------------------------------
            //----------------------------------------------------------------

            // [] Capacity 100  - case-sensitive

            hd = new HybridDictionary(100, false);

            if (hd == null)
            {
                Assert.False(true, string.Format("Error, dictionary is null after default ctor"));
            }

            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} after default ctor", hd.Count));
            }

            if (hd["key"] != null)
            {
                Assert.False(true, string.Format("Error, Item(some_key) returned non-null after default ctor"));
            }

            //
            // [] Add(string, string)
            //
            // should be able to add keys that differ only in casing
            hd.Add("Name", "Value");
            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", hd.Count));
            }
            if (String.Compare(hd["Name"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value"));
            }
            hd.Add("NaMe", "Value");
            if (hd.Count != 2)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 2", hd.Count));
            }
            if (String.Compare(hd["NaMe"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value"));
            }
            // by default should be case sensitive
            if (hd["NAME"] != null)
            {
                Assert.False(true, string.Format("Error, Item() returned non-null value for uppercase key"));
            }

            //
            // [] Clear() short dictionary
            //
            hd.Clear();
            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", hd.Count));
            }
            if (hd["Name"] != null)
            {
                Assert.False(true, string.Format("Error, Item() returned non-null value after Clear()"));
            }

            //
            // [] numerous Add(string, string)
            //
            len = valuesLong.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", hd.Count, len));
            }
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(hd[keysLong[i]].ToString(), valuesLong[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, Item() returned unexpected value", i));
                }
                if (hd[keysLong[i].ToUpper()] != null)
                {
                    Assert.False(true, string.Format("Error, Item() returned non-null for uppercase key", i));
                }
            }

            //
            // [] Clear() long dictionary
            //
            hd.Clear();
            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", hd.Count));
            }

            //
            // [] few elements not overriding Equals()
            //
            hd.Clear();
            lbls = new Hashtable[2];
            bs = new ArrayList[2];
            lbls[0] = new Hashtable();
            lbls[1] = new Hashtable();
            bs[0] = new ArrayList();
            bs[1] = new ArrayList();
            hd.Add(lbls[0], bs[0]);
            hd.Add(lbls[1], bs[1]);
            if (hd.Count != 2)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 2", hd.Count));
            }
            if (!hd.Contains(lbls[0]))
            {
                Assert.False(true, string.Format("Error, doesn't contain 1st special item"));
            }
            if (!hd.Contains(lbls[1]))
            {
                Assert.False(true, string.Format("Error, doesn't contain 2nd special item"));
            }
            if (hd.Values.Count != 2)
            {
                Assert.False(true, string.Format("Error, Values.Count returned {0} instead of 2", hd.Values.Count));
            }

            hd.Remove(lbls[1]);
            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, failed to remove item"));
            }
            if (hd.Contains(lbls[1]))
            {
                Assert.False(true, string.Format("Error, failed to remove special item"));
            }

            //
            // [] many elements not overriding Equals()
            //
            hd.Clear();
            lbls = new Hashtable[BIG_LENGTH];
            bs = new ArrayList[BIG_LENGTH];
            for (int i = 0; i < BIG_LENGTH; i++)
            {
                lbls[i] = new Hashtable();
                bs[i] = new ArrayList();
                hd.Add(lbls[i], bs[i]);
            }
            if (hd.Count != BIG_LENGTH)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", hd.Count, BIG_LENGTH));
            }
            for (int i = 0; i < BIG_LENGTH; i++)
            {
                if (!hd.Contains(lbls[0]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain 1st special item"));
                }
            }
            if (hd.Values.Count != BIG_LENGTH)
            {
                Assert.False(true, string.Format("Error, Values.Count returned {0} instead of {1}", hd.Values.Count, BIG_LENGTH));
            }

            hd.Remove(lbls[0]);
            if (hd.Count != BIG_LENGTH - 1)
            {
                Assert.False(true, string.Format("Error, failed to remove item"));
            }
            if (hd.Contains(lbls[0]))
            {
                Assert.False(true, string.Format("Error, failed to remove special item"));
            }


            // **************************************************************//
            ///// Case-insensitive ctor ///////////////////////////////////////


            // [] Capacity 0   -  Case-insensitive ctor

            hd = new HybridDictionary(0, true);

            if (hd == null)
            {
                Assert.False(true, string.Format("Error, dictionary is null after default ctor"));
            }

            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} after default ctor", hd.Count));
            }

            if (hd["key"] != null)
            {
                Assert.False(true, string.Format("Error, Item(some_key) returned non-null after default ctor"));
            }

            //
            // [] Add(string, string)
            //
            hd.Add("Name", "Value");
            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", hd.Count));
            }
            if (String.Compare(hd["Name"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value"));
            }

            // should not allow keys that differ only in casing
            Assert.Throws<ArgumentException>(() => { hd.Add("NaMe", "vl"); });

            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", hd.Count));
            }
            // we created case-insensitive  - should find this key
            if (String.Compare(hd["NAME"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value for uppercase key"));
            }

            //
            // [] Clear() short dictionary
            //
            hd.Clear();
            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", hd.Count));
            }
            if (hd["Name"] != null)
            {
                Assert.False(true, string.Format("Error, Item() returned non-null value after Clear()"));
            }

            //
            // [] numerous Add(string, string)
            //
            len = valuesLong.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", hd.Count, len));
            }
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(hd[keysLong[i]].ToString(), valuesLong[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, Item() returned unexpected value", i));
                }
                // should have case-insensitive dictionary
                if (String.Compare(hd[keysLong[i].ToUpper()].ToString(), valuesLong[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, Item() returned unexpected value for uppercase key", i));
                }
            }

            //
            // [] Clear() long dictionary
            //
            hd.Clear();
            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", hd.Count));
            }

            //
            // [] few elements not overriding Equals()
            //
            hd.Clear();
            lbls = new Hashtable[2];
            bs = new ArrayList[2];
            lbls[0] = new Hashtable();
            lbls[1] = new Hashtable();
            bs[0] = new ArrayList();
            bs[1] = new ArrayList();
            hd.Add(lbls[0], bs[0]);
            // should get ArgumentException here
            Assert.Throws<ArgumentException>(() => { hd.Add(lbls[1], bs[1]); });

            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", hd.Count));
            }

            // ---------------------------------------------------------------
            // ---------------------------------------------------------------

            //  [] Capacity 10  - case-insensitive

            hd = new HybridDictionary(10, true);

            if (hd == null)
            {
                Assert.False(true, string.Format("Error, dictionary is null after default ctor"));
            }

            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} after default ctor", hd.Count));
            }

            if (hd["key"] != null)
            {
                Assert.False(true, string.Format("Error, Item(some_key) returned non-null after default ctor"));
            }

            //
            // [] Add(string, string)
            //
            hd.Add("Name", "Value");
            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", hd.Count));
            }
            if (String.Compare(hd["Name"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value"));
            }

            // should not allow keys that differ only in casing
            Assert.Throws<ArgumentException>(() => { hd.Add("NaMe", "vl"); });

            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", hd.Count));
            }
            // we created case-insensitive  - should find this key
            if (String.Compare(hd["NAME"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value for uppercase key"));
            }

            //
            // [] Clear() short dictionary
            //
            hd.Clear();
            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", hd.Count));
            }
            if (hd["Name"] != null)
            {
                Assert.False(true, string.Format("Error, Item() returned non-null value after Clear()"));
            }

            //
            // [] numerous Add(string, string)
            //
            len = valuesLong.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", hd.Count, len));
            }
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(hd[keysLong[i]].ToString(), valuesLong[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, Item() returned unexpected value", i));
                }
                // should have case-insensitive dictionary
                if (String.Compare(hd[keysLong[i].ToUpper()].ToString(), valuesLong[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, Item() returned unexpected value for uppercase key", i));
                }
            }

            //
            // [] Clear() long dictionary
            //
            hd.Clear();
            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", hd.Count));
            }

            //
            // [] few elements not overriding Equals()
            //
            hd.Clear();
            lbls = new Hashtable[2];
            bs = new ArrayList[2];
            lbls[0] = new Hashtable();
            lbls[1] = new Hashtable();
            bs[0] = new ArrayList();
            bs[1] = new ArrayList();
            hd.Add(lbls[0], bs[0]);
            // should get ArgumentException here
            Assert.Throws<ArgumentException>(() => { hd.Add(lbls[1], bs[1]); });

            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", hd.Count));
            }

            // ---------------------------------------------------------------
            // ---------------------------------------------------------------

            // [] Capacity 100, case-insensitive

            hd = new HybridDictionary(100, true);

            if (hd == null)
            {
                Assert.False(true, string.Format("Error, dictionary is null after default ctor"));
            }

            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} after default ctor", hd.Count));
            }

            if (hd["key"] != null)
            {
                Assert.False(true, string.Format("Error, Item(some_key) returned non-null after default ctor"));
            }

            //
            // [] Add(string, string)
            //
            hd.Add("Name", "Value");
            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", hd.Count));
            }
            if (String.Compare(hd["Name"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value"));
            }

            // should not allow keys that differ only in casing
            Assert.Throws<ArgumentException>(() => { hd.Add("NaMe", "vl"); });

            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", hd.Count));
            }
            // we created case-insensitive  - should find this key
            if (String.Compare(hd["NAME"].ToString(), "Value") != 0)
            {
                Assert.False(true, string.Format("Error, Item() returned unexpected value for uppercase key"));
            }

            //
            // [] Clear()
            //
            hd.Clear();
            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", hd.Count));
            }
            if (hd["Name"] != null)
            {
                Assert.False(true, string.Format("Error, Item() returned non-null value after Clear()"));
            }

            //
            // [] numerous Add(string, string)
            //
            len = valuesLong.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", hd.Count, len));
            }
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(hd[keysLong[i]].ToString(), valuesLong[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, Item() returned unexpected value", i));
                }
                // should have case-insensitive dictionary
                if (String.Compare(hd[keysLong[i].ToUpper()].ToString(), valuesLong[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, Item() returned unexpected value for uppercase key", i));
                }
            }

            //
            // [] Clear() long dictionary
            //
            hd.Clear();
            if (hd.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", hd.Count));
            }

            //
            // [] few elements not overriding Equals()
            //
            hd.Clear();
            lbls = new Hashtable[2];
            bs = new ArrayList[2];
            lbls[0] = new Hashtable();
            lbls[1] = new Hashtable();
            bs[0] = new ArrayList();
            bs[1] = new ArrayList();
            hd.Add(lbls[0], bs[0]);
            // should get ArgumentException here
            Assert.Throws<ArgumentException>(() => { hd.Add(lbls[1], bs[1]); });

            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", hd.Count));
            }
        }
    }
}
