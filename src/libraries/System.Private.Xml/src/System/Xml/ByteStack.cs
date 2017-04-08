// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Xml
{
    // This stack is designed to minimize object creation for the
    // objects being stored in the stack by allowing them to be
    // re-used over time.  It basically pushes the objects creating
    // a high water mark then as Pop() is called they are not removed
    // so that next time Push() is called it simply returns the last
    // object that was already on the stack.

    internal class ByteStack
    {
        private byte[] _stack;
        private int _growthRate;
        private int _top;
        private int _size;

        public ByteStack(int growthRate)
        {
            _growthRate = growthRate;
            _top = 0;
            _stack = new byte[growthRate];
            _size = growthRate;
        }

        public void Push(byte data)
        {
            if (_size == _top)
            {
                byte[] newstack = new byte[_size + _growthRate];
                if (_top > 0)
                {
                    Buffer.BlockCopy(_stack, 0, newstack, 0, _top);
                }
                _stack = newstack;
                _size += _growthRate;
            }
            _stack[_top++] = data;
        }

        public byte Pop()
        {
            if (_top > 0)
            {
                return _stack[--_top];
            }
            else
            {
                return 0;
            }
        }
    }
}
