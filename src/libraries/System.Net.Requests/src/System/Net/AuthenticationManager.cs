// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;

namespace System.Net
{
    public class AuthenticationManager
    {
        private AuthenticationManager() { }

        public static ICredentialPolicy CredentialPolicy { get; set; }

        public static StringDictionary CustomTargetNameDictionary { get; } = new StringDictionary();

        public static Authorization Authenticate(string challenge, WebRequest request, ICredentials credentials)
        {
            throw new PlatformNotSupportedException();
        }

        public static Authorization PreAuthenticate(WebRequest request, ICredentials credentials)
        {
            throw new PlatformNotSupportedException();
        }

        public static void Register(IAuthenticationModule authenticationModule)
        {
            if (authenticationModule == null)
            {
                throw new ArgumentNullException(nameof(authenticationModule));
            }
        }

        public static void Unregister(IAuthenticationModule authenticationModule)
        {
        }

        public static void Unregister(string authenticationScheme)
        {
        }

        public static IEnumerator RegisteredModules => Array.Empty<IAuthenticationModule>().GetEnumerator();
    }
}
