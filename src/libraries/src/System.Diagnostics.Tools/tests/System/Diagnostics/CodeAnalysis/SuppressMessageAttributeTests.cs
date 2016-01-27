// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class SuppressMessageAttributeTests
    {
        [Theory]
        [InlineData("Category", "CheckId", "Justification", "MessageId", "Scope", "Target")]
        [InlineData("", "", "", "", "", "")]
        [InlineData(null, null, null, null, null, null)]
        [InlineData("", null, "Justification", null, "Scope", "")]
        public void TestConstructor(string category, string id, string justification, string messageId, string scope, string target)
        {
            SuppressMessageAttribute sma = new SuppressMessageAttribute(category, id)
            {
                Justification = justification,
                MessageId = messageId,
                Scope = scope,
                Target = target
            };

            Assert.Equal(category, sma.Category);
            Assert.Equal(id, sma.CheckId);
            Assert.Equal(justification, sma.Justification);
            Assert.Equal(messageId, sma.MessageId);
            Assert.Equal(scope, sma.Scope);
            Assert.Equal(target, sma.Target);
        }
    }
}
