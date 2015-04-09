// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValue_str : IDisposable
    {
        // Variables needed
        private RegistryKey _rk1, _rk2;
        private String _testKeyName = "REG_TEST_9";
        private String _testStringName = "TestString";
        private String _getValueBug = @"Software\GetValueBug";
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
            _rk2 = _rk1.CreateSubKey(_testKeyName);
        }

        public RegistryKey_GetValue_str()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            Assert.True(_rk2.SetDefaultValue());
            Assert.Equal(Helpers._DefaultValue, _rk2.GetValue(null));
            Assert.Equal(Helpers._DefaultValue, _rk2.GetValue(String.Empty));
        }

        [Fact]
        public void RegistryKeyGetValueMultiStringDoesNotDiscardZeroLengthStrings()
        {
            string[] before = new string[] { "", "Hello", "", "World", "" };
            string[] after;
            RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_getValueBug);
            key.SetValue("Test", before);
            after = key.GetValue("Test") as string[];
            key.Dispose();
            Assert.Equal(before, after);
        }

        [Fact]
        public void Test03()
        {
            // [] Vanilla case , add a  bunch different objects, then get them

            Random rand = new Random(-55);

            var objArr = new Object[16];
            objArr[0] = (Byte)rand.Next(Byte.MinValue, Byte.MaxValue);
            objArr[1] = (SByte)rand.Next(SByte.MinValue, SByte.MaxValue);
            objArr[2] = (Int16)rand.Next(Int16.MinValue, Int16.MaxValue);
            objArr[3] = (UInt16)rand.Next(UInt16.MinValue, UInt16.MaxValue);
            objArr[4] = rand.Next(Int32.MinValue, Int32.MaxValue);
            objArr[5] = (UInt32)rand.Next((int)UInt32.MinValue, Int32.MaxValue);
            objArr[6] = (Int64)rand.Next(Int32.MinValue, Int32.MaxValue);
            objArr[7] = new Decimal(((Double)Decimal.MaxValue) * rand.NextDouble());
            objArr[8] = new Decimal(((Double)Decimal.MinValue) * rand.NextDouble());
            objArr[9] = new Decimal(((Double)Decimal.MinValue) * rand.NextDouble());
            objArr[10] = new Decimal(((Double)Decimal.MaxValue) * rand.NextDouble());
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
        public void Test04()
        {
            // [] Getting back a string

            String str = "Here is a little test string";
            _rk2.SetValue(_testStringName, str);
            if (!_rk2.GetValue(_testStringName).Equals(str))
            {
                Assert.False(true, "Error Expected==" + str + " , value==" + _rk2.GetValue(_testStringName));
            }
        }

        [Fact]
        public void Test05()
        {
            // [] Getting binary

            Byte[] ubArr = new Byte[3];
            ubArr[0] = 1;
            ubArr[1] = 2;
            ubArr[2] = 3;

            // [] Getting byte array
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
            // [] Getting a string array

            String[] strArr = new String[3];
            strArr[0] = "Her kommer nissen";
            strArr[1] = "med sine korte";
            strArr[2] = "sokker og smilehull";
            _rk2.SetValue("StringArr", strArr);

            String[] strResult = (String[])_rk2.GetValue("StringArr");
            Assert.Equal(strResult, strArr);
        }

        public void Dispose()
        {
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
            if (_rk1.OpenSubKey(_getValueBug) != null)
                _rk1.DeleteSubKeyTree(_getValueBug);
            if (_rk1.GetValue(_testStringName) != null)
                _rk1.DeleteValue(_testStringName);
        }
    }
}