// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Net.Security;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public class MsQuicPlatformDetectionTests : QuicTestBase
    {
        public MsQuicPlatformDetectionTests(ITestOutputHelper output) : base(output) { }

        public static bool IsQuicUnsupported => !IsSupported;

        [ConditionalFact(nameof(IsQuicUnsupported))]
        public void UnsupportedPlatforms_ThrowsPlatformNotSupportedException()
        {
            Assert.ThrowsAsync<PlatformNotSupportedException>(async () => await CreateQuicListener());
            Assert.ThrowsAsync<PlatformNotSupportedException>(async () => await CreateQuicConnection(new IPEndPoint(IPAddress.Loopback, 0)));
        }
    }
}
