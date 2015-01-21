// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_Flush : IDisposable
    {
        // [] Flush is called if the state of the registry key is dirty - modifiable operation has occured in the key - and the 
        // registry key is still a valid handle. From the MSDN
        /**
        It is not necessary to call RegFlushKey to change a key. Registry changes are flushed to disk by the registry using its lazy flusher. 
        Lazy flushing occurs automatically and regularly after a system-specified interval of time. Registry changes are also flushed to disk at 
        system shutdown. Unlike RegCloseKey, the RegFlushKey function returns only when all the data has been written to the registry. 
        The RegFlushKey function may also write out parts of or all of the other keys. Calling this function excessively can have a negative effect 
        on an application's performance. An application should only call RegFlushKey if it requires absolute certainty that registry changes are on 
        disk. 

        In general, RegFlushKey rarely, if ever, need be used. Windows 95/98: No registry subkey or value name may exceed 255 characters. 
        **/
        private RegistryKey _rk1 = Microsoft.Win32.Registry.CurrentUser;
        private static String _testKeyName = "Test_1";

        [Fact]
        public void Test01()
        {
            //[]We will call Flush on a newly opened key
            var rk1 = Microsoft.Win32.Registry.CurrentUser;

            try
            {
                rk1.Flush();
            }
            catch (Exception exc)
            {
                Assert.False(true, "Error Unexpected Exception, got exc==" + exc.GetType().Name);
            }
        }

        [Fact]
        public void Test02()
        {
            //[]We will call Flush after closing the key
            RegistryKey rk1 = Microsoft.Win32.Registry.CurrentUser;
            rk1.Dispose();
            try
            {
                rk1.Flush();
            }
            catch (Exception exc)
            {
                Assert.False(true, "Error Unexpected Exception, got exc==" + exc.GetType().Name);
            }
        }

        [Fact]
        public void Test03()
        {
            _testKeyName += _testKeyName + "Test03";
            //[]We will write a value to the key and then flush
            var rk2 = _rk1.CreateSubKey(_testKeyName);
            if (rk2 == null)
            {
                Assert.False(true, "Error Null returned");
            }
            rk2.SetValue("Key", "Value");
            //Now we call Flush but this is really redundant 
            rk2.Flush();
            if ((String)rk2.GetValue("Key") != "Value")
            {
                Assert.False(true, "Error wrong value returned");
            }
        }

        [Fact]
        public void Test04()
        {
            _testKeyName += _testKeyName + "Test04";
            //[]We will delete SubKeyTree from the parent of this key and then call Flush from this key
            var rk2 = _rk1.CreateSubKey(_testKeyName);
            _rk1.DeleteSubKeyTree(_testKeyName);
            try
            {
                rk2.Flush();
            }
            catch (Exception exc)
            {
                Assert.False(true, "Error Unexpected Exception, got exc==" + exc.GetType().Name);
            }
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