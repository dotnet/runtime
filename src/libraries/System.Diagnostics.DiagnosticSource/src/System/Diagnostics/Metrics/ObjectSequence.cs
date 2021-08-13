// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics
{
    internal partial struct ObjectSequence1 : IEquatable<ObjectSequence1>, IObjectSequence
    {
        public object? Value1;

        public ObjectSequence1(object? value1)
        {
            Value1 = value1;
        }

        public override int GetHashCode() => Value1?.GetHashCode() ?? 0;

        public bool Equals(ObjectSequence1 other)
        {
            return Value1 is null ? other.Value1 is null : Value1.Equals(other.Value1);
        }

        //GetHashCode() is in the platform specific files
        public override bool Equals(object? obj)
        {
            return obj is ObjectSequence1 os1 && Equals(os1);
        }
    }

    internal partial struct ObjectSequence2 : IEquatable<ObjectSequence2>, IObjectSequence
    {
        public object? Value1;
        public object? Value2;

        public ObjectSequence2(object? value1, object? value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public bool Equals(ObjectSequence2 other)
        {
            return (Value1 is null ? other.Value1 is null : Value1.Equals(other.Value1)) &&
                   (Value2 is null ? other.Value2 is null : Value2.Equals(other.Value2));
        }

        //GetHashCode() is in the platform specific files
        public override bool Equals(object? obj)
        {
            return obj is ObjectSequence2 os2 && Equals(os2);
        }
    }

    internal partial struct ObjectSequence3 : IEquatable<ObjectSequence3>, IObjectSequence
    {
        public object? Value1;
        public object? Value2;
        public object? Value3;

        public ObjectSequence3(object? value1, object? value2, object? value3)
        {
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
        }

        public bool Equals(ObjectSequence3 other)
        {
            return (Value1 is null ? other.Value1 is null : Value1.Equals(other.Value1)) &&
                   (Value2 is null ? other.Value2 is null : Value2.Equals(other.Value2)) &&
                   (Value3 is null ? other.Value3 is null : Value3.Equals(other.Value3));
        }

        //GetHashCode() is in the platform specific files
        public override bool Equals(object? obj)
        {
            return obj is ObjectSequence3 os3 && Equals(os3);
        }
    }

    internal partial struct ObjectSequenceMany : IEquatable<ObjectSequenceMany>, IObjectSequence
    {
        private readonly object?[] _values;

        public ObjectSequenceMany(object[] values)
        {
            _values = values;
        }

        public bool Equals(ObjectSequenceMany other)
        {
            if (_values.Length != other._values.Length)
            {
                return false;
            }
            for (int i = 0; i < _values.Length; i++)
            {
                object? value = _values[i], otherValue = other._values[i];
                if (value is null)
                {
                    if (otherValue is not null)
                    {
                        return false;
                    }
                }
                else if (!value.Equals(otherValue))
                {
                    return false;
                }
            }
            return true;
        }

        //GetHashCode() is in the platform specific files
        public override bool Equals(object? obj)
        {
            return obj is ObjectSequenceMany osm && Equals(osm);
        }
    }
}
