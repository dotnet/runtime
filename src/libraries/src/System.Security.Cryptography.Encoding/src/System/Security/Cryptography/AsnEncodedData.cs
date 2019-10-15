// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public class AsnEncodedData
    {
        protected AsnEncodedData()
        {
        }

        public AsnEncodedData(byte[] rawData)
        {
            Reset(null, rawData);
        }

        public AsnEncodedData(AsnEncodedData asnEncodedData)
        {
            if (asnEncodedData == null)
                throw new ArgumentNullException(nameof(asnEncodedData));
            Reset(asnEncodedData._oid, asnEncodedData._rawData);
        }

        public AsnEncodedData(Oid oid, byte[] rawData)
        {
            Reset(oid, rawData);
        }

        public AsnEncodedData(string oid, byte[] rawData)
        {
            Reset(new Oid(oid), rawData);
        }

        public Oid Oid
        {
            get
            {
                return _oid;
            }

            set
            {
                _oid = (value == null) ? null : new Oid(value);
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
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _rawData = value.CloneByteArray();
            }
        }

        public virtual void CopyFrom(AsnEncodedData asnEncodedData)
        {
            if (asnEncodedData == null)
                throw new ArgumentNullException(nameof(asnEncodedData));
            Reset(asnEncodedData._oid, asnEncodedData._rawData);
        }

        public virtual string Format(bool multiLine)
        {
            // Return empty string if no data to format.
            if (_rawData == null || _rawData.Length == 0)
                return string.Empty;

            return AsnFormatter.Instance.Format(_oid, _rawData, multiLine);
        }

        private void Reset(Oid oid, byte[] rawData)
        {
            this.Oid = oid;
            this.RawData = rawData;
        }

        private Oid _oid = null;
        private byte[] _rawData = null;
    }
}
