// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.HashtableTests
{
    public class AddTests
    {
        [Fact]
        public void TestAddBasic()
        {
            Hashtable ht = null;
            string[] keys = { "key_0", "key_1"};
            string[] values = { "val_0", "val_1" };

            var k1 = new StringBuilder(keys[0]);
            var k2 = new StringBuilder(keys[1]);
            var v1 = new StringBuilder(values[0]);
            var v2 = new StringBuilder(values[1]);

            ht = new Hashtable();
            ht.Add(k1, v1);
            ht.Add(k2, v2);

            Assert.True(ht.ContainsKey(k2));
            Assert.True(ht.ContainsValue(v2));

            ICollection allkeys = ht.Keys;
            Assert.Equal(allkeys.Count, ht.Count);

            IEnumerator allkeysEnum = allkeys.GetEnumerator();
            int index = 0;
            string[] ary = new string[2];
            while (allkeysEnum.MoveNext())
            {
                ary[index++] = allkeysEnum.Current.ToString();
            }

            // Not necessary the same order
            if (keys[0] == ary[0])
            {
                Assert.Equal(keys[1], ary[1]);
            }
            else
            {
                Assert.Equal(keys[0], ary[1]);
            }

            ICollection allvalues = ht.Values;
            Assert.Equal(allvalues.Count, ht.Count);

            IEnumerator allvaluesEnum = allvalues.GetEnumerator();
            index = 0;
            while (allvaluesEnum.MoveNext())
            {
                ary[index++] = (string)allvaluesEnum.Current.ToString();
            }

            if (values[0] == ary[0])
            {
                Assert.Equal(values[1], ary[1]);
            }
            else
            {
                Assert.Equal(values[0], ary[1]);
            }
        }

        [Fact]
        public void TestAddBasic01()
        { 
            Hashtable ht2 = null;
            int[] in4a = new int[9];

            string str5 = null;
            string str6 = null;
            string str7 = null;

            // Construct
            ht2 = new Hashtable();

            // Add the first obj
            str5 = "key_150"; str6 = "value_150";
            ht2.Add(str5, str6);
            in4a[0] = ht2.Count;
            Assert.Equal(in4a[0], 1);

            str7 = (string)ht2[str5];
            Assert.Equal(str7 ,str6);

            // Add another obj, verify the previously added pair still exists.
            str5 = "key_130"; str6 = "value_130"; //equiv. to <i>"value_130";</i>
            ht2.Add(str5, str6);
            in4a[2] = ht2.Count;  // size verification
            Assert.Equal(in4a[2], 2);

            // verify the Values added
            str7 = (string)ht2["key_150"];
            Assert.NotNull(str7);
            Assert.Equal("value_150", str7);

            str7 = (string)ht2[str5];
            Assert.NotNull(str7);
            Assert.Equal(str7, str6);

            // Cause expected exception by attempting to add duplicate keys.
            Assert.Throws<ArgumentException>(() => { ht2.Add(str5, str6 + "_b"); }); // Only the key is dupl.

            // Cause expected exception by attempting to add null key.
            str5 = null; str6 = "value_null";
            Assert.Throws<ArgumentNullException>(() => { ht2.Add(str5, str6); });
        }

        [Fact]
        public void TestAddWithReferenceTypeValues()
        {
            StringBuilder sbl3 = new StringBuilder(99);
            StringBuilder sbl4 = new StringBuilder(99);
            StringBuilder sbl5 = new StringBuilder(99);
            StringBuilder sblWork1 = new StringBuilder(99);

            // Examine whether this Collection stores a ref to the provided object or a copy of the object (should store ref).
            var ht2 = new Hashtable();

            sbl3.Length = 0;
            sbl3.Append("key_f3");

            sbl4.Length = 0;
            sbl4.Append("value_f3");

            ht2.Add(sbl3, sbl4);

            sbl4.Length = 0;
            sbl4.Append("value_f4");  // Modify object referenced by ht2, changing its value.

            sblWork1 = (StringBuilder)ht2[sbl3];
            Assert.True(sblWork1.ToString().Equals(sbl4.ToString()));

            // Examine "backdoor duplicate" behavior - should be ok as both
            // GetHashCode && Equals are checked before insert/get in the current impl.
            ht2 = new Hashtable();

            sbl3.Length = 0;
            sbl3.Append("key_m5");

            sbl4.Length = 0;
            sbl4.Append("value_m5");

            ht2.Add(sbl3, sbl4);

            sbl5 = new StringBuilder("key_p7"); //new memory SBL Obj

            sbl4.Length = 0;
            sbl4.Append("value_p7");

            ht2.Add(sbl5, sbl4);
            sbl5.Length = 0; //"No Object" key

            Assert.Equal(2, ht2.Count);
            sbl5.Append("key_m5"); // Backdoor duplicate!

            sblWork1 = (StringBuilder)ht2[sbl5];
            Assert.True(ht2.ContainsKey(sbl5));

            ht2.Clear();
          }

        [Fact]
        public void TestAddClearRepeatedly()
        {
            int inLoops0 = 2;
            int inLoops1 = 2;

            var ht2 = new Hashtable();
            for (int aa = 0; aa < inLoops0; aa++)
            {
                for (int bb = 0; bb < inLoops1; bb++)
                {
                    ht2.Add("KEY: aa==" + aa + " ,bb==" + bb, "VALUE: aa==" + aa + " ,bb==" + bb);
                }

                Assert.Equal(inLoops1, ht2.Count);
                ht2.Clear();
            }
        }

        const int iterations = 1600;

        [Fact]
        public void TestDuplicatedKeysWithInitialCapacity()
        {
            // Make rehash get called because to many items with duplicated keys have been added to the hashtable
            var ht = new Hashtable(200);

            for (int i = 0; i < iterations; i += 2)
            {
                ht.Add(new BadHashCode(i), i.ToString());
                ht.Add(new BadHashCode(i + 1), (i + 1).ToString());

                ht.Remove(new BadHashCode(i));
                ht.Remove(new BadHashCode(i + 1));
            }

            for (int i = 0; i < iterations; i++)
            {
                ht.Add(i.ToString(), i);
            }

            for (int i = 0; i < iterations; i++)
            {
                Assert.Equal(i, (int)ht[i.ToString()]);
            }
        }

        [Fact]
        public void TestDuplicatedKeysWithDefaultCapacity()
        {
            // Make rehash get called because to many items with duplicated keys have been added to the hashtable
            var ht = new Hashtable();

            for (int i = 0; i < iterations; i += 2)
            {
                ht.Add(new BadHashCode(i), i.ToString());
                ht.Add(new BadHashCode(i + 1), (i + 1).ToString());

                ht.Remove(new BadHashCode(i));
                ht.Remove(new BadHashCode(i + 1));
            }

            for (int i = 0; i < iterations; i++)
            {
                ht.Add(i.ToString(), i);
            }

            for (int i = 0; i < iterations; i++)
            {
                Assert.Equal(i, (int)ht[i.ToString()]);
            }
        }
    }

    public class BadHashCode
    {
        private uint _value;

        public BadHashCode(int value)
        {
            _value = (uint)value;
        }

        public override bool Equals(object o)
        {
            BadHashCode rhValue = o as BadHashCode;

            if (null != rhValue)
            {
                return _value.Equals(rhValue);
            }
            else
            {
                throw new ArgumentException("o", "is not BadHashCode type actual " + o.GetType());
            }
        }

        public override int GetHashCode()
        {
            // Return 0 for everything to force hash collisions.
            return 0;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}
