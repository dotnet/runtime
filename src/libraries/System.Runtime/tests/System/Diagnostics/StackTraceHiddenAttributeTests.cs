// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.Tests
{
    public static class StackTraceHiddenAttributeTests
    {
        [Fact]
        public static void Ctor()
        {
            new StackTraceHiddenAttribute();
        }
    }
}
