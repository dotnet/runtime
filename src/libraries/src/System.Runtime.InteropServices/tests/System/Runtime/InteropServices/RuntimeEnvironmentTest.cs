// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.IO;
using Xunit;

namespace System.Runtime.InteropServices
{
    public static class RuntimeEnvironmentTest
    {
        [Fact]
        public static void RuntimeEnvironmentPosTest()
        {
            Assert.True(Directory.Exists(RuntimeEnvironment.GetRuntimeDirectory()));
            Assert.True(!String.IsNullOrEmpty(RuntimeEnvironment.GetSystemVersion()));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void RuntimeEnvironmentNegTest()
        {
            Type clsType = typeof(RuntimeEnvironmentTest);
            Assembly assem = clsType.Assembly;
            Assert.True(!RuntimeEnvironment.FromGlobalAccessCache(assem));

            Assert.Throws<PlatformNotSupportedException>(() => RuntimeEnvironment.SystemConfigurationFile);

            Guid guid;
            Assert.Throws<PlatformNotSupportedException>(() => RuntimeEnvironment.GetRuntimeInterfaceAsObject(guid, guid));
            Assert.Throws<PlatformNotSupportedException>(() => RuntimeEnvironment.GetRuntimeInterfaceAsIntPtr(guid, guid));
        }
    }
}
