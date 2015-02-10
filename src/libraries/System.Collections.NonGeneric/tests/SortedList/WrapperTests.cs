// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    /// <summary>
    /// The goal is to call the methods of the base class on the wrapper
    /// classes to ensure that the right thing is happening in SortedList
    /// </summary>
    public class WrapperTests
    {
        [Fact]
        public void TestKeys()
        {
            var hsh1 = new Hashtable();
            //Keys
            var dic1 = new SortedList();
            var icol1 = dic1.Keys;

            for (int i = 0; i < 100; i++)
                dic1.Add("Key_" + i, "Value_" + i);

            DoICollectionTests(dic1, icol1, hsh1, DicType.Key);
            Assert.False(hsh1.Count > 0, "Error, Keys");
        }

        [Fact]
        public void TestValues()
        {
            var hsh1 = new Hashtable();
            var dic1 = new SortedList();

            for (int i = 0; i < 100; i++)
                dic1.Add("Key_" + i, "Value_" + i);

            var icol1 = dic1.Values;

            DoICollectionTests(dic1, icol1, hsh1, DicType.Value);

            Assert.False(hsh1.Count > 0, "Error, Values");
        }

        [Fact]
        public void TestGetKeyValueList()
        {
            var dic1 = new SortedList();

            for (int i = 0; i < 100; i++)
                dic1.Add("Key_" + i, "Value_" + i);

            var ilst1 = dic1.GetKeyList();
            var hsh1 = new Hashtable();

            DoIListTests(dic1, ilst1, hsh1, DicType.Key);

            Assert.False(hsh1.Count != 2
                || !hsh1.ContainsKey("IsReadOnly")
                || !hsh1.ContainsKey("IsFixedSize"), "Error, KeyList");


            dic1 = new SortedList();
            for (int i = 0; i < 100; i++)
                dic1.Add("Key_" + i, "Value_" + i);

            ilst1 = dic1.GetValueList();
            hsh1 = new Hashtable();
            DoIListTests(dic1, ilst1, hsh1, DicType.Value);

            Assert.False(hsh1.Count != 2
                || !hsh1.ContainsKey("IsReadOnly")
                || !hsh1.ContainsKey("IsFixedSize"), "Error, ValueList");
        }

        [Fact]
        public void TestIDictionaryEnumerator()
        {
            object oValue;
            DictionaryEntry dicEnt;

            var dic1 = new SortedList();

            for (int i = 0; i < 100; i++)
                dic1.Add("Key_" + i, "Value_" + i);

            var idicTest = dic1.GetEnumerator();
            var hsh1 = new Hashtable();
            var hsh2 = new Hashtable();

            //making sure that we throw if looking at values before MoveNext
            try
            {
                oValue = idicTest.Key;
                hsh1["Enumerator"] = "NoEXception";
                Assert.False(true);
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }
            try
            {
                oValue = idicTest.Value;
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }
            try
            {
                oValue = idicTest.Current;
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }

            while (idicTest.MoveNext())
            {
                if (!dic1.Contains(idicTest.Key))
                    hsh1["IDictionaryEnumerator"] = "";
                if (dic1[idicTest.Key] != idicTest.Value)
                    hsh1["IDictionaryEnumerator"] = "";

                dicEnt = idicTest.Entry;
                if (!dic1.Contains(dicEnt.Key))
                    hsh1["IDictionaryEnumerator"] = "";
                if (dic1[dicEnt.Key] != dicEnt.Value)
                    hsh1["IDictionaryEnumerator"] = "";

                try
                {
                    hsh2.Add(idicTest.Key, idicTest.Value);
                }
                catch (Exception)
                {
                    hsh1["IDictionaryEnumerator"] = "";
                }

                dicEnt = (DictionaryEntry)idicTest.Current;
                if (!dic1.Contains(dicEnt.Key))
                    hsh1["IDictionaryEnumerator"] = "";
                if (dic1[dicEnt.Key] != dicEnt.Value)
                    hsh1["IDictionaryEnumerator"] = "";
            }

            try
            {
                oValue = idicTest.Key;
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }
            try
            {
                oValue = idicTest.Value;
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }
            try
            {
                oValue = idicTest.Current;
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }

            idicTest.Reset();
            hsh2 = new Hashtable();
            while (idicTest.MoveNext())
            {
                if (!dic1.Contains(idicTest.Key))
                    hsh1["IDictionaryEnumerator"] = "";
                if (dic1[idicTest.Key] != idicTest.Value)
                    hsh1["IDictionaryEnumerator"] = "";

                dicEnt = idicTest.Entry;
                if (!dic1.Contains(dicEnt.Key))
                    hsh1["IDictionaryEnumerator"] = "";
                if (dic1[dicEnt.Key] != dicEnt.Value)
                    hsh1["IDictionaryEnumerator"] = "";

                try
                {
                    hsh2.Add(idicTest.Key, idicTest.Value);
                }
                catch (Exception)
                {
                    hsh1["IDictionaryEnumerator"] = "";
                }

                dicEnt = (DictionaryEntry)idicTest.Current;
                if (!dic1.Contains(dicEnt.Key))
                    hsh1["IDictionaryEnumerator"] = "";
                if (dic1[dicEnt.Key] != dicEnt.Value)
                    hsh1["IDictionaryEnumerator"] = "";
            }

            idicTest.Reset();
            dic1.Add("Key_Blah", "Value_Blah");
            try
            {
                idicTest.MoveNext();
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }

            try
            {
                object blah = idicTest.Current;
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }

            try
            {
                idicTest.Reset();
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }

            try
            {
                oValue = idicTest.Key;
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }

            try
            {
                oValue = idicTest.Value;
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }
        }

        [Fact]
        public void TestCloneWithEnumerator()
        {
            //WE will test the Clone() method of the enumerator
            DictionaryEntry dicEnt;

            var dic1 = new SortedList();

            for (int i = 0; i < 100; i++)
                dic1.Add("Key_" + i, "Value_" + i);

           var  idicTest = dic1.GetEnumerator();
           var  idicTest1 = dic1.GetEnumerator();

            var hsh1 = new Hashtable();

            var hsh2 = new Hashtable();
            while (idicTest1.MoveNext())
            {
                if (!dic1.Contains(idicTest1.Key))
                    hsh1["IDictionaryEnumerator"] = "";
                if (dic1[idicTest1.Key] != idicTest1.Value)
                    hsh1["IDictionaryEnumerator"] = "";

                dicEnt = idicTest1.Entry;
                if (!dic1.Contains(dicEnt.Key))
                    hsh1["IDictionaryEnumerator"] = "";
                if (dic1[dicEnt.Key] != dicEnt.Value)
                    hsh1["IDictionaryEnumerator"] = "";

                try
                {
                    hsh2.Add(idicTest1.Key, idicTest1.Value);
                }
                catch (Exception)
                {
                    hsh1["IDictionaryEnumerator"] = "";
                }

                dicEnt = (DictionaryEntry)idicTest1.Current;
                if (!dic1.Contains(dicEnt.Key))
                    hsh1["IDictionaryEnumerator"] = "";
                if (dic1[dicEnt.Key] != dicEnt.Value)
                    hsh1["IDictionaryEnumerator"] = "";

            }

            idicTest1.Reset();
            hsh2 = new Hashtable();
            while (idicTest1.MoveNext())
            {
                if (!dic1.Contains(idicTest1.Key))
                    hsh1["IDictionaryEnumerator"] = "";
                if (dic1[idicTest1.Key] != idicTest1.Value)
                    hsh1["IDictionaryEnumerator"] = "";

                dicEnt = idicTest1.Entry;
                if (!dic1.Contains(dicEnt.Key))
                    hsh1["IDictionaryEnumerator"] = "";
                if (dic1[dicEnt.Key] != dicEnt.Value)
                    hsh1["IDictionaryEnumerator"] = "";

                try
                {
                    hsh2.Add(idicTest1.Key, idicTest1.Value);
                }
                catch (Exception)
                {
                    hsh1["IDictionaryEnumerator"] = "";
                }

                dicEnt = (DictionaryEntry)idicTest1.Current;
                if (!dic1.Contains(dicEnt.Key))
                    hsh1["IDictionaryEnumerator"] = "";
                if (dic1[dicEnt.Key] != dicEnt.Value)
                    hsh1["IDictionaryEnumerator"] = "";
            }

            idicTest1.Reset();
            dic1.Add("Key_Blah", "Value_Blah");
            try
            {
                idicTest1.MoveNext();
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }

            try
            {
                object blah = idicTest1.Current;
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }

            Assert.False(hsh1.Count > 0, "Error, Values");
        }

        [Fact]
        public void TestSynchronized()
        {
            string[] strKeyArr = null;
            string[] strValueArr = null;

            //Synchronized SortedList
            ///////////////////////////////

            var hsh1 = new Hashtable();
            var dic1 = new SortedList();
            var icol1 = dic1.Keys;

            for (int i = 0; i < 100; i++)
                dic1.Add("Key_" + i, "Value_" + i);

            var dic2 = SortedList.Synchronized(dic1);

            //Count
            if (dic2.Count != dic1.Count)
            {
                hsh1["Synchronized"] = "Count";
            }

            var dicEnt1 = new DictionaryEntry[dic2.Count];
            dic2.CopyTo(dicEnt1, 0);
            //ContainsXXX
           var hsh3 = new Hashtable();
            var iNumberOfElements = 100;
            for (int i = 0; i < iNumberOfElements; i++)
            {
                if (!dic2.Contains("Key_" + i))
                {
                    hsh1["Synchronized"] = "Contains";
                }
                if (!((string)dic2["Key_" + i]).Equals("Value_" + i))
                {
                    hsh1["Synchronized"] = ", " + dic2["Key_" + i] + " " + ("Value_" + i) + " " + ((string)dic2["Key_" + i] != ("Value_" + i));
                }
                try
                {
                    hsh3.Add(((DictionaryEntry)dicEnt1[i]).Key, null);
                }
                catch (Exception ex)
                {
                    hsh1["Synchronized"] = ex.GetType().Name;
                }
            }

            // Back to other methods
            var idicTest = dic2.GetEnumerator();
            hsh3 = new Hashtable();
            var hsh4 = new Hashtable();
            while (idicTest.MoveNext())
            {
                if (!dic2.Contains(idicTest.Key))
                {
                    hsh1["Enumerator"] = "Key";
                }
                if (dic2[idicTest.Key] != idicTest.Value)
                {
                    hsh1["Enumerator"] = "Key";
                }
                try
                {
                    hsh3.Add(idicTest.Key, null);
                }
                catch (Exception ex)
                {
                    hsh1["Enumerator"] = ex.GetType().Name;
                }
                try
                {
                    hsh4.Add(idicTest.Value, null);
                }
                catch (Exception ex)
                {
                    hsh1["Enumerator"] = ex.GetType().Name;
                }
            }

            //we will serialize the Synchronized SortedList to make sure that the serialization methods there work corerctly

            var dic3 = SortedList.Synchronized((SortedList)dic2.Clone());

            if (dic3.Count != dic1.Count)
            {
                hsh1["Serialized"] = "";
            }
            dicEnt1 = new DictionaryEntry[dic3.Count];
            dic3.CopyTo(dicEnt1, 0);
            hsh3 = new Hashtable();
            for (int i = 0; i < iNumberOfElements; i++)
            {
                if (!dic3.Contains("Key_" + i))
                {
                    hsh1["SErialized"] = "";
                }
                if (!((string)dic3["Key_" + i]).Equals(("Value_" + i)))
                {
                    hsh1["SErialized"] = "";
                }
                try
                {
                    hsh3.Add(((DictionaryEntry)dicEnt1[i]).Key, null);
                }
                catch (Exception ex)
                {
                    hsh1["SErialized"] = ex.GetType().Name;
                }
            }
            if (dic3.IsReadOnly)
            {
                hsh1["SErialized"] = "";
            }
            Assert.Equal(hsh1.Count, 0);
            if (!dic3.IsSynchronized)
            {
                hsh1["SErialized"] = "";
            }

            //Properties
            if (dic2.IsReadOnly)
            {
                hsh1["IsReadOnly"] = "";
            }
            if (!dic2.IsSynchronized)
            {
                hsh1["IsSynchronized"] = "";
            }
            if (dic2.SyncRoot != dic1.SyncRoot)
            {
                hsh1["SyncRoot"] = "";
            }
            Assert.Equal(hsh1.Count, 0);
            //we will test the Keys & Values
            strValueArr = new string[dic1.Count];
            strKeyArr = new string[dic1.Count];
            dic2.Keys.CopyTo(strKeyArr, 0);
            dic2.Values.CopyTo(strValueArr, 0);
            hsh3 = new Hashtable();
            hsh4 = new Hashtable();
            for (int i = 0; i < iNumberOfElements; i++)
            {
                if (!dic2.Contains(strKeyArr[i]))
                {
                    hsh1["Synchronized"] = "Keys";
                }
            }

            //now we test the modifying methods
            dic2.Remove("Key_1");
            if (dic2.Contains("Key_1"))
            {
                hsh1["Synchronized"] = "Remove";
            }
            dic2.Add("Key_1", "Value_1");
            if (!dic2.Contains("Key_1"))
            {
                hsh1["Synchronized"] = "Contains";
            }
            if ((string)dic2["Key_1"] != ("Value_1"))
            {
                hsh1["Synchronized"] = "Contains";
            }
            dic2["Key_1"] = "Value_Modified_1";
            if (!dic2.Contains("Key_1"))
            {
                hsh1["Synchronized"] = "Contains";
            }
            if ((string)dic2["Key_1"] != ("Value_Modified_1"))
            {
                hsh1["Synchronized"] = "Contains";
            }
            dic3 = SortedList.Synchronized(dic2);

            if (dic3.Count != dic1.Count)
            {
                hsh1["Synchronized"] = "Synchronized";
            }
            if (!dic3.IsSynchronized)
            {
                hsh1["Synchronized"] = "Synchronized";
            }
            dic2.Clear();
            if (dic2.Count != 0)
            {
                hsh1["Synchronized"] = "Synchronized";
            }
  
            //Set/GetByIndex()
            dic1 = new SortedList();
            icol1 = dic1.Keys;

            for (int i = 0; i < 100; i++)
                dic1.Add("Key_" + i, "Value_" + i);

            dic2 = SortedList.Synchronized(dic1);
            for (int i = 0; i < 100; i++)
                dic2.SetByIndex(i, "Changed_By_SetByIndex_Value_" + (i + 500));

            for (int i = 0; i < 100; i++)
            {
                if (!((string)dic2.GetByIndex(i)).Equals("Changed_By_SetByIndex_Value_" + (i + 500)))
                    hsh1["Syncronized"] = "Get/SetByIndex";
                if (!dic2.ContainsKey("Key_" + i))
                    hsh1["Syncronized"] = "ContainsKEy";
                if (dic2.ContainsKey("Key_" + (i + 5000)))
                    hsh1["Syncronized"] = "ContainsKEy";
                if (!dic2.ContainsValue("Changed_By_SetByIndex_Value_" + (i + 500)))
                    hsh1["Syncronized"] = "ContainsValue, True";
                if (dic2.ContainsValue("Changed_By_SetByIndex_Value_" + (i + 5000)))
                    hsh1["Syncronized"] = "ContainsValue, False";
            }

            //lets try outside the range for Set/GetByIndex
            try
            {
                dic2.SetByIndex(dic2.Count, "Who?");

                hsh1["SetByIndex"] = "";
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception ex)
            {
                hsh1["SetByIndex"] = ex.GetType().Name;
            }
            try
            {
                dic2.SetByIndex(-1, "Who?");

                hsh1["SetByIndex"] = "";
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception ex)
            {
                hsh1["SetByIndex"] = ex.GetType().Name;
            }
            try
            {
                dic2.GetByIndex(dic2.Count);

                hsh1["SetByIndex"] = "";
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception ex)
            {
                hsh1["SetByIndex"] = ex.GetType().Name;
            }
            try
            {
                dic2.GetByIndex(-1);

                hsh1["SetByIndex"] = "";
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception ex)
            {
                hsh1["SetByIndex"] = ex.GetType().Name;
            }

            while (dic2.Count == dic2.Capacity)
            {
                dic2.Add("Key_" + dic2.Count, "Value_" + dic2.Count);
            }

            dic2.TrimToSize();
            if (dic2.Count != dic2.Capacity)
            {
                hsh1["TrimToSize"] = dic2.Count + " " + dic2.Capacity + " " + dic1.Capacity;
            }

            dic2.Clear();
            for (int i = 0; i < 10; i++)
                dic1.Add("Key_" + i, "Value_" + i);

            for (int i = 0; i < 10; i++)
            {
                if (!((string)dic2.GetKey(i)).Equals("Key_" + i))
                    hsh1["Syncronized"] = "GetKey";
                if (dic2.IndexOfKey("Key_" + i) != i)
                    hsh1["Syncronized"] = "IndexOfKey, " + dic2.IndexOfKey("Key_" + i);
                if (dic2.IndexOfKey("Key_" + i + 500) != -1)
                    hsh1["Syncronized"] = "IndexOfKey, " + dic2.IndexOfKey("Key_" + i + 500);
                if (dic2.IndexOfValue("Value_" + i) != i)
                    hsh1["Syncronized"] = "IndexOfValue";
                if (dic2.IndexOfValue("Value_" + i + 500) != -1)
                    hsh1["Syncronized"] = "IndexOfValue";
            }

            for (int i = 9; i >= 0; i--)
            {
                dic2.RemoveAt(i);
                if (dic2.ContainsKey("Key_" + i))
                    hsh1["Syncronized"] = "ContainsKEy";
                if (dic2.ContainsValue("Value_" + i))
                    hsh1["Syncronized"] = "ContainsValue";
            }
            if (dic2.Count != 0)
                hsh1["Syncronized"] = "Count";

            try
            {
                dic2.GetKey(-1);

                hsh1["GetKey"] = "";
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception ex)
            {
                hsh1["GetKey"] = ex.GetType().Name;
            }
            try
            {
                dic2.GetKey(dic2.Count);

                hsh1["GetKey"] = "";
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception ex)
            {
                hsh1["GetKey"] = ex.GetType().Name;
            }

            //Synchronized - parameter
            try
            {
                dic2 = SortedList.Synchronized(null);

                hsh1["Synchronized"] = "";
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception ex)
            {
                hsh1["GetObjectData"] = ex;
            }

            Assert.False(hsh1.Count > 0, "Error, Synchrnoized");

            //Adding clone for the Synchronized method
            dic1 = new SortedList();
            for (int i = 0; i < 100; i++)
                dic1.Add("Key_" + i, "Value_" + i);

            dic2 = SortedList.Synchronized(dic1);
            dic3 = (SortedList)dic2.Clone();
            iNumberOfElements = dic3.Count;
            if (iNumberOfElements != dic1.Count)
                hsh1["Synchronized"] = "Clone";
            for (int i = 0; i < iNumberOfElements; i++)
            {
                if (!dic3.Contains("Key_" + i))
                {
                    hsh1["Synchronized"] = "Clone";
                }
                if (!((string)dic3["Key_" + i]).Equals("Value_" + i))
                {
                    hsh1["Synchronized"] = ", " + dic3["Key_" + i] + " " + ("Value_" + i) + " " + ((string)dic3["Key_" + i] != ("Value_" + i));
                }
            }
        }

        [Fact]
        public void TestMiscellaneous()
        {
            short i16 = 1;
            int i32 = 2;
            long i64 = 3;
            ushort ui16 = 4;
            uint ui32 = 5;
            ulong ui64 = 6;

            var dic1 = new SortedList();
            dic1.Add(i16, null);

            Assert.Throws<InvalidOperationException>(() => { dic1.Add(i32, null); });

            Assert.Throws<InvalidOperationException>(() => { dic1.Add(i64, null); });

            Assert.Throws<InvalidOperationException>(() => { dic1.Add(ui16, null); });

            Assert.Throws<InvalidOperationException>(() => { dic1.Add(ui32, null); });

            Assert.Throws<InvalidOperationException>(() => { dic1.Add(ui64, null); });
        }

        private void DoICollectionTests(SortedList good, ICollection bad, Hashtable hsh1, DicType dic)
        {
            if (good.Count != bad.Count)
                hsh1["Count"] = "";

            if (good.IsSynchronized != bad.IsSynchronized)
                hsh1["IsSynchronized"] = "";

            if (good.SyncRoot != bad.SyncRoot)
                hsh1["SyncRoot"] = "";

            //CopyTo
            string[] iArr1 = null;
            string[] iArr2 = null;
            DictionaryEntry[] dicEnt1;

            //CopyTo() copies the values!!!
            iArr1 = new string[good.Count];
            iArr2 = new string[good.Count];
            bad.CopyTo(iArr2, 0);

            if (dic == DicType.Value)
            {
                dicEnt1 = new DictionaryEntry[good.Count];
                good.CopyTo(dicEnt1, 0);
                for (int i = 0; i < good.Count; i++)
                    iArr1[i] = (string)((DictionaryEntry)dicEnt1[i]).Value;

                for (int i = 0; i < iArr1.Length; i++)
                {
                    if (!iArr1[i].Equals(iArr2[i]))
                        hsh1["CopyTo"] = "vanila";
                }
                iArr1 = new string[good.Count + 5];
                iArr2 = new string[good.Count + 5];
                for (int i = 0; i < good.Count; i++)
                    iArr1[i + 5] = (string)((DictionaryEntry)dicEnt1[i]).Value;
                bad.CopyTo(iArr2, 5);
                for (int i = 5; i < iArr1.Length; i++)
                {
                    if (!iArr1[i].Equals(iArr2[i]))
                    {
                        hsh1["CopyTo"] = "5";
                    }
                }
            }
            else if (dic == DicType.Key)
            {
                for (int i = 0; i < iArr1.Length; i++)
                {
                    if (!good.Contains(iArr2[i]))
                        hsh1["CopyTo"] = "Key";
                }
            }

            try
            {
                bad.CopyTo(iArr2, -1);
                hsh1["CopyTo"] = "";
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["CopyTo"] = ex;
            }

            try
            {
                bad.CopyTo(null, 5);
                hsh1["CopyTo"] = "";
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception ex)
            {
                hsh1["CopyTo"] = ex;
            }

            //Enumerator
            IEnumerator ienm1;
            IEnumerator ienm2;
            ienm1 = good.GetEnumerator();
            ienm2 = bad.GetEnumerator();
            DoTheEnumerator(ienm1, ienm2, hsh1, dic, good);
        }

        private void DoIListTests(SortedList good, IList bad, Hashtable hsh1, DicType dic)
        {

            if (bad.IsReadOnly != good.IsReadOnly)
                hsh1["IsReadOnly"] = "";

            if (bad.IsFixedSize != good.IsFixedSize)
                hsh1["IsFixedSize"] = "";

            try
            {
                if (dic == DicType.Key)
                {
                    DoICollectionTests(good, bad, hsh1, DicType.Key);
                    for (int i = 0; i < good.Count; i++)
                    {
                        if (!bad.Contains(good.GetKey(i)))
                            hsh1["Contains"] = i;
                        if (bad[i] != good.GetKey(i))
                            hsh1["Item"] = "get";
                        if (bad.IndexOf(good.GetKey(i)) != i)
                            hsh1["IndexOf"] = i;
                    }
                    //we change the original and see if it is reflected in the IList
                    good.Add("ThisHasToBeThere", null);
                    if (!bad.Contains("ThisHasToBeThere"))
                        hsh1["Contains"] = "Modified";
                    if (bad.Count != bad.Count)
                        hsh1["Count"] = "Modified";
                }
                else if (dic == DicType.Value)
                {
                    DoICollectionTests(good, bad, hsh1, DicType.Value);
                    for (int i = 0; i < good.Count; i++)
                    {
                        if (!bad.Contains(good.GetByIndex(i)))
                            hsh1["Contains"] = i;
                        if (bad[i] != good.GetByIndex(i))
                            hsh1["Item"] = "get";
                        if (bad.IndexOf(good.GetByIndex(i)) != i)
                            hsh1["IndexOf"] = i;
                    }
                    //we change the original and see if it is reflected in the IList
                    good.Add("ThisHasToBeThere", "ValueWasJustAdded");
                    if (!bad.Contains(good["ThisHasToBeThere"]))
                        hsh1["Contains"] = "Modified";
                    if (bad.Count != bad.Count)
                        hsh1["Count"] = "Modified";
                }

                try
                {
                    bad.Add("AnyValue");
                    hsh1["Add"] = "No_Exception thrown";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception ex)
                {
                    hsh1["Add"] = ex.GetType().Name;
                }

                if (bad.Count != bad.Count)
                    hsh1["Count"] = "ReadWrite";

                try
                {
                    bad.Clear();
                    hsh1["Clear"] = "No_Exception thrown";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception ex)
                {
                    hsh1["Clear"] = ex.GetType().Name;
                }

                try
                {
                    bad.Insert(0, "AnyValue");
                    hsh1["Insert"] = "No_Exception thrown";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception ex)
                {
                    hsh1["Insert"] = ex.GetType().Name;
                }

                try
                {
                    bad.Remove("AnyValue");
                    hsh1["Remove"] = "No_Exception thrown";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception ex)
                {
                    hsh1["Remove"] = ex.GetType().Name;
                }

                try
                {
                    bad.RemoveAt(0);
                    hsh1["RemoveAt"] = "No_Exception thrown";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception ex)
                {
                    hsh1["RemoveAt"] = ex.GetType().Name;
                }

            }
            catch (Exception ex)
            {
                hsh1["DoIListTests"] = ex;
            }
        }

        private void DoTheEnumerator(IEnumerator ienm1, IEnumerator ienm2, Hashtable hsh1, DicType dic, SortedList original)
        {
            while (ienm1.MoveNext())
            {
                ienm2.MoveNext();
                switch (dic)
                {
                    case DicType.Key:
                        if (((DictionaryEntry)ienm1.Current).Key != ienm2.Current)
                            hsh1["Enumrator"] = "";
                        break;
                    case DicType.Value:
                        if (((DictionaryEntry)ienm1.Current).Value != ienm2.Current)
                            hsh1["Enumrator"] = "";
                        break;
                    case DicType.Both:
                        if (((DictionaryEntry)ienm1.Current).Value != ((DictionaryEntry)ienm2.Current).Value)
                            hsh1["Enumrator"] = "";
                        break;
                }
            }

            ienm1.Reset();
            ienm2.Reset();
            while (ienm1.MoveNext())
            {
                ienm2.MoveNext();
                switch (dic)
                {
                    case DicType.Key:
                        if (((DictionaryEntry)ienm1.Current).Key != ienm2.Current)
                            hsh1["Enumrator"] = "";
                        break;
                    case DicType.Value:
                        if (((DictionaryEntry)ienm1.Current).Value != ienm2.Current)
                            hsh1["Enumrator"] = "";
                        break;
                }
            }

            //we need to modify the original collection and make sure that the enum throws
            ienm2.Reset();
            original.Add("Key_Blah", "Value_Blah");
            try
            {
                ienm2.MoveNext();
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }

            try
            {
                object blah = ienm2.Current;
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }

            try
            {
                ienm2.Reset();
                hsh1["Enumerator"] = "NoEXception";
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Enumerator"] = ex;
            }
        }
    }

    internal enum DicType
    {
        Key,
        Value,
        Both,
    }
}
