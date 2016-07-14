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

    internal class HWStack : ICloneable
    {
        internal HWStack(int GrowthRate) : this(GrowthRate, int.MaxValue) { }

        internal HWStack(int GrowthRate, int limit)
        {
            _growthRate = GrowthRate;
            _used = 0;
            _stack = new Object[GrowthRate];
            _size = GrowthRate;
            _limit = limit;
        }

        internal Object Push()
        {
            if (_used == _size)
            {
                if (_limit <= _used)
                {
                    throw new XmlException(SR.Xml_StackOverflow, string.Empty);
                }
                Object[] newstack = new Object[_size + _growthRate];
                if (_used > 0)
                {
                    System.Array.Copy(_stack, 0, newstack, 0, _used);
                }
                _stack = newstack;
                _size += _growthRate;
            }
            return _stack[_used++];
        }

        internal Object Pop()
        {
            if (0 < _used)
            {
                _used--;
                Object result = _stack[_used];
                return result;
            }
            return null;
        }

        internal object Peek()
        {
            return _used > 0 ? _stack[_used - 1] : null;
        }

        internal void AddToTop(object o)
        {
            if (_used > 0)
            {
                _stack[_used - 1] = o;
            }
        }

        internal Object this[int index]
        {
            get
            {
                if (index >= 0 && index < _used)
                {
                    Object result = _stack[index];
                    return result;
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
            set
            {
                if (index >= 0 && index < _used)
                {
                    _stack[index] = value;
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        internal int Length
        {
            get { return _used; }
        }

        //
        // ICloneable
        //

        private HWStack(object[] stack, int growthRate, int used, int size)
        {
            _stack = stack;
            _growthRate = growthRate;
            _used = used;
            _size = size;
        }

        public object Clone()
        {
            return new HWStack((object[])_stack.Clone(), _growthRate, _used, _size);
        }

        private Object[] _stack;
        private int _growthRate;
        private int _used;
        private int _size;
        private int _limit;
    };
}
