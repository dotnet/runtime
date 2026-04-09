// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static class ParamCollectionAttributeTests
    {
        [Fact]
        public static void Ctor()
        {
            var attribute = new ParamCollectionAttribute();
        }
    }
}
