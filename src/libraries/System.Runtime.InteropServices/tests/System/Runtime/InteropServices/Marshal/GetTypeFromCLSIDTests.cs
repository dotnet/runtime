// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GetTypeFromCLSIDTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypeFromCLSID_NoSuchCLSIDExists_ReturnsExpected()
        {
            Type type = Marshal.GetTypeFromCLSID(Guid.Empty);
            Assert.NotNull(type);
            Assert.Same(type, Marshal.GetTypeFromCLSID(Guid.Empty));

            Assert.Throws<COMException>(() => Activator.CreateInstance(type));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void GetTypeFromCLSID_CLSIDExists_ReturnsExpected()
        {
            var guid = new Guid("927971f5-0939-11d1-8be1-00c04fd8d503");

            Type type = Marshal.GetTypeFromCLSID(guid);
            Assert.NotNull(type);
            Assert.Same(type, Marshal.GetTypeFromCLSID(guid));

            Assert.NotNull(Activator.CreateInstance(type));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetTypeFromCLSID_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetTypeFromCLSID(Guid.Empty));
        }
    }
}
