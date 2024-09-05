// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Linq;
using Claunia.PropertyList;

namespace Melanzana.CodeSign.Blobs
{
    public class CmsWrapperBlob
    {
        private static X509Certificate2 GetManifestCertificate(string name)
        {
            var memoryStream = new MemoryStream();
            using (var manifestStream = typeof(CmsWrapperBlob).Assembly.GetManifestResourceStream(name))
            {
                Debug.Assert(manifestStream != null);
                manifestStream!.CopyTo(memoryStream);
            }
            return new X509Certificate2(memoryStream.ToArray());
        }

        public static byte[] Create(
            X509Certificate2? developerCertificate,
            byte[] dataToSign,
            HashType[] hashTypes,
            byte[][] cdHashes)
        {
            if (dataToSign == null)
                throw new ArgumentNullException(nameof(dataToSign));
            if (hashTypes == null)
                throw new ArgumentNullException(nameof(hashTypes));
            if (cdHashes == null)
                throw new ArgumentNullException(nameof(cdHashes));
            if (hashTypes.Length != cdHashes.Length)
                throw new ArgumentException($"Length of hashType ({hashTypes.Length} is different from length of cdHashes ({cdHashes.Length})");

            // Ad-hoc signature
            if (developerCertificate == null)
            {
                var adhocBlobBuffer = new byte[8];
                BinaryPrimitives.WriteUInt32BigEndian(adhocBlobBuffer.AsSpan(0, 4), (uint)BlobMagic.CmsWrapper);
                BinaryPrimitives.WriteUInt32BigEndian(adhocBlobBuffer.AsSpan(4, 4), (uint)adhocBlobBuffer.Length);
                return adhocBlobBuffer;
            }

            var certificatesList = new X509Certificate2Collection();

            // Try to build full chain
            var chain = new X509Chain();
            // Retry with default policy and system certificate store
            chain.ChainPolicy = new X509ChainPolicy();
            if (chain.Build(developerCertificate))
            {
                foreach(var ce in chain.ChainElements)
                {
                    certificatesList.Add(ce.Certificate);
                }
            }

            var cmsSigner = new CmsSigner(developerCertificate);
            cmsSigner.Certificates.AddRange(certificatesList);
            cmsSigner.IncludeOption = X509IncludeOption.None;

            cmsSigner.SignedAttributes.Add(new Pkcs9SigningTime());

            // DER version of the hash attribute
            var values = new AsnEncodedDataCollection();
            var oid = new Oid("1.2.840.113635.100.9.2", null);
            var plistCdHashes = new NSArray();
            for (int i = 0; i < hashTypes.Length; i++)
            {
                var codeDirectoryAttrWriter = new AsnWriter(AsnEncodingRules.DER);
                using (codeDirectoryAttrWriter.PushSequence())
                {
                    codeDirectoryAttrWriter.WriteObjectIdentifier(hashTypes[i].GetOid());
                    codeDirectoryAttrWriter.WriteOctetString(cdHashes[i]);
                }
                values.Add(new AsnEncodedData(oid, codeDirectoryAttrWriter.Encode()));
                plistCdHashes.Add(new NSData(cdHashes[i].AsSpan(0, 20).ToArray()));
            }
            cmsSigner.SignedAttributes.Add(new CryptographicAttributeObject(oid, values));

            // PList version of the hash attribute
            var plistBytes = Encoding.UTF8.GetBytes(new NSDictionary() { ["cdhashes"] = plistCdHashes }.ToXmlPropertyList());
            var codeDirectoryPListAttrWriter = new AsnWriter(AsnEncodingRules.DER);
            codeDirectoryPListAttrWriter.WriteOctetString(plistBytes);
            cmsSigner.SignedAttributes.Add(new AsnEncodedData("1.2.840.113635.100.9.1", codeDirectoryPListAttrWriter.Encode()));

            var signedCms = new SignedCms(new ContentInfo(dataToSign), true);
            signedCms.ComputeSignature(cmsSigner);

            var encodedCms = signedCms.Encode();

            var blobBuffer = new byte[8 + encodedCms.Length];
            BinaryPrimitives.WriteUInt32BigEndian(blobBuffer.AsSpan(0, 4), (uint)BlobMagic.CmsWrapper);
            BinaryPrimitives.WriteUInt32BigEndian(blobBuffer.AsSpan(4, 4), (uint)blobBuffer.Length);
            encodedCms.CopyTo(blobBuffer.AsSpan(8));

            return blobBuffer;
        }
    }
}
