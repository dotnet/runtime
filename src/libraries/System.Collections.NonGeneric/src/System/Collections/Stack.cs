// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Class: Stack
**
** Purpose: Represents a simple last-in-first-out (LIFO)
**          non-generic collection of objects.
**
**
=============================================================================*/

using System.Diagnostics;

namespace System.Collections
{
    // A simple stack of objects.  Internally it is implemented as an array,
    // so Push can be O(n).  Pop is O(1).
    [DebuggerTypeProxy(typeof(System.Collections.Stack.StackDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class Stack : ICollection, ICloneable
    {
        private object?[] _array; // Storage for stack elements. Do not rename (binary serialization)
        private int _size; // Number of items in the stack. Do not rename (binary serialization)
        private int _version; // Used to keep enumerator in sync w/ collection. Do not rename (binary serialization)

        private const int _defaultCapacity = 10;

        public Stack()
        {
            _array = new object[_defaultCapacity];
            _size = 0;
            _version = 0;
        }

        // Create a stack with a specific initial capacity.  The initial capacity
        // must be a non-negative number.
        public Stack(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (initialCapacity < _defaultCapacity)
                initialCapacity = _defaultCapacity;  // Simplify doubling logic in Push.
            _array = new object[initialCapacity];
            _size = 0;
            _version = 0;
        }

        // Fills a Stack with the contents of a particular collection.  The items are
        // pushed onto the stack in the same order they are read by the enumerator.
        //
        public Stack(ICollection col) : this((col == null ? 32 : col.Count))
        {
            if (col == null)
                throw new ArgumentNullException(nameof(col));

            IEnumerator en = col.GetEnumerator();
            while (en.MoveNext())
                Push(en.Current);
        }

        public virtual int Count
        {
            get
            {
                return _size;
            }
        }

        public virtual bool IsSynchronized
        {
            get { return false; }
        }

        public virtual object SyncRoot => this;

        // Removes all Objects from the Stack.
        public virtual void Clear()
        {
            Array.Clear(_array, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
            _size = 0;
            _version++;
        }

        public virtual object Clone()
        {
            Stack s = new Stack(_size);
            s._size = _size;
            Array.Copy(_array, s._array, _size);
            s._version = _version;
            return s;
        }

        public virtual bool Contains(object? obj)
        {
            int count = _size;

            while (count-- > 0)
            {
                if (obj == null)
                {
                    if (_array[count] == null)
                        return true;
                }
                else if (_array[count] != null && _array[count]!.Equals(obj))
                {
                    return true;
                }
            }
            return false;
        }

        // Copies the stack into an array.
        public virtual void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - index < _size)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            int i = 0;
            object?[]? objArray = array as object[];
            if (objArray != null)
            {
                while (i < _size)
                {
                    objArray[i + index] = _array[_size - i - 1];
                    i++;
                }
            }
            else
            {
                while (i < _size)
                {
                    array.SetValue(_array[_size - i - 1], i + index);
                    i++;
                }
            }
        }

        // Returns an IEnumerator for this Stack.
        public virtual IEnumerator GetEnumerator()
        {
            return new StackEnumerator(this);
        }

        // Returns the top object on the stack without removing it.  If the stack
        // is empty, Peek throws an InvalidOperationException.
        public virtual object? Peek()
        {
            if (_size == 0)
                throw new InvalidOperationException(SR.InvalidOperation_EmptyStack);

            return _array[_size - 1];
        }

        // Pops an item from the top of the stack.  If the stack is empty, Pop
        // throws an InvalidOperationException.
        public virtual object? Pop()
        {
            if (_size == 0)
                throw new InvalidOperationException(SR.InvalidOperation_EmptyStack);

            _version++;
            object? obj = _array[--_size];
            _array[_size] = null;     // Free memory quicker.
            return obj;
        }

        // Pushes an item to the top of the stack.
        //
        public virtual void Push(object? obj)
        {
            if (_size == _array.Length)
            {
                object[] newArray = new object[2 * _array.Length];
                Array.Copy(_array, newArray, _size);
                _array = newArray;
            }
            _array[_size++] = obj;
            _version++;
        }

        // Returns a synchronized Stack.
        //
        public static Stack Synchronized(Stack stack)
        {
            if (stack == null)
                throw new ArgumentNullException(nameof(stack));

            return new SyncStack(stack);
        }


        // Copies the Stack to an array, in the same order Pop would return the items.
        public virtual object?[] ToArray()
        {
            if (_size == 0)
                return Array.Empty<object>();

            object?[] objArray = new object[_size];
            int i = 0;
            while (i < _size)
            {
                objArray[i] = _array[_size - i - 1];
                i++;
            }
            return objArray;
        }

        private sealed class SyncStack : Stack
        {
            private readonly Stack _s;
            private readonly object _root;

            internal SyncStack(Stack stack)
            {
                _s = stack;
                _root = stack.SyncRoot;
            }

            public override bool IsSynchronized
            {
                get { return true; }
            }

            public override object SyncRoot
            {
                get
                {
                    return _root;
                }
            }

            public override int Count
            {
                get
                {
                    lock (_root)
                    {
                        return _s.Count;
                    }
                }
            }

            public override bool Contains(object? obj)
            {
                lock (_root)
                {
                    return _s.Contains(obj);
                }
            }

            public override object Clone()
            {
                lock (_root)
                {
                    return new SyncStack((Stack)_s.Clone());
                }
            }

            public override void Clear()
            {
                lock (_root)
                {
                    _s.Clear();
                }
            }

            public override void CopyTo(Array array, int arrayIndex)
            {
                lock (_root)
                {
                    _s.CopyTo(array, arrayIndex);
                }
            }

            public override void Push(object? value)
            {
                lock (_root)
                {
                    _s.Push(value);
                }
            }

            public override object? Pop()
            {
                lock (_root)
                {
                    return _s.Pop();
                }
            }

            public override IEnumerator GetEnumerator()
            {
                lock (_root)
                {
                    return _s.GetEnumerator();
                }
            }

            public override object? Peek()
            {
                lock (_root)
                {
                    return _s.Peek();
                }
            }

            public override object?[] ToArray()
            {
                lock (_root)
                {
                    return _s.ToArray();
                }
            }
        }

        private sealed class StackEnumerator : IEnumerator, ICloneable
        {
            private readonly Stack _stack;
            private int _index;
            private readonly int _version;
            private object? _currentElement;

            internal StackEnumerator(Stack stack)
            {
                _stack = stack;
                _version = _stack._version;
                _index = -2;
                _currentElement = null;
            }

            public object Clone() => MemberwiseClone();

            public bool MoveNext()
            {
                bool retval;
                if (_version != _stack._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                if (_index == -2)
                {  // First call to enumerator.
                    _index = _stack._size - 1;
                    retval = (_index >= 0);
                    if (retval)
                        _currentElement = _stack._array[_index];
                    return retval;
                }
                if (_index == -1)
                {  // End of enumeration.
                    return false;
                }

                retval = (--_index >= 0);
                if (retval)
                    _currentElement = _stack._array[_index];
                else
                    _currentElement = null;
                return retval;
            }

            public object? Current
            {
                get
                {
                    if (_index == -2) throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                    if (_index == -1) throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                    return _currentElement;
                }
            }

            public void Reset()
            {
                if (_version != _stack._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                _index = -2;
                _currentElement = null;
            }
        }

        internal sealed class StackDebugView
        {
            private readonly Stack _stack;

            public StackDebugView(Stack stack)
            {
                if (stack == null)
                    throw new ArgumentNullException(nameof(stack));

                _stack = stack;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object?[] Items
            {
                get
                {
                    return _stack.ToArray();
                }
            }
        }
    }
}
