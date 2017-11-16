// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.SymbolStore
{
    public readonly struct SymbolToken
    {
        private readonly int _token;
        
        public SymbolToken(int val)
        {
            _token = val;
        }
    
        public int GetToken() => _token;
        
        public override int GetHashCode() => _token;
        
        public override bool Equals(object obj)
        {
            if (obj is SymbolToken)
                return Equals((SymbolToken)obj);
            else
                return false;
        }
    
        public bool Equals(SymbolToken obj) => obj._token == _token;
    
        public static bool operator ==(SymbolToken a, SymbolToken b) => a.Equals(b);
        
        public static bool operator !=(SymbolToken a, SymbolToken b) => !(a == b);
    }
}
