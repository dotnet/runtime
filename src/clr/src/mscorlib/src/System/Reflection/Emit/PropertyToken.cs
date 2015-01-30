// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
namespace System.Reflection.Emit {
    
    using System;
    using System.Reflection;
    using System.Security.Permissions;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public struct PropertyToken {
    
        public static readonly PropertyToken Empty = new PropertyToken();

        internal int m_property;

        internal PropertyToken(int str) {
            m_property=str;
        }
    
        public int Token {
            get { return m_property; }
        }
        
        // Satisfy value class requirements
        public override int GetHashCode()
        {
            return m_property;
        }

        // Satisfy value class requirements
        public override bool Equals(Object obj)
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
