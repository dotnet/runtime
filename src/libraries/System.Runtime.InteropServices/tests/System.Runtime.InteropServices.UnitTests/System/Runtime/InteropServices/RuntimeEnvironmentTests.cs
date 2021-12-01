// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Runtime.InteropServices
{
    public class RuntimeEnvironmentTests
    {
        [Fact]
        public void RuntimeEnvironmentRuntimeDirectory()
        {
            Assert.True(Directory.Exists(RuntimeEnvironment.GetRuntimeDirectory()));
        }

        [Fact]
        public void RuntimeEnvironmentSysVersion()
        {
            Assert.NotEmpty(RuntimeEnvironment.GetSystemVersion());
        }

#pragma warning disable SYSLIB0019 // RuntimeEnvironment members SystemConfigurationFile, GetRuntimeInterfaceAsIntPtr, and GetRuntimeInterfaceAsObject are no longer supported and throw PlatformNotSupportedException.
        [Fact]
        public void SystemConfigurationFile_Get_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => RuntimeEnvironment.SystemConfigurationFile);
        }

        [Fact]
        public void GetRuntimeInterfaceAsObject_Invoke_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => RuntimeEnvironment.GetRuntimeInterfaceAsObject(Guid.Empty, Guid.Empty));
        }

        [Fact]
        public void GetRuntimeInterfaceAsIntPtr_Invoke_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => RuntimeEnvironment.GetRuntimeInterfaceAsIntPtr(Guid.Empty, Guid.Empty));
        }
#pragma warning restore SYSLIB0019

        [Fact]
        public void FromGlobalAccessCache_nNvoke_ReturnsFalse()
        {
            Assert.False(RuntimeEnvironment.FromGlobalAccessCache(typeof(RuntimeEnvironmentTests).Assembly));
            Assert.False(RuntimeEnvironment.FromGlobalAccessCache(null));
        }
    }
}
