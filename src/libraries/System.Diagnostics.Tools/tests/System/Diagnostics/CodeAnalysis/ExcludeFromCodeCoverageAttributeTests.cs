// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class ExcludeFromCodeCoverageAttributeTests
    {
        [Fact]
        public void ExcludeFromCodeCoverageAttribute_Instantiate()
        {
            new ExcludeFromCodeCoverageAttribute();
        }
    }
}
