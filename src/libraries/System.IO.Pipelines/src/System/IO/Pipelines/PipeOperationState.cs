// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Pipelines
{
    [DebuggerDisplay("State: {_state}")]
    internal struct PipeOperationState
    {
        private State _state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginRead()
        {
            if ((_state & State.Reading) == State.Reading)
            {
                ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
            }

            _state |= State.Reading;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginReadTentative()
        {
            if ((_state & State.Reading) == State.Reading)
            {
                ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
            }

            _state |= State.ReadingTentative;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndRead()
        {
            if ((_state & State.Reading) != State.Reading && 
                (_state & State.ReadingTentative) != State.ReadingTentative)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadToComplete();
            }

            _state &= ~(State.Reading | State.ReadingTentative);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginWrite()
        {
            _state |= State.Writing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndWrite()
        {
            _state &= ~State.Writing;
        }

        public bool IsWritingActive => (_state & State.Writing) == State.Writing;

        public bool IsReadingActive => (_state & State.Reading) == State.Reading;

        [Flags]
        internal enum State : byte
        {
            Reading = 1,
            ReadingTentative = 2,
            Writing = 4
        }
    }
}
