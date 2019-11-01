// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class InitializationEventAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("test name")]
        public void Ctor_EventName(string eventName)
        {
            var attribute = new InitializationEventAttribute(eventName);
            Assert.Equal(eventName, attribute.EventName);
        }
    }
}
