// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Tests
{
    public class HttpEnvironmentProxyTest
    {
        private readonly ITestOutputHelper _output;
        private static readonly Uri fooHttp = new Uri("http://foo.com");
        private static readonly Uri fooHttps = new Uri("https://foo.com");
        private static readonly Uri fooWs = new Uri("ws://foo.com");
        private static readonly Uri fooWss = new Uri("wss://foo.com");

        // This will clean specific environmental variables
        // to be sure they do not interfere with the test.
        private void CleanEnv()
        {
            var envVars = new List<string>() { "http_proxy", "HTTP_PROXY",
                                               "https_proxy", "HTTPS_PROXY",
                                               "all_proxy", "ALL_PROXY",
                                               "no_proxy", "NO_PROXY",
                                               "GATEWAY_INTERFACE" };

            foreach (string v in envVars)
            {
                Environment.SetEnvironmentVariable(v, null);
            }
        }

        public HttpEnvironmentProxyTest(ITestOutputHelper output)
        {
            _output = output;
            CleanEnv();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void HttpProxy_EnvironmentProxy_Loaded()
        {
            RemoteExecutor.Invoke(() =>
            {

                IWebProxy p;
                Uri u;

                // It should not return object if there are no variables set.
                Assert.False(HttpEnvironmentProxy.TryCreate(out p));

                Environment.SetEnvironmentVariable("all_proxy", "http://1.1.1.1:3000");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);
                Assert.Null(p.Credentials);

                u = p.GetProxy(fooHttp);
                Assert.True(u != null && u.Host == "1.1.1.1");
                u = p.GetProxy(fooHttps);
                Assert.True(u != null && u.Host == "1.1.1.1");

                u = p.GetProxy(fooWs);
                Assert.True(u != null && u.Host == "1.1.1.1");
                u = p.GetProxy(fooWss);
                Assert.True(u != null && u.Host == "1.1.1.1");

                Environment.SetEnvironmentVariable("http_proxy", "http://1.1.1.2:3001");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);

                // Protocol specific variables should take precedence over all_
                // and https should still use all_proxy.
                u = p.GetProxy(fooHttp);
                Assert.True(u != null && u.Host == "1.1.1.2" && u.Port == 3001);
                u = p.GetProxy(fooHttps);
                Assert.True(u != null && u.Host == "1.1.1.1" && u.Port == 3000);

                u = p.GetProxy(fooWs);
                Assert.True(u != null && u.Host == "1.1.1.2" && u.Port == 3001);
                u = p.GetProxy(fooWss);
                Assert.True(u != null && u.Host == "1.1.1.1" && u.Port == 3000);

                // Set https to invalid strings and use only IP & port for http.
                Environment.SetEnvironmentVariable("http_proxy", "1.1.1.3:3003");
                Environment.SetEnvironmentVariable("https_proxy", "ab!cd");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);

                u = p.GetProxy(fooHttp);
                Assert.True(u != null && u.Host == "1.1.1.3" && u.Port == 3003);
                u = p.GetProxy(fooHttps);
                Assert.True(u != null && u.Host == "1.1.1.1" && u.Port == 3000);

                u = p.GetProxy(fooWs);
                Assert.True(u != null && u.Host == "1.1.1.3" && u.Port == 3003);
                u = p.GetProxy(fooWss);
                Assert.True(u != null && u.Host == "1.1.1.1" && u.Port == 3000);

                // Try valid URI with unsupported protocol. It will be ignored
                // to mimic curl behavior.
                Environment.SetEnvironmentVariable("https_proxy", "socks5://1.1.1.4:3004");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);
                u = p.GetProxy(fooHttps);
                Assert.True(u != null && u.Host == "1.1.1.1" && u.Port == 3000);

                u = p.GetProxy(fooWss);
                Assert.True(u != null && u.Host == "1.1.1.1" && u.Port == 3000);

                // Set https to valid URI but different from http.
                Environment.SetEnvironmentVariable("https_proxy", "http://1.1.1.5:3005");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);

                u = p.GetProxy(fooHttp);
                Assert.True(u != null && u.Host == "1.1.1.3" && u.Port == 3003);
                u = p.GetProxy(fooHttps);
                Assert.True(u != null && u.Host == "1.1.1.5" && u.Port == 3005);

                u = p.GetProxy(fooWs);
                Assert.True(u != null && u.Host == "1.1.1.3" && u.Port == 3003);
                u = p.GetProxy(fooWss);
                Assert.True(u != null && u.Host == "1.1.1.5" && u.Port == 3005);
            }).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("1.1.1.5", "1.1.1.5", "80", null, null)]
        [InlineData("http://1.1.1.5:3005", "1.1.1.5", "3005", null, null)]
        [InlineData("http://foo@1.1.1.5", "1.1.1.5", "80", "foo", "")]
        [InlineData("http://[::1]:80", "[::1]", "80", null, null)]
        [InlineData("foo:PLACEHOLDER@[::1]:3128", "[::1]", "3128", "foo", "PLACEHOLDER")]
        [InlineData("foo:Pass$!#\\.$@127.0.0.1:3128", "127.0.0.1", "3128", "foo", "Pass$!#\\.$")]
        [InlineData("[::1]", "[::1]", "80", null, null)]
        [InlineData("domain\\foo:PLACEHOLDER@1.1.1.1", "1.1.1.1", "80", "foo", "PLACEHOLDER")]
        [InlineData("domain%5Cfoo:PLACEHOLDER@1.1.1.1", "1.1.1.1", "80", "foo", "PLACEHOLDER")]
        [InlineData("HTTP://ABC.COM/", "abc.com", "80", null, null)]
        [InlineData("http://10.30.62.64:7890/", "10.30.62.64", "7890", null, null)]
        [InlineData("http://1.2.3.4:8888/foo", "1.2.3.4", "8888", null, null)]
        public void HttpProxy_Uri_Parsing(string _input, string _host, string _port, string _user, string _password)
        {
            RemoteExecutor.Invoke((input, host, port, user, password) =>
            {
                // Remote exec does not allow to pass null at this moment.
                if (user == "null")
                {
                    user = null;
                }
                if (password == "null")
                {
                    password = null;
                }

                Environment.SetEnvironmentVariable("all_proxy", input);
                IWebProxy p;
                Uri u;

                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);

                u = p.GetProxy(fooHttp);
                Assert.Equal(host, u.Host);
                Assert.Equal(Convert.ToInt32(port), u.Port);

                if (user != null)
                {
                    NetworkCredential nc = p.Credentials.GetCredential(u, "Basic");
                    Assert.NotNull(nc);
                    Assert.Equal(user, nc.UserName);
                    Assert.Equal(password, nc.Password);
                }

                return RemoteExecutor.SuccessExitCode;
            }, _input, _host, _port, _user ?? "null", _password ?? "null").Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void HttpProxy_CredentialParsing_Basic()
        {
            RemoteExecutor.Invoke(() =>
            {
                IWebProxy p;

                Environment.SetEnvironmentVariable("all_proxy", "http://foo:PLACEHOLDER@1.1.1.1:3000");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);
                Assert.NotNull(p.Credentials);

                // Use user only without password.
                Environment.SetEnvironmentVariable("all_proxy", "http://foo@1.1.1.1:3000");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);
                Assert.NotNull(p.Credentials);

                // Use different user for http and https
                Environment.SetEnvironmentVariable("https_proxy", "http://foo1:PLACEHOLDER1@1.1.1.1:3000");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);
                Uri u = p.GetProxy(fooHttp);
                Assert.NotNull(p.Credentials.GetCredential(u, "Basic"));
                u = p.GetProxy(fooHttps);
                Assert.NotNull(p.Credentials.GetCredential(u, "Basic"));
                // This should not match Proxy Uri
                Assert.Null(p.Credentials.GetCredential(fooHttp, "Basic"));
                Assert.Null(p.Credentials.GetCredential(null, null));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void HttpProxy_Exceptions_Match()
        {
            RemoteExecutor.Invoke(() =>
            {
                IWebProxy p;

                Environment.SetEnvironmentVariable("no_proxy", ".test.com,, foo.com");
                Environment.SetEnvironmentVariable("all_proxy", "http://foo:PLACEHOLDER@1.1.1.1:3000");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);

                Assert.True(p.IsBypassed(fooHttp));
                Assert.True(p.IsBypassed(fooHttps));
                Assert.True(p.IsBypassed(new Uri("http://test.com")));
                Assert.False(p.IsBypassed(new Uri("http://1test.com")));
                Assert.True(p.IsBypassed(new Uri("http://www.test.com")));
            }).Dispose();
        }

        public static IEnumerable<object[]> HttpProxyNoProxyEnvVarMemberData()
        {
            yield return new object[] { "http_proxy", "no_proxy" };
            yield return new object[] { "http_proxy", "NO_PROXY" };
            yield return new object[] { "HTTP_PROXY", "no_proxy" };
            yield return new object[] { "HTTP_PROXY", "NO_PROXY" };
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(HttpProxyNoProxyEnvVarMemberData))]
        public void HttpProxy_TryCreate_CaseInsensitiveVariables(string proxyEnvVar, string noProxyEnvVar)
        {
            string proxy = "http://foo:PLACEHOLDER@1.1.1.1:3000";

            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables.Add(proxyEnvVar, proxy);
            options.StartInfo.EnvironmentVariables.Add(noProxyEnvVar, ".test.com, foo.com");
            RemoteExecutor.Invoke((proxy) =>
            {
                var directUri = new Uri("http://test.com");
                var thruProxyUri = new Uri("http://atest.com");

                Assert.True(HttpEnvironmentProxy.TryCreate(out IWebProxy p));
                Assert.NotNull(p);

                Assert.True(p.IsBypassed(directUri));
                Assert.False(p.IsBypassed(thruProxyUri));
                Assert.Equal(new Uri(proxy), p.GetProxy(thruProxyUri));
            }, proxy, options).Dispose();
        }

        public static IEnumerable<object[]> HttpProxyCgiEnvVarMemberData()
        {
            foreach (bool cgi in new object[] { false, true })
            {
                yield return new object[] { "http_proxy", cgi, !cgi || !PlatformDetection.IsWindows };
                yield return new object[] { "HTTP_PROXY", cgi, !cgi };
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(HttpProxyCgiEnvVarMemberData))]
        public void HttpProxy_TryCreateAndPossibleCgi_HttpProxyUpperCaseDisabledInCgi(
            string proxyEnvVar, bool cgi, bool expectedProxyUse)
        {
            string proxy = "http://foo:PLACEHOLDER@1.1.1.1:3000";

            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables.Add(proxyEnvVar, proxy);
            if (cgi)
            {
                options.StartInfo.EnvironmentVariables.Add("GATEWAY_INTERFACE", "CGI/1.1");
            }

            RemoteExecutor.Invoke((proxy, expectedProxyUseString) =>
            {
                bool expectedProxyUse = bool.Parse(expectedProxyUseString);
                var destinationUri = new Uri("http://test.com");

                bool created = HttpEnvironmentProxy.TryCreate(out IWebProxy p);
                if (expectedProxyUse)
                {
                    Assert.True(created);
                    Assert.NotNull(p);
                    Assert.Equal(new Uri(proxy), p.GetProxy(destinationUri));
                }
                else
                {
                    Assert.False(created);
                }
            }, proxy, expectedProxyUse.ToString(), options).Dispose();
        }
    }
}
