// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Tests
{
    public class GlobalProxySelectionTest
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
            RemoteExecutor.Invoke(() =>
            {
                var myProxy = new MyWebProxy();

#pragma warning disable 0618 //GlobalProxySelection is Deprecated.
                Assert.NotNull(GlobalProxySelection.Select);
                // On .NET Framework, the default value for Select property
                // is an internal WebRequest.WebProxyWrapper object which
                // works similarly to DefaultWebProxy but is not the same object.
                Assert.Equal(GlobalProxySelection.Select, WebRequest.DefaultWebProxy);
#pragma warning restore 0618

                WebRequest.DefaultWebProxy = myProxy;

                Assert.Equal(WebRequest.DefaultWebProxy, myProxy);
#pragma warning disable 0618 //GlobalProxySelection is Deprecated.
                Assert.Equal(GlobalProxySelection.Select, myProxy);
#pragma warning restore 0618

                // GlobalProxySelection will return an instance of the internal class EmptyWebProxy instead of null.
                WebRequest.DefaultWebProxy = null;

                Assert.Null(WebRequest.DefaultWebProxy);
#pragma warning disable 0618 //GlobalProxySelection is Deprecated.
                Assert.NotNull(GlobalProxySelection.Select);
                Assert.True(GlobalProxySelection.Select.IsBypassed(null)); // This is true for EmptyWebProxy, but not for most proxies

                GlobalProxySelection.Select = myProxy;
#pragma warning restore 0618

                Assert.Equal(WebRequest.DefaultWebProxy, myProxy);
#pragma warning disable 0618 //GlobalProxySelection is Deprecated.
                Assert.Equal(GlobalProxySelection.Select, myProxy);

                // GlobalProxySelection will return an instance of the internal class EmptyWebProxy instead of null.
                GlobalProxySelection.Select = null;
#pragma warning restore 0618

                Assert.Null(WebRequest.DefaultWebProxy);
#pragma warning disable 0618  //GlobalProxySelection is Deprecated.
                Assert.NotNull(GlobalProxySelection.Select);
                Assert.True(GlobalProxySelection.Select.IsBypassed(null)); // This is true for EmptyWebProxy, but not for most proxies
#pragma warning restore 0618

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void GetEmptyWebProxy_Success()
        {
#pragma warning disable 0618  //GlobalProxySelection is Deprecated.
            var empty1 = GlobalProxySelection.GetEmptyWebProxy();
#pragma warning restore 0618
            Assert.True(empty1.IsBypassed(null));
            Assert.Null(empty1.GetProxy(null));

            Uri someUri = new Uri("http://foo.com/bar");
            Assert.True(empty1.IsBypassed(someUri));
            Assert.Equal(someUri, empty1.GetProxy(someUri));

            Assert.Null(empty1.Credentials);

#pragma warning disable 0618  //GlobalProxySelection is Deprecated.
            var empty2 = GlobalProxySelection.GetEmptyWebProxy();
#pragma warning restore 0618
            Assert.NotEqual(empty1, empty2);    // new instance each time
        }
    }
}
