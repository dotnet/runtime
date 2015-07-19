// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Security;
using System.IO;
using Xunit;
using System.Threading;

namespace System.Diagnostics.ProcessTests
{
    public class ProcessModuleTests : ProcessTestBase
    {
        [Fact, PlatformSpecific(~PlatformID.OSX)]
        public void TestModulePropertiesExceptOnOSX()
        {
            string fileName = CoreRunName;
            if (global::Interop.IsWindows)
                fileName = string.Format("{0}.exe", CoreRunName);

            // Ensure the process has loaded the modules.
            Assert.True(SpinWait.SpinUntil(() =>
            {
                if (_process.Modules.Count > 0)
                    return true;
                _process.Refresh();
                return false;
            }, WaitInMS));

            ProcessModuleCollection moduleCollection = _process.Modules;
            Assert.True(moduleCollection.Count > 0);

            ProcessModule coreRunModule = _process.MainModule;
            Assert.True(coreRunModule.BaseAddress.ToInt64() > 0);
            Assert.True(coreRunModule.EntryPointAddress.ToInt64() >= 0);
            Assert.Equal(fileName, coreRunModule.ModuleName);
            Assert.True(coreRunModule.ModuleMemorySize > 0);
            Assert.EndsWith(fileName, coreRunModule.FileName);

            Assert.Equal(string.Format("System.Diagnostics.ProcessModule ({0})", fileName), coreRunModule.ToString());
        }

        [Fact, PlatformSpecific(PlatformID.OSX)]
        public void TestModulePropertiesOnOSX()
        {
            // Getting modules for a process given a pid is not supported on OSX.
            ProcessModuleCollection moduleCollection = _process.Modules;
            Assert.Equal(0, moduleCollection.Count);
            Assert.Null(_process.MainModule);
        }
    }
}
