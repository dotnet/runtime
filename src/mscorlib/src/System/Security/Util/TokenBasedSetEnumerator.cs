// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Util 
{
    using System;
    using System.Collections;

    internal struct TokenBasedSetEnumerator
    {
        public Object Current;
        public int Index;
                
        private TokenBasedSet _tb;
                            
        public bool MoveNext()
        {
            return _tb != null ? _tb.MoveNext(ref this) : false;
        }
                
        public void Reset()
        {
            Index = -1;
            Current = null;
        }
                            
        public TokenBasedSetEnumerator(TokenBasedSet tb)
        {
            Index = -1;
            Current = null;
            _tb = tb;
        }
    }
}

