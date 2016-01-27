// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** ValueType: StreamingContext
**
**
** Purpose: A value type indicating the source or destination of our streaming.
**
**
===========================================================*/

namespace System.Runtime.Serialization
{
    public struct StreamingContext
    {
        internal Object m_additionalContext;
        internal StreamingContextStates m_state;

        internal StreamingContext(StreamingContextStates state)
            : this(state, null)
        {
        }

        internal StreamingContext(StreamingContextStates state, Object additional)
        {
            m_state = state;
            m_additionalContext = additional;
        }

        internal Object Context
        {
            get { return m_additionalContext; }
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is StreamingContext))
            {
                return false;
            }
            StreamingContext ctx = (StreamingContext)obj;
            return ctx.m_additionalContext == m_additionalContext && ctx.m_state == m_state;
        }

        public override int GetHashCode()
        {
            return (int)m_state;
        }

        internal StreamingContextStates State
        {
            get { return m_state; }
        }
    }

    // **********************************************************
    // Keep these in sync with the version in vm\runtimehandles.h
    // **********************************************************
    [Flags]
    internal enum StreamingContextStates
    {
        CrossProcess = 0x01,
        CrossMachine = 0x02,
        File = 0x04,
        Persistence = 0x08,
        Remoting = 0x10,
        Other = 0x20,
        Clone = 0x40,
        CrossAppDomain = 0x80,
        All = 0xFF,
    }
}
