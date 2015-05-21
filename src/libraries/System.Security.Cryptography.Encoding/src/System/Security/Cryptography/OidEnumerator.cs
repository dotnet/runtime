// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
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

        public Oid Current
        {
            get
            {
                return _oids[_current];
            }
        }

        Object IEnumerator.Current
        {
            get
            {
                return _oids[_current];
            }
        }

        public bool MoveNext()
        {
            if (_current == _oids.Count - 1)
                return false;
            _current++;
            return true;
        }

        public void Reset()
        {
            _current = -1;
        }

        private OidCollection _oids;
        private int _current;
    }
}
