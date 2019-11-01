// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.PrivateUri.Tests
{
    public class AppxUriValues
    {
        [Fact]
        public static void SupportStringsOfForm_MsApp()
        {
            var uri = new Uri("ms-app://s-1-15-2-123456789-1234567890-1234567890-098765432-123456789-0987765432-0987654321");
            Assert.NotNull(uri);
        }
    }
}
