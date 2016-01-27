// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Numerics;

internal partial class VectorTest
{
    public static bool CheckValue<T>(T value, T expectedValue)
    {
        bool returnVal;
        if (typeof(T) == typeof(float))
        {
            returnVal = Math.Abs(((float)(object)value) - ((float)(object)expectedValue)) <= Single.Epsilon;
        }
        if (typeof(T) == typeof(double))
        {
            returnVal = Math.Abs(((double)(object)value) - ((double)(object)expectedValue)) <= Double.Epsilon;
        }
        else
        {
            returnVal = value.Equals(expectedValue);
        }
        if (returnVal == false)
        {
            Console.WriteLine("CheckValue failed for " + expectedValue + " of type " + typeof(T).ToString());
        }
        return returnVal;
    }

    private static bool CheckVector<T>(Vector<T> V, T value) where T : struct, IComparable<T>, IEquatable<T>
    {
        for (int i = 0; i < Vector<T>.Count; i++)
        {
            if (!(CheckValue<T>(V[i], value)))
            {
                return false;
            }
        }
        return true;
    }

    public static T GetValueFromInt<T>(int value)
    {
        if (typeof(T) == typeof(float))
        {
            float floatValue = (float)value;
            return (T)(object)floatValue;
        }
        if (typeof(T) == typeof(double))
        {
            double doubleValue = (double)value;
            return (T)(object)doubleValue;
        }
        if (typeof(T) == typeof(int))
        {
            return (T)(object)value;
        }
        if (typeof(T) == typeof(uint))
        {
            uint uintValue = (uint)value;
            return (T)(object)uintValue;
        }
        if (typeof(T) == typeof(long))
        {
            long longValue = (long)value;
            return (T)(object)longValue;
        }
        if (typeof(T) == typeof(ulong))
        {
            ulong longValue = (ulong)value;
            return (T)(object)longValue;
        }
        if (typeof(T) == typeof(ushort))
        {
            return (T)(object)(ushort)value;
        }
        if (typeof(T) == typeof(byte))
        {
            return (T)(object)(byte)value;
        }
        if (typeof(T) == typeof(short))
        {
            return (T)(object)(short)value;
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (T)(object)(sbyte)value;
        }
        else
        {
            throw new ArgumentException();
        }
    }

    private static void VectorPrint<T>(string mesg, Vector<T> v) where T : struct, IComparable<T>, IEquatable<T>
    {
        Console.Write(mesg + "[");
        for (int i = 0; i < Vector<T>.Count; i++)
        {
            Console.Write(" " + v[i]);
            if (i < (Vector<T>.Count - 1)) Console.Write(",");
        }
        Console.WriteLine(" ]");
    }

    private static T Add<T>(T left, T right) where T : struct, IComparable<T>, IEquatable<T>
    {
        if (typeof(T) == typeof(float))
        {
            return (T)(object)(((float)(object)left) + ((float)(object)right));
        }
        if (typeof(T) == typeof(double))
        {
            return (T)(object)(((double)(object)left) + ((double)(object)right));
        }
        if (typeof(T) == typeof(int))
        {
            return (T)(object)(((int)(object)left) + ((int)(object)right));
        }
        if (typeof(T) == typeof(uint))
        {
            return (T)(object)(((uint)(object)left) + ((uint)(object)right));
        }
        if (typeof(T) == typeof(ushort))
        {
            return (T)(object)(((ushort)(object)left) + ((ushort)(object)right));
        }
        if (typeof(T) == typeof(byte))
        {
            return (T)(object)(((byte)(object)left) + ((byte)(object)right));
        }
        if (typeof(T) == typeof(short))
        {
            return (T)(object)(((short)(object)left) + ((short)(object)right));
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (T)(object)(((sbyte)(object)left) + ((sbyte)(object)right));
        }
        if (typeof(T) == typeof(long))
        {
            return (T)(object)(((long)(object)left) + ((long)(object)right));
        }
        if (typeof(T) == typeof(ulong))
        {
            return (T)(object)(((ulong)(object)left) + ((ulong)(object)right));
        }
        else
        {
            throw new ArgumentException();
        }
    }
    private static T Multiply<T>(T left, T right) where T : struct, IComparable<T>, IEquatable<T>
    {
        if (typeof(T) == typeof(float))
        {
            return (T)(object)(((float)(object)left) * ((float)(object)right));
        }
        if (typeof(T) == typeof(double))
        {
            return (T)(object)(((double)(object)left) * ((double)(object)right));
        }
        if (typeof(T) == typeof(int))
        {
            return (T)(object)(((int)(object)left) * ((int)(object)right));
        }
        if (typeof(T) == typeof(uint))
        {
            return (T)(object)(((uint)(object)left) * ((uint)(object)right));
        }
        if (typeof(T) == typeof(ushort))
        {
            return (T)(object)(((ushort)(object)left) * ((ushort)(object)right));
        }
        if (typeof(T) == typeof(byte))
        {
            return (T)(object)(((byte)(object)left) * ((byte)(object)right));
        }
        if (typeof(T) == typeof(short))
        {
            return (T)(object)(((short)(object)left) * ((short)(object)right));
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (T)(object)(((sbyte)(object)left) * ((sbyte)(object)right));
        }
        if (typeof(T) == typeof(long))
        {
            return (T)(object)(((long)(object)left) * ((long)(object)right));
        }
        if (typeof(T) == typeof(ulong))
        {
            return (T)(object)(((ulong)(object)left) * ((ulong)(object)right));
        }
        else
        {
            throw new ArgumentException();
        }
    }

    public static T[] GetRandomArray<T>(int size, Random random)
        where T : struct, IComparable<T>, IEquatable<T>
    {
        T[] result = new T[size];
        for (int i = 0; i < size; i++)
        {
            int data = random.Next(100);
            result[i] = GetValueFromInt<T>(data);
        }
        return result;
    }
}
