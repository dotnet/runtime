// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_OpenSubKey_str_rkpc : IDisposable
    {
        // Variables needed
        private RegistryKey _rk1, _rk2, _rk3;
        private  String _testKeyName, _testValueName, _testStringName, _testString;
        private  int _testValue;
        private static int s_keyCount = 0;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKeyName = "REG_TEST_12" + counter.ToString();
            _testValueName = "TestValue";
            _testStringName = "TestString" + counter.ToString();
            _testString = "Hello World!†þ";
            _testValue = 11;

            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
            _rk2 = _rk1.CreateSubKey(_testKeyName);
            _rk2.Dispose();
        }

        public RegistryKey_OpenSubKey_str_rkpc()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            // [] Vanilla; open a subkey in read/write mode and write to it

            try
            {
                _rk2 = _rk1.OpenSubKey(_testKeyName, true);
                _rk2.SetValue(_testValueName, _testValue);
                if (_rk2.ValueCount != 1)
                {
                    Assert.False(true, "Error Value not correctly created.");
                }
                _rk2.Dispose();
            }
            catch (Exception exc)
            {
                Assert.False(true, "ErrorUnexpected Exception: " + exc.ToString());
            }

            // [] Vanilla; open a subkey in default mode and write to it
            Action a = () =>
            {
                _rk2 = _rk1.OpenSubKey(_testKeyName, false);
                _rk2.SetValue(_testStringName, _testString);
            };
            Assert.Throws<UnauthorizedAccessException>(() => { a(); });

            // [] Vanilla; read those values (here, create subkey should open the key)

            try
            {
                _rk2 = _rk1.OpenSubKey(_testKeyName, false);
                if (((int)(_rk2.GetValue(_testValueName)) != _testValue) || (_rk2.GetValue(_testStringName) != null))
                {
                    Assert.False(true, "Error Value not correctly created.");
                }
                _rk2.Dispose();
            }
            catch (Exception exc)
            {
                Assert.False(true, "ErrorUnexpected Exception: " + exc.ToString());
            }

            // [] Open with the read permissions only and try to create a value
            Action a1 = () =>
            {
                _rk2 = _rk1.OpenSubKey(_testKeyName, false);
                _rk2.CreateSubKey("sub" + _testKeyName);
            };

            Assert.Throws<UnauthorizedAccessException>(() => { a1(); });
        }

        [Fact]
        public void Test02()
        {
            // [] Open a subkey with a null name
            Action a = () => {_rk2 = _rk1.OpenSubKey(null, false);  };
            Assert.Throws<ArgumentNullException>(() => { a(); });
        }

        [Fact]
        public void Test03()
        {
            // [] Let the name of the opened subkey be empty

            if (_rk1.OpenSubKey("") == null)
                _rk1.CreateSubKey("");

            int prevValCount = 0;
            try
            {
                if ((_rk2 = _rk1.OpenSubKey("", true)) != null)
                {
                    _rk2.DeleteValue(_testStringName, false);
                    prevValCount = _rk2.ValueCount;
                    _rk2.Dispose();
                }
                _rk2 = _rk1.OpenSubKey("", true);
                _rk2.SetValue(_testStringName, _testString);
                if (_rk2.ValueCount != prevValCount + 1)
                {
                    Assert.False(true, "Error Value not correctly created.");
                }
                _rk2.Dispose();
            }
            catch (Exception exc)
            {
                Assert.False(true, "ErrorUnexpected Exception: " + exc.ToString());
            }

            // [] Read from the subkey written to in the previous test

            try
            {
                _rk2 = _rk1.OpenSubKey("", false);
                if ((string)(_rk2.GetValue(_testStringName)) != _testString)
                {
                    Assert.False(true, "Error Value not correctly created.");
                }
                _rk2.Dispose();
            }
            catch (Exception exc)
            {
                Assert.False(true, "ErrorUnexpected Exception: " + exc.ToString());
            }
        }

        [Fact]
        public void Test04()
        {
            // [] Try to open a subkey with a name greater than 255 chars
            Action a = () => {_rk2 = _rk1.OpenSubKey("0abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", true);  };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test05()
        {
            // [] Try to open a subkey of a closed key
            Action a = () =>
            {
                _rk2 = _rk1.CreateSubKey(_testKeyName, true);
                _rk2.CreateSubKey("sub" + _testKeyName);
                _rk2.Dispose();
                _rk3 = _rk2.OpenSubKey("sub" + _testKeyName, true);
            };

            Assert.Throws<ObjectDisposedException>(() => { a(); });
        }

        public void Dispose()
        {
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
            if (_rk1.GetValue(_testValueName) != null)
                _rk1.DeleteValue(_testValueName);
            if (_rk1.GetValue(_testStringName) != null)
                _rk1.DeleteValue(_testStringName);
        }
    }
}