// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
