// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Security;
using System.IO;
using Xunit;
using System.Threading;
using System.ComponentModel;

namespace System.Diagnostics.ProcessTests
{
    public class ProcessModuleTests : ProcessTestBase
    {
        [Fact, PlatformSpecific(~PlatformID.OSX)]
        public void TestModulePropertiesExceptOnOSX()
        {
            ProcessModuleCollection moduleCollection = Process.GetCurrentProcess().Modules;
            Assert.True(moduleCollection.Count > 0);

            for (int i = 0; i < moduleCollection.Count; i++)
            {
                Assert.True(moduleCollection[i].BaseAddress.ToInt64() > 0);
                // From MSDN: Due to changes in the way that Windows loads assemblies, 
                // EntryPointAddress will always return 0 on Windows 8 or Windows 8.1 and should not be relied on for those platforms.
                Assert.True(moduleCollection[i].EntryPointAddress.ToInt64() >= 0);
                Assert.True(moduleCollection[i].ModuleMemorySize > 0);
            }
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
