// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class SubjectAlternativeNameBuilder
    {
        // Because GeneralNames is a SEQUENCE, just make a rolling list, it doesn't need to be re-sorted.
        private readonly List<byte[][]> _encodedTlvs = new List<byte[][]>();
        private readonly GeneralNameEncoder _generalNameEncoder = new GeneralNameEncoder();

        public void AddEmailAddress(string emailAddress)
        {
            if (string.IsNullOrEmpty(emailAddress))
                throw new ArgumentOutOfRangeException(nameof(emailAddress), SR.Arg_EmptyOrNullString);

            _encodedTlvs.Add(_generalNameEncoder.EncodeEmailAddress(emailAddress));
        }

        public void AddDnsName(string dnsName)
        {
            if (string.IsNullOrEmpty(dnsName))
                throw new ArgumentOutOfRangeException(nameof(dnsName), SR.Arg_EmptyOrNullString);

            _encodedTlvs.Add(_generalNameEncoder.EncodeDnsName(dnsName));
        }

        public void AddUri(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            _encodedTlvs.Add(_generalNameEncoder.EncodeUri(uri));
        }

        public void AddIpAddress(IPAddress ipAddress)
        {
            if (ipAddress == null)
                throw new ArgumentNullException(nameof(ipAddress));

            _encodedTlvs.Add(_generalNameEncoder.EncodeIpAddress(ipAddress));
        }

        public void AddUserPrincipalName(string upn)
        {
            if (string.IsNullOrEmpty(upn))
                throw new ArgumentOutOfRangeException(nameof(upn), SR.Arg_EmptyOrNullString);

            _encodedTlvs.Add(_generalNameEncoder.EncodeUserPrincipalName(upn));
        }

        public X509Extension Build(bool critical=false)
        {
            return new X509Extension(
                Oids.SubjectAltName,
                DerEncoder.ConstructSequence(_encodedTlvs),
                critical);
        }
    }
}
