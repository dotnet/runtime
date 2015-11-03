// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509ExtensionEnumerator : IEnumerator
    {
        internal X509ExtensionEnumerator(X509ExtensionCollection extensions)
        {
            _extensions = extensions;
            _current = -1;
        }

        public X509Extension Current
        {
            get { return _extensions[_current]; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public bool MoveNext()
        {
            if (_current == _extensions.Count - 1)
                return false;
            _current++;
            return true;
        }

        public void Reset()
        {
            _current = -1;
        }

        private X509ExtensionCollection _extensions;
        private int _current;
    }
}

