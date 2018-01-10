// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

#pragma warning disable 0809  //warning CS0809: Obsolete member overrides non-obsolete member

namespace System
{
    // A wrapped integer that invokes a custom delegate every time IEquatable<TInt>.Equals() is invoked.
    internal struct TInt : IEquatable<TInt>, IComparable<TInt>
    {
        public TInt(int value)
            : this(value, (Action<int, int>)null)
        {
            // This constructor does not report comparisons but is still useful for catching uses of the boxing Equals().
        }

        public TInt(int value, Action<int, int> onCompare)
        {
            Value = value;
            _onCompare = onCompare;
        }

        public TInt(int value, TIntLog log)
        {
            Value = value;
            _onCompare = (x, y) => log.Add(x, y);
        }

        public bool Equals(TInt other)
        {
            if (_onCompare != null)
            {
                _onCompare(Value, other.Value);
            }
            return Value == other.Value;
        }

        public int CompareTo(TInt other)
        {
            if (_onCompare != null)
            {
                _onCompare(Value, other.Value);
            }
            return Value.CompareTo(other.Value);
        }

        [Obsolete("Don't call this. Call IEquatable<T>.Equals(T)")]
        public override bool Equals(object obj)
        {
            throw new NotSupportedException("Unexpected use of boxing Equals().");
        }

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString()
        {
            return Value.ToString();
        }

        public int Value { get; }

        private Action<int, int> _onCompare;
    }

    internal sealed class TIntLog
    {
        public void Add(int x, int y) => _log.Add(Tuple.Create(x, y));
        public int Count => _log.Count;
        public int CountCompares(int x, int y) => _log.Where(t => (t.Item1 == x && t.Item2 == y) || (t.Item1 == y && t.Item2 == x)).Count();

        private List<Tuple<int, int>> _log = new List<Tuple<int, int>>();
    }
}
