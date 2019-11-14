// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Security.Cryptography
{
    public sealed class OidEnumerator : IEnumerator
    {
        internal OidEnumerator(OidCollection oids)
        {
            _oids = oids;
            _current = -1;
        }

        public Oid Current => _oids[_current];

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_current >= _oids.Count - 1)
                return false;

            _current++;
            return true;
        }

        public void Reset()
        {
            _current = -1;
        }

        private readonly OidCollection _oids;
        private int _current;
    }
}
