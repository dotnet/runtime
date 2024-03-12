// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

#pragma warning disable CS0618 // obsolete warnings
#pragma warning disable SYSLIB0009 // The class is obsolete

namespace System.Net.Tests
{
    public class AuthenticationManagerTest
    {
        [Fact]
        public void Authenticate_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => AuthenticationManager.Authenticate(null, null, null));
            Assert.Throws<PlatformNotSupportedException>(() => AuthenticationManager.PreAuthenticate(null, null));
        }

        [Fact]
        public void Register_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => AuthenticationManager.Register(null));
        }

        [Fact]
        public void Unregister_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => AuthenticationManager.Unregister((IAuthenticationModule)null));
            Assert.Throws<ArgumentNullException>(() => AuthenticationManager.Unregister((string)null));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Register_Unregister_ModuleCountUnchanged()
        {
            RemoteExecutor.Invoke(() =>
            {
                int initialCount = GetModuleCount();
                IAuthenticationModule module = new CustomModule();
                AuthenticationManager.Register(module);
                AuthenticationManager.Unregister(module);
                Assert.Equal(initialCount, GetModuleCount());
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Register_UnregisterByScheme_ModuleCountUnchanged()
        {
            RemoteExecutor.Invoke(() =>
            {
                int initialCount = GetModuleCount();
                IAuthenticationModule module = new CustomModule();
                AuthenticationManager.Register(module);
                AuthenticationManager.Unregister("custom");
                Assert.Equal(initialCount, GetModuleCount());
            }).Dispose();
        }

        [Fact]
        public void RegisteredModules_DefaultCount_ExpectedValue()
        {
            int count = 0;
            IEnumerator modules = AuthenticationManager.RegisteredModules;
            while (modules.MoveNext()) count++;
            Assert.Equal(0, count);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CredentialPolicy_Roundtrip()
        {
            Assert.Null(AuthenticationManager.CredentialPolicy);

            RemoteExecutor.Invoke(() =>
            {
                ICredentialPolicy cp = new DummyCredentialPolicy();
                AuthenticationManager.CredentialPolicy = cp;
                Assert.Same(cp, AuthenticationManager.CredentialPolicy);

                AuthenticationManager.CredentialPolicy = null;
                Assert.Null(AuthenticationManager.CredentialPolicy);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CustomTargetNameDictionary_ValidCollection()
        {
            Assert.NotNull(AuthenticationManager.CustomTargetNameDictionary);
            Assert.Empty(AuthenticationManager.CustomTargetNameDictionary);
            Assert.Same(AuthenticationManager.CustomTargetNameDictionary, AuthenticationManager.CustomTargetNameDictionary);

            RemoteExecutor.Invoke(() =>
            {
                string theKey = "http://www.contoso.com";
                string theValue = "HTTP/www.contoso.com";
                AuthenticationManager.CustomTargetNameDictionary.Add(theKey, theValue);
                Assert.Equal(theValue, AuthenticationManager.CustomTargetNameDictionary[theKey]);

                AuthenticationManager.CustomTargetNameDictionary.Clear();
                Assert.Equal(0, AuthenticationManager.CustomTargetNameDictionary.Count);
            }).Dispose();
        }

        private static int GetModuleCount()
        {
            int count = 0;
            IEnumerator modules = AuthenticationManager.RegisteredModules;
            while (modules.MoveNext()) count++;

            return count;
        }

        private sealed class DummyCredentialPolicy : ICredentialPolicy
        {
            public bool ShouldSendCredential(Uri challengeUri, WebRequest request, NetworkCredential credential, IAuthenticationModule authenticationModule) => true;
        }

        private sealed class CustomModule : IAuthenticationModule
        {
            public bool CanPreAuthenticate
            {
                get
                {
                    return false;
                }
            }

            public string AuthenticationType
            {
                get
                {
                    return "custom";
                }
            }

            public Authorization Authenticate(string challenge, WebRequest request, ICredentials credentials)
            {
                throw new NotImplementedException();
            }

            public Authorization PreAuthenticate(WebRequest request, ICredentials credentials)
            {
                throw new NotImplementedException();
            }
        }
    }
}
