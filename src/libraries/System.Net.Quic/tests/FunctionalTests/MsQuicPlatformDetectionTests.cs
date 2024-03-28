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
        public async Task UnsupportedPlatforms_ThrowsPlatformNotSupportedException()
        {
            PlatformNotSupportedException listenerEx = await Assert.ThrowsAsync<PlatformNotSupportedException>(async () => await CreateQuicListener());
            PlatformNotSupportedException connectionEx = await Assert.ThrowsAsync<PlatformNotSupportedException>(async () => await CreateQuicConnection(new IPEndPoint(IPAddress.Loopback, 0)));
            Assert.Equal(listenerEx.Message, connectionEx.Message);
            _output.WriteLine(listenerEx.Message);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/73290", typeof(PlatformDetection), nameof(PlatformDetection.IsSingleFile))]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls13))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SupportedWindowsPlatforms_IsSupportedIsTrue()
        {
            Assert.True(QuicListener.IsSupported);
            Assert.True(QuicConnection.IsSupported);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task SupportedLinuxPlatformsWithMsQuic_IsSupportedIsTrue()
        {
            using Process find = new Process();
            find.StartInfo.FileName = "find";
            find.StartInfo.Arguments = "/usr/ -iname libmsquic.so*";
            find.StartInfo.RedirectStandardOutput = true;
            find.StartInfo.RedirectStandardError = true;
            find.Start();
            string output = await find.StandardOutput.ReadToEndAsync();
            _output.WriteLine(output);
            await find.WaitForExitAsync();
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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/82154", typeof(PlatformDetection), nameof(PlatformDetection.IsRaspbian10), nameof(PlatformDetection.IsArmv6Process), nameof(PlatformDetection.IsInContainer))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/82154", typeof(PlatformDetection), nameof(PlatformDetection.IsPpc64leProcess))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/82154", typeof(PlatformDetection), nameof(PlatformDetection.IsUbuntu2004), nameof(PlatformDetection.IsS390xProcess))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/91757", typeof(PlatformDetection), nameof(PlatformDetection.IsAlpine), nameof(PlatformDetection.IsArmProcess))]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsInHelix))]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void SupportedLinuxPlatforms_IsSupportedIsTrue()
        {
            _output.WriteLine($"Running on {PlatformDetection.GetDistroVersionString()}");
            Assert.True(QuicListener.IsSupported);
            Assert.True(QuicConnection.IsSupported);
        }
    }
}
