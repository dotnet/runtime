// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Text;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteSubKey_str : IDisposable
    {
        // Variables needed
        private RegistryKey _rk1, _rk2;
        private string _testKeyName = "REG_TEST_3";
        private static int s_keyCount = 0;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKeyName = _testKeyName + counter.ToString();
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
        }

        public RegistryKey_DeleteSubKey_str()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            // [] Passing in null should throw ArgumentNullException
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            Action a = () => { _rk1.DeleteSubKey(null); };
            Assert.Throws<ArgumentNullException>(() => { a(); });
        }

        [Fact]
        public void Test02()
        {
            // [] Delete a sub that doesn't exists. //To improve code coverage
            String strTemp = "This is the key name that doesn't exist's currently";
            Action a = () => { _rk1.DeleteSubKey(strTemp); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test03()
        {
            // [] Creating new SubKey and then deleting it
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
            // CreateSubKey should just open a SubKeyIfIt already exists
            _rk2 = _rk1.CreateSubKey(_testKeyName);
            _rk2.CreateSubKey("BLAH");
            if (_rk1.OpenSubKey(_testKeyName).OpenSubKey("BLAH") == null)
            {
                Assert.False(true, "Error Expected get not returned");
            }
            _rk2.DeleteSubKey("BLAH");
            if (_rk2.OpenSubKey("BLAH") != null)
            {
                Assert.False(true, "Error SubKey was not deleted");
            }

            _rk2 = _rk1.CreateSubKey(_testKeyName);
            for (int i = 0; i < 10; i++)
                _rk2.CreateSubKey("BLAH_" + i.ToString());

            // [] Should throw invalid operation if I now try to delete parent key with values or subkeys
            Action a = () => { _rk1.DeleteSubKey(_testKeyName); };
            Assert.Throws<InvalidOperationException>(() => { a(); });

            _rk2.DeleteSubKey("blah_9");

            if (_rk2.SubKeyCount != 9)
            {
                Assert.False(true, "Error Incorrect number of subkeys , count==" + _rk2.SubKeyCount);
            }
            for (int i = 0; i < 9; i++)
            {
                if (!_rk2.GetSubKeyNames()[i].Equals("BLAH_" + i.ToString()))
                {
                    Assert.False(true, "Error" + i.ToString() + "! Incorrect name of subKey");
                }
            }

            _rk1.DeleteSubKeyTree(_testKeyName);
        }

        [Fact]
        public void Test05()
        {
            StringBuilder sb = new StringBuilder("");
            for (int i = 0; i < 256; i++)
                sb.Append(",");
            Action a = () => { _rk1.CreateSubKey(sb.ToString());  };
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