// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;

namespace System.Net
{
    internal abstract partial class NegotiateAuthenticationPal
    {
        public static NegotiateAuthenticationPal Create(NegotiateAuthenticationClientOptions clientOptions)
        {
            switch (clientOptions.Package)
            {
                case NegotiationInfoClass.NTLM:
                    return ManagedNtlmNegotiateAuthenticationPal.Create(clientOptions);

                case NegotiationInfoClass.Negotiate:
                    return new ManagedSpnegoNegotiateAuthenticationPal(clientOptions);

                default:
                    return new UnsupportedNegotiateAuthenticationPal(clientOptions);
            }
        }

        public static NegotiateAuthenticationPal Create(NegotiateAuthenticationServerOptions serverOptions)
        {
            return new UnsupportedNegotiateAuthenticationPal(serverOptions);
        }
    }
}
