// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Text.Json
{
    public sealed partial class JsonDocument
    {
        private struct StackRowStack : IDisposable
        {
            private byte[] _rentedBuffer;
            private int _topOfStack;

            internal StackRowStack(int initialSize)
            {
                _rentedBuffer = ArrayPool<byte>.Shared.Rent(initialSize);
                _topOfStack = _rentedBuffer.Length;
            }

            public void Dispose()
            {
                byte[] toReturn = _rentedBuffer;
                _rentedBuffer = null;
                _topOfStack = 0;

                if (toReturn != null)
                {
                    // The data in this rented buffer only conveys the positions and
                    // lengths of tokens in a document, but no content; so it does not
                    // need to be cleared.
                    ArrayPool<byte>.Shared.Return(toReturn);
                }
            }

            internal void Push(StackRow row)
            {
                if (_topOfStack < StackRow.Size)
                {
                    Enlarge();
                }

                _topOfStack -= StackRow.Size;
                MemoryMarshal.Write(_rentedBuffer.AsSpan(_topOfStack), ref row);
            }

            internal StackRow Pop()
            {
                Debug.Assert(_topOfStack <= _rentedBuffer.Length - StackRow.Size);
                StackRow row = MemoryMarshal.Read<StackRow>(_rentedBuffer.AsSpan(_topOfStack));
                _topOfStack += StackRow.Size;
                return row;
            }

            private void Enlarge()
            {
                byte[] toReturn = _rentedBuffer;
                _rentedBuffer = ArrayPool<byte>.Shared.Rent(toReturn.Length * 2);

                Buffer.BlockCopy(
                    toReturn,
                    _topOfStack,
                    _rentedBuffer,
                    _rentedBuffer.Length - toReturn.Length + _topOfStack,
                    toReturn.Length - _topOfStack);

                _topOfStack += _rentedBuffer.Length - toReturn.Length;

                // The data in this rented buffer only conveys the positions and
                // lengths of tokens in a document, but no content; so it does not
                // need to be cleared.
                ArrayPool<byte>.Shared.Return(toReturn);
            }
        }
    }
}
