// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class StandardModuleAttributeTests
    {
        [Fact]
        public void Ctor_Empty_Success()
        {
            new StandardModuleAttribute();
        }
    }
}
