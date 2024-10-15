// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class DynamicallyAccessedMembersAttributeTests
    {
        [Theory]
        [InlineData(DynamicallyAccessedMemberTypes.None)]
        [InlineData(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
        [InlineData(DynamicallyAccessedMemberTypes.All)]
        public void TestConstructor(DynamicallyAccessedMemberTypes memberTypes)
        {
            var dama = new DynamicallyAccessedMembersAttribute(memberTypes);

            Assert.Equal(memberTypes, dama.MemberTypes);
        }
    }
}
