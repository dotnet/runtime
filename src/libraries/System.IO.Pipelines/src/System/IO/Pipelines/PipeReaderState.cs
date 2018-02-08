// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace System.IO.Pipelines
{
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
                ThrowHelper.CreateInvalidOperationException_NoReadToComplete();
            }

            _state = State.Inactive;
        }

        public bool IsActive => _state == State.Active;

        public override string ToString()
        {
            return $"State: {_state}";
        }
    }

    internal enum State: byte
    {
        Inactive = 1,
        Active = 2,
        Tentative = 3
    }
}
