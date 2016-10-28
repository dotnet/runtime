// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Xunit;

namespace System.Net.Tests
{
    public class GlobalProxySelectionTest : RemoteExecutorTestBase
    {
        private class MyWebProxy : IWebProxy
        {
            public MyWebProxy() { }

            public ICredentials Credentials
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public Uri GetProxy(Uri destination)
            {
                throw new NotImplementedException();
            }

            public bool IsBypassed(Uri host)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void Select_Success()
        {
            RemoteInvoke(() =>
            {
                var myProxy = new MyWebProxy();

                Assert.NotNull(GlobalProxySelection.Select);
                Assert.Equal(GlobalProxySelection.Select, WebRequest.DefaultWebProxy);

                WebRequest.DefaultWebProxy = myProxy;

                Assert.Equal(WebRequest.DefaultWebProxy, myProxy);
                Assert.Equal(GlobalProxySelection.Select, myProxy);

                // GlobalProxySelection will return an instance of the internal class EmptyWebProxy instead of null.
                WebRequest.DefaultWebProxy = null;

                Assert.Null(WebRequest.DefaultWebProxy);
                Assert.NotNull(GlobalProxySelection.Select);
                Assert.True(GlobalProxySelection.Select.IsBypassed(null)); // This is true for EmptyWebProxy, but not for most proxies

                GlobalProxySelection.Select = myProxy;

                Assert.Equal(WebRequest.DefaultWebProxy, myProxy);
                Assert.Equal(GlobalProxySelection.Select, myProxy);

                // GlobalProxySelection will return an instance of the internal class EmptyWebProxy instead of null.
                GlobalProxySelection.Select = null;

                Assert.Null(WebRequest.DefaultWebProxy);
                Assert.NotNull(GlobalProxySelection.Select);
                Assert.True(GlobalProxySelection.Select.IsBypassed(null)); // This is true for EmptyWebProxy, but not for most proxies

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void GetEmptyWebProxy_Success()
        {
            var empty1 = GlobalProxySelection.GetEmptyWebProxy();
            Assert.True(empty1.IsBypassed(null));
            Assert.Null(empty1.GetProxy(null));

            Uri someUri = new Uri("http://foo.com/bar");
            Assert.True(empty1.IsBypassed(someUri));
            Assert.Equal(someUri, empty1.GetProxy(someUri));

            Assert.Null(empty1.Credentials);

            var empty2 = GlobalProxySelection.GetEmptyWebProxy();
            Assert.NotEqual(empty1, empty2);    // new instance each time
        }
    }
}
