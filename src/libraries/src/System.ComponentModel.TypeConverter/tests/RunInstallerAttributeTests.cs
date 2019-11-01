// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class RunInstallerAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool runInstaller)
        {
            var attribute = new RunInstallerAttribute(runInstaller);
            Assert.Equal(runInstaller, attribute.RunInstaller);
            Assert.Equal(!runInstaller, attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Default_Get_ReturnsExpected()
        {
            RunInstallerAttribute attribute = RunInstallerAttribute.Default;
            Assert.Same(attribute, RunInstallerAttribute.Default);
            Assert.Same(attribute, RunInstallerAttribute.No);
            Assert.False(attribute.RunInstaller);
            Assert.True(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void Yes_Get_ReturnsExpected()
        {
            RunInstallerAttribute attribute = RunInstallerAttribute.Yes;
            Assert.Same(attribute, RunInstallerAttribute.Yes);
            Assert.True(attribute.RunInstaller);
            Assert.False(attribute.IsDefaultAttribute());
        }

        [Fact]
        public void No_Get_ReturnsExpected()
        {
            RunInstallerAttribute attribute = RunInstallerAttribute.No;
            Assert.Same(attribute, RunInstallerAttribute.No);
            Assert.False(attribute.RunInstaller);
            Assert.True(attribute.IsDefaultAttribute());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new RunInstallerAttribute(true);

            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new RunInstallerAttribute(true), true };
            yield return new object[] { attribute, new RunInstallerAttribute(false), false };
            yield return new object[] { new RunInstallerAttribute(false), new RunInstallerAttribute(false), true };
            yield return new object[] { new RunInstallerAttribute(false), new RunInstallerAttribute(true), false };

            yield return new object[] { attribute, new object(), false };
            yield return new object[] { attribute, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(RunInstallerAttribute attribute, object other, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(other));
            if (other is RunInstallerAttribute)
            {
                Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
            }
        }
    }
}
