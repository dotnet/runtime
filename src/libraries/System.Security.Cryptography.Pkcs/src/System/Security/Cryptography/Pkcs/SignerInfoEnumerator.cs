// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class SignerInfoEnumerator : IEnumerator
    {
        private readonly SignerInfoCollection _signerInfos;
        private int _position;

        internal SignerInfoEnumerator(SignerInfoCollection signerInfos)
        {
            Debug.Assert(signerInfos != null);

            _signerInfos = signerInfos;
            _position = -1;
        }

        public SignerInfo Current => _signerInfos[_position];
        object IEnumerator.Current => _signerInfos[_position];

        public bool MoveNext()
        {
            int next = _position + 1;

            if (next >= _signerInfos.Count)
                return false;

            _position = next;
            return true;
        }

        public void Reset()
        {
            _position = -1;
        }
    }
}
