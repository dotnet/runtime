// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Signature:  SignatureToken
** 
** 
**
**
** Purpose: Represents a Signature to the ILGenerator signature.
**
** 
===========================================================*/

using System;
using System.Reflection;

namespace System.Reflection.Emit
{
    public struct SignatureToken
    {
        public static readonly SignatureToken Empty = new SignatureToken();

        internal int m_signature;
        internal ModuleBuilder m_moduleBuilder;

        internal SignatureToken(int str, ModuleBuilder mod)
        {
            m_signature = str;
            m_moduleBuilder = mod;
        }

        public int Token
        {
            get { return m_signature; }
        }

        public override int GetHashCode()
        {
            return m_signature;
        }

        public override bool Equals(object obj)
        {
            if (obj is SignatureToken)
                return Equals((SignatureToken)obj);
            else
                return false;
        }

        public bool Equals(SignatureToken obj)
        {
            return obj.m_signature == m_signature;
        }

        public static bool operator ==(SignatureToken a, SignatureToken b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(SignatureToken a, SignatureToken b)
        {
            return !(a == b);
        }
    }
}
