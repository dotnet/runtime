// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Text;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_SetValueKind_str_obj_valueKind : IDisposable
    {
        // Variables needed
        private RegistryKey _rk1, _rk2;
        private  String _testKeyName = "Test_Key";
        private  String _valueName = "";
        private  String _strExpected = "";
        private  RegistryValueKind[] _ExpectedKinds;
        private  Object[] _objArr2 = new Object[2], _objArr;
        private  Byte[] _byteArr;
        private static int s_keyCount = 0;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKeyName += counter.ToString();

            Random rand = new Random(10);
            _ExpectedKinds = new RegistryValueKind[38];

            Object obj = new Object();
            _objArr2[0] = obj;
            _objArr2[1] = obj;
            _byteArr = new Byte[rand.Next(0, 100)];
            rand.NextBytes(_byteArr);

            _objArr = new Object[38];
            // Standard Random Numbers
            _objArr[0] = (Byte)(rand.Next(Byte.MinValue, Byte.MaxValue));
            _objArr[1] = (SByte)(rand.Next(SByte.MinValue, SByte.MaxValue));
            _objArr[2] = (Int16)(rand.Next(Int16.MinValue, Int16.MaxValue));
            _objArr[3] = (UInt16)(rand.Next(UInt16.MinValue, UInt16.MaxValue));
            _objArr[4] = (Char)(rand.Next(UInt16.MinValue, UInt16.MaxValue));
            _objArr[5] = (Int32)(rand.Next(Int32.MinValue, Int32.MaxValue));

            // Random Numbers that can fit into Int32
            _objArr[6] = (UInt32)(rand.NextDouble() * Int32.MaxValue);
            _objArr[7] = (Int64)(rand.NextDouble() * Int32.MaxValue);
            _objArr[8] = (Int64)(rand.NextDouble() * Int32.MinValue);
            _objArr[9] = (UInt64)(rand.NextDouble() * Int32.MaxValue);
            _objArr[10] = (Decimal)(rand.NextDouble() * Int32.MaxValue);
            _objArr[11] = (Decimal)(rand.NextDouble() * Int32.MinValue);
            _objArr[12] = (Single)(rand.NextDouble() * Int32.MaxValue);
            _objArr[13] = (Single)(rand.NextDouble() * Int32.MinValue);
            _objArr[14] = (Double)(rand.NextDouble() * Int32.MaxValue);
            _objArr[15] = (Double)(rand.NextDouble() * Int32.MinValue);

            // Random Numbers that can't fit into Int32 but can fit into Int64			
            _objArr[16] = (UInt32)(rand.NextDouble() * (UInt32.MaxValue - (UInt32)Int32.MaxValue) + (UInt32)Int32.MaxValue);
            _objArr[17] = (Int64)(rand.NextDouble() * (Int64.MaxValue - (Int64)Int32.MaxValue) + (Int64)Int32.MaxValue);
            _objArr[18] = (Int64)(rand.NextDouble() * (Int64.MinValue - (Int64)Int32.MinValue) + (Int64)Int32.MinValue);
            _objArr[19] = (UInt64)(rand.NextDouble() * ((UInt64)Int64.MaxValue - (UInt64)Int32.MaxValue) + (UInt64)Int32.MaxValue);
            _objArr[20] = (Decimal)(rand.NextDouble() * (Int64.MaxValue - (Int64)Int32.MaxValue) + (Int64)Int32.MaxValue);
            _objArr[21] = (Decimal)(rand.NextDouble() * (Int64.MinValue - (Int64)Int32.MinValue) + (Int64)Int32.MinValue);
            _objArr[22] = (Single)(rand.NextDouble() * (Int64.MaxValue - Int32.MaxValue) + Int32.MaxValue);
            _objArr[23] = (Single)(rand.NextDouble() * (Int64.MinValue - Int32.MinValue) + Int32.MinValue);
            _objArr[24] = (Double)(rand.NextDouble() * (Int64.MaxValue - Int32.MaxValue) + Int32.MaxValue);
            _objArr[25] = (Double)(rand.NextDouble() * (Int64.MinValue - Int32.MinValue) + Int32.MinValue);

            // Random Numbers that can't fit into Int32 or Int64
            _objArr[26] = (UInt64)(rand.NextDouble() * (UInt64.MaxValue - (UInt64)Int64.MaxValue) + (UInt64)Int64.MaxValue);
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
            _objArr[36] = (Object)obj;
            _objArr[37] = (Byte[])(_byteArr);

            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
            if (_rk1.GetValue(_testKeyName) != null)
                _rk1.DeleteValue(_testKeyName);

            _rk2 = _rk1.CreateSubKey(_testKeyName);
        }

        public RegistryKey_SetValueKind_str_obj_valueKind()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            // [] Test RegistryValueKind.Unknown

            try
            {
                _ExpectedKinds[0] = RegistryValueKind.String;
                _ExpectedKinds[1] = RegistryValueKind.String;
                _ExpectedKinds[2] = RegistryValueKind.String;
                _ExpectedKinds[3] = RegistryValueKind.String;
                _ExpectedKinds[4] = RegistryValueKind.String;
                _ExpectedKinds[5] = RegistryValueKind.DWord;
                _ExpectedKinds[6] = RegistryValueKind.String;
                _ExpectedKinds[7] = RegistryValueKind.String;
                _ExpectedKinds[8] = RegistryValueKind.String;
                _ExpectedKinds[9] = RegistryValueKind.String;
                _ExpectedKinds[10] = RegistryValueKind.String;
                _ExpectedKinds[11] = RegistryValueKind.String;
                _ExpectedKinds[12] = RegistryValueKind.String;
                _ExpectedKinds[13] = RegistryValueKind.String;
                _ExpectedKinds[14] = RegistryValueKind.String;
                _ExpectedKinds[15] = RegistryValueKind.String;
                _ExpectedKinds[16] = RegistryValueKind.String;
                _ExpectedKinds[17] = RegistryValueKind.String;
                _ExpectedKinds[18] = RegistryValueKind.String;
                _ExpectedKinds[19] = RegistryValueKind.String;
                _ExpectedKinds[20] = RegistryValueKind.String;
                _ExpectedKinds[21] = RegistryValueKind.String;
                _ExpectedKinds[22] = RegistryValueKind.String;
                _ExpectedKinds[22] = RegistryValueKind.String;
                _ExpectedKinds[23] = RegistryValueKind.String;
                _ExpectedKinds[24] = RegistryValueKind.String;
                _ExpectedKinds[25] = RegistryValueKind.String;
                _ExpectedKinds[26] = RegistryValueKind.String;
                _ExpectedKinds[27] = RegistryValueKind.String;
                _ExpectedKinds[28] = RegistryValueKind.String;
                _ExpectedKinds[29] = RegistryValueKind.String;
                _ExpectedKinds[30] = RegistryValueKind.String;
                _ExpectedKinds[31] = RegistryValueKind.String;
                _ExpectedKinds[32] = RegistryValueKind.String;
                _ExpectedKinds[33] = RegistryValueKind.String;
                _ExpectedKinds[34] = RegistryValueKind.String;
                _ExpectedKinds[35] = RegistryValueKind.MultiString;
                _ExpectedKinds[36] = RegistryValueKind.String;
                _ExpectedKinds[37] = RegistryValueKind.Binary;

                for (int i = 0; i < _objArr.Length; i++)
                {
                    _valueName = "Testing " + i;
                    _rk2.SetValue(_valueName, _objArr[i], RegistryValueKind.Unknown);

                    if (_rk2.GetValue(_valueName).ToString() != _objArr[i].ToString() || _rk2.GetValueKind(_valueName) != _ExpectedKinds[i])
                    {
                        Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + _ExpectedKinds[i].ToString() + ", got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_2121 Unexpected exception :: " + e.ToString());
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
                    _valueName = "Testing " + i;
                    _rk2.SetValue(_valueName, _objArr[i], RegistryValueKind.String);

                    if (_rk2.GetValue(_valueName).ToString() != _objArr[i].ToString() || _rk2.GetValueKind(_valueName) != RegistryValueKind.String)
                    {
                        Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.String + ", got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_782sg Unexpected exception :: " + e.ToString());
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
                    _valueName = "Testing " + i;
                    _rk2.SetValue(_valueName, _objArr[i], RegistryValueKind.ExpandString);

                    if (_rk2.GetValue(_valueName).ToString() != _objArr[i].ToString() || _rk2.GetValueKind(_valueName) != RegistryValueKind.ExpandString)
                    {
                        Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.ExpandString + ", got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_482hv Unexpected exception :: " + e.ToString());
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
                        _valueName = "Testing " + i;
                        _rk2.SetValue(_valueName, _objArr[i], RegistryValueKind.MultiString);

                        if (_rk2.GetValue(_valueName).ToString() != _objArr[i].ToString() || _rk2.GetValueKind(_valueName) != RegistryValueKind.MultiString)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.MultiString + ", got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                        }
                    }
                    catch (ArgumentException)
                    { }
                    catch (Exception ioe)
                    {
                        if ((ioe.GetType() == typeof(ArgumentException)) && (_objArr[i].GetType() == (new string[0]).GetType()))
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.MultiString + ", got exception==" + ioe.ToString());
                        }
                        else
                        {
                            Assert.False(true, "Error Incorrect exception , got exc==" + ioe.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_152wk Unexpected exception :: " + e.ToString());
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
                        _valueName = "Testing " + i;
                        _rk2.SetValue(_valueName, _objArr[i], RegistryValueKind.Binary);

                        if (_rk2.GetValue(_valueName).ToString() != _objArr[i].ToString() || _rk2.GetValueKind(_valueName) != RegistryValueKind.Binary)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.Binary + ", got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                        }
                    }
                    catch (ArgumentException)
                    { }
                    catch (Exception ioe)
                    {
                        if ((ioe.GetType() == typeof(ArgumentException)) && (_objArr[i].GetType() == (new byte[0]).GetType()))
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.Binary + ", got exception==" + ioe.ToString());
                        }
                        else
                        {
                            Assert.False(true, "Error Incorrect exception , got exc==" + ioe.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_152ex Unexpected exception :: " + e.ToString());
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

                        _valueName = "Testing " + i;
                        _rk2.SetValue(_valueName, _objArr[i], RegistryValueKind.DWord);

                        if (_rk2.GetValue(_valueName).ToString() != _strExpected || _rk2.GetValueKind(_valueName) != RegistryValueKind.DWord)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _strExpected + " kind==" + RegistryValueKind.DWord + ", got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                        }
                        if (i > 15)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==Exception, got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                        }
                    }
                    catch (ArgumentException)
                    { }
                    catch (Exception ioe)
                    {
                        if (i < 15)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.DWord + ", got exception==" + ioe.ToString());
                        }
                        else
                        {
                            Assert.False(true, "Error Incorrect exception , got exc==" + ioe.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_744ss Unexpected exception :: " + e.ToString());
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

                        _valueName = "Testing " + i;
                        _rk2.SetValue(_valueName, _objArr[i], RegistryValueKind.QWord);

                        if (_rk2.GetValue(_valueName).ToString() != _strExpected || _rk2.GetValueKind(_valueName) != RegistryValueKind.QWord)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _strExpected + " kind==" + RegistryValueKind.QWord + ", got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                        }
                        if (i > 25)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==Exception, got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                        }
                    }
                    catch (ArgumentException)
                    { }
                    catch (Exception ioe)
                    {
                        if (i < 25)
                        {
                            Assert.False(true, "Error Type==" + _objArr[i].GetType() + " Expected==" + _objArr[i].ToString() + " kind==" + RegistryValueKind.QWord + ", got exception==" + ioe.ToString());
                        }
                        else
                        {
                            Assert.False(true, "Error Incorrect exception , got exc==" + ioe.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_745ss Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test08()
        {
            // [] Registry Key does not exist

            try
            {
                _valueName = "FooBar";
                _rk2.SetValue(_valueName, _objArr[5], RegistryValueKind.DWord);

                if (_rk2.GetValue(_valueName).ToString() != _objArr[5].ToString() || _rk2.GetValueKind(_valueName) != RegistryValueKind.DWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[5].GetType() + " Expected==" + _objArr[5].ToString() + " kind==" + RegistryValueKind.DWord + ", got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_484ec Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test09()
        {
            // [] Registry Key already exists

            try
            {
                _valueName = "FooBar";
                _rk2.SetValue(_valueName, _objArr[7], RegistryValueKind.QWord);

                if (_rk2.GetValue(_valueName).ToString() != _objArr[7].ToString() || _rk2.GetValueKind(_valueName) != RegistryValueKind.QWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[7].GetType() + " Expected==" + _objArr[7].ToString() + " kind==" + RegistryValueKind.QWord + ", got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_541ll Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test10()
        {
            // [] Name is null

            try
            {
                _valueName = null;
                _rk2.SetValue(_valueName, _objArr[5], RegistryValueKind.DWord);

                if (_rk2.GetValue(_valueName).ToString() != _objArr[5].ToString() || _rk2.GetValueKind(_valueName) != RegistryValueKind.DWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[5].GetType() + " Expected==" + _objArr[5].ToString() + " kind==" + RegistryValueKind.DWord + ", got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_556po Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test11()
        {
            // [] Name is ""

            try
            {
                _valueName = "";
                _rk2.SetValue(_valueName, _objArr[7], RegistryValueKind.QWord);

                if (_rk2.GetValue(_valueName).ToString() != _objArr[7].ToString() || _rk2.GetValueKind(_valueName) != RegistryValueKind.QWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[7].GetType() + " Expected==" + _objArr[7].ToString() + " kind==" + RegistryValueKind.QWord + ", got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_548ov Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test12()
        {
            // [] Name is 255 characters

            try
            {
                _valueName = "12345678901111111110222222222033333333304444444440555555555066666666607777777770888888888099999999901234567890111111111022222222203333333330444444444055555555506666666660777777777088888888809999999990123456789011111111102222222220333333333044444444405555";
                _rk2.SetValue(_valueName, _objArr[7], RegistryValueKind.QWord);

                if (_rk2.GetValue(_valueName).ToString() != _objArr[7].ToString() || _rk2.GetValueKind(_valueName) != RegistryValueKind.QWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[7].GetType() + " Expected==" + _objArr[7].ToString() + " kind==" + RegistryValueKind.QWord + ", got value==" + _rk2.GetValue(_valueName).ToString() + " kind==" + _rk2.GetValueKind(_valueName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_458mn Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test13()
        {
            // [] Name is longer then 16383 characters
            Action a = () =>
            {
                int MaxValueNameLength = 16383; // prior to V4, the limit is 255

                StringBuilder sb = new StringBuilder(MaxValueNameLength + 1);
                for (int i = 0; i <= MaxValueNameLength; i++)
                    sb.Append('a');
                _valueName = sb.ToString();
                _rk2.SetValue(_valueName, _objArr[7], RegistryValueKind.QWord);
            };

            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test14()
        {
            // [] Value is null
            Action a = () =>
            {
                _valueName = "FooBar";
                _rk2.SetValue(_valueName, null, RegistryValueKind.QWord);
            };

            Assert.Throws<ArgumentNullException>(() => { a(); });
        }

        [Fact]
        public void Test15()
        {
            // [] ValueKind is equal to -2 which is not an acceptable value
            Action a = () =>
            {
                _valueName = "FooBar";
                _rk2.SetValue(_valueName, _objArr[5], (RegistryValueKind)(-2));
            };

            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test16()
        {
            // [] value is a string[] with null values
            Action a = () =>
            {
                _valueName = "FooBar";
                string[] strArr = new String[] { "one", "two", null, "three" };

                _rk2.SetValue(_valueName, strArr, RegistryValueKind.MultiString);
            };

            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test17()
        {
            // [] value is a object[]
            Action a = () =>
            {
                _valueName = "FooBar";

                _rk2.SetValue(_valueName, _objArr2, RegistryValueKind.MultiString);
            };

            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test18()
        {
            // [] RegistryKey is readonly
            Action a = () =>
            {
                _valueName = "FooBar";
                RegistryKey rk3 = _rk1.OpenSubKey(_testKeyName, false);

                rk3.SetValue(_valueName, _objArr[5], RegistryValueKind.DWord);
            };

            Assert.Throws<UnauthorizedAccessException>(() => { a(); });
        }

        [Fact]
        public void Test19()
        {
            // [] Set RegistryKey to bad array type.  // To improve code coverage
            Action a = () =>
            {
                object[] objTemp = new object[] { "my string", "your string", "Any once string" };
                _rk2.SetValue(_valueName, objTemp, RegistryValueKind.Unknown);
            };

            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test20()
        {
            // [] RegistryKey is closed
            Action a = () =>
            {
                _rk2.Dispose();
                _valueName = "FooBar";

                _rk2.SetValue(_valueName, _objArr[5], RegistryValueKind.DWord);
            };

            Assert.Throws<ObjectDisposedException>(() => { a(); });
        }

        public void Dispose()
        {
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
            if (_rk1.GetValue(_testKeyName) != null)
                _rk1.DeleteValue(_testKeyName);
        }
    }
}