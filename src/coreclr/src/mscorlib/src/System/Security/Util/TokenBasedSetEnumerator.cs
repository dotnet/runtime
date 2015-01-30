// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

