// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Pipelines
{
    [DebuggerDisplay("State: {_state}")]
    internal struct PipeReaderState
    {
        private State _state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin()
        {
            // Inactive and Tentative are allowed
            if (_state == State.Active)
            {
                ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
            }

            _state = State.Active;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginTentative()
        {
            // Inactive and Tentative are allowed
            if (_state == State.Active)
            {
                ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
            }

            _state = State.Tentative;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void End()
        {
            if (_state == State.Inactive)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadToComplete();
            }

            _state = State.Inactive;
        }

        public bool IsActive => _state == State.Active;

        internal enum State: byte
        {
            Inactive = 1,
            Active = 2,
            Tentative = 3
        }
    }
}
