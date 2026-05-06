// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics
{
    internal interface IObjectSequence
    {
        object? this[int i] { get; set; }
    }

    internal partial struct ObjectSequence1 : IEquatable<ObjectSequence1>, IObjectSequence
    {
        public object? this[int i]
        {
            get
            {
                if (i == 0)
                {
                    return Value1;
                }
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (i == 0)
                {
                    Value1 = value;
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    internal partial struct ObjectSequence2 : IEquatable<ObjectSequence2>, IObjectSequence
    {
        public object? this[int i]
        {
            get
            {
                if (i == 0)
                {
                    return Value1;
                }
                else if (i == 1)
                {
                    return Value2;
                }
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (i == 0)
                {
                    Value1 = value;
                }
                else if (i == 1)
                {
                    Value2 = value;
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        // this isn't exactly identical to the netcore algorithm, but good enough
        public override int GetHashCode() => (Value1?.GetHashCode() ?? 0) ^ (Value2?.GetHashCode() ?? 0 << 3);
    }

    internal partial struct ObjectSequence3 : IEquatable<ObjectSequence3>, IObjectSequence
    {
        public object? this[int i]
        {
            get
            {
                if (i == 0)
                {
                    return Value1;
                }
                else if (i == 1)
                {
                    return Value2;
                }
                else if (i == 2)
                {
                    return Value3;
                }
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (i == 0)
                {
                    Value1 = value;
                }
                else if (i == 1)
                {
                    Value2 = value;
                }
                else if (i == 2)
                {
                    Value3 = value;
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        // this isn't exactly identical to the netcore algorithm, but good enough
        public override int GetHashCode() => (Value1?.GetHashCode() ?? 0) ^ (Value2?.GetHashCode() ?? 0 << 3) ^ (Value3?.GetHashCode() ?? 0 << 6);
    }

    internal partial struct ObjectSequenceMany : IEquatable<ObjectSequenceMany>, IObjectSequence
    {
        public object? this[int i]
        {
            get
            {
                return _values[i];
            }
            set
            {
                _values[i] = value;
            }
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < _values.Length; i++)
            {
                // this isn't exactly identical to the netcore algorithm, but good enough
                hash <<= 3;
                object? value = _values[i];
                if (value != null)
                {
                    hash ^= value.GetHashCode();
                }
            }
            return hash;
        }
    }
}
