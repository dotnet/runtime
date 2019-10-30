// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
