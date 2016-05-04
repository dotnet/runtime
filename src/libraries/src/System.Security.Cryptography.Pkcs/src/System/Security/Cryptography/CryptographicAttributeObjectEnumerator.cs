// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public sealed class CryptographicAttributeObjectEnumerator : IEnumerator
    {
        internal CryptographicAttributeObjectEnumerator(CryptographicAttributeObjectCollection attributes)
        {
            _attributes = attributes;
            _current = -1;
        }

        public CryptographicAttributeObject Current
        {
            get
            {
                return _attributes[_current];
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return _attributes[_current];
            }
        }

        public bool MoveNext()
        {
            if (_current >= _attributes.Count - 1)
                return false;
            _current++;
            return true;
        }

        public void Reset()
        {
            _current = -1;
        }

        private readonly CryptographicAttributeObjectCollection _attributes;
        private int _current;
    }
}

