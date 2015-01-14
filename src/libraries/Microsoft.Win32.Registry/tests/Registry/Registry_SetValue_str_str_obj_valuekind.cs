// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Text;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class Registry_SetValue_str_str_obj_valueKind : IDisposable
    {
        private string _testKey = "CM3001_TEST";
        private string _keyString = "";
        private RegistryKey _rk1, _rk2;
        private string _subName = "";
        private static int s_keyCount = 0;

        // Variables needed
        private String _strExpected = "";
        private Random _rand = new Random(-55);
        private RegistryValueKind[] _ExpectedKinds = new RegistryValueKind[38];
        private Object _obj = new Object();
        private Object[] _objArr2;
        private Byte[] _byteArr;
        private Object[] _objArr = new Object[38];

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKey += counter.ToString();
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKey) != null)
                _rk1.DeleteSubKeyTree(_testKey);
            if (_rk1.GetValue(_testKey) != null)
                _rk1.DeleteValue(_testKey);
            _rk2 = _rk1.CreateSubKey(_testKey);
            _keyString = _rk2.ToString();
        }

        public void SetObjArr()
        {
            _byteArr = new Byte[_rand.Next(0, 100)];
            _objArr2 = new Object[2] { _obj, _obj };
            _rand.NextBytes(_byteArr);
            // Standard Random Numbers
            _objArr[0] = (Byte)(_rand.Next(Byte.MinValue, Byte.MaxValue));
            _objArr[1] = (SByte)(_rand.Next(SByte.MinValue, SByte.MaxValue));
            _objArr[2] = (Int16)(_rand.Next(Int16.MinValue, Int16.MaxValue));
            _objArr[3] = (UInt16)(_rand.Next(UInt16.MinValue, UInt16.MaxValue));
            _objArr[4] = (Char)(_rand.Next(UInt16.MinValue, UInt16.MaxValue));
            _objArr[5] = (Int32)(_rand.Next(Int32.MinValue, Int32.MaxValue));
            // Random Numbers that can fit into Int32
            _objArr[6] = (UInt32)(_rand.NextDouble() * Int32.MaxValue);
            _objArr[7] = (Int64)(_rand.NextDouble() * Int32.MaxValue);
            _objArr[8] = (Int64)(_rand.NextDouble() * Int32.MinValue);
            _objArr[9] = (UInt64)(_rand.NextDouble() * Int32.MaxValue);
            _objArr[10] = (Decimal)(_rand.NextDouble() * Int32.MaxValue);
            _objArr[11] = (Decimal)(_rand.NextDouble() * Int32.MinValue);
            _objArr[12] = (Single)(_rand.NextDouble() * Int32.MaxValue);
            _objArr[13] = (Single)(_rand.NextDouble() * Int32.MinValue);
            _objArr[14] = (Double)(_rand.NextDouble() * Int32.MaxValue);
            _objArr[15] = (Double)(_rand.NextDouble() * Int32.MinValue);
            // Random Numbers that can't fit into Int32 but can fit into Int64			
            _objArr[16] = (UInt32)(_rand.NextDouble() * (UInt32.MaxValue - (UInt32)Int32.MaxValue) + (UInt32)Int32.MaxValue);
            _objArr[17] = (Int64)(_rand.NextDouble() * (Int64.MaxValue - (Int64)Int32.MaxValue) + (Int64)Int32.MaxValue);
            _objArr[18] = (Int64)(_rand.NextDouble() * (Int64.MinValue - (Int64)Int32.MinValue) + (Int64)Int32.MinValue);
            _objArr[19] = (UInt64)(_rand.NextDouble() * ((UInt64)Int64.MaxValue - (UInt64)Int32.MaxValue) + (UInt64)Int32.MaxValue);
            _objArr[20] = (Decimal)(_rand.NextDouble() * (Int64.MaxValue - (Int64)Int32.MaxValue) + (Int64)Int32.MaxValue);
            _objArr[21] = (Decimal)(_rand.NextDouble() * (Int64.MinValue - (Int64)Int32.MinValue) + (Int64)Int32.MinValue);
            _objArr[22] = (Single)(_rand.NextDouble() * (Int64.MaxValue - Int32.MaxValue) + Int32.MaxValue);
            _objArr[23] = (Single)(_rand.NextDouble() * (Int64.MinValue - Int32.MinValue) + Int32.MinValue);
            _objArr[24] = (Double)(_rand.NextDouble() * (Int64.MaxValue - Int32.MaxValue) + Int32.MaxValue);
            _objArr[25] = (Double)(_rand.NextDouble() * (Int64.MinValue - Int32.MinValue) + Int32.MinValue);
            // Random Numbers that can't fit into Int32 or Int64
            _objArr[26] = (UInt64)(_rand.NextDouble() * (UInt64.MaxValue - (UInt64)Int64.MaxValue) + (UInt64)Int64.MaxValue);
            _objArr[27] = Decimal.MaxValue;
            _objArr[28] = Decimal.MinValue;
            _objArr[29] = Single.MaxValue;
            _objArr[30] = Single.MinValue;
            _objArr[31] = Double.MaxValue;
            _objArr[32] = Double.MinValue;
            // Various other types
            _objArr[33] = (String)"Hello World";
            _objArr[34] = (String)"Hello %path5% World";
            _objArr[35] = new String[] { "Hello World", "Hello %path% World" };
            _objArr[36] = (Object)_obj;
            _objArr[37] = (Byte[])(_byteArr);
        }

        public Registry_SetValue_str_str_obj_valueKind()
        {
            TestInitialize();
            SetObjArr();
        }

        [Fact]
        public void Test01()
        {
            // [] Test RegistryValueKind.Unknown
            try
            {
                for (int i = 0; i < _ExpectedKinds.Length; _ExpectedKinds[i++] = RegistryValueKind.String) ;
                //special cases.
                _ExpectedKinds[5] = RegistryValueKind.DWord;
                _ExpectedKinds[35] = RegistryValueKind.MultiString;
                _ExpectedKinds[37] = RegistryValueKind.Binary;
                for (int i = 0; i < _objArr.Length; i++)
                {
                    _subName = "Testing " + i;
                    Registry.SetValue(_keyString, _subName, _objArr[i], RegistryValueKind.Unknown);
                    if (_rk2.GetValue(_subName).ToString() != _objArr[i].ToString() || _rk2.GetValueKind(_subName) != _ExpectedKinds[i])
                    {
                        Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + _ExpectedKinds[i].ToString() + ", got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test02()
        {
            // [] Test RegistryValueKind.String
            try
            {
                for (int i = 0; i < _objArr.Length; i++)
                {
                    _subName = "Testing " + i;
                    Registry.SetValue(_keyString, _subName, _objArr[i], RegistryValueKind.String);
                    if (_rk2.GetValue(_subName).ToString() != _objArr[i].ToString() || _rk2.GetValueKind(_subName) != RegistryValueKind.String)
                    {
                        Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.String + ", got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test03()
        {
            // [] Test RegistryValueKind.ExpandString
            try
            {
                for (int i = 0; i < _objArr.Length; i++)
                {
                    _subName = "Testing " + i;
                    Registry.SetValue(_keyString, _subName, _objArr[i], RegistryValueKind.ExpandString);
                    if (_rk2.GetValue(_subName).ToString() != _objArr[i].ToString() || _rk2.GetValueKind(_subName) != RegistryValueKind.ExpandString)
                    {
                        Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.ExpandString + ", got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test04()
        {
            // [] Test RegistryValueKind.MultiString
            try
            {
                for (int i = 0; i < _objArr.Length; i++)
                {
                    try
                    {
                        _subName = "Testing " + i;
                        Registry.SetValue(_keyString, _subName, _objArr[i], RegistryValueKind.MultiString);
                        if (_rk2.GetValue(_subName).ToString() != _objArr[i].ToString() || _rk2.GetValueKind(_subName) != RegistryValueKind.MultiString)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.MultiString + ", got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                        }
                    }
                    catch (ArgumentException ioe)
                    {
                        if (_objArr[i].GetType() == (new string[0]).GetType())
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.MultiString + ", got exception==" + ioe.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test05()
        {
            // [] Test RegistryValueKind.Binary
            try
            {
                for (int i = 0; i < _objArr.Length; i++)
                {
                    try
                    {
                        _subName = "Testing " + i;
                        Registry.SetValue(_keyString, _subName, _objArr[i], RegistryValueKind.Binary);
                        if (_rk2.GetValue(_subName).ToString() != _objArr[i].ToString() || _rk2.GetValueKind(_subName) != RegistryValueKind.Binary)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.Binary + ", got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                        }
                    }
                    catch (ArgumentException ioe)
                    {
                        if (_objArr[i].GetType() == (new byte[0]).GetType())
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.Binary + ", got exception==" + ioe.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test06()
        {
            // [] Test RegistryValueKind.DWord
            try
            {
                for (int i = 0; i < _objArr.Length; i++)
                {
                    try
                    {
                        if (i <= 15)
                        {
                            _strExpected = (Convert.ToInt32(_objArr[i])).ToString();
                        }
                        _subName = "Testing " + i;
                        Registry.SetValue(_keyString, _subName, _objArr[i], RegistryValueKind.DWord);
                        if (_rk2.GetValue(_subName).ToString() != _strExpected || _rk2.GetValueKind(_subName) != RegistryValueKind.DWord)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _strExpected + " kind==" + RegistryValueKind.DWord + ", got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                        }
                        if (i > 15)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==Exception, got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                        }
                    }
                    catch (ArgumentException ioe)
                    {
                        if (i <= 15)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _strExpected + " kind==" + RegistryValueKind.DWord + ", got Exception==" + ioe.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        Assert.False(true, "Err i==" + i + " Unexpected exception :: " + e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test07()
        {
            // [] Test RegistryValueKind.QWord
            try
            {
                for (int i = 0; i < _objArr.Length; i++)
                {
                    try
                    {
                        if (i <= 25)
                        {
                            _strExpected = (Convert.ToInt64(_objArr[i])).ToString();
                        }
                        _subName = "Testing " + i;
                        Registry.SetValue(_keyString, _subName, _objArr[i], RegistryValueKind.QWord);
                        if (_rk2.GetValue(_subName).ToString() != _strExpected || _rk2.GetValueKind(_subName) != RegistryValueKind.QWord)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _strExpected + " kind==" + RegistryValueKind.QWord + ", got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                        }
                        if (i > 25)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==Exception, got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                        }
                    }
                    catch (ArgumentException ioe)
                    {
                        if (i <= 25)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _strExpected + " kind==" + RegistryValueKind.QWord + ", got Exception==" + ioe.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        Assert.False(true, "Err i==" + i + " Unexpected exception :: " + e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test08()
        {
            // [] Registry Key does not exist
            try
            {
                // Create the key
                _subName = "FooBar";
                Registry.SetValue(_keyString, _subName, _objArr[5], RegistryValueKind.DWord);
                if (_rk2.GetValue(_subName).ToString() != _objArr[5].ToString() || _rk2.GetValueKind(_subName) != RegistryValueKind.DWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[5].GetType() + " Expected==" + _objArr[5].ToString() + " kind==" + RegistryValueKind.DWord + ", got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test09()
        {
            // [] Registry Key already exists
            try
            {
                // Create the key
                _subName = "FooBar";
                Registry.SetValue(_keyString, _subName, _objArr[7], RegistryValueKind.QWord);

                if (_rk2.GetValue(_subName).ToString() != _objArr[7].ToString() || _rk2.GetValueKind(_subName) != RegistryValueKind.QWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[7].GetType() + " Expected==" + _objArr[7].ToString() + " kind==" + RegistryValueKind.QWord + ", got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test10()
        {
            // [] Name is null
            try
            {
                // Create the key
                _subName = null;
                Registry.SetValue(_keyString, _subName, _objArr[5], RegistryValueKind.DWord);
                if (_rk2.GetValue(_subName).ToString() != _objArr[5].ToString() || _rk2.GetValueKind(_subName) != RegistryValueKind.DWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[5].GetType() + " Expected==" + _objArr[5].ToString() + " kind==" + RegistryValueKind.DWord + ", got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test11()
        {
            // [] Name is ""
            try
            {
                // Create the key
                _subName = "";
                Registry.SetValue(_keyString, _subName, _objArr[7], RegistryValueKind.QWord);

                if (_rk2.GetValue(_subName).ToString() != _objArr[7].ToString() || _rk2.GetValueKind(_subName) != RegistryValueKind.QWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[7].GetType() + " Expected==" + _objArr[7].ToString() + " kind==" + RegistryValueKind.QWord + ", got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test12()
        {
            // [] Name is 255 characters
            try
            {
                // Create the key	  
                _subName = "12345678901111111110222222222033333333304444444440555555555066666666607777777770888888888099999999901234567890111111111022222222203333333330444444444055555555506666666660777777777088888888809999999990123456789011111111102222222220333333333044444444405555";
                Registry.SetValue(_keyString, _subName, _objArr[7], RegistryValueKind.QWord);

                if (_rk2.GetValue(_subName).ToString() != _objArr[7].ToString() || _rk2.GetValueKind(_subName) != RegistryValueKind.QWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[7].GetType() + " Expected==" + _objArr[7].ToString() + " kind==" + RegistryValueKind.QWord + ", got value==" + _rk2.GetValue(_subName).ToString() + " kind==" + _rk2.GetValueKind(_subName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test13()
        {
            // [] Name is longer then 255 characters
            // Create the key
            int MaxValueNameLength = 16383; // prior to V4, the limit is 255

            StringBuilder sb = new StringBuilder(MaxValueNameLength + 1);
            for (int i = 0; i <= MaxValueNameLength; i++)
                sb.Append('a');
            _subName = sb.ToString();
            Action a = () => { Registry.SetValue(_keyString, _subName, _objArr[7], RegistryValueKind.QWord); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test14()
        {
            // [] Value is null
            // Create the key
            _subName = "FooBar";
            Action a = () => { Registry.SetValue(_keyString, _subName, null, RegistryValueKind.QWord); };
            Assert.Throws<ArgumentNullException>(() => { a(); });
        }

        [Fact]
        public void Test15()
        {
            // [] ValueKind is equal to -2 which is not an acceptable value

            // Create the key
            _subName = "FooBar";
            Action a = () => { Registry.SetValue(_keyString, _subName, _objArr[5], (RegistryValueKind)(-2)); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test16()
        {
            // [] value is a string[] with null values

            // Create the key
            _subName = "FooBar";
            string[] strArr = new String[] { "one", "two", null, "three" };
            Action a = () => { Registry.SetValue(_keyString, _subName, strArr, RegistryValueKind.MultiString); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test17()
        {
            // [] value is a object[]

            // Create the key
            _subName = "FooBar";
            Action a = () => { Registry.SetValue(_keyString, _subName, _objArr2, RegistryValueKind.MultiString); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test18()
        {
            // [] Set RegistryKey to bad array type.  // To improve code coverage

            object[] objTemp = new object[] { "my string", "your string", "Any once string" };

            Action a = () => { Registry.SetValue(_keyString, _subName, objTemp, RegistryValueKind.Unknown); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        public void Dispose()
        {
            var _rk = Microsoft.Win32.Registry.CurrentUser;
            if (_rk.OpenSubKey(_testKey) != null)
                _rk.DeleteSubKeyTree(_testKey);
            if (_rk.GetValue(_testKey) != null)
                _rk.DeleteValue(_testKey);
        }
    }
}