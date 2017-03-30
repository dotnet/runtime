// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Net.Security;
using Xunit;

#pragma warning disable CS0618 // obsolete warnings

namespace System.Net.Tests
{
    public class AuthenticationManagerTest
    {
        [Fact]
        public static void Authenticate_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => AuthenticationManager.Authenticate(null, null, null));
            Assert.Throws<PlatformNotSupportedException>(() => AuthenticationManager.PreAuthenticate(null, null));
        }

        [Fact]
        public static void Register_Unregister_Nop()
        {
            AuthenticationManager.Register(null);

            int count = 0;
            IEnumerator modules = AuthenticationManager.RegisteredModules;
            while (modules.MoveNext()) count++;
            Assert.Equal(0, count);

            AuthenticationManager.Unregister((IAuthenticationModule)null);
            AuthenticationManager.Unregister((string)null);
        }

        [Fact]
        public static void CredentialPolicy_Roundtrip()
        {
            Assert.Null(AuthenticationManager.CredentialPolicy);

            ICredentialPolicy cp = new DummyCredentialPolicy();
            AuthenticationManager.CredentialPolicy = cp;
            Assert.Same(cp, AuthenticationManager.CredentialPolicy);

            AuthenticationManager.CredentialPolicy = null;
            Assert.Null(AuthenticationManager.CredentialPolicy);
        }

        [Fact]
        public static void CustomTargetNameDictionary_ValidCollection()
        {
            Assert.NotNull(AuthenticationManager.CustomTargetNameDictionary);
            Assert.Empty(AuthenticationManager.CustomTargetNameDictionary);
            Assert.Same(AuthenticationManager.CustomTargetNameDictionary, AuthenticationManager.CustomTargetNameDictionary);

            AuthenticationManager.CustomTargetNameDictionary.Add("some key", "some value");
            Assert.Equal("some value", AuthenticationManager.CustomTargetNameDictionary["some key"]);

            AuthenticationManager.CustomTargetNameDictionary.Clear();
            Assert.Equal(0, AuthenticationManager.CustomTargetNameDictionary.Count);
        }

        private sealed class DummyCredentialPolicy : ICredentialPolicy
        {
            public bool ShouldSendCredential(Uri challengeUri, WebRequest request, NetworkCredential credential, IAuthenticationModule authenticationModule) => true;
        }
    }
}
