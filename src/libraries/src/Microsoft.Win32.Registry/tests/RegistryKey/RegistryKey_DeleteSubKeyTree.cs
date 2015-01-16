// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteSubKeyTree : IDisposable
    {
        private RegistryKey _rk;
        private string _madeUpKey = "MadeUpKey";
        private string _subKeyExists = "SubkeyExists";
        private string _subKeyExists2 = "SubkeyExists2";
        private static int s_keyCount = 0;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _madeUpKey = _madeUpKey + counter.ToString();
            _rk = Registry.CurrentUser.OpenSubKey("Software", true);
            if (_rk.OpenSubKey(_madeUpKey) != null)
                _rk.DeleteSubKeyTree(_madeUpKey);
            if (_rk.OpenSubKey(_subKeyExists) != null)
                _rk.DeleteSubKeyTree(_subKeyExists);
            if (_rk.OpenSubKey(_subKeyExists2) != null)
                _rk.DeleteSubKeyTree(_subKeyExists2);
        }

        public RegistryKey_DeleteSubKeyTree()
        {
            TestInitialize();
        }

        [Fact]
        public void SubkeyMissingTests()
        {
            //throwOnMissing is true with subkey missing
            Action a = () => { _rk.DeleteSubKeyTree("MadeUpKey", true);  };
            Assert.Throws<ArgumentException>(() => { a(); });

            //throwOnMissing is false with subkey missing
            try
            {
                _rk.DeleteSubKeyTree(_madeUpKey, false);
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Unexpected Exception: {0}", e));
            }
        }

        [Fact]
        public void SubkeyExistsTests()
        {
            //throwOnMissing is true with subkey present
            try
            {
                RegistryKey rk2 = _rk.CreateSubKey(_subKeyExists);
                rk2.CreateSubKey("a");
                rk2.CreateSubKey("b");
                _rk.DeleteSubKeyTree(_subKeyExists, false);
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Unexpected Exception: {0}", e));
            }

            //throwOnMissing is false with subkey present
            try
            {
                RegistryKey rk2 = _rk.CreateSubKey(_subKeyExists2);
                rk2.CreateSubKey("a");
                rk2.CreateSubKey("b");
                _rk.DeleteSubKeyTree(_subKeyExists2, true);
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Unexpected Exception: {0}", e));
            }
        }

        public void Dispose()
        {
            // Clean up
            _rk = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
            if (_rk.OpenSubKey(_madeUpKey) != null)
                _rk.DeleteSubKeyTree(_madeUpKey);
            if (_rk.OpenSubKey(_subKeyExists) != null)
                _rk.DeleteSubKeyTree(_subKeyExists);
            if (_rk.OpenSubKey(_subKeyExists2) != null)
                _rk.DeleteSubKeyTree(_subKeyExists2);
        }
    }
}