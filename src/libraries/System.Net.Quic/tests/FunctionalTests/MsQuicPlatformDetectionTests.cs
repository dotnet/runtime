// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.SupportsTls13))]
        public void SupportedWindowsPlatforms_IsSupportedIsTrue()
        {
            if (PlatformDetection.HasAssemblyFiles)
            {
                Assert.True(QuicListener.IsSupported);
                Assert.True(QuicConnection.IsSupported);
            }
            else
            {
                // The above if check can be deleted when https://github.com/dotnet/runtime/issues/73290
                // gets fixed and this test starts failing.
                Assert.False(QuicListener.IsSupported);
                Assert.False(QuicConnection.IsSupported);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsLinux))]
        public async Task SupportedLinuxPlatforms_IsSupportedIsTrue()
        {
            using Process ldconfig = new Process();
            ldconfig.StartInfo.FileName = "ldconfig";
            ldconfig.StartInfo.Arguments = "-p";
            ldconfig.StartInfo.RedirectStandardOutput = true;
            ldconfig.Start();
            string output = await ldconfig.StandardOutput.ReadToEndAsync();
            await ldconfig.WaitForExitAsync();
            if (output.Contains("libmsquic.so"))
            {
                Assert.True(QuicListener.IsSupported);
                Assert.True(QuicConnection.IsSupported);
            }
            else
            {
                _output.WriteLine("No msquic library found.");
            }
        }
    }
}
