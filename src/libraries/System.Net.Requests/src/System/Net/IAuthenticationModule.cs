// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    public interface IAuthenticationModule
    {
        Authorization? Authenticate(string challenge, WebRequest request, ICredentials credentials);
        Authorization? PreAuthenticate(WebRequest request, ICredentials credentials);
        bool CanPreAuthenticate { get; }
        string AuthenticationType { get; }
    }
}
