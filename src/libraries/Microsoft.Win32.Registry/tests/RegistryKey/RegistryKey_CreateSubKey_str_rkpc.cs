// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_CreateSubKey_str_rkpc : IDisposable
    {
        // Variables needed
        private RegistryKey _rk1, _rk2;
        private String _testKeyName, _testValueName, _testStringName, _testString;
        private int s_testValue;
        private static int s_keyCount = 0;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKeyName = "REG_TEST_2" + counter.ToString();
            _testValueName = "TestValue";
            _testStringName = "TestString" + counter.ToString();
            _testString = "Hello World!†þ";

            s_testValue = 11;

            _rk1 = Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);

        }

        public RegistryKey_CreateSubKey_str_rkpc()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            // [] Vanilla; create a new subkey in read/write mode and write to it

            try
            {
                _rk2 = _rk1.CreateSubKey(_testKeyName, true /* write */);
                _rk2.SetValue(_testValueName, s_testValue);
                if (_rk2.ValueCount != 1)
                {
                    Assert.False(true, "Error Value not correctly created.");
                }
            }
            catch (Exception exc)
            {
                Assert.False(true, "Error Unexpected Exception: " + exc.ToString());
            }
            // [] Vanilla; create a new subkey in default mode and write to it

            try
            {
                _rk2.SetValue(_testStringName, _testString);
                if (_rk2.ValueCount != 2)
                {
                    Assert.False(true, "Error Value not correctly created.");
                }
            }
            catch (Exception exc)
            {
                Assert.False(true, "ErrorUnexpected Exception: " + exc.ToString());
            }

            // [] Vanilla; read those values (here, create subkey should open the key)

            try
            {
                if (((int)(_rk2.GetValue(_testValueName)) != s_testValue) || ((string)(_rk2.GetValue(_testStringName)) != _testString))
                {
                    Assert.False(true, "Error Value not correctly created.");
                }
                _rk2.Dispose();
            }
            catch (Exception exc)
            {
                Assert.False(true, "Error!Unexpected Exception: " + exc.ToString());
            }
        }

        [Fact]
        public void Test02()
        {
            // [] Create with the read permissions only and try to create a value
            Action a = () =>
            {
                _rk2 = _rk1.CreateSubKey(_testKeyName, false);
                _rk2.SetValue(_testValueName, s_testValue);
            };

            Assert.Throws<UnauthorizedAccessException>(() => { a(); });
        }

        [Fact]
        public void Test03()
        {
            // [] Create a subkey with a null name
            Action a = () =>
            {
                _rk2 = _rk1.CreateSubKey(null, true);
            };

            Assert.Throws<ArgumentNullException>(() => { a(); });
        }

        [Fact]
        public void Test04()
        {
            // [] Let the name of the created subkey be empty

            int prevValCount = 0;
            try
            {
                if ((_rk2 = _rk1.OpenSubKey("", true)) != null)
                {
                    _rk2.DeleteValue(_testStringName, false);
                    prevValCount = _rk2.ValueCount;
                    _rk2.Dispose();
                }
                _rk2 = _rk1.CreateSubKey("", true);
                _rk2.SetValue(_testStringName, _testString);
                if (_rk2.ValueCount != prevValCount + 1)
                {
                    Assert.False(true, "Error Value not correctly created.");
                }
                _rk2.Dispose();
            }
            catch (Exception exc)
            {
                Assert.False(true, "Error!Unexpected Exception: " + exc.ToString());
            }
        }

        [Fact]
        public void Test05()
        {
            // [] Read from the subkey created in the previous test

            try
            {
                _rk2 = _rk1.CreateSubKey("", true);
                _rk2.SetValue(_testStringName, _testString);
                if ((string)(_rk2.GetValue(_testStringName)) != _testString)
                {
                    Assert.False(true, "Error! Value not correctly created.");
                }
                _rk2.Dispose();
            }
            catch (Exception exc)
            {
                Assert.False(true, string.Format("Error!Unexpected Exception: " + exc.ToString()));
            }
        }


        [Fact]
        public void Test06()
        {
            // [] Create a subkey with a name greater than 255 chars
            Action a = () =>
            {
                _rk2 = _rk1.CreateSubKey("0abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", true);
            };

            Assert.Throws<ArgumentException>(() => { a(); });
        }

        public void Dispose()
        {
            if (_rk1.GetValue(_testStringName) != null)
                _rk1.DeleteValue(_testStringName);
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
        }
    }
}