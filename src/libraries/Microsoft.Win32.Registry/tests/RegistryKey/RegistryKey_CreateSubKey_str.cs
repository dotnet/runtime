// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Text;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_CreateSubKey_str : IDisposable
    {
        // Variables needed
        private RegistryKey _rk1, _rk2;
        private string _str1;
        private String _testKeyName = "REG_TEST_1";
        private static int s_keyCount = 0;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKeyName = _testKeyName + counter.ToString();
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
        }

        public RegistryKey_CreateSubKey_str()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            // [] Passing in null should throw ArgumentNullException

            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            Action a = () => { _rk2 = _rk1.CreateSubKey(null); };
            Assert.Throws<ArgumentNullException>(() => { a(); });
        }

        [Fact]
        public void Test02()
        {
            try
            {
                _rk2 = _rk1.CreateSubKey(String.Empty);
                if (_rk2.ToString() != (_rk1.ToString() + @"\"))
                {
                    Assert.False(true, "Error CreateSubKey returned some unexpected results... ");
                }
            }
            catch (Exception exc)
            {
                Assert.False(true, "Error Unexpected expected occured, got exc==" + exc.ToString());
            }
        }

        [Fact]
        public void Test03()
        {
            // [] Creating new SubKey and check that it exists
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            _rk1.CreateSubKey(_testKeyName);
            if (_rk1.OpenSubKey(_testKeyName) == null)
            {
                Assert.False(true, "Error SubKey does not exist.");
            }

            _rk1.DeleteSubKey(_testKeyName);
            if (_rk1.OpenSubKey(_testKeyName) != null)
            {
                Assert.False(true, "Error SubKey not removed properly");
            }
        }

        [Fact]
        public void Test04()
        {
            // [] Give subkey a value and get it back
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            _rk1.CreateSubKey(_testKeyName);
            _rk1.SetValue(_testKeyName, new Decimal(5));
            if (_rk1.OpenSubKey(_testKeyName) == null)
            {
                Assert.False(true, "Error Could not get subkey");
            }
            //Remember, this will be written as a string value
            Object obj11 = _rk1.GetValue(_testKeyName);
            if (Convert.ToDecimal(_rk1.GetValue(_testKeyName)) != new Decimal(5))
            {
                Assert.False(true, "Error got value==" + _rk1.GetValue(_testKeyName));
            }
        }

        [Fact]
        public void Test05()
        {
            // [] CreateSubKey should open subkey if it already exists
            _rk2 = _rk1.CreateSubKey(_testKeyName);
            _rk2.CreateSubKey("BLAH");
            
            if (_rk1.OpenSubKey(_testKeyName).OpenSubKey("BLAH") == null)
            {
                Assert.False(true, "Error Expected get not returned");
            }
            _rk2.DeleteSubKey("BLAH");
        }

        [Fact]
        public void Test06()
        {
            // [] Create subkey and check GetSubKeyCount

            _rk2 = _rk1.CreateSubKey(_testKeyName);
            for (int i = 0; i < 10; i++)
                _rk2.CreateSubKey("BLAH_" + i.ToString());

            if (_rk2.SubKeyCount != 10)
            {
                Assert.False(true, "Error Incorrect number of subkeys , count==" + _rk2.SubKeyCount);
            }
            for (int i = 0; i < 10; i++)
            {
                if (!_rk2.GetSubKeyNames()[i].Equals("BLAH_" + i.ToString()))
                {
                    Assert.False(true, "Error" + i.ToString() + "! Incorrect name of subKey");
                }
            }
        }

        [Fact]
        public void Test07()
        {
            _rk2 = _rk1.CreateSubKey(_testKeyName);

            _str1 = "Dyalog APL/W 10.0";
            _rk2.CreateSubKey(_str1);
            if (_rk2.OpenSubKey(_str1) == null)
            {
                Assert.False(true, "Error SubKey does not exist.");
            }
        }

        [Fact]
        public void Test08()
        {
            //[]we should open keys with multiple \ in the name
            _rk2 = _rk1.CreateSubKey(_testKeyName);

            _str1 = @"a\b\c\d\e\f\g\h";
            _rk2.CreateSubKey(_str1);
            if (_rk2.OpenSubKey(_str1) == null)
            {
                Assert.False(true, "Error SubKey does not exist.");
            }

            _rk1.DeleteSubKeyTree(_testKeyName);
        }

        [Fact]
        public void Test09()
        {
            //[]play around with the \ and the / keys
            _rk2 = _rk1.CreateSubKey(_testKeyName);

            _str1 = @"a\b\c\/d\//e\f\g\h\//\\";
            _rk2.CreateSubKey(_str1);
            if (_rk2.OpenSubKey(_str1) == null)
            {
                Assert.False(true, "Error SubKey does not exist.");
            }

            _rk1.DeleteSubKeyTree(_testKeyName);
        }

        [Fact]
        public void Test10()
        {
            //[] how deep can we go with this

            _rk2 = _rk1.CreateSubKey(_testKeyName);

            _str1 = String.Empty;

            // Changed the number of times we repeat str1 from 100 to 30 in response to the Windows OS
            //There is a restriction of 255 characters for the keyname even if it is multikeys. Not worth to pursue as a bug
            // reduced further to allow for WoW64 changes to the string.
            for (int i = 0; i < 25 && _str1.Length < 230; i++)
                _str1 = _str1 + i.ToString() + @"\";
            _rk2.CreateSubKey(_str1);
            if (_rk2.OpenSubKey(_str1) == null)
            {
                Assert.False(true, "Error SubKey does not exist.");
            }

            //However, we are interested in ensuring that there are no buffer overflow issues with a deeply nested keys
            for (int i = 0; i < 3; i++)
            {
                _rk2 = _rk2.OpenSubKey(_str1, true);
                if (_rk2 == null)
                {
                    Assert.False(true, "Err Wrong value returned, " + i);
                    break;
                }
                _rk2.CreateSubKey(_str1);
            }

            _rk1.DeleteSubKeyTree(_testKeyName);
        }

        [Fact]
        public void Test11()
        {
            // [] Should throw ArgumentException if key name is too long
            StringBuilder sb = new StringBuilder("");
            for (int i = 0; i < 256; i++)
                sb.Append(",");
            Action a = () => { _rk1.CreateSubKey(sb.ToString()); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        public void Dispose()
        {
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
            if (_rk1.GetValue(_testKeyName) != null)
                _rk1.DeleteValue(_testKeyName);
        }
    }
}