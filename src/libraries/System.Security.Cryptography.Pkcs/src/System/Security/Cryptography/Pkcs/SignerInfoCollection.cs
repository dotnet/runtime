// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public SignerInfo this[int index]
        {
            get
            {
                if (index < 0 || index >= _signerInfos.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _signerInfos[index];
            }
        }

        public int Count => _signerInfos.Length;

        public SignerInfoEnumerator GetEnumerator() => new SignerInfoEnumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new SignerInfoEnumerator(this);

        public void CopyTo(Array array!!, int index)
        {
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLess);
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

        private static int FindIndexForSigner(SignerInfo[] signerInfos, SignerInfo signer)
        {
            Debug.Assert(signer != null);
            SubjectIdentifier id = signer.SignerIdentifier;

            for (int i = 0; i < signerInfos.Length; i++)
            {
                SignerInfo current = signerInfos[i];
                SubjectIdentifier currentId = current.SignerIdentifier;

                if (id.IsEquivalentTo(currentId))
                {
                    return i;
                }
            }

            return -1;
        }

        internal int FindIndexForSigner(SignerInfo signer)
        {
            return FindIndexForSigner(_signerInfos, signer);
        }
    }
}
