// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using System;
using System.Collections;
using System.Threading;
using System.Text;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class Registry_SetValue_str_str_obj : IDisposable
    {
        private string _testKey = "CM2001_TEST";
        private static int s_keyCount = 0;
        private string _keyString = "";

        // Variables needed
        private RegistryKey _rk1 = null, _rk2 = null;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKey += counter.ToString();
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKey) != null)
                _rk1.DeleteSubKeyTree(_testKey);
            if (_rk1.GetValue(_testKey) != null)
                _rk1.DeleteValue(_testKey);

            //created the test key. if that failed it will cause many of the test scenarios below fail.
            try
            {
                _rk1 = Microsoft.Win32.Registry.CurrentUser;
                if ((_rk2 = _rk1.CreateSubKey(_testKey)) == null)
                {
                    Assert.False(true, "ERROR: Could not create test subkey, this will fail a couple of scenarios below.");
                }
                else
                    _keyString = _rk2.ToString();
            }
            catch (Exception e)
            {
                Assert.False(true, "ERROR: unexpected exception, " + e.ToString());
            }
        }

        public Registry_SetValue_str_str_obj()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            // [] Passing in null should throw ArgumentNullException
            //UPDATE: This sets the default value. We should move this test to a newly defined reg key so as not to screw up the system
            try
            {
                String str1 = "This is a test";
                Registry.SetValue(_keyString, null, str1);
                if ((String)_rk2.GetValue(null) != str1)
                {
                    Assert.False(true, "Error_unexpected error");
                }
            }
            catch (ArgumentNullException)
            { }
            catch (Exception exc)
            {
                Assert.False(true, "Error Unexpected exception exc==" + exc.ToString());
            }
        }

        [Fact]
        public void Test02()
        {
            // [] Passing null object should throw
            Action a = () => { Registry.SetValue(_keyString, "test", null); };
            Assert.Throws<ArgumentNullException>(() => { a(); });
        }

        [Fact]
        public void Test03()
        {
            //passing null for registry key name should throw ArgumentNullException	
            Action a = () => { Registry.SetValue(null, "test", "test"); };
            Assert.Throws<ArgumentNullException>(() => { a(); });
        }

        [Fact]
        public void Test04()
        {
            //passing a string which does NOT start with one of the valid base key names, that should throw ArgumentException.
            Action a = () => { Registry.SetValue("HHHH_MMMM", "test", "test"); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test05()
        {
            // [] Key length above 255 characters should throw

            int MaxValueNameLength = 16383; // prior to V4, the limit is 255

            StringBuilder sb = new StringBuilder(MaxValueNameLength + 1);
            for (int i = 0; i <= MaxValueNameLength; i++)
                sb.Append('a');
            String strName = sb.ToString();

            Action a = () => { Registry.SetValue(_keyString, strName, 5); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test06()
        {
            Random rand = new Random(-55);
            // [] Vanilla case , set a  bunch different objects
            _rk2 = _rk1.CreateSubKey(_testKey);
            var objArr = new Object[16];
            objArr[0] = (Byte)rand.Next(Byte.MinValue, Byte.MaxValue);
            objArr[1] = (SByte)rand.Next(SByte.MinValue, SByte.MaxValue);
            objArr[2] = (Int16)rand.Next(Int16.MinValue, Int16.MaxValue);
            objArr[3] = (UInt16)rand.Next(UInt16.MinValue, UInt16.MaxValue);
            objArr[4] = rand.Next(Int32.MinValue, Int32.MaxValue);
            objArr[5] = (UInt32)rand.Next((int)UInt32.MinValue, Int32.MaxValue);
            objArr[6] = (Int64)rand.Next(Int32.MinValue, Int32.MaxValue);
            objArr[7] = ((Double)Decimal.MaxValue) * rand.NextDouble();
            objArr[8] = ((Double)Decimal.MinValue) * rand.NextDouble();
            objArr[9] = ((Double)Decimal.MinValue) * rand.NextDouble();
            objArr[10] = ((Double)Decimal.MaxValue) * rand.NextDouble();
            objArr[11] = (Double)Int32.MaxValue * rand.NextDouble();
            objArr[12] = (Double)Int32.MinValue * rand.NextDouble();
            objArr[13] = (float)Int32.MaxValue * (float)rand.NextDouble();
            objArr[14] = (float)Int32.MinValue * (float)rand.NextDouble();
            objArr[15] = (UInt64)rand.Next((int)UInt64.MinValue, Int32.MaxValue);
            for (int i = 0; i < objArr.Length; i++)
                Registry.SetValue(_keyString, "Test_" + i.ToString(), objArr[i]);
            for (int i = 0; i < objArr.Length; i++)
            {
                Object v = _rk2.GetValue("Test_" + i.ToString());
                if (!v.ToString().Equals(objArr[i].ToString()))
                {
                    Assert.False(true, "Error Expected==" + objArr[i].ToString() + " , got value==" + v.ToString());
                }
            }
            for (int i = 0; i < objArr.Length; i++)
                _rk2.DeleteValue("Test_" + i.ToString());
        }

        [Fact]
        public void Test07()
        {
            // [] Set an existing it to an Int32
            Registry.SetValue(_keyString, "Int32", -5);
            if (((Int32)_rk2.GetValue("Int32")) != -5)
            {
                Assert.False(true, "Error Expected==5, got value==" + _rk2.GetValue("Int32"));
            }
            _rk2.DeleteValue("Int32");
        }

        [Fact]
        public void Test08()
        {
            // [] Set some UInt values - this will be written as REG_SZ
            Object o = Int64.MaxValue;
            Registry.SetValue(_keyString, "UInt64", UInt64.MaxValue);
            if (Convert.ToUInt64(_rk2.GetValue("UInt64")) != UInt64.MaxValue)
            {
                Assert.False(true, "Error Expected==" + UInt64.MaxValue + " , got==" + _rk2.GetValue("UInt64"));
            }
            _rk2.DeleteValue("UInt64");
        }

        [Fact]
        public void Test09()
        {
            // [] Set a value to hold an Array - why is an exception throw here???
            //because we only allow String[] - REG_MULTI_SZ and byte[] REG_BINARY. Here is the official explanation
            //RegistryKey.SetValue does not support arrays of type UInt32[].  Only Byte[] and String[] are supported.
            int[] iArr = new int[3];
            iArr[0] = 1;
            iArr[1] = 2;
            iArr[2] = 3;
            Action a = () => { Registry.SetValue(_keyString, "IntArray", iArr); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test10()
        {
            // [] Set a value to hold a Ubyte array -  - REG_BINARY
            Byte[] ubArr = new Byte[3];
            ubArr[0] = 1;
            ubArr[1] = 2;
            ubArr[2] = 3;
            Registry.SetValue(_keyString, "UBArr", ubArr);
            Byte[] ubResult = (Byte[])_rk2.GetValue("UBArr");
            if (ubResult.Length != 3)
            {
                Assert.False(true, "error! Incorrect Length of returned array");
            }
            if (ubResult[0] != 1)
            {
                Assert.False(true, "Error Expected==<1> , value==<" + ubResult[0] + ">");
            }
            if (ubResult[1] != 2)
            {
                Assert.False(true, "Error Expected==2 , value==" + ubResult[1]);
            }
            if (ubResult[2] != 3)
            {
                Assert.False(true, "Error Expected==3 , value==" + ubResult[2]);
            }
            _rk2.DeleteValue("UBArr");
        }

        [Fact]
        public void RegistrySetValueMultiStringRoundTrips()
        {
            // [] Put in a string array - REG_MULTI_SZ
            String[] strArr = new String[3];
            strArr[0] = "This is a public";
            strArr[1] = "broadcast intend to test";
            strArr[2] = "lot of things. one of which";
            Registry.SetValue(_keyString, "StringArr", strArr);

            String[] strResult = (String[])_rk2.GetValue("StringArr");
            Assert.Equal(strArr, strResult);
            _rk2.DeleteValue("StringArr");
        }

        [Fact]
        public void Test12()
        {
            // [] Passing in array with uninitialized elements should throw
            string[] strArr = new String[1];
            Action a = () => { Registry.SetValue(_keyString, "StringArr", strArr); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test13()
        {
            IDictionary envs;
            IDictionaryEnumerator idic1;
            String[] keys;
            String[] values;
            Int32 iCount;

            envs = Environment.GetEnvironmentVariables();
            keys = new String[envs.Keys.Count];
            values = new String[envs.Values.Count];
            idic1 = envs.GetEnumerator();
            iCount = 0;
            while (idic1.MoveNext())
            {
                keys[iCount] = (String)idic1.Key;
                values[iCount] = (String)idic1.Value;
                iCount++;
            }

            for (int i = 0; i < keys.Length; i++)
                Registry.SetValue(_keyString, "ExpandedTest_" + i, "%" + keys[i] + "%");

            //we dont expand for the user, REG_SZ_EXPAND not supported
            for (int i = 0; i < keys.Length; i++)
            {
                String result = (String)_rk2.GetValue("ExpandedTest_" + i);
                if (Environment.ExpandEnvironmentVariables(result) != values[i])
                {
                    Assert.False(true, string.Format("Error Wrong value returned, Expected - <{0}>, Returned - <{1}>", values[i], _rk2.GetValue("ExpandedTest_" + i)));
                }
            }
        }

        [Fact]
        public void Test14()
        {
            // passing emtpy string: Creating REG_SZ key with an empty string value does not add a null terminating char.
            try
            {
                Registry.SetValue(_keyString, "test_122018", string.Empty);
                if ((string)_rk2.GetValue("test_122018") != string.Empty)
                {
                    Assert.False(true, string.Format("Error Wrong value returned, Expected - <{0}>, Returned - <{1}>", string.Empty, _rk2.GetValue("test_122018")));
                }
            }
            catch (Exception exc)
            {
                Assert.False(true, string.Format("Error got exc==" + exc.ToString()));
            }
        }

        public void Dispose()
        {
            // Clean up
            _rk1 = Microsoft.Win32.Registry.CurrentUser;

            if (_rk1.OpenSubKey(_testKey) != null)
                _rk1.DeleteSubKeyTree(_testKey);
            if (_rk1.GetValue(_testKey) != null)
                _rk1.DeleteValue(_testKey);
        }
    }
}