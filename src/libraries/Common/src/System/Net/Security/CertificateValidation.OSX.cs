// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    internal static class CertificateValidation
    {
        private static readonly IdnMapping s_idnMapping = new IdnMapping();

#pragma warning disable IDE0060
        internal static SslPolicyErrors BuildChainAndVerifyProperties(X509Chain chain, X509Certificate2 remoteCertificate, bool checkCertName, bool isServer, string? hostName, Span<byte> certificateBuffer)
#pragma warning restore IDE0060
        {
            SslPolicyErrors errors = chain.Build(remoteCertificate) ?
                SslPolicyErrors.None :
                SslPolicyErrors.RemoteCertificateChainErrors;

            if (!checkCertName)
            {
                return errors;
            }

            if (string.IsNullOrEmpty(hostName))
            {
                return errors | SslPolicyErrors.RemoteCertificateNameMismatch;
            }

            bool match;

            if (IPAddress.TryParse(hostName, out _))
            {
                match = remoteCertificate.MatchesHostname(hostName);
            }
            else
            {
                string matchName = s_idnMapping.GetAscii(hostName);
                match = remoteCertificate.MatchesHostname(matchName);
            }

            return match ?
                errors :
                errors | SslPolicyErrors.RemoteCertificateNameMismatch;
        }
    }
}
