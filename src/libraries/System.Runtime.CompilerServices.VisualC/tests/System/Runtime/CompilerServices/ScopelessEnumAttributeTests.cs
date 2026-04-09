// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class EmptyAttributeTests
    {
        [Fact]
        public void EmptyAttributes_Ctor_Success()
        {
            new HasCopySemanticsAttribute();
            new NativeCppClassAttribute();
            new ScopelessEnumAttribute();
        }
    }
}
