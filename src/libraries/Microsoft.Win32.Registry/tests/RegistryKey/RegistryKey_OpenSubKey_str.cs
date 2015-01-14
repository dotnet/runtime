// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Text;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_OpenSubKey_str : IDisposable
    {
        // Variables needed
        private RegistryKey _rk1, _rk2;
        private  int _iCount = 0;
        private  string _testKeyName = "REG_TEST_11";
        private static int s_keyCount = 0;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKeyName += counter.ToString();
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
        }

        public RegistryKey_OpenSubKey_str()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            // [] Passing in null should throw ArgumentNullException

            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            Action a = () => { _rk1.OpenSubKey(null); };
            Assert.Throws<ArgumentNullException>(() => { a(); });
            
        }

        [Fact]
        public void Test02()
        {
            // [] Getting subkey name of newly created subkey SubKey

            _rk1 = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_testKeyName);
            _rk1.CreateSubKey(_testKeyName);
            if (_rk1.OpenSubKey(_testKeyName) == null)
            {
                Assert.False(true, "Error SubKey does not exist.");
            }
            _iCount = _rk1.SubKeyCount;
            _rk1.DeleteSubKey(_testKeyName);
            if (_rk1.SubKeyCount != _iCount - 1)
            {
                Assert.False(true, "Error expected==" + ((Int32)_iCount - 1).ToString() + ", got==" + _rk1.SubKeyCount);
            }
            if (_rk1.OpenSubKey(_testKeyName) != null)
            {
                Assert.False(true, "Error SubKey not removed properly");
            }
        }

        [Fact]
        public void Test03()
        {
            // Give subkey a value and get it back

            _rk1 = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_testKeyName);
            _iCount = _rk1.SubKeyCount;
            _rk1.CreateSubKey(_testKeyName);
            if (_rk1.OpenSubKey(_testKeyName) == null)
            {
                Assert.False(true, "Error Could not get subkey");
            }
            if (_rk1.SubKeyCount != _iCount + 1)
            {
                Assert.False(true, "Error Incorrect subkeyCount");
            }
            _rk1.DeleteSubKeyTree(_testKeyName);
            if (_rk1.OpenSubKey(_testKeyName) != null)
            {
                Assert.False(true, "Error SubKey still there");
            }

            // CreateSubKey should just open a SubKeyIfIt already exists
            _rk1.CreateSubKey(_testKeyName);
            _rk2 = _rk1.CreateSubKey(_testKeyName);
            _rk2.CreateSubKey("BLAH");
            
            if (_rk1.OpenSubKey(_testKeyName).OpenSubKey("BLAH") == null)
            {
                Assert.False(true, "Error Expected get not returned");
            }
            if (_rk2.SubKeyCount != 1)
            {
                Assert.False(true, "Error Incorrect SubKeycount");
            }

            _rk2.DeleteSubKey("BLAH");
            if (_rk2.OpenSubKey("BLAH") != null)
            {
                Assert.False(true, "Error SubKey was not deleted");
            }
        }

        [Fact]
        public void Test04()
        {
            // Add in tests for GetSubKeyCount

            _rk2 = _rk1.CreateSubKey(_testKeyName);
            for (int i = 0; i < 9; i++)
                _rk2.CreateSubKey("BLAH_" + i.ToString());

            // this should have no effect, key does not exist
            Action a = () => { _rk2.DeleteSubKey("blah_9"); };
            Assert.Throws<ArgumentException>(() => { a(); });

            if (_rk2.SubKeyCount != 9)
            {
                Assert.False(true, "Error Incorrect number of subkeys , count==" + _rk2.SubKeyCount);
            }

            for (int i = 0; i < 9; i++)
            {
                if (_rk2.OpenSubKey("BLAH_" + i.ToString()) == null)
                {
                    Assert.False(true, "Error SubKey not found");
                }
            }

            _rk1.DeleteSubKeyTree(_testKeyName);
            if (_rk1.OpenSubKey(_testKeyName) != null)
            {
                Assert.False(true, "Error Subtree not deleted");
            }
        }

        [Fact]
        public void Test05()
        {
            StringBuilder sb = new StringBuilder("");
            for (int i = 0; i < 256; i++)
                sb.Append(",");
            Action a = () => { _rk1.CreateSubKey(sb.ToString()); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test06()
        {
            // [] OpenSubKey should be read only

            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            _rk1.CreateSubKey(_testKeyName);
            _rk1 = _rk1.OpenSubKey(_testKeyName);
            Action a = () => {_rk1.SetValue("Test", "String");  };
            Assert.Throws<UnauthorizedAccessException>(() => { a(); });
            
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            _rk1.DeleteSubKeyTree(_testKeyName);
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