// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Represents a Method to the ILGenerator class.
**
** 
===========================================================*/

using System;
using System.Reflection;

namespace System.Reflection.Emit
{
    public struct MethodToken
    {
        public static readonly MethodToken Empty = new MethodToken();
        internal int m_method;

        internal MethodToken(int str)
        {
            m_method = str;
        }

        public int Token
        {
            get { return m_method; }
        }

        public override int GetHashCode()
        {
            return m_method;
        }

        public override bool Equals(Object obj)
        {
            if (obj is MethodToken)
                return Equals((MethodToken)obj);
            else
                return false;
        }

        public bool Equals(MethodToken obj)
        {
            return obj.m_method == m_method;
        }

        public static bool operator ==(MethodToken a, MethodToken b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(MethodToken a, MethodToken b)
        {
            return !(a == b);
        }
    }
}
