// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using Xunit;

#pragma warning disable CS0618 // obsolete warnings

namespace System.Net.Tests
{
    public class AuthenticationManagerTest : RemoteExecutorTestBase
    {
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "AuthenticationManager supported on NETFX")]
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
        public void Register_Unregister_ModuleCountUnchanged()
        {
            RemoteInvoke(() =>
            {
                int initialCount = GetModuleCount();
                IAuthenticationModule module = new CustomModule();
                AuthenticationManager.Register(module);
                AuthenticationManager.Unregister(module);
                Assert.Equal(initialCount, GetModuleCount());

                return SuccessExitCode;
            }).Dispose();           
        }

        public void Register_UnregisterByScheme_ModuleCountUnchanged()
        {
            RemoteInvoke(() =>
            {
                int initialCount = GetModuleCount();
                IAuthenticationModule module = new CustomModule();
                AuthenticationManager.Register(module);
                AuthenticationManager.Unregister("custom");
                Assert.Equal(initialCount, GetModuleCount());

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void RegisteredModules_DefaultCount_ExpectedValue()
        {
            int count = 0;
            IEnumerator modules = AuthenticationManager.RegisteredModules;
            while (modules.MoveNext()) count++;
            Assert.Equal(PlatformDetection.IsFullFramework ? 5 : 0, count);
        }

        [Fact]
        public void CredentialPolicy_Roundtrip()
        {
            Assert.Null(AuthenticationManager.CredentialPolicy);

            RemoteInvoke(() =>
            {
                ICredentialPolicy cp = new DummyCredentialPolicy();
                AuthenticationManager.CredentialPolicy = cp;
                Assert.Same(cp, AuthenticationManager.CredentialPolicy);

                AuthenticationManager.CredentialPolicy = null;
                Assert.Null(AuthenticationManager.CredentialPolicy);

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void CustomTargetNameDictionary_ValidCollection()
        {
            Assert.NotNull(AuthenticationManager.CustomTargetNameDictionary);
            Assert.Empty(AuthenticationManager.CustomTargetNameDictionary);
            Assert.Same(AuthenticationManager.CustomTargetNameDictionary, AuthenticationManager.CustomTargetNameDictionary);

            RemoteInvoke(() =>
            {
                string theKey = "http://www.contoso.com";
                string theValue = "HTTP/www.contoso.com";
                AuthenticationManager.CustomTargetNameDictionary.Add(theKey, theValue);
                Assert.Equal(theValue, AuthenticationManager.CustomTargetNameDictionary[theKey]);

                AuthenticationManager.CustomTargetNameDictionary.Clear();
                Assert.Equal(0, AuthenticationManager.CustomTargetNameDictionary.Count);

                return SuccessExitCode;
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
