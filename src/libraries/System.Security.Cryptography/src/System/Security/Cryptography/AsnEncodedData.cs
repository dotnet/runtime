// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public class AsnEncodedData
    {
        protected AsnEncodedData()
        {
            // Initialize _rawData to an empty array so that RawData may reasonably be a non-nullable byte[].
            // It naturally is for the base type as well as for derived types behaving as intended.
            // This, however, is a deviation from the original .NET Framework behavior.
            _rawData = Array.Empty<byte>();
        }

        public AsnEncodedData(byte[] rawData)
        {
            Reset(null, rawData);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AsnEncodedData"/> class from existing encoded data.
        /// </summary>
        /// <param name="rawData">
        ///   The Abstract Syntax Notation One (ASN.1)-encoded data.
        /// </param>
        public AsnEncodedData(ReadOnlySpan<byte> rawData)
        {
            Reset(null, rawData);
        }

        public AsnEncodedData(AsnEncodedData asnEncodedData)
        {
            ArgumentNullException.ThrowIfNull(asnEncodedData);

            Reset(asnEncodedData._oid, asnEncodedData._rawData);
        }

        public AsnEncodedData(Oid? oid, byte[] rawData)
        {
            Reset(oid, rawData);
        }

        public AsnEncodedData(string oid, byte[] rawData)
        {
            Reset(new Oid(oid), rawData);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AsnEncodedData"/> class from an object identifier
        ///   (OID) and existing encoded data.
        /// </summary>
        /// <param name="oid">
        ///   The object identifier for this data.
        /// </param>
        /// <param name="rawData">
        ///   The Abstract Syntax Notation One (ASN.1)-encoded data.
        /// </param>
        public AsnEncodedData(Oid? oid, ReadOnlySpan<byte> rawData)
        {
            Reset(oid, rawData);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AsnEncodedData"/> class from an object identifier
        ///   (OID) and existing encoded data.
        /// </summary>
        /// <param name="oid">
        ///   The object identifier for this data.
        /// </param>
        /// <param name="rawData">
        ///   The Abstract Syntax Notation One (ASN.1)-encoded data.
        /// </param>
        public AsnEncodedData(string oid, ReadOnlySpan<byte> rawData)
        {
            Reset(new Oid(oid), rawData);
        }

        public Oid? Oid
        {
            get => _oid;
            set => _oid = value;
        }

        public byte[] RawData
        {
            get
            {
                // .NET Framework compat demands we return the array without copying.
                return _rawData;
            }

            [MemberNotNull(nameof(_rawData))]
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _rawData = value.CloneByteArray();
            }
        }

        public virtual void CopyFrom(AsnEncodedData asnEncodedData)
        {
            ArgumentNullException.ThrowIfNull(asnEncodedData);

            Reset(asnEncodedData._oid, asnEncodedData._rawData);
        }

        public virtual string Format(bool multiLine)
        {
            // Return empty string if no data to format.
            if (_rawData == null || _rawData.Length == 0)
                return string.Empty;

            return AsnFormatter.Instance.Format(_oid, _rawData, multiLine);
        }

        [MemberNotNull(nameof(_rawData))]
        private void Reset(Oid? oid, byte[] rawData)
        {
            this.Oid = oid;
            this.RawData = rawData;
        }

        [MemberNotNull(nameof(_rawData))]
        private void Reset(Oid? oid, ReadOnlySpan<byte> rawData)
        {
            Oid = oid;
            _rawData = rawData.ToArray();
        }

        private Oid? _oid;
        private byte[] _rawData;
    }
}
