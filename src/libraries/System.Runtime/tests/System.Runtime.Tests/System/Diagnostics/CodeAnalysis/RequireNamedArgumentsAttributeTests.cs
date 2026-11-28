// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class RequireNamedArgumentsAttributeTests
    {
        [Fact]
        public void TestConstructor()
        {
            new RequireNamedArgumentsAttribute();
        }

        [Fact]
        public void TestAttributeUsage()
        {
            AttributeUsageAttribute? usage = typeof(RequireNamedArgumentsAttribute).GetCustomAttribute<AttributeUsageAttribute>();

            Assert.NotNull(usage);
            Assert.Equal(
                AttributeTargets.Method |
                AttributeTargets.Constructor |
                AttributeTargets.Property |
                AttributeTargets.Delegate |
                AttributeTargets.Parameter,
                usage.ValidOn);
            Assert.False(usage.AllowMultiple);
            Assert.False(usage.Inherited);
        }
    }
}
