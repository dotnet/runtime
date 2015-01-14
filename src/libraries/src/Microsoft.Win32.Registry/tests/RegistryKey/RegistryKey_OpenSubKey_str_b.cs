// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_OpenSubKey_str_b : IDisposable
    {
        private RegistryKey _rk1, _rk2;
        private String _testKeyName = "BCL_TEST_9";
        private string _testStringName = "TestString";
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

        public RegistryKey_OpenSubKey_str_b()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            // [] Passing in null should throw ArgumentNullException

            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            Action a = () => { _rk1.OpenSubKey(null, false); };
            Assert.Throws<ArgumentNullException>(() => { a(); });
        }

        [Fact]
        public void Test02()
        {
            // [] Should not be able to modify values when false is passed

            _rk2 = _rk1.CreateSubKey(_testKeyName);
            _rk2.SetValue(_testStringName, "TestValue");

            _rk2 = _rk1.OpenSubKey(_testKeyName, false);
            Action a = () => { _rk2.DeleteValue(_testStringName); };
            Assert.Throws<UnauthorizedAccessException>(() => { a(); });
        }

        [Fact]
        public void Test03()
        {
            // [] Should have write rights when true is passed

            _rk2 = _rk1.CreateSubKey(_testKeyName);
            if (_rk2.GetValue(_testStringName) != null)
            {
                Assert.False(true, "Error Value not deleted");
            }

            _rk2 = _rk1.OpenSubKey("", true);
            _rk2.SetValue(_testStringName, "Test");
            if (!_rk1.GetValue(_testStringName).Equals("Test"))
            {
                Assert.False(true, "Error Value not set properly");
            }
        }

        [Fact]
        public void Test04()
        {
            // [] Open one length subkey

            _rk2 = _rk1.CreateSubKey(_testKeyName);
            _rk2.SetValue("StringTest", "BeforeTest");
            _rk2 = _rk1.OpenSubKey(_testKeyName, false);
            Action a = () => { _rk2.SetValue("StringTest", "Test"); };
            Assert.Throws<UnauthorizedAccessException>(() => { a(); });
        }

        [Fact]
        public void Test05()
        {
            // [] Check that I can read
            _rk2 = _rk1.CreateSubKey(_testKeyName);
            _rk2.SetValue("StringTest", "BeforeTest");
            if (!_rk2.GetValue("StringTest").Equals("BeforeTest"))
            {
                Assert.False(true, "Error Expected==BeforeTest , value==" + _rk2.GetValue("StringTest").ToString());
            }
        }

        [Fact]
        public void Test06()
        {
            // [] Same thing for true

            _rk2 = _rk1.CreateSubKey(_testKeyName);
            _rk2.SetValue(_testStringName, "BeforeTest");
            _rk2 = _rk1.OpenSubKey(_testKeyName, true);
            _rk2.SetValue(_testStringName, "Test");
            if (!_rk2.GetValue(_testStringName).Equals("Test"))
            {
                Assert.False(true, "Error Incorrect value set");
            }
        }


        public void Dispose()
        {
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
            if (_rk1.GetValue(_testStringName) != null)
                _rk1.DeleteValue(_testStringName);
        }
    }
}