// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    internal struct StreamingContext
    {
        internal Object m_additionalContext;
        internal StreamingContextStates m_state;

        public StreamingContext(StreamingContextStates state)
            : this(state, null)
        {
        }

        public StreamingContext(StreamingContextStates state, Object additional)
        {
            m_state = state;
            m_additionalContext = additional;
        }

        public Object Context
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

        public StreamingContextStates State
        {
            get { return m_state; }
        }
    }

    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
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
