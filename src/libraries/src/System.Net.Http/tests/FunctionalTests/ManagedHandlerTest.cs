// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;

// NOTE:
// Currently the managed handler is opt-in on both Windows and Unix, due to still being a nascent implementation
// that's missing features, robustness, perf, etc.  One opts into it currently by setting an environment variable,
// which makes it a bit difficult to test.  There are two straightforward ways to test it:
// - This file contains test classes that derive from the other test classes in the project that create
//   HttpClient{Handler} instances, and in the ctor sets the env var and in Dispose removes the env var.
//   That has the effect of running all of those same tests again, but with the managed handler enabled.
// - By setting the env var prior to running tests, every test will implicitly use the managed handler,
//   at which point the tests in this file are duplicative and can be commented out.

// For now parallelism is disabled because we use an env var to turn on the managed handler, and the env var
// impacts any tests running concurrently in the process.  We can remove this restriction in the future once
// plans around the ManagedHandler are better understood.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]

namespace System.Net.Http.Functional.Tests
{
    public sealed class ManagedHandler_HttpClientTest : HttpClientTest, IDisposable
    {
        public ManagedHandler_HttpClientTest() => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    public sealed class ManagedHandler_DiagnosticsTest : DiagnosticsTest, IDisposable
    {
        public ManagedHandler_DiagnosticsTest() => ManagedHandlerTestHelpers.SetEnvVar();
        public new void Dispose()
        {
            ManagedHandlerTestHelpers.RemoveEnvVar();
            base.Dispose();
        }
    }

    public sealed class ManagedHandler_HttpClientEKUTest : HttpClientEKUTest, IDisposable
    {
        public ManagedHandler_HttpClientEKUTest() => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    public sealed class ManagedHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test : HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test, IDisposable
    {
        public ManagedHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test() => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    //public sealed class ManagedHandler_HttpClientHandler_ClientCertificates_Test : HttpClientHandler_ClientCertificates_Test, IDisposable
    //{
    //    public ManagedHandler_HttpClientHandler_ClientCertificates_Test() => ManagedHandlerTestHelpers.SetEnvVar();
    //    public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    //}

    public sealed class ManagedHandler_HttpClientHandler_DefaultProxyCredentials_Test : HttpClientHandler_DefaultProxyCredentials_Test, IDisposable
    {
        public ManagedHandler_HttpClientHandler_DefaultProxyCredentials_Test() => ManagedHandlerTestHelpers.SetEnvVar();
        public new void Dispose()
        {
            ManagedHandlerTestHelpers.RemoveEnvVar();
            base.Dispose();
        }
    }

    public sealed class ManagedHandler_HttpClientHandler_MaxConnectionsPerServer_Test : HttpClientHandler_MaxConnectionsPerServer_Test, IDisposable
    {
        public ManagedHandler_HttpClientHandler_MaxConnectionsPerServer_Test() => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    //public sealed class ManagedHandler_HttpClientHandler_ServerCertificates_Test : HttpClientHandler_ServerCertificates_Test, IDisposable
    //{
    //    public ManagedHandler_HttpClientHandler_ServerCertificates_Test() => ManagedHandlerTestHelpers.SetEnvVar();
    //    public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    //}

    public sealed class ManagedHandler_PostScenarioTest : PostScenarioTest, IDisposable
    {
        public ManagedHandler_PostScenarioTest(ITestOutputHelper output) : base(output) => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    public sealed class ManagedHandler_ResponseStreamTest : ResponseStreamTest, IDisposable
    {
        public ManagedHandler_ResponseStreamTest(ITestOutputHelper output) : base(output) => ManagedHandlerTestHelpers.SetEnvVar();
        public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    }

    // TODO #21452: Uncomment once tests are fixed

    //public sealed class ManagedHandler_HttpClientHandler_SslProtocols_Test : HttpClientHandler_SslProtocols_Test, IDisposable
    //{
    //    public ManagedHandler_HttpClientHandler_SslProtocols_Test() => ManagedHandlerTestHelpers.SetEnvVar();
    //    public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    //}

    //public sealed class ManagedHandler_SchSendAuxRecordHttpTest : SchSendAuxRecordHttpTest, IDisposable
    //{
    //    public ManagedHandler_SchSendAuxRecordHttpTest(ITestOutputHelper output) : base(output) => ManagedHandlerTestHelpers.SetEnvVar();
    //    public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    //}

    //public sealed class ManagedHandler_CancellationTest : CancellationTest, IDisposable
    //{
    //    public ManagedHandler_CancellationTest(ITestOutputHelper output) : base(output) => ManagedHandlerTestHelpers.SetEnvVar();
    //    public void Dispose() => ManagedHandlerTestHelpers.RemoveEnvVar();
    //}

    //public sealed class ManagedHandler_HttpClientHandler_MaxResponseHeadersLength_Test : HttpClientHandler_MaxResponseHeadersLength_Test, IDisposable
    //{
    //    public ManagedHandler_HttpClientHandler_MaxResponseHeadersLength_Test() => ManagedHandlerTestHelpers.SetEnvVar();
    //    public new void Dispose()
    //    {
    //        ManagedHandlerTestHelpers.RemoveEnvVar();
    //        base.Dispose();
    //    }
    //}
}
