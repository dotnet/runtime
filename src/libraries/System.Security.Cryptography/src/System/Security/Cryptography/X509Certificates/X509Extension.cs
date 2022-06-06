// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public class X509Extension : AsnEncodedData
    {
        protected X509Extension()
            : base()
        {
        }

        public X509Extension(AsnEncodedData encodedExtension, bool critical)
            : this(encodedExtension.Oid!, encodedExtension.RawData, critical)
        {
        }

        public X509Extension(Oid oid, byte[] rawData, bool critical)
            : this(oid, (ReadOnlySpan<byte>)(rawData ?? throw new ArgumentNullException(nameof(rawData))), critical)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="X509Extension"/> class.
        /// </summary>
        /// <param name="oid">
        ///   The object identifier used to identify the extension.
        /// </param>
        /// <param name="rawData">
        ///   The encoded data used to create the extension.
        /// </param>
        /// <param name="critical">
        ///   <see langword="true" /> if the extension is critical;
        ///   otherwise, <see langword="false" />.
        /// </param>
        public X509Extension(Oid oid, ReadOnlySpan<byte> rawData, bool critical)
            : base(oid, rawData)
        {
            if (base.Oid?.Value == null)
                throw new ArgumentNullException(nameof(oid));
            if (base.Oid.Value.Length == 0)
                throw new ArgumentException(SR.Format(SR.Arg_EmptyOrNullString_Named, "oid.Value"), nameof(oid));
            Critical = critical;
        }

        public X509Extension(string oid, byte[] rawData, bool critical)
            : this(new Oid(oid), rawData, critical)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="X509Extension"/> class.
        /// </summary>
        /// <param name="oid">
        ///   The object identifier used to identify the extension.
        /// </param>
        /// <param name="rawData">
        ///   The encoded data used to create the extension.
        /// </param>
        /// <param name="critical">
        ///   <see langword="true" /> if the extension is critical;
        ///   otherwise, <see langword="false" />.
        /// </param>
        public X509Extension(string oid, ReadOnlySpan<byte> rawData, bool critical)
            : this(new Oid(oid), rawData, critical)
        {
        }

        public bool Critical { get; set; }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            ArgumentNullException.ThrowIfNull(asnEncodedData);

            X509Extension? extension = asnEncodedData as X509Extension;
            if (extension == null)
                throw new ArgumentException(SR.Cryptography_X509_ExtensionMismatch);
            base.CopyFrom(asnEncodedData);
            Critical = extension.Critical;
        }

        internal X509Extension(string oidValue)
        {
            base.Oid = Oid.FromOidValue(oidValue, OidGroup.ExtensionOrAttribute);
        }

        internal X509Extension(Oid oid)
        {
            base.Oid = oid;
        }
    }
}
