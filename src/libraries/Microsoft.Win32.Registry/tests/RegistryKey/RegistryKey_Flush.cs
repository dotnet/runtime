// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_Flush : RegistryTestsBase
    {
        // [] Flush is called if the state of the registry key is dirty - modifiable operation has occurred in the key - and the 
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

        [Fact]
        public void FlushNewlyOpenedKey()
        {
            TestRegistryKey.Flush();
        }

        [Fact]
        public void FlushRegistryKeyAfterClosing()
        {
            TestRegistryKey.Dispose();
            TestRegistryKey.Flush();
        }

        [Fact]
        public void SetValueAndFlush()
        {
            const string valueName = "Key";
            const string expectedValue = "Value";
            TestRegistryKey.SetValue(valueName, expectedValue);
            //Now we call Flush but this is really redundant 
            TestRegistryKey.Flush();
            Assert.Equal(expectedValue, TestRegistryKey.GetValue(valueName));
        }

        [Fact]
        public void FlushDeletedRegistryKey()
        {
            Registry.CurrentUser.DeleteSubKeyTree(TestRegistryKeyName);
            TestRegistryKey.Flush();
        }
    }
}
