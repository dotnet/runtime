// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    // This class is used to determine if NTLM or
    // Kerberos are used in the context of a Negotiate handshake
    internal static partial class NegotiationInfoClass
    {
        internal const string NTLM = "NTLM";
        internal const string Kerberos = "Kerberos";
        internal const string Negotiate = "Negotiate";
        internal const string Basic = "Basic";
    }
}
