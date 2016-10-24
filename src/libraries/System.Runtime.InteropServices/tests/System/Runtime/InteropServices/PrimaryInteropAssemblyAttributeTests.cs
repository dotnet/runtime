// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
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
            var type = typeof(PrimaryInteropAssemblyAttributeTests);
            var assembly = type.GetTypeInfo().Assembly;
            var attr = assembly.GetCustomAttributes(typeof(PrimaryInteropAssemblyAttribute), false).OfType<PrimaryInteropAssemblyAttribute>().SingleOrDefault();
            Assert.NotNull(attr);
            Assert.Equal(1, attr.MajorVersion);
            Assert.Equal(2, attr.MinorVersion);
        }
    }
}
