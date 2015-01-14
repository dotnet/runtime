// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValueKind_str : IDisposable
    {
        private RegistryKey _rk1, _rk2;
        private String _testKeyName2 = "Test2";
        private String _subKeyName = "";
        private Object[] _objArr;
        private static int s_keyCount = 0;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKeyName2 += counter.ToString();
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName2) != null)
                _rk1.DeleteSubKeyTree(_testKeyName2);
            if (_rk1.GetValue(_testKeyName2) != null)
                _rk1.DeleteValue(_testKeyName2);
            _rk2 = _rk1.CreateSubKey(_testKeyName2);

            Random rand = new Random(10);
            Byte[] byteArr = new Byte[rand.Next(0, 100)];
            rand.NextBytes(byteArr);

            _objArr = new Object[6];
            _objArr[0] = (Int32)(rand.Next(Int32.MinValue, Int32.MaxValue));
            _objArr[1] = (Int64)(rand.NextDouble() * Int64.MaxValue);
            _objArr[2] = (String)"Hello World";
            _objArr[3] = (String)"Hello %path5% World";
            _objArr[4] = new String[] { "Hello World", "Hello %path% World" };
            _objArr[5] = (Byte[])(byteArr);
        }

        public RegistryKey_GetValueKind_str()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            // [] Registry Key does not exist

            if (_rk1.OpenSubKey(_testKeyName2) != null)
                _rk1.DeleteSubKeyTree(_testKeyName2);
            if (_rk1.GetValue(_testKeyName2) != null)
                _rk1.DeleteValue(_testKeyName2);
            _rk2 = _rk1.CreateSubKey(_testKeyName2);

            Action a = () =>
            {
                _subKeyName = "DoesNotExist";
                _rk2.GetValueKind(_subKeyName);
            };
            Assert.Throws<IOException>(() => { a(); });
        }

        [Fact]
        public void Test02()
        {
            // [] Name is null

            _rk2 = _rk1.CreateSubKey(_testKeyName2);
            try
            {
                // Create the key
                _subKeyName = null;
                _rk2.SetValue(_subKeyName, _objArr[0], RegistryValueKind.DWord);

                if (_rk2.GetValue(_subKeyName).ToString() != _objArr[0].ToString() || _rk2.GetValueKind(_subKeyName) != RegistryValueKind.DWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[0].GetType() + " Expected==" + _objArr[0].ToString() + " kind==" + RegistryValueKind.DWord + ", got value==" + _rk2.GetValue(_subKeyName).ToString() + " kind==" + _rk2.GetValueKind(_subKeyName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_556po Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test03()
        {
            // [] Name is ""

            _rk2 = _rk1.CreateSubKey(_testKeyName2);
            try
            {
                // Create the key
                _subKeyName = "";
                _rk2.SetValue(_subKeyName, _objArr[1], RegistryValueKind.QWord);

                if (_rk2.GetValue(_subKeyName).ToString() != _objArr[1].ToString() || _rk2.GetValueKind(_subKeyName) != RegistryValueKind.QWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[1].GetType() + " Expected==" + _objArr[1].ToString() + " kind==" + RegistryValueKind.QWord + ", got value==" + _rk2.GetValue(_subKeyName).ToString() + " kind==" + _rk2.GetValueKind(_subKeyName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_548ov Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test04()
        {
            // [] Name is longer then 255 characters

            _rk2 = _rk1.CreateSubKey(_testKeyName2);
            Action a = () =>
            {
                // Create the key
                _subKeyName = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                _rk2.GetValueKind(_subKeyName);
            };
            Assert.Throws<IOException>(() => { a(); });
        }

        [Fact]
        public void Test05()
        {
            // [] RegistryKey is readonly

            _rk2 = _rk1.CreateSubKey(_testKeyName2);
            try
            {
                // Create the key
                _subKeyName = null;

                _rk2.SetValue(_subKeyName, _objArr[1], RegistryValueKind.QWord);

                RegistryKey rk3 = _rk1.OpenSubKey(_testKeyName2, false);

                rk3.GetValue(_subKeyName);

                if (rk3.GetValue(_subKeyName).ToString() != _objArr[1].ToString() || rk3.GetValueKind(_subKeyName) != RegistryValueKind.QWord)
                {
                    Assert.False(true, "Error Type==" + _objArr[1].GetType() + " Expected==" + _objArr[1].ToString() + " kind==" + RegistryValueKind.QWord + ", got value==" + rk3.GetValue(_subKeyName).ToString() + " kind==" + rk3.GetValueKind(_subKeyName).ToString());
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Err_556po Unexpected exception :: " + e.ToString());
            }
        }

        [Fact]
        public void Test06()
        {
            // [] RegistryKey is closed

            _rk2 = _rk1.CreateSubKey(_testKeyName2);
            Action a = () =>
            {
                // Create the key
                _rk2.Dispose();
                _subKeyName = "FooBar";

                _rk2.GetValue(_subKeyName);
            };

            Assert.Throws<ObjectDisposedException>(() => { a(); });
        }


        public void Dispose()
        {
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName2) != null)
                _rk1.DeleteSubKeyTree(_testKeyName2);
            if (_rk1.GetValue(_testKeyName2) != null)
                _rk1.DeleteValue(_testKeyName2);
        }
    }
}