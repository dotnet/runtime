// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Type type = typeof(ComCompatibleVersionAttributeTests);
            Assembly assembly = type.GetTypeInfo().Assembly;
            ComCompatibleVersionAttribute attribute = Assert.Single(assembly.GetCustomAttributes<ComCompatibleVersionAttribute>());
            Assert.Equal(1, attribute.MajorVersion);
            Assert.Equal(2, attribute.MinorVersion);
            Assert.Equal(3, attribute.BuildNumber);
            Assert.Equal(4, attribute.RevisionNumber);
        }

        [Theory]
        [InlineData(-1, -2, -3, -4)]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 2, 3, 4)]
        public void Ctor_Major_Minor_Build_Revision(int major, int minor, int build, int revision)
        {
            var attribute = new ComCompatibleVersionAttribute(major, minor, build, revision);
            Assert.Equal(major, attribute.MajorVersion);
            Assert.Equal(minor, attribute.MinorVersion);
            Assert.Equal(build, attribute.BuildNumber);
            Assert.Equal(revision, attribute.RevisionNumber);
        }
    }
}
