// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Xml
{
    /// <summary>
    /// Manages a stack of bits.  Exposes push, pop, and peek operations.
    /// </summary>
    internal sealed class BitStack
    {
        private uint[]? _bitStack;
        private int _stackPos;
        private uint _curr;

        /// <summary>
        /// Initialize stack.
        /// </summary>
        public BitStack()
        {
            // Set sentinel bit in 1st position.  As bits are shifted onto this.curr, this sentinel
            // bit shifts to the left.  When it's about to overflow, this.curr will be pushed
            // onto an unsigned int stack and the sentinel bit will be reset to 0x1.
            _curr = 0x1;
        }

        /// <summary>
        /// Push a 0 or 1 bit onto the stack.
        /// </summary>
        public void PushBit(bool bit)
        {
            if ((_curr & 0x80000000) != 0)
            {
                // If sentinel bit has reached the last position, push this.curr
                PushCurr();
            }

            // Shift new bit onto this.curr (which must have at least one open position)
            _curr = (_curr << 1) | (bit ? 1u : 0u);
        }

        /// <summary>
        /// Pop the top bit from the stack and return it.
        /// </summary>
        public bool PopBit()
        {
            bool bit;
            Debug.Assert(_curr != 0x1, "Stack empty");

            // Shift rightmost bit from this.curr
            bit = (_curr & 0x1) != 0;

            _curr >>= 1;

            if (_curr == 0x1)
            {
                // If sentinel bit has reached the rightmost position, pop this.curr
                PopCurr();
            }

            return bit;
        }

        /// <summary>
        /// Return the top bit on the stack without pushing or popping.
        /// </summary>
        public bool PeekBit()
        {
            Debug.Assert(_curr != 0x1, "Stack empty");
            return (_curr & 0x1) != 0;
        }

        /// <summary>
        /// this.curr has enough space for 31 bits (minus 1 for sentinel bit).  Once this space is
        /// exhausted, a uint stack is created to handle the overflow.
        /// </summary>
        private void PushCurr()
        {
            int len;

            _bitStack ??= new uint[16];

            // Push current unsigned int (which has been filled) onto a stack
            // and initialize this.curr to be used for future pushes.
            _bitStack[_stackPos++] = _curr;
            _curr = 0x1;

            // Resize stack if necessary
            len = _bitStack.Length;
            if (_stackPos >= len)
            {
                uint[] bitStackNew = new uint[2 * len];
                Array.Copy(_bitStack, bitStackNew, len);
                _bitStack = bitStackNew;
            }
        }

        /// <summary>
        /// If all bits have been popped from this.curr, then pop the previous uint value from the stack in
        /// order to provide another 31 bits.
        /// </summary>
        private void PopCurr()
        {
            if (_stackPos > 0)
                _curr = _bitStack![--_stackPos];
        }
    }
}
