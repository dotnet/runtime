// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Tests.Expressions
{
    public struct S1
    {
        private int _x;

        public S1(int x)
        {
            _x = x;
        }

        public static S1 operator +(S1 l, S1 r)
        {
            return new S1(l._x + r._x);
        }

        public static S1 operator -(S1 l, S1 r)
        {
            return new S1(l._x - r._x);
        }

        public static S1 operator *(S1 l, S1 r)
        {
            return new S1(l._x * r._x);
        }

        public static S1 operator /(S1 l, S1 r)
        {
            return new S1(l._x / r._x);
        }

        public static S1 operator %(S1 l, S1 r)
        {
            return new S1(l._x % r._x);
        }

        public static S1 operator &(S1 l, S1 r)
        {
            return new S1(l._x & r._x);
        }

        public static S1 operator |(S1 l, S1 r)
        {
            return new S1(l._x | r._x);
        }

        public static S1 operator ^(S1 l, S1 r)
        {
            return new S1(l._x ^ r._x);
        }

        public static S1 operator <<(S1 l, int r)
        {
            return new S1(l._x << r);
        }

        public static S1 operator >>(S1 l, int r)
        {
            return new S1(l._x >> r);
        }

        public static bool operator <(S1 l, S1 r)
        {
            return l._x < r._x;
        }

        public static bool operator <=(S1 l, S1 r)
        {
            return l._x <= r._x;
        }

        public static bool operator >(S1 l, S1 r)
        {
            return l._x > r._x;
        }

        public static bool operator >=(S1 l, S1 r)
        {
            return l._x >= r._x;
        }

        public static bool operator ==(S1 l, S1 r)
        {
            return l._x == r._x;
        }

        public static bool operator !=(S1 l, S1 r)
        {
            return l._x != r._x;
        }

        public static S1 operator +(S1 o)
        {
            return o;
        }

        public static S1 operator -(S1 o)
        {
            return new S1(-o._x);
        }

        public static S1 operator ~(S1 o)
        {
            return new S1(~o._x);
        }

        public static S1 operator ++(S1 o)
        {
            return new S1(o._x++);
        }

        public static S1 operator --(S1 o)
        {
            return new S1(o._x--);
        }

        public static explicit operator S1(int x)
        {
            return new S1(x);
        }

        public static S1 Pow(S1 l, S1 r)
        {
            return new S1((int)Math.Pow(l._x, r._x));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is S1))
            {
                return false;
            }

            return ((S1)obj)._x == _x;
        }

        public override int GetHashCode()
        {
            return _x.GetHashCode();
        }
    }

    public struct S2
    {
        private bool _b;

        public S2(bool b)
        {
            _b = b;
        }

        public static S2 operator !(S2 o)
        {
            return new S2(!o._b);
        }

        public static bool operator true(S2 o)
        {
            return o._b;
        }

        public static bool operator false(S2 o)
        {
            return !o._b;
        }

        public static S2 operator &(S2 l, S2 r)
        {
            return new S2(l._b & r._b);
        }

        public static S2 operator |(S2 l, S2 r)
        {
            return new S2(l._b | r._b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is S2))
            {
                return false;
            }

            return ((S2)obj)._b == _b;
        }

        public override int GetHashCode()
        {
            return _b.GetHashCode();
        }
    }

    public enum E
    {
        Red,
        Green,
        Blue
    }

    public class Holder<T>
    {
        public T Value { get; set; }
    }

    public class HolderWithLog<T>
    {
        private readonly Action<string> _addToLog;
        private T _value;

        public HolderWithLog(Action<string> addToLog)
        {
            _addToLog = addToLog;
        }

        public T Value
        {
            get
            {
                _addToLog("get_Value");
                return _value;
            }

            set
            {
                _addToLog("set_Value(" + _value + ")");
                _value = value;
            }
        }
    }

    public class Vector<T>
    {
        private T _value;

        public T this[int x]
        {
            get
            {
                return _value;
            }

            set
            {
                _value = value;
            }
        }
    }

    public class VectorWithLog<T>
    {
        private readonly Action<string> _addToLog;
        private T _value;

        public VectorWithLog(Action<string> addToLog)
        {
            _addToLog = addToLog;
        }

        public T this[int x]
        {
            get
            {
                _addToLog("get_Item(" + x + ")");
                return _value;
            }

            set
            {
                _addToLog("set_Item(" + x + ", " + _value + ")");
                _value = value;
            }
        }
    }
}
