// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public sealed class AsnEncodedDataEnumerator : IEnumerator
    {
        internal AsnEncodedDataEnumerator(AsnEncodedDataCollection asnEncodedDatas)
        {
            _asnEncodedDatas = asnEncodedDatas;
            _current = -1;
        }

        public AsnEncodedData Current
        {
            get
            {
                return _asnEncodedDatas[_current];
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return _asnEncodedDatas[_current];
            }
        }

        public bool MoveNext()
        {
            if (_current >= _asnEncodedDatas.Count - 1)
                return false;
            _current++;
            return true;
        }

        public void Reset()
        {
            _current = -1;
        }

        private readonly AsnEncodedDataCollection _asnEncodedDatas;
        private int _current;
    }
}
