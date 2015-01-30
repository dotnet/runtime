// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
namespace System.Reflection.Emit {
    
    using System;
    using System.Reflection;
    using System.Security.Permissions;

    // The ParameterToken class is an opaque representation of the Token returned
    // by the Metadata to represent the parameter. 
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public struct ParameterToken {
    
        public static readonly ParameterToken Empty = new ParameterToken();
        internal int m_tkParameter;
    
#if false
        public ParameterToken() {
            m_tkParameter=0;
        }
#endif
        
        internal ParameterToken(int tkParam) {
            m_tkParameter = tkParam;
        }
    
        public int Token {
            get { return m_tkParameter; }
        }
        
        public override int GetHashCode()
        {
            return m_tkParameter;
        }
        
        public override bool Equals(Object obj)
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
