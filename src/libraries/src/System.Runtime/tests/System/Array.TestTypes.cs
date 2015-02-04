// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;

public class ComparableRefType : IComparable, IEquatable<ComparableRefType>
{
    public int Id;

    public ComparableRefType(int id)
    {
        this.Id = id;
    }

    public int CompareTo(Object other)
    {
        ComparableRefType o = (ComparableRefType)other;
        if (o.Id == this.Id)
        {
            return 0;
        }
        else if (this.Id > o.Id)
        {
            return 1;
        }
        else
        {
            return -1;
        }
    }

    public override string ToString()
    {
        return "C:" + Id;
    }

    public override bool Equals(object obj)
    {
        return obj is ComparableRefType && ((ComparableRefType)obj).Id == this.Id;
    }

    public bool Equals(ComparableRefType other)
    {
        return other.Id == this.Id;
    }

    public override int GetHashCode()
    {
        return this.Id.GetHashCode();
    }
}

public class ComparableValueType : IComparable, IEquatable<ComparableValueType>
{
    public int Id;

    public ComparableValueType(int id)
    {
        this.Id = id;
    }

    public int CompareTo(Object other)
    {
        ComparableValueType o = (ComparableValueType)other;
        if (o.Id == this.Id)
        {
            return 0;
        }
        else if (this.Id > o.Id)
        {
            return 1;
        }
        else
        {
            return -1;
        }
    }

    public override string ToString()
    {
        return "S:" + Id;
    }

    public override bool Equals(object obj)
    {
        return obj is ComparableValueType && ((ComparableValueType)obj).Equals(this);
    }

    public bool Equals(ComparableValueType other)
    {
        return other.Id == this.Id;
    }

    public override int GetHashCode()
    {
        return this.Id.GetHashCode();
    }
}

public class RefTypeReverseComparer : IComparer
{
    public int Compare(ComparableRefType x, ComparableRefType y)
    {
        return -1 * x.CompareTo(y);
    }

    public int Compare(object x, object y)
    {
        return Compare((ComparableRefType)x, (ComparableRefType)y);
    }
}

public class RefTypeNormalComparer : IComparer
{
    public int Compare(ComparableRefType x, ComparableRefType y)
    {
        return x.CompareTo(y);
    }

    public int Compare(object x, object y)
    {
        return Compare((ComparableRefType)x, (ComparableRefType)y);
    }
}


public static class TestObjects
{
    public static readonly ComparableRefType[] customRefTypeArray = new ComparableRefType[]
    {
        new ComparableRefType(-5),
        new ComparableRefType(-4),
        new ComparableRefType(-6),
        new ComparableRefType(-8),
        new ComparableRefType(-10),
        new ComparableRefType(2),
        new ComparableRefType(1),
        new ComparableRefType(5),
        new ComparableRefType(-11),
        new ComparableRefType(-10),
    };

    public static readonly ComparableRefType[] sortedRefTypeArray = new ComparableRefType[]
    {
        new ComparableRefType(-11),
        new ComparableRefType(-10),
        new ComparableRefType(-10),
        new ComparableRefType(-8),
        new ComparableRefType(-6),
        new ComparableRefType(-5),
        new ComparableRefType(-4),
        new ComparableRefType(1),
        new ComparableRefType(2),
        new ComparableRefType(5),
    };

    public static readonly ComparableValueType[] customValueTypeArray = new ComparableValueType[]
    {
        new ComparableValueType(-5),
        new ComparableValueType(-4),
        new ComparableValueType(-6),
        new ComparableValueType(-8),
        new ComparableValueType(-10),
        new ComparableValueType(2),
        new ComparableValueType(1),
        new ComparableValueType(5),
        new ComparableValueType(-11),
        new ComparableValueType(-10),
    };

    public static readonly ComparableValueType[] sortedValueTypeArray = new ComparableValueType[]
    {
        new ComparableValueType(-11),
        new ComparableValueType(-10),
        new ComparableValueType(-10),
        new ComparableValueType(-8),
        new ComparableValueType(-6),
        new ComparableValueType(-5),
        new ComparableValueType(-4),
        new ComparableValueType(1),
        new ComparableValueType(2),
        new ComparableValueType(5),
    };

    public static readonly Array integerArray = new int[] { 5, 4, 3, 2, -1, -3, 7, 6, 10, 9, 20, 15 };
    public static Array stringArray = new string[] { "cat", "dog", "bird", "cookie", "cat-spider", "cat-bird", "alligator", "fox", "rabbit", "ferret", "frog", "squirrel" };

    public static readonly Array sortedIntegerArray = new int[] { -3, -1, 2, 3, 4, 5, 6, 7, 9, 10, 15, 20 };
    // Sort order for these strings assumes that the integer array was used as the sorting key with the above string array as the extra param.
    public static readonly Array sortedStringArray = new string[] { "cat-bird", "cat-spider", "cookie", "bird", "dog", "cat", "fox", "alligator", "ferret", "rabbit", "squirrel", "frog" };

    /// <summary>
    /// Returns an array of random integers, between Int32.MinValue and Int32.MaxValue.
    /// </summary>
    /// <param name="random">The random number generator.</param>
    /// <param name="length">The length of the array.</param>
    /// <returns>The array.</returns>
    public static Array GetRandomIntegerArray(Random random, int length)
    {
        Array array = new int[length];
        for (int g = 0; g < length; g++)
        {
            array.SetValue(random.Next(Int32.MinValue, Int32.MaxValue), g);
        }
        return array;
    }

    /// <summary>
    /// Returns an array of strings with length 4-8 and random lower-case chars filling it.
    /// </summary>
    /// <param name="random">The random number generator.</param>
    /// <param name="length">The length of the array.</param>
    /// <returns>The array.</returns>
    public static Array GetRandomStringArray(Random random, int length)
    {
        Array array = new string[length];
        for (int g = 0; g < length; g++)
        {
            string value = "";
            for (int i = 0; i < random.Next(4, 8); i++)
            {
                value += (char)('a' + random.Next(0, 26));
            }
            array.SetValue(value, g);
        }
        return array;
    }
}

public class RegularIntComparer : IComparer
{
    public int Compare(object x, object y)
    {
        return ((int)x).CompareTo((int)y);
    }
}

public class ReverseIntComparer : IComparer
{
    public int Compare(object x, object y)
    {
        return -((int)x).CompareTo((int)y);
    }
}

public class RegularStringComparer : IComparer
{
    public int Compare(object x, object y)
    {
        return ((string)x).CompareTo((string)y);
    }
}