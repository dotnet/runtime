// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: metadata tokens for a parameter
**
** 
===========================================================*/

using System;
using System.Reflection;

namespace System.Reflection.Emit
{
    // The ParameterToken class is an opaque representation of the Token returned
    // by the Metadata to represent the parameter. 
    public struct ParameterToken
    {
        public static readonly ParameterToken Empty = new ParameterToken();
        internal int m_tkParameter;


        internal ParameterToken(int tkParam)
        {
            m_tkParameter = tkParam;
        }

        public int Token
        {
            get { return m_tkParameter; }
        }

        public override int GetHashCode()
        {
            return m_tkParameter;
        }

        public override bool Equals(object obj)
        {
            if (obj is ParameterToken)
                return Equals((ParameterToken)obj);
            else
                return false;
        }

        public bool Equals(ParameterToken obj)
        {
            return obj.m_tkParameter == m_tkParameter;
        }

        public static bool operator ==(ParameterToken a, ParameterToken b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ParameterToken a, ParameterToken b)
        {
            return !(a == b);
        }
    }
}
