// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System
{
    /// <summary>Supports iterating over a <see cref="string"/> object and reading its individual characters.</summary>
    public sealed class CharEnumerator : IEnumerator, IEnumerator<char>, IDisposable, ICloneable
    {
        private string _str; // null after disposal
        private int _index = -1;

        internal CharEnumerator(string str) => _str = str;

        public object Clone() => MemberwiseClone();

        public bool MoveNext()
        {
            int index = _index + 1;
            int length = _str.Length;

            if (index < length)
            {
                _index = index;
                return true;
            }

            _index = length;
            return false;
        }

        public void Dispose() => _str = String.Empty;

        object? IEnumerator.Current => Current;

        public char Current
        {
            get
            {
                int index = _index;
                string s = _str;
                if ((uint)index >= (uint)s.Length)
                {
                    ThrowHelper.ThrowInvalidOperationException_EnumCurrent(_index);
                }

                return s[index];
            }
        }

        public void Reset() => _index = -1;
    }
}
