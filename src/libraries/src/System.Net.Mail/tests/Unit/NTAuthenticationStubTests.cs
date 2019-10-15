// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Mail.Tests
{
    public class NTAuthenticationStubTests
    {
        [Fact]
        public void TestReflectedTypes()
        {
            Assert.NotNull(NTAuthentication.s_type);
            Assert.NotNull(NTAuthentication.s_ntAuthentication);
            Assert.NotNull(NTAuthentication.s_getOutgoingBlogString);
            Assert.NotNull(NTAuthentication.s_getOutgoingBlobBytes);
            Assert.NotNull(NTAuthentication.s_verifySignature);
            Assert.NotNull(NTAuthentication.s_makeSignature);
            Assert.NotNull(NTAuthentication.s_closeContext);
            Assert.NotNull(NTAuthentication.s_isCompleted);
        }
    }
}
