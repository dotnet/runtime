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

using System;

namespace System.Runtime.Serialization
{
    public struct StreamingContext
    {
        internal readonly object _additionalContext;
        internal readonly StreamingContextStates _state;

        public StreamingContext(StreamingContextStates state)
            : this(state, null)
        {
        }

        public StreamingContext(StreamingContextStates state, object additional)
        {
            _state = state;
            _additionalContext = additional;
        }

        public object Context => _additionalContext;
        
        public StreamingContextStates State => _state;
        
        public override bool Equals(object obj)
        {
            if (!(obj is StreamingContext))
            {
                return false;
            }
            StreamingContext ctx = (StreamingContext)obj;
            return ctx._additionalContext == _additionalContext && ctx._state == _state;
        }

        public override int GetHashCode()
        {
            return (int)_state;
        }
    }

    // **********************************************************
    // Keep these in sync with the version in vm\runtimehandles.h
    // **********************************************************
    [Flags]
    public enum StreamingContextStates
    {
        //CrossProcess = 0x01,
        //CrossMachine = 0x02,
        //File = 0x04,
        //Persistence = 0x08,
        //Remoting = 0x10,
        //Other = 0x20,
        //Clone = 0x40,
        //CrossAppDomain = 0x80,
        All = 0xFF,
    }
}
