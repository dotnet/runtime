// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
** Small value class used by the SymbolStore package for passing
** around metadata tokens.
**
===========================================================*/

using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.SymbolStore
{
    internal struct SymbolToken
    {
        internal int m_token;

        public SymbolToken(int val) { m_token = val; }

        public int GetToken() { return m_token; }

        public override int GetHashCode() { return m_token; }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is SymbolToken)
                return Equals((SymbolToken)obj);
            else
                return false;
        }

        public bool Equals(SymbolToken obj)
        {
            return obj.m_token == m_token;
        }
    }
}
