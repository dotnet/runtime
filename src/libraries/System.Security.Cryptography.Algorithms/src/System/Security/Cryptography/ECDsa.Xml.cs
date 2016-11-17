// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public abstract partial class ECDsa : AsymmetricAlgorithm
    {
        // There is currently not a standard XML format for ECC keys, so we will not implement the default
        // To/FromXmlString so that we're not tied to one format when a standard one does exist. Instead we'll
        // use an overload which allows the user to specify the format they'd like to serialize into.
        //
        // See code:System.Security.Cryptography.Rfc4050KeyFormatter#RFC4050ECKeyFormat for information about
        // the currently supported format.
        public override void FromXmlString(string xmlString)
        {
            throw new NotImplementedException(SR.Cryptography_ECXmlSerializationFormatRequired);
        }

        public void FromXmlString(string xml, ECKeyXmlFormat format)
        {
            if (xml == null)
                throw new ArgumentNullException(nameof(xml));

            if (format != ECKeyXmlFormat.Rfc4050)
                throw new ArgumentOutOfRangeException(nameof(format));

            bool isEcdh;
            ECParameters parameters = Rfc4050KeyFormatter.FromXml(xml, out isEcdh);

            // ECDsaCng may wrap ECDH keys because of interop with non-Windows PFX files.
            // As a result XML marked as ECDiffieHellman loaded just fine, so no check should be done on the
            // key type.
            ImportParameters(parameters);
        }

        // See code:System.Security.Cryptography.ECDsaCng#ECCXMLFormat and 
        // code:System.Security.Cryptography.Rfc4050KeyFormatter#RFC4050ECKeyFormat for information about
        // XML serialization of elliptic curve keys.
        public override string ToXmlString(bool includePrivateParameters)
        {
            throw new NotImplementedException(SR.Cryptography_ECXmlSerializationFormatRequired);
        }

        public string ToXmlString(ECKeyXmlFormat format)
        {
            if (format != ECKeyXmlFormat.Rfc4050)
            {
                throw new ArgumentOutOfRangeException(nameof(format));
            }

            ECParameters ecParams = ExportParameters(false);
            return Rfc4050KeyFormatter.ToXml(ecParams, isEcdh: false);
        }
    }
}
