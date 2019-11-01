// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.Tests
{
    public class EmptyAttributeTests
    {
        [Fact]
        public void Ctor_Default_DoesNotThrow()
        {
            new DebuggerStepperBoundaryAttribute();
            new DebuggerHiddenAttribute();
            new DebuggerNonUserCodeAttribute();
            new DebuggerStepThroughAttribute();
        }
    }
}
