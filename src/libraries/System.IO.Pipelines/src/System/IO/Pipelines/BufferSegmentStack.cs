// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.IO.Pipelines
{
    internal struct BufferSegmentStack
    {
        private BufferSegment[] _array;
        private int _size;

        public BufferSegmentStack(int size)
        {
            _array = new BufferSegment[size];
            _size = 0;
        }

        public int Count => _size;

        public bool TryPop(out BufferSegment result)
        {
            int size = _size - 1;
            BufferSegment[] array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                result = default;
                return false;
            }

            _size = size;
            result = array[size];
            array[size] = default;
            return true;
        }

        // Pushes an item to the top of the stack.
        public void Push(BufferSegment item)
        {
            int size = _size;
            BufferSegment[] array = _array;

            if ((uint)size < (uint)array.Length)
            {
                array[size] = item;
                _size = size + 1;
            }
            else
            {
                PushWithResize(item);
            }
        }

        // Non-inline from Stack.Push to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PushWithResize(BufferSegment item)
        {
            Array.Resize(ref _array, 2 * _array.Length);
            _array[_size] = item;
            _size++;
        }
    }
}
