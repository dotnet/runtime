// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Propertybuilder is for client to define properties for a class
**
** 
===========================================================*/

using System;
using System.Reflection;

namespace System.Reflection.Emit
{
    public struct PropertyToken
    {
        public static readonly PropertyToken Empty = new PropertyToken();

        internal int m_property;

        internal PropertyToken(int str)
        {
            m_property = str;
        }

        public int Token
        {
            get { return m_property; }
        }

        // Satisfy value class requirements
        public override int GetHashCode()
        {
            return m_property;
        }

        // Satisfy value class requirements
        public override bool Equals(object obj)
        {
            if (obj is PropertyToken)
                return Equals((PropertyToken)obj);
            else
                return false;
        }

        public bool Equals(PropertyToken obj)
        {
            return obj.m_property == m_property;
        }

        public static bool operator ==(PropertyToken a, PropertyToken b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(PropertyToken a, PropertyToken b)
        {
            return !(a == b);
        }
    }
}
