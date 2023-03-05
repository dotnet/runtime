// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: Enumerates the characters on a string.  skips range
**          checks.
**
**
============================================================*/

using System.Collections;
using System.Collections.Generic;

namespace System
{
    public sealed class CharEnumerator : IEnumerator, IEnumerator<char>, IDisposable, ICloneable
    {
        private string? _str;
        private int _index;
        private char _currentElement;

        internal CharEnumerator(string str)
        {
            _str = str;
            _index = -1;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool MoveNext()
        {
            int index = _index + 1;
            string s = _str!;

            if ((uint)index < (uint)s.Length)
            {
                _currentElement = s[index];
                _index = index;
                return true;
            }

            _index = s.Length;
            return false;
        }

        public void Dispose()
        {
            if (_str != null)
            {
                _index = _str.Length;
                _str = null;
            }
        }

        object? IEnumerator.Current => Current;

        public char Current
        {
            get
            {
                if ((uint)_index >= _str!.Length)
                {
                    ThrowHelper.ThrowInvalidOperationException_EnumCurrent(_index);
                }

                return _currentElement;
            }
        }

        public void Reset()
        {
            _currentElement = (char)0;
            _index = -1;
        }
    }
}
