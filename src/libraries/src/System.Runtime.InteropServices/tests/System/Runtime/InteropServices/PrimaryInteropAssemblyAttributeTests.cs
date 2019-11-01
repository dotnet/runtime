// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

[assembly: PrimaryInteropAssembly(1, 2)]

namespace System.Runtime.InteropServices.Tests
{
    public class PrimaryInteropAssemblyAttributeTests
    {
        [Fact]
        public void Exists()
        {
            Type type = typeof(PrimaryInteropAssemblyAttributeTests);
            Assembly assembly = type.GetTypeInfo().Assembly;
            PrimaryInteropAssemblyAttribute attribute = Assert.Single(assembly.GetCustomAttributes<PrimaryInteropAssemblyAttribute>());
            Assert.Equal(1, attribute.MajorVersion);
            Assert.Equal(2, attribute.MinorVersion);
        }

        [Theory]
        [InlineData(-1, -2)]
        [InlineData(0, 0)]
        [InlineData(1, 2)]
        public void Ctor_MajorVersion_MinorVersion(int major, int minor)
        {
            var attribute = new PrimaryInteropAssemblyAttribute(major, minor);
            Assert.Equal(major, attribute.MajorVersion);
            Assert.Equal(minor, attribute.MinorVersion);
        }
    }
}
