// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Metrics
{
    internal interface IStringSequence
    {
        string this[int i] { get; set; }
        int Length { get; }
    }

    internal partial struct StringSequence1 : IEquatable<StringSequence1>, IStringSequence
    {
        public string this[int i]
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

        public int Length => 1;
    }

    internal partial struct StringSequence2 : IEquatable<StringSequence2>, IStringSequence
    {

        public string this[int i]
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

        public int Length => 2;

        // this isn't exactly identical to the netcore algorithm, but good enough
        public override int GetHashCode() => (Value1?.GetHashCode() ?? 0) ^ (Value2?.GetHashCode() ?? 0 << 3);
    }

    internal partial struct StringSequence3 : IEquatable<StringSequence3>, IStringSequence
    {
        public string this[int i]
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

        public int Length => 3;

        // this isn't exactly identical to the netcore algorithm, but good enough
        public override int GetHashCode() => (Value1?.GetHashCode() ?? 0) ^ (Value2?.GetHashCode() ?? 0 << 3) ^ (Value3?.GetHashCode() ?? 0 << 6);
    }

    internal partial struct StringSequenceMany : IEquatable<StringSequenceMany>, IStringSequence
    {
        public string this[int i]
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

        public int Length => _values.Length;

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < _values.Length; i++)
            {
                // this isn't exactly identical to the netcore algorithm, but good enough
                hash <<= 3;
                hash ^= _values[i].GetHashCode();
            }
            return hash;
        }
    }
}
