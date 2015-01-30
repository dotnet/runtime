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
    public struct EventToken
    {
        public static readonly EventToken Empty = new EventToken();
    
        internal int m_event;

        internal EventToken(int str) {
            m_event=str;
        }
        
        public int Token {
            get { return m_event; }
        }
        
        public override int GetHashCode()
        {
            return m_event;
        }
        
        public override bool Equals(Object obj)
        {
            if (obj is EventToken)
                return Equals((EventToken)obj);
            else
                return false;
        }
        
        public bool Equals(EventToken obj)
        {
            return obj.m_event == m_event;
        }
    
        public static bool operator ==(EventToken a, EventToken b)
        {
            return a.Equals(b);
        }
        
        public static bool operator !=(EventToken a, EventToken b)
        {
            return !(a == b);
        }
            
    }




}
