// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO.Pipelines
{
    [DebuggerDisplay("State: {State}")]
    [StructLayout(LayoutKind.Explicit)]
    internal struct PipeOperationState
    {
        [FieldOffset(0)]
        private ReadState _readState;
        [FieldOffset(64)]
        private volatile WriteState _writeState;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginRead()
        {
            if ((_readState & ReadState.Reading) != 0)
            {
                ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
            }

            _readState |= ReadState.Reading;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginReadTentative()
        {
            if ((_readState & ReadState.Reading) != 0)
            {
                ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
            }

            _readState |= ReadState.ReadingTentative;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndRead()
        {
            if (_readState == ReadState.Inactive)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadToComplete();
            }

            _readState = ReadState.Inactive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginWrite()
        {
            _writeState = (WriteState.Allocating | WriteState.Writing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndWrite()
        {
            _writeState = WriteState.Inactive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndWriteAllocation()
        {
            Debug.Assert(_writeState == (WriteState.Allocating | WriteState.Writing));
            _writeState = WriteState.Writing;
        }

        public bool IsWritingActive => (_writeState & WriteState.Writing) != 0;

        public bool IsWritingAllocating => (_writeState & WriteState.Allocating) != 0;

        public bool IsReadingActive => (_readState & ReadState.Reading) != 0;

        private string State => $"WriteState: {_writeState}; ReadState: {_readState}";

        [Flags]
        internal enum ReadState : int
        {
            Inactive = 0,
            Reading = 1,
            ReadingTentative = 2
        }

        [Flags]
        internal enum WriteState : int
        {
            Inactive = 0,
            Allocating = 1,
            Writing = 2
        }
    }
}
