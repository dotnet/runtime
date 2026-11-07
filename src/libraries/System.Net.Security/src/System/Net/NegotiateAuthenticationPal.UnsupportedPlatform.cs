// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;

namespace System.Net
{
    internal partial class NegotiateAuthenticationPal
    {
        public static NegotiateAuthenticationPal Create(NegotiateAuthenticationClientOptions clientOptions) =>
            new UnsupportedNegotiateAuthenticationPal(clientOptions);

        public static NegotiateAuthenticationPal Create(NegotiateAuthenticationServerOptions serverOptions) =>
            new UnsupportedNegotiateAuthenticationPal(serverOptions);
    }
}
