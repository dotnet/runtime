// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.Xml;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class SignerInfoCollection : ICollection, IEnumerable
    {
        private readonly SignerInfo[] _signerInfos;

        internal SignerInfoCollection()
        {
            _signerInfos = Array.Empty<SignerInfo>();
        }

        internal SignerInfoCollection(SignerInfo[] signerInfos)
        {
            Debug.Assert(signerInfos != null);

            _signerInfos = signerInfos;
        }

        internal SignerInfoCollection(SignerInfoAsn[] signedDataSignerInfos, SignedCms ownerDocument)
        {
            Debug.Assert(signedDataSignerInfos != null);

            _signerInfos = new SignerInfo[signedDataSignerInfos.Length];

            for (int i = 0; i < signedDataSignerInfos.Length; i++)
            {
                _signerInfos[i] = new SignerInfo(ref signedDataSignerInfos[i], ownerDocument);
            }
        }

        public SignerInfo this[int index] => _signerInfos[index];

        public int Count => _signerInfos.Length;

        public SignerInfoEnumerator GetEnumerator() => new SignerInfoEnumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new SignerInfoEnumerator(this);

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index),SR.ArgumentOutOfRange_Index);
            if (index + Count > array.Length)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            for (int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], index + i);
            }
        }

        // The collections are usually small (usually Count == 1) so there's not value in repeating
        // the validation of the Array overload to defer to a faster copy routine.
        public void CopyTo(SignerInfo[] array, int index) => ((ICollection)this).CopyTo(array, index);

        public bool IsSynchronized => false;
        public object SyncRoot => this;

        internal int FindIndexForSigner(SignerInfo signer)
        {
            Debug.Assert(signer != null);
            SubjectIdentifier id = signer.SignerIdentifier;
            X509IssuerSerial issuerSerial = default;

            if (id.Type == SubjectIdentifierType.IssuerAndSerialNumber)
            {
                issuerSerial = (X509IssuerSerial)id.Value;
            }

            for (int i = 0; i < _signerInfos.Length; i++)
            {
                SignerInfo current = _signerInfos[i];
                SubjectIdentifier currentId = current.SignerIdentifier;

                if (currentId.Type != id.Type)
                {
                    continue;
                }

                bool equal = false;

                switch (id.Type)
                {
                    case SubjectIdentifierType.IssuerAndSerialNumber:
                    {
                        X509IssuerSerial currentIssuerSerial = (X509IssuerSerial)currentId.Value;

                        if (currentIssuerSerial.IssuerName == issuerSerial.IssuerName &&
                            currentIssuerSerial.SerialNumber == issuerSerial.SerialNumber)
                        {
                            equal = true;
                        }

                        break;
                    }
                    case SubjectIdentifierType.SubjectKeyIdentifier:
                        if ((string)id.Value == (string)currentId.Value)
                        {
                            equal = true;
                        }

                        break;
                    case SubjectIdentifierType.NoSignature:
                        equal = true;
                        break;
                    default:
                        Debug.Fail($"No match logic for SubjectIdentifierType {id.Type}");
                        throw new CryptographicException();
                }

                if (equal)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
