// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    [Collection(nameof(QuicTestCollection))]
    [ConditionalClass(typeof(QuicTestBase), nameof(QuicTestBase.IsSupported), nameof(QuicTestBase.IsNotArm32CoreClrStressTest))]
    public class MsQuicRemoteExecutorTests : QuicTestBase
    {
        public MsQuicRemoteExecutorTests()
            : base(null!) { }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SslKeyLogFile_IsCreatedAndFilled()
        {
            if (PlatformDetection.IsReleaseLibrary(typeof(QuicConnection).Assembly))
            {
                throw new SkipTestException("Retrieving SSL secrets is not supported in Release mode.");
            }

            var psi = new ProcessStartInfo();
            var tempFile = Path.GetTempFileName();
            psi.Environment.Add("SSLKEYLOGFILE", tempFile);

            RemoteExecutor.Invoke(async () =>
            {
                (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();
                await clientConnection.DisposeAsync();
                await serverConnection.DisposeAsync();
            }, new RemoteInvokeOptions { StartInfo = psi }).Dispose();

            Assert.True(File.Exists(tempFile));
            Assert.True(File.ReadAllText(tempFile).Length > 0);
        }
    }
}
