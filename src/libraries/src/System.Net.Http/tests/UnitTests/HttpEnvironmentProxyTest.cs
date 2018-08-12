// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Http;
using Xunit;
using Xunit.Abstractions;
using System.Diagnostics;

namespace System.Net.Http.Tests
{
    public class HttpEnvironmentProxyTest : RemoteExecutorTestBase
    {
        private readonly ITestOutputHelper _output;
        private static readonly Uri fooHttp = new Uri("http://foo.com");
        private static readonly Uri fooHttps = new Uri("https://foo.com");

        // This will clean specific environmental variables
        // to be sure they do not interfere with the test.
        private void CleanEnv()
        {
            List<string>  vars = new List<string>() { "http_proxy", "HTTPS_PROXY", "https_proxy",
                                                      "all_proxy", "ALL_PROXY",
                                                      "NO_PROXY" };
            foreach (string v in vars)
            {
                Environment.SetEnvironmentVariable(v, null);
            }
        }

        public HttpEnvironmentProxyTest(ITestOutputHelper output)
        {
            _output = output;
            CleanEnv();
        }

        [Fact]
        public void HttpProxy_EnvironmentProxy_Loaded()
        {
            RemoteInvoke(() =>
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

                Environment.SetEnvironmentVariable("http_proxy", "http://1.1.1.2:3001");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);

                // Protocol specific variables should take precedence over all_
                // and https should still use all_proxy.
                u = p.GetProxy(fooHttp);
                Assert.True(u != null && u.Host == "1.1.1.2" && u.Port == 3001);
                u = p.GetProxy(fooHttps);
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

                // Try valid URI with unsupported protocol. It will be ignored
                // to mimic curl behavior.
                Environment.SetEnvironmentVariable("https_proxy", "socks5://1.1.1.4:3004");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);
                u = p.GetProxy(fooHttps);
                Assert.True(u != null && u.Host == "1.1.1.1" && u.Port == 3000);

                // Set https to valid URI but different from http.
                Environment.SetEnvironmentVariable("https_proxy", "http://1.1.1.5:3005");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);

                u = p.GetProxy(fooHttp);
                Assert.True(u != null && u.Host == "1.1.1.3" && u.Port == 3003);
                u = p.GetProxy(fooHttps);
                Assert.True(u != null && u.Host == "1.1.1.5" && u.Port == 3005);

                return SuccessExitCode;
            }).Dispose();
        }

        [Theory]
        [InlineData("1.1.1.5", "1.1.1.5", "80", null, null)]
        [InlineData("http://1.1.1.5:3005", "1.1.1.5", "3005", null, null)]
        [InlineData("http://foo@1.1.1.5", "1.1.1.5", "80", "foo", "")]
        [InlineData("http://[::1]:80", "[::1]", "80", null, null)]
        [InlineData("foo:bar@[::1]:3128", "[::1]", "3128", "foo", "bar")]
        [InlineData("foo:Pass$!#\\.$@127.0.0.1:3128", "127.0.0.1", "3128", "foo", "Pass$!#\\.$")]
        [InlineData("[::1]", "[::1]", "80", null, null)]
        [InlineData("domain\\foo:bar@1.1.1.1", "1.1.1.1", "80", "foo", "bar")]
        [InlineData("domain%5Cfoo:bar@1.1.1.1", "1.1.1.1", "80", "foo", "bar")]
        [InlineData("HTTP://ABC.COM/", "abc.com", "80", null, null)]
        [InlineData("http://10.30.62.64:7890/", "10.30.62.64", "7890", null, null)]
        [InlineData("http://1.2.3.4:8888/foo", "1.2.3.4", "8888", null, null)]
        public void HttpProxy_Uri_Parsing(string _input, string _host, string _port, string _user , string _password)
        {
            RemoteInvoke((input, host, port, user, password) =>
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

                return SuccessExitCode;
            }, _input, _host, _port, _user ?? "null" , _password ?? "null").Dispose();
        }

        [Fact]
        public void HttpProxy_CredentialParsing_Basic()
        {
            RemoteInvoke(() =>
            {
                IWebProxy p;

                Environment.SetEnvironmentVariable("all_proxy", "http://foo:bar@1.1.1.1:3000");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);
                Assert.NotNull(p.Credentials);

                // Use user only without password.
                Environment.SetEnvironmentVariable("all_proxy", "http://foo@1.1.1.1:3000");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);
                Assert.NotNull(p.Credentials);

                // Use different user for http and https
                Environment.SetEnvironmentVariable("https_proxy", "http://foo1:bar1@1.1.1.1:3000");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);
                Uri u = p.GetProxy(fooHttp);
                Assert.NotNull(p.Credentials.GetCredential(u, "Basic"));
                u = p.GetProxy(fooHttps);
                Assert.NotNull(p.Credentials.GetCredential(u, "Basic"));
                // This should not match Proxy Uri
                Assert.Null(p.Credentials.GetCredential(fooHttp, "Basic"));
                Assert.Null(p.Credentials.GetCredential(null, null));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void HttpProxy_Exceptions_Match()
        {
            RemoteInvoke(() =>
            {
                IWebProxy p;

                Environment.SetEnvironmentVariable("no_proxy", ".test.com,, foo.com");
                Environment.SetEnvironmentVariable("all_proxy", "http://foo:bar@1.1.1.1:3000");
                Assert.True(HttpEnvironmentProxy.TryCreate(out p));
                Assert.NotNull(p);

                Assert.True(p.IsBypassed(fooHttp));
                Assert.True(p.IsBypassed(fooHttps));
                Assert.True(p.IsBypassed(new Uri("http://test.com")));
                Assert.False(p.IsBypassed(new Uri("http://1test.com")));
                Assert.True(p.IsBypassed(new Uri("http://www.test.com")));

                return SuccessExitCode;
           }).Dispose();
        }
    }
}
