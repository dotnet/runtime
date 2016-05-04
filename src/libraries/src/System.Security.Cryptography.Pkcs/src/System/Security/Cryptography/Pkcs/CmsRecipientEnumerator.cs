// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class CmsRecipientEnumerator : IEnumerator
    {
        internal CmsRecipientEnumerator(CmsRecipientCollection recipients)
        {
            _recipients = recipients;
            _current = -1;
        }

        public CmsRecipient Current
        {
            get
            {
                return _recipients[_current];
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return _recipients[_current];
            }
        }

        public bool MoveNext()
        {
            if (_current >= _recipients.Count - 1)
                return false;
            _current++;
            return true;
        }

        public void Reset()
        {
            _current = -1;
        }

        private readonly CmsRecipientCollection _recipients;
        private int _current;
    }
}


