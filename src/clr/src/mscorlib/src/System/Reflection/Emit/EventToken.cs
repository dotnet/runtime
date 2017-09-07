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
    public struct EventToken
    {
        public static readonly EventToken Empty = new EventToken();

        internal int m_event;

        internal EventToken(int str)
        {
            m_event = str;
        }

        public int Token
        {
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
