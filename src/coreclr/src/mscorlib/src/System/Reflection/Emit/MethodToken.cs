// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
namespace System.Reflection.Emit {
    
    using System;
    using System.Reflection;
    using System.Security.Permissions;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public struct MethodToken
    {
        public static readonly MethodToken Empty = new MethodToken();
        internal int m_method;
            
        internal MethodToken(int str) {
            m_method=str;
        }
    
        public int Token {
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
