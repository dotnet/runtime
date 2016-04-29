// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class RecipientInfoEnumerator : IEnumerator
    {
        internal RecipientInfoEnumerator(RecipientInfoCollection RecipientInfos)
        {
            _recipientInfos = RecipientInfos;
            _current = -1;
        }

        public RecipientInfo Current
        {
            get
            {
                return _recipientInfos[_current];
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return _recipientInfos[_current];
            }
        }

        public bool MoveNext()
        {
            if (_current >= _recipientInfos.Count - 1)
            {
                return false;
            }
            _current++;
            return true;
        }

        public void Reset()
        {
            _current = -1;
        }

        private readonly RecipientInfoCollection _recipientInfos;
        private int _current;
    }
}


