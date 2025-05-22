// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Text.Json
{
    public sealed partial class JsonDocument
    {
        private struct StackRowStack : IDisposable
        {
            private StackRow[] _rentedBuffer;
            private int _topOfStack;

            public StackRowStack(int initialSize)
            {
                _rentedBuffer = ArrayPool<StackRow>.Shared.Rent(initialSize);
                _topOfStack = _rentedBuffer.Length;
            }

            public void Dispose()
            {
                StackRow[] toReturn = _rentedBuffer;
                bool shouldClear = toReturn != null && _topOfStack < toReturn.Length;
                _rentedBuffer = null!;
                _topOfStack = 0;

                if (toReturn != null)
                {
                    // The data might have references and keep objects alive.
                    ArrayPool<StackRow>.Shared.Return(toReturn, shouldClear);
                }
            }

            internal void Push(StackRow row)
            {
                if (_topOfStack <= 0)
                {
                    Enlarge();
                }

                _topOfStack--;
                _rentedBuffer[_topOfStack] = row;
            }

            internal StackRow Pop()
            {
                Debug.Assert(_rentedBuffer != null);
                Debug.Assert(_topOfStack < _rentedBuffer!.Length);

                StackRow row = _rentedBuffer[_topOfStack];
                _rentedBuffer[_topOfStack] = default; // Avoid keeping references alive.
                _topOfStack++;

                return row;
            }

            private void Enlarge()
            {
                StackRow[] toReturn = _rentedBuffer;
                _rentedBuffer = ArrayPool<StackRow>.Shared.Rent(toReturn.Length * 2);

                int _newTopOfStack = _rentedBuffer.Length - toReturn.Length + _topOfStack;
                toReturn.AsSpan(_topOfStack).CopyTo(_rentedBuffer.AsSpan(_newTopOfStack));

                _topOfStack = _newTopOfStack;

                // The data might have references and keep objects alive.
                ArrayPool<StackRow>.Shared.Return(toReturn, clearArray: true);
            }
        }
    }
}
