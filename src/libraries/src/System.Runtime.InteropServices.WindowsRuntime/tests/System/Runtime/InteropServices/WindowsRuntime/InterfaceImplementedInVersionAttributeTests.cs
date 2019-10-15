// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.WindowsRuntime.Tests
{
    public class InterfaceImplementedInVersionAttributeTests
    {
        [Theory]
        [InlineData(null, 0, 0, 0, 0)]
        [InlineData(typeof(int), 255, 255, 255, 255)]
        public void Ctor_DefaultInterface(Type interfaceType, byte majorVersion, byte minorVersion, byte buildVersion, byte revisionVersion)
        {
            var attribute = new InterfaceImplementedInVersionAttribute(interfaceType, majorVersion, minorVersion, buildVersion, revisionVersion);
            Assert.Equal(interfaceType, attribute.InterfaceType);
            Assert.Equal(majorVersion, attribute.MajorVersion);
            Assert.Equal(minorVersion, attribute.MinorVersion);
            Assert.Equal(buildVersion, attribute.BuildVersion);
            Assert.Equal(revisionVersion, attribute.RevisionVersion);
        }
    }
}
