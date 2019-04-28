// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Http;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Http.Tests
{
    public class SystemProxyInfoTest
    {
        // This will clean specific environmental variables
        // to be sure they do not interfere with the test.
        private void CleanEnv()
        {
            var envVars = new List<string> { "http_proxy", "HTTP_PROXY",
                                             "https_proxy", "HTTPS_PROXY",
                                             "all_proxy", "ALL_PROXY",
                                             "no_proxy", "NO_PROXY",
                                             "GATEWAY_INTERFACE" };

            foreach (string v in envVars)
            {
                Environment.SetEnvironmentVariable(v, null);
            }
        }

        public SystemProxyInfoTest()
        {
            CleanEnv();
        }

        [Fact]
        public void Ctor_NoEnvironmentVariables_NotHttpEnvironmentProxy()
        {
            RemoteExecutor.Invoke(() =>
            {
                IWebProxy proxy = SystemProxyInfo.ConstructSystemProxy();
                HttpEnvironmentProxy envProxy = proxy as HttpEnvironmentProxy;
                Assert.Null(envProxy);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void Ctor_ProxyEnvironmentVariableSet_IsHttpEnvironmentProxy()
        {
            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables.Add("http_proxy", "http://proxy.contoso.com");
            RemoteExecutor.Invoke(() =>
            {
                IWebProxy proxy = SystemProxyInfo.ConstructSystemProxy();
                HttpEnvironmentProxy envProxy = proxy as HttpEnvironmentProxy;
                Assert.NotNull(envProxy);

                return RemoteExecutor.SuccessExitCode;
            }, options).Dispose();
        }
    }
}
