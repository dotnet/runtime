// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class DefaultMemberAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("configuration")]
        public void Ctor_String(string memberName)
        {
            var attribute = new DefaultMemberAttribute(memberName);
            Assert.Equal(memberName, attribute.MemberName);
        }
    }
}
