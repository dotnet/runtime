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

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SslKeyLogFile_IsCreatedAndFilled(bool enabledBySwitch)
        {
            if (PlatformDetection.IsDebugLibrary(typeof(QuicConnection).Assembly) && !enabledBySwitch)
            {
                // AppCtxSwitch is not checked for SSLKEYLOGFILE in Debug builds, the same code path
                // will be tested by the enabledBySwitch = true case. Skip it here.
                return;
            }

            var psi = new ProcessStartInfo();
            var tempFile = Path.GetTempFileName();
            psi.Environment.Add("SSLKEYLOGFILE", tempFile);

            await RemoteExecutor.Invoke(async (enabledBySwitch) =>
            {
                if (bool.Parse(enabledBySwitch))
                {
                    AppContext.SetSwitch("System.Net.EnableSslKeyLogging", true);
                }

                (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection();
                await clientConnection.DisposeAsync();
                await serverConnection.DisposeAsync();
            }
            , enabledBySwitch.ToString(), new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();

            if (enabledBySwitch)
            {
                Assert.True(File.ReadAllText(tempFile).Length > 0);
            }
            else
            {
                Assert.True(File.ReadAllText(tempFile).Length == 0);
            }
        }
    }
}
