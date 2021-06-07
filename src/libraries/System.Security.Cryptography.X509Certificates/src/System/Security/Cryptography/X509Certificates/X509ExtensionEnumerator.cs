// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509ExtensionEnumerator : IEnumerator, IEnumerator<X509Extension>
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

        void IDisposable.Dispose()
        {
        }

        private readonly X509ExtensionCollection _extensions;
        private int _current;
    }
}
