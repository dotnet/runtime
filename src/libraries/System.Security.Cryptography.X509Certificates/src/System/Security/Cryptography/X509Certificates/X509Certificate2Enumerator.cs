// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509Certificate2Enumerator : IEnumerator
    {
        internal X509Certificate2Enumerator(IEnumerator baseEnumerator)
        {
            _baseEnumerator = baseEnumerator;
        }

        public X509Certificate2 Current
        {
            get
            {
                return (X509Certificate2)(_baseEnumerator.Current);
            }
        }

        Object IEnumerator.Current
        {
            get
            {
                return _baseEnumerator.Current;
            }
        }

        public bool MoveNext()
        {
            return _baseEnumerator.MoveNext();
        }

        bool IEnumerator.MoveNext()
        {
            return _baseEnumerator.MoveNext();
        }

        public void Reset()
        {
            _baseEnumerator.Reset();
        }

        void IEnumerator.Reset()
        {
            _baseEnumerator.Reset();
        }

        private IEnumerator _baseEnumerator;
    }
}

