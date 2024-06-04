// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class FeatureGuardAttributeTests
    {
        [Fact]
        public void TestConstructor()
        {
            var attr = new FeatureGuardAttribute(typeof(RequiresUnreferencedCodeAttribute));
            Assert.Equal(typeof(RequiresUnreferencedCodeAttribute), attr.FeatureType);
        }
    }
}
