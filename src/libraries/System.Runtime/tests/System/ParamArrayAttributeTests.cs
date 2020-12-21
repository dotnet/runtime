// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Tests
{
    public static class ParamArrayAttributeTests
    {
        [Fact]
        public static void Ctor()
        {
            var attribute = new ParamArrayAttribute();
        }
    }
}
