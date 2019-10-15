// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class DirectoryConnectionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var connection = new SubDirectoryConnection();
            Assert.Empty(connection.ClientCertificates);
            Assert.Null(connection.Directory);
            Assert.Equal(new TimeSpan(0, 0, 30), connection.Timeout);
        }

        [Fact]
        public void Timeout_SetValid_GetReturnsExpected()
        {
            var connection = new SubDirectoryConnection { Timeout = TimeSpan.Zero };
            Assert.Equal(TimeSpan.Zero, connection.Timeout);
        }

        [Fact]
        public void Timeout_SetNegative_ThrowsArgumentException()
        {
            var connection = new SubDirectoryConnection();
            AssertExtensions.Throws<ArgumentException>("value", () => connection.Timeout = TimeSpan.FromTicks(-1));
        }

        [Fact]
        public void Credential_Set_Success()
        {
            var connection = new SubDirectoryConnection { Credential = new NetworkCredential("username", "password") };
            connection.Credential = null;
        }

        public class SubDirectoryConnection : DirectoryConnection
        {
            public override DirectoryResponse SendRequest(DirectoryRequest request) => throw new NotImplementedException();
        }
    }
}
