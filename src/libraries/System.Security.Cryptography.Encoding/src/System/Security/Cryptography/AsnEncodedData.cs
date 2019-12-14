// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public class AsnEncodedData
    {
        protected AsnEncodedData()
        {
            _oid = null;
            _rawData = Array.Empty<byte>();
        }

        public AsnEncodedData(byte[] rawData)
            : this ((Oid?)null, rawData)
        {
            // nothing to do here
        }

        public AsnEncodedData(AsnEncodedData asnEncodedData)
            : this ()
        {
            if (asnEncodedData is null)
                throw new ArgumentNullException(nameof(asnEncodedData));
            _oid = asnEncodedData._oid;
            _rawData = asnEncodedData._rawData;
        }

        public AsnEncodedData(Oid? oid, byte[] rawData)
        {
            if (rawData is null)
                throw new ArgumentNullException(nameof(rawData));
            _oid = oid;
            _rawData = rawData;
        }

        public AsnEncodedData(string oid, byte[] rawData)
            : this(new Oid(oid), rawData)
        {
            // nothing to do here
        }

        public Oid? Oid
        {
            get
            {
                return _oid;
            }

            set
            {
                _oid = (value is null) ? null : new Oid(value);
            }
        }

        public byte[] RawData
        {
            get
            {
                // Desktop compat demands we return the array without copying.
                return _rawData;
            }

            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                _rawData = value.CloneByteArray();
            }
        }

        public virtual void CopyFrom(AsnEncodedData asnEncodedData)
        {
            if (asnEncodedData is null)
                throw new ArgumentNullException(nameof(asnEncodedData));
            _oid = asnEncodedData._oid;
            _rawData = asnEncodedData._rawData;
        }

        public virtual string? Format(bool multiLine)
        {
            // Return empty string if no data to format.
            if (_rawData.Length == 0)
                return string.Empty;

            return AsnFormatter.Instance.Format(_oid, _rawData, multiLine);
        }

        private Oid? _oid;
        private byte[] _rawData;
    }
}
