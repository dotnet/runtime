// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class UnconditionalSuppressMessageAttributeTests
    {
        [Theory]
        [InlineData("Category", "CheckId", "Justification", "MessageId", "Scope", "Target")]
        [InlineData("", "", "", "", "", "")]
        [InlineData(null, null, null, null, null, null)]
        [InlineData("", null, "Justification", null, "Scope", "")]
        public void TestConstructor(string category, string id, string justification, string messageId, string scope, string target)
        {
            var usma = new UnconditionalSuppressMessageAttribute(category, id)
            {
                Justification = justification,
                MessageId = messageId,
                Scope = scope,
                Target = target
            };

            Assert.Equal(category, usma.Category);
            Assert.Equal(id, usma.CheckId);
            Assert.Equal(justification, usma.Justification);
            Assert.Equal(messageId, usma.MessageId);
            Assert.Equal(scope, usma.Scope);
            Assert.Equal(target, usma.Target);
        }
    }
}
