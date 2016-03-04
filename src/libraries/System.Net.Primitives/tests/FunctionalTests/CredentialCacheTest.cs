// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class CredentialCacheTest
    {
        private static readonly Uri uriPrefix1 = new Uri("http://microsoft:80");
        private static readonly Uri uriPrefix2 = new Uri("http://softmicro:80");

        private static readonly string host1 = "host1";
        private static readonly string host2 = "host2";

        private static readonly int port1 = 500;
        private static readonly int port2 = 700;

        private static readonly string authenticationType1 = "authenticationType1";
        private static readonly string authenticationType2 = "authenticationType2";

        private static readonly NetworkCredential credential1 = new NetworkCredential("username1", "password");
        private static readonly NetworkCredential credential2 = new NetworkCredential("username2", "password");
        private static readonly NetworkCredential credential3 = new NetworkCredential("username3", "password");
        private static readonly NetworkCredential credential4 = new NetworkCredential("username4", "password");

        private static readonly NetworkCredential credential5 = new NetworkCredential("username5", "password");
        private static readonly NetworkCredential credential6 = new NetworkCredential("username6", "password");
        private static readonly NetworkCredential credential7 = new NetworkCredential("username7", "password");
        private static readonly NetworkCredential credential8 = new NetworkCredential("username8", "password");

        private static CredentialCache UriAuthenticationTypeCredentialCache()
        {
            CredentialCache cc = new CredentialCache();

            cc.Add(uriPrefix1, authenticationType1, credential1);
            cc.Add(uriPrefix1, authenticationType2, credential2);

            cc.Add(uriPrefix2, authenticationType1, credential3);
            cc.Add(uriPrefix2, authenticationType2, credential4);

            return cc;
        }

        private static CredentialCache HostPortAuthenticationTypeCredentialCache()
        {
            CredentialCache cc = new CredentialCache();

            cc.Add(host1, port1, authenticationType1, credential1);
            cc.Add(host1, port1, authenticationType2, credential2);
            cc.Add(host1, port2, authenticationType1, credential3);
            cc.Add(host1, port2, authenticationType2, credential4);

            cc.Add(host2, port1, authenticationType1, credential5);
            cc.Add(host2, port1, authenticationType2, credential6);
            cc.Add(host2, port2, authenticationType1, credential7);
            cc.Add(host2, port2, authenticationType2, credential8);

            return cc;
        }

        [Fact]
        public static void Ctor_Empty_Success()
        {
            CredentialCache cc = new CredentialCache();
        }

        [Fact]
        public static void Add_UriAuthenticationTypeCredential_Success()
        {
            CredentialCache cc = UriAuthenticationTypeCredentialCache();

            Assert.Equal(credential1, cc.GetCredential(uriPrefix1, authenticationType1));
            Assert.Equal(credential2, cc.GetCredential(uriPrefix1, authenticationType2));

            Assert.Equal(credential3, cc.GetCredential(uriPrefix2, authenticationType1));
            Assert.Equal(credential4, cc.GetCredential(uriPrefix2, authenticationType2));
        }
        
        [Fact]
        public static void Add_UriAuthenticationTypeCredential_Invalid()
        {
            CredentialCache cc = UriAuthenticationTypeCredentialCache();

            Assert.Null(cc.GetCredential(new Uri("http://invalid.uri"), authenticationType1)); //No such uriPrefix
            Assert.Null(cc.GetCredential(uriPrefix1, "invalid-authentication-type")); //No such authenticationType

            Assert.Throws<ArgumentNullException>(() => cc.Add(null, "some", new NetworkCredential())); //Null uriPrefix
            Assert.Throws<ArgumentNullException>(() => cc.Add(new Uri("http://microsoft:80"), null, new NetworkCredential())); //Null authenticationType
        }

        [Fact]
        public static void Add_HostPortAuthenticationTypeCredential_Success()
        {
            CredentialCache cc = HostPortAuthenticationTypeCredentialCache();

            Assert.Equal(credential1, cc.GetCredential(host1, port1, authenticationType1));
            Assert.Equal(credential2, cc.GetCredential(host1, port1, authenticationType2));
            Assert.Equal(credential3, cc.GetCredential(host1, port2, authenticationType1));
            Assert.Equal(credential4, cc.GetCredential(host1, port2, authenticationType2));

            Assert.Equal(credential5, cc.GetCredential(host2, port1, authenticationType1));
            Assert.Equal(credential6, cc.GetCredential(host2, port1, authenticationType2));
            Assert.Equal(credential7, cc.GetCredential(host2, port2, authenticationType1));
            Assert.Equal(credential8, cc.GetCredential(host2, port2, authenticationType2));
        }

        [Fact]
        public static void Add_HostPortAuthenticationTypeCredential_Invalid()
        {
            CredentialCache cc = HostPortAuthenticationTypeCredentialCache();
            
            Assert.Null(cc.GetCredential("invalid-host", port1, authenticationType1)); //No such host
            Assert.Null(cc.GetCredential(host1, 900, authenticationType1)); //No such port
            Assert.Null(cc.GetCredential(host1, port1, "invalid-authentication-type")); //No such authenticationType

            Assert.Throws<ArgumentNullException>(() => cc.Add(null, 500, "authenticationType", new NetworkCredential())); //Null host
            Assert.Throws<ArgumentNullException>(() => cc.Add("host", 500, null, new NetworkCredential())); //Null authenticationType

            Assert.Throws<ArgumentException>(() => cc.Add("", 500, "authenticationType", new NetworkCredential())); //Empty host
            Assert.Throws<ArgumentOutOfRangeException>(() => cc.Add("host", -1, "authenticationType", new NetworkCredential())); //Port < 0
        }

        [Fact]
        public static void Remove_UriAuthenticationType_Success()
        {
            CredentialCache cc = UriAuthenticationTypeCredentialCache();
            
            cc.Remove(uriPrefix1, authenticationType1);
            Assert.Null(cc.GetCredential(uriPrefix1, authenticationType1));
        }

        [Fact]
        public static void Remove_UriAuthenticationType_Invalid()
        {
            CredentialCache cc = new CredentialCache();

            //Doesn't throw, just returns
            cc.Remove(null, "authenticationType");
            cc.Remove(new Uri("http://some.com"), null);
            cc.Remove(new Uri("http://some.com"), "authenticationType");
        }

        [Fact]
        public static void Remove_HostPortAuthenticationType_Success()
        {
            CredentialCache cc = HostPortAuthenticationTypeCredentialCache();

            cc.Remove(host1, port1, authenticationType1);
            Assert.Null(cc.GetCredential(host1, port1, authenticationType1));
        }

        [Fact]
        public static void Remove_HostPortAuthenticationType_Invalid()
        {
            CredentialCache cc = new CredentialCache();

            //Doesn't throw, just returns
            cc.Remove(null, 500, "authenticationType");
            cc.Remove("host", 500, null);
            cc.Remove("host", -1, "authenticationType");
            cc.Remove("host", 500, "authenticationType");
        }

        [Fact]
        public static void GetCredential_SimilarUriAuthenticationType_GetLongestUriPrefix()
        {
            CredentialCache cc = new CredentialCache();
            cc.Add(new Uri("http://microsoft:80/greaterpath"), authenticationType1, credential2);
            cc.Add(new Uri("http://microsoft:80/"), authenticationType1, credential1);

            NetworkCredential nc = cc.GetCredential(new Uri("http://microsoft:80"), authenticationType1);
            Assert.Equal(nc, credential2);
        }

        [Fact]
        public static void GetCredential_UriAuthenticationType_Invalid()
        {
            CredentialCache cc = new CredentialCache();

            Assert.Throws<ArgumentNullException>(() => cc.GetCredential(null, "authenticationType")); //Null uriPrefix
            Assert.Throws<ArgumentNullException>(() => cc.GetCredential(new Uri("http://microsoft:80"), null)); //Null authenticationType
        }

        [Fact]
        public static void GetCredential_HostPortAuthenticationType_Invalid()
        {
            CredentialCache cc = new CredentialCache();

            Assert.Throws<ArgumentNullException>(() => cc.GetCredential(null, 500, "authenticationType")); //Null host
            Assert.Throws<ArgumentNullException>(() => cc.GetCredential("host", 500, null)); //Null authenticationType

            Assert.Throws<ArgumentException>(() => cc.GetCredential("", 500, "authenticationType")); //Empty host

            Assert.Throws<ArgumentOutOfRangeException>(() => cc.GetCredential("host", -1, "authenticationType")); //Port < 0
        }

        [Fact]
        public static void GetEnumerator_Enumerate_Success()
        {
            CredentialCache cc = HostPortAuthenticationTypeCredentialCache();
            IEnumerator enumerator = cc.GetEnumerator();

            Assert.NotNull(enumerator);

            while (enumerator.MoveNext())
            {
                object item = enumerator.Current;
                Assert.NotNull(item);
            }
        }

        [Fact]
        public static void GetEnumerator_MoveNextSynchronization_Invalid()
        {
            //An InvalidOperationException is thrown when moving the enumerator
            //when a credential is added to the cache after getting the enumerator
            CredentialCache cc = HostPortAuthenticationTypeCredentialCache();
            IEnumerator enumerator = cc.GetEnumerator();

            cc.Add(uriPrefix1, authenticationType1, credential1);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public static void GetEnumerator_CurrentSynchronization_Invalid()
        {
            //An InvalidOperationException is thrown when getting the current enumerated object
            //when a credential is added to the cache after getting the enumerator
            CredentialCache cc = HostPortAuthenticationTypeCredentialCache();
            IEnumerator enumerator = cc.GetEnumerator();

            enumerator.MoveNext();
            cc.Add(uriPrefix1, authenticationType1, credential1);

            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        public static void GetEnumerator_ResetIndexGetCurrent_Invalid()
        {
            CredentialCache cc = new CredentialCache();
            IEnumerator enumerator = cc.GetEnumerator();
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        public static void GetEnumerator_MoveNextIndex_Invalid()
        {
            CredentialCache cc = new CredentialCache();
            IEnumerator enumerator = cc.GetEnumerator();
            enumerator.MoveNext();

            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        public static void DefaultCredentials_Get_Success()
        {
            NetworkCredential c = CredentialCache.DefaultCredentials as NetworkCredential;
            Assert.NotNull(c);

            Assert.Equal(String.Empty, c.UserName);
            Assert.Equal(String.Empty, c.Password);
            Assert.Equal(String.Empty, c.Domain);
        }

        [Fact]
        public static void AddRemove_UriAuthenticationTypeDefaultCredentials_Success()
        {
            NetworkCredential nc = CredentialCache.DefaultNetworkCredentials as NetworkCredential;

            CredentialCache cc = new CredentialCache();
            cc.Add(uriPrefix1, authenticationType1, nc);

            Assert.Equal(nc, cc.GetCredential(uriPrefix1, authenticationType1));

            cc.Remove(uriPrefix1, authenticationType1);
            Assert.Null(cc.GetCredential(uriPrefix1, authenticationType1));
        }

        [Fact]
        public static void AddRemove_HostPortAuthenticationTypeDefaultCredentials_Success()
        {
            NetworkCredential nc = CredentialCache.DefaultNetworkCredentials as NetworkCredential;

            CredentialCache cc = new CredentialCache();
            cc.Add(host1, port1, authenticationType1, nc);

            Assert.Equal(nc, cc.GetCredential(host1, port1, authenticationType1));

            cc.Remove(host1, port1, authenticationType1);
            Assert.Null(cc.GetCredential(host1, port1, authenticationType1));
        }

        [Fact]
        public static void DefaultNetworkCredentials_Get_Success()
        {
            NetworkCredential nc = CredentialCache.DefaultNetworkCredentials as NetworkCredential;
            Assert.NotNull(nc);

            Assert.Equal(String.Empty, nc.UserName);
            Assert.Equal(String.Empty, nc.Password);
            Assert.Equal(String.Empty, nc.Domain);
        }
    }
}
