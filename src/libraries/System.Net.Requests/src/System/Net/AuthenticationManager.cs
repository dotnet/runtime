// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;

namespace System.Net
{
    public class AuthenticationManager
    {
        private AuthenticationManager() { }

        public static ICredentialPolicy? CredentialPolicy { get; set; }

        public static StringDictionary CustomTargetNameDictionary { get; } = new StringDictionary();

        [Obsolete(Obsoletions.AuthenticationManagerMessage, DiagnosticId = Obsoletions.AuthenticationManagerDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static Authorization? Authenticate(string challenge, WebRequest request, ICredentials credentials) =>
            throw new PlatformNotSupportedException();

        [Obsolete(Obsoletions.AuthenticationManagerMessage, DiagnosticId = Obsoletions.AuthenticationManagerDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static Authorization? PreAuthenticate(WebRequest request, ICredentials credentials) =>
            throw new PlatformNotSupportedException();

        public static void Register(IAuthenticationModule authenticationModule)
        {
            ArgumentNullException.ThrowIfNull(authenticationModule);
        }

        public static void Unregister(IAuthenticationModule authenticationModule)
        {
            ArgumentNullException.ThrowIfNull(authenticationModule);
        }

        public static void Unregister(string authenticationScheme)
        {
            ArgumentNullException.ThrowIfNull(authenticationScheme);
        }

        public static IEnumerator RegisteredModules => Array.Empty<IAuthenticationModule>().GetEnumerator();
    }
}
