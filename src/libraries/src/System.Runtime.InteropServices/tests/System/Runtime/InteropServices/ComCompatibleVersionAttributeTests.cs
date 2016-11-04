// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

[assembly: ComCompatibleVersion(1, 2, 3, 4)]

namespace System.Runtime.InteropServices.Tests
{
    public class ComCompatibleVersionAttributeTests
    {
        [Fact]
        public void Exists()
        {
            var type = typeof(ComCompatibleVersionAttributeTests);
            var assembly = type.GetTypeInfo().Assembly;
            var attr = assembly.GetCustomAttributes(typeof(ComCompatibleVersionAttribute), false).OfType<ComCompatibleVersionAttribute>().SingleOrDefault();
            Assert.NotNull(attr);
            Assert.Equal(1, attr.MajorVersion);
            Assert.Equal(2, attr.MinorVersion);
            Assert.Equal(3, attr.BuildNumber);
            Assert.Equal(4, attr.RevisionNumber);
        }
    }
}
