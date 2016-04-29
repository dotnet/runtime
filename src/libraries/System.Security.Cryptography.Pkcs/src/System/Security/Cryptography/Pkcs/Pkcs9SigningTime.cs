// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Pkcs9SigningTime : Pkcs9AttributeObject
    {
        //
        // Constructors.
        //

        public Pkcs9SigningTime()
            : this(DateTime.Now)
        {
        }

        public Pkcs9SigningTime(DateTime signingTime)
            : base(Oids.SigningTime, Encode(signingTime))
        {
            _lazySigningTime = signingTime;
        }

        public Pkcs9SigningTime(byte[] encodedSigningTime)
            : base(Oids.SigningTime, encodedSigningTime)
        {
        }

        //
        // Public properties.
        //

        public DateTime SigningTime
        {
            get
            {
                if (!_lazySigningTime.HasValue)
                {
                    _lazySigningTime = Decode(RawData);
                    Interlocked.MemoryBarrier();
                }
                return _lazySigningTime.Value;
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _lazySigningTime = default(DateTime?);
        }

        //
        // Private methods.
        //

        private static DateTime Decode(byte[] rawData)
        {
            if (rawData == null)
                return default(DateTime);

            return PkcsPal.Instance.DecodeUtcTime(rawData);
        }

        private static byte[] Encode(DateTime signingTime)
        {
            return PkcsPal.Instance.EncodeUtcTime(signingTime);
        }

        private DateTime? _lazySigningTime = default(DateTime?);
    }
}
