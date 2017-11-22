// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public partial class UnixDomainSocketTest
    {
        private readonly ITestOutputHelper _log;

        public UnixDomainSocketTest(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // CreateUnixDomainSocket should throw on Windows
        public void Socket_CreateUnixDomainSocket_Throws_OnWindows()
        {
            SocketException e = Assert.Throws<SocketException>(() => new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified));
            Assert.Equal(SocketError.AddressFamilyNotSupported, e.SocketErrorCode);
        }
    }
}
