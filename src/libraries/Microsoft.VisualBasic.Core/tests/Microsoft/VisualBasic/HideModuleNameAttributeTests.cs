// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class HideModuleNameAttributeTests
    {
        [Fact]
        public void Ctor_Empty_Success()
        {
            new HideModuleNameAttribute();
        }
    }
}
