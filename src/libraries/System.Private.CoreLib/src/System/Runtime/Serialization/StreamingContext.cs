// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA1066 // Implement IEquatable when overriding Object.Equals

namespace System.Runtime.Serialization
{
    public readonly struct StreamingContext
    {
        private readonly object? _additionalContext;
        private readonly StreamingContextStates _state;

        public StreamingContext(StreamingContextStates state) : this(state, null)
        {
        }

        public StreamingContext(StreamingContextStates state, object? additional)
        {
            _state = state;
            _additionalContext = additional;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is StreamingContext))
            {
                return false;
            }
            StreamingContext ctx = (StreamingContext)obj;
            return ctx._additionalContext == _additionalContext && ctx._state == _state;
        }

        public override int GetHashCode() => (int)_state;

        public StreamingContextStates State => _state;

        public object? Context => _additionalContext;
    }

    [Flags]
    public enum StreamingContextStates
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
