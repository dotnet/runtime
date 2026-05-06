// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.Serialization.Tests
{
    public class IgnoreDataMemberAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            // This ctor does nothing - make sure it does not throw.
            new IgnoreDataMemberAttribute();
        }
    }
}
