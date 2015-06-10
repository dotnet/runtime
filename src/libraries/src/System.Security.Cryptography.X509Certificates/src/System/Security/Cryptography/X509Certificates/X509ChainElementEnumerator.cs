// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509ChainElementEnumerator : IEnumerator
    {
        public X509ChainElement Current
        {
            get
            {
                return _chainElements[_current];
            }
        }

        Object IEnumerator.Current
        {
            get
            {
                return _chainElements[_current];
            }
        }

        public bool MoveNext()
        {
            if (_current == _chainElements.Count - 1)
                return false;
            _current++;
            return true;
        }

        public void Reset()
        {
            _current = -1;
        }

        internal X509ChainElementEnumerator(X509ChainElementCollection chainElements)
        {
            _chainElements = chainElements;
            _current = -1;
        }

        private X509ChainElementCollection _chainElements;
        private int _current;
    }
}

