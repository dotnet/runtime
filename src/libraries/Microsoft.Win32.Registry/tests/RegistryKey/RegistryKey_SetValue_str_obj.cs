// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Win32;
using System.Reflection;
using System.Collections;
using System.Text;
using Xunit;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_SetValue_str_obj : IDisposable
    {
        // Variables needed
        private RegistryKey _rk1, _rk2;
        private String _strName;
        private String _testKeyName = "REG_TEST_13";
        private static int s_keyCount = 0;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKeyName += counter.ToString();
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
            if (_rk1.GetValue(_testKeyName) != null)
                _rk1.DeleteValue(_testKeyName);
        }

        public RegistryKey_SetValue_str_obj()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            // [] Passing in null should throw ArgumentNullException
            //UPDATE: This sets the default value. We should move this test to a newly defined reg key so as not to screw up the system
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            _rk2 = _rk1.CreateSubKey(_testKeyName);

            try
            {
                var str1 = "This is a test";
                _rk2.SetValue(null, str1);
                if ((String)_rk2.GetValue(null) != str1)
                {
                    Assert.False(true, "Error_unexpected error");
                }
            }
            catch (Exception exc)
            {
                Assert.False(true, "Error ArgumentNullException expected , got exc==" + exc.ToString());
            }
        }

        [Fact]
        public void Test02()
        {
            // [] Passing null object should throw
            _rk2 = _rk1.CreateSubKey(_testKeyName);
            Action a = () => { _rk2.SetValue("test", null); };
            Assert.Throws<ArgumentNullException>(() => { a(); });
        }

        [Fact]
        public void Test03()
        {
            // [] Name length above 16383 characters should throw

            int MaxValueNameLength = 16383; // prior to V4, the limit is 255
            _rk2 = _rk1.CreateSubKey(_testKeyName);
            StringBuilder sb = new StringBuilder(MaxValueNameLength + 1);
            for (int i = 0; i <= MaxValueNameLength; i++)
                sb.Append('a');
            _strName = sb.ToString();

            Action a = () => { _rk2.SetValue(_strName, 5); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test04()
        {
            // [] Vanilla case , set a  bunch different objects

            _rk2 = _rk1.CreateSubKey(_testKeyName);

            Random rand = new Random(-55);

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
                _rk2.SetValue("Test_" + i.ToString(), objArr[i]);

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
        public void Test05()
        {
            // [] Set an existing it to an Int32
            _rk2 = _rk1.CreateSubKey(_testKeyName);

            _rk2.SetValue("Int32", -5);
            if (((Int32)_rk2.GetValue("Int32")) != -5)
            {
                Assert.False(true, "Error Expected==5, got value==" + _rk2.GetValue("Int32"));
            }
            _rk2.DeleteValue("Int32");
        }

        [Fact]
        public void Test06()
        {
            // [] Set some UInt values - this will be written as REG_SZ

            _rk2 = _rk1.CreateSubKey(_testKeyName);
            Object o = Int64.MaxValue;
            _rk2.SetValue("UInt64", UInt64.MaxValue);
            if (Convert.ToUInt64(_rk2.GetValue("UInt64")) != UInt64.MaxValue)
            {
                Assert.False(true, "Error Expected==" + UInt64.MaxValue + " , got==" + _rk2.GetValue("UInt64"));
            }
            _rk2.DeleteValue("UInt64");
        }

        [Fact]
        public void Test07()
        {
            // [] Set a value to hold an Array - why is an exception throw here???
            //because we only allow String[] - REG_MULTI_SZ and byte[] REG_BINARY. Here is the official explanation
            //RegistryKey.SetValue does not support arrays of type UInt32[].  Only Byte[] and String[] are supported.

            _rk2 = _rk1.CreateSubKey(_testKeyName);
            int[] iArr = new int[3];
            iArr[0] = 1;
            iArr[1] = 2;
            iArr[2] = 3;

            Action a = () => { _rk2.SetValue("IntArray", iArr); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test08()
        {
            // [] Set a value to hold a Ubyte array -  - REG_BINARY

            _rk2 = _rk1.CreateSubKey(_testKeyName);
            Byte[] ubArr = new Byte[3];
            ubArr[0] = 1;
            ubArr[1] = 2;
            ubArr[2] = 3;

            _rk2.SetValue("UBArr", ubArr);
            Byte[] ubResult = (Byte[])_rk2.GetValue("UBArr");
            if (ubResult.Length != 3)
            {
                Assert.False(true, "error_4908f! Incorrect Length of returned array");
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
        public void RegistryKeySetValueMultiStringRoundTrips()
        {
            // [] Put in a string array - REG_MULTI_SZ

            _rk2 = _rk1.CreateSubKey(_testKeyName);
            String[] strArr = new String[3];
            strArr[0] = "This is a public";
            strArr[1] = "broadcast intend to test";
            strArr[2] = "lot of things. one of which";
            _rk2.SetValue("StringArr", strArr);

            String[] strResult = (String[])_rk2.GetValue("StringArr");
            Assert.Equal(strArr, strResult);
            _rk2.DeleteValue("StringArr");
        }

        [Fact]
        public void Test10()
        {
            // [] Passing in array with uninitialized elements should throw

            _rk2 = _rk1.CreateSubKey(_testKeyName);
            var strArr = new String[1];
            Action a = () => { _rk2.SetValue("StringArr", strArr); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test11()
        {
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            _rk2 = _rk1.CreateSubKey(_testKeyName);

            var envs = Environment.GetEnvironmentVariables();
            var keys = new String[envs.Keys.Count];
            var values = new String[envs.Values.Count];
            var idic1 = envs.GetEnumerator();
            var iCount = 0;
            while (idic1.MoveNext())
            {
                keys[iCount] = (String)idic1.Key;
                values[iCount] = (String)idic1.Value;
                iCount++;
            }

            for (int i = 0; i < keys.Length; i++)
                _rk2.SetValue("ExpandedTest_" + i, "%" + keys[i] + "%");

            //we don't expand for the user, REG_SZ_EXPAND not supported
            for (int i = 0; i < keys.Length; i++)
            {
                var result = (String)_rk2.GetValue("ExpandedTest_" + i);
                if (Environment.ExpandEnvironmentVariables(result) != values[i])
                {
                    Assert.False(true, string.Format("Error Wrong value returned, Expected - <{0}>, Returned - <{1}>", values[i], _rk2.GetValue("ExpandedTest_" + i)));
                }
            }
        }

        [Fact]
        public void Test12()
        {
            _rk2 = _rk1.CreateSubKey(_testKeyName);
            // passing empty string.
            try
            {
                _rk2.SetValue("test_122018", string.Empty);
                if ((string)_rk2.GetValue("test_122018") != string.Empty)
                {
                    Assert.False(true, string.Format("Error Wrong value returned, Expected - <{0}>, Returned - <{1}>", string.Empty, _rk2.GetValue("test_122018")));
                }
            }
            catch (Exception exc)
            {
                Assert.False(true, "Error ArgumentNullException Expected , got exc==" + exc.ToString());
            }
        }


        public void Dispose()
        {
            // Clean up
            _rk1 = Microsoft.Win32.Registry.CurrentUser;

            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
            if (_rk1.GetValue(_testKeyName) != null)
                _rk1.DeleteValue(_testKeyName);
        }
    }
}