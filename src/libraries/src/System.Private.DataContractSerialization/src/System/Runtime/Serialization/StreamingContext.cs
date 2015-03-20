// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            if (((StreamingContext)obj).m_additionalContext == m_additionalContext &&
                ((StreamingContext)obj).m_state == m_state)
            {
                return true;
            }
            return false;
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
