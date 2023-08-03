// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.IO;

public partial class VectorTest
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
            if ((typeof(T) == typeof(double)) || (typeof(T) == typeof(float)))
            {
                Console.WriteLine("CheckValue failed for type " + typeof(T).ToString() + ". Expected: {0} , Got: {1}", expectedValue, value);
            }
            else
            {
                Console.WriteLine("CheckValue failed for type " + typeof(T).ToString() + ". Expected: {0} (0x{0:X}), Got: {1} (0x{1:X})", expectedValue, value);
            }
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
        if (typeof(T) == typeof(nint))
        {
            nint nintValue = (nint)value;
            return (T)(object)nintValue;
        }
        if (typeof(T) == typeof(nuint))
        {
            nuint nuintValue = (nuint)value;
            return (T)(object)nuintValue;
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
        if (typeof(T) == typeof(nint))
        {
            return (T)(object)(((nint)(object)left) + ((nint)(object)right));
        }
        if (typeof(T) == typeof(nuint))
        {
            return (T)(object)(((nuint)(object)left) + ((nuint)(object)right));
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
        if (typeof(T) == typeof(nint))
        {
            return (T)(object)(((nint)(object)left) * ((nint)(object)right));
        }
        if (typeof(T) == typeof(nuint))
        {
            return (T)(object)(((nuint)(object)left) * ((nuint)(object)right));
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

class JitLog : IDisposable
{
    FileStream      fileStream;
    bool            simdIntrinsicsSupported;

    private static String GetLogFileName()
    {
        String jitLogFileName = Environment.GetEnvironmentVariable("DOTNET_JitFuncInfoLogFile");
        return jitLogFileName;
    }

    public JitLog()
    {
        fileStream = null;
        simdIntrinsicsSupported = Vector.IsHardwareAccelerated;
        String jitLogFileName = GetLogFileName();
        if (jitLogFileName == null)
        {
            Console.WriteLine("No JitLogFile");
            return;
        }
        if (!File.Exists(jitLogFileName))
        {
            Console.WriteLine("JitLogFile " + jitLogFileName + " not found.");
            return;
        }
        File.Copy(jitLogFileName, "Temp.log", true);
        fileStream = new FileStream("Temp.log", FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public void Dispose()
    {
        if (fileStream != null)
        {
            fileStream.Dispose();
        }
    }
    public bool IsEnabled()
    {
        return (fileStream != null);
    }

    //------------------------------------------------------------------------
    // Check: Check to see whether 'method' was recognized as an intrinsic by the JIT.
    //
    // Arguments:
    //    method - The method name, as a string
    //
    // Return Value:
    //    If the JitLog is not enabled (either the environment variable is not set,
    //    or the file was not found, e.g. if the JIT doesn't support it):
    //     - Returns true
    //    If SIMD intrinsics are enabled:
    //     - Returns true if the method was NOT compiled, otherwise false
    //    Else (if SIMD intrinsics are NOT enabled):
    //     - Returns true.
    //       Note that it might be useful to return false if the method was not compiled,
    //       but it will not be logged as compiled if it is inlined.
    //
    // Assumptions:
    //    The JitLog constructor queries Vector.IsHardwareAccelerated to determine
    //    if SIMD intrinsics are enabled, and depends on its correctness.
    //
    // Notes:
    //    It searches for the string verbatim. If 'method' is not fully qualified
    //    or its signature is not provided, it may result in false matches.
    //
    // Example:
    //    CheckJitLog("System.Numerics.Vector4:op_Addition(struct,struct):struct");
    //
    public bool Check(String method)
    {
        // Console.WriteLine("Checking for " + method + ":");
        if (!IsEnabled())
        {
            Console.WriteLine("JitLog not enabled.");
            return true;
        }
        try
        {
            fileStream.Position = 0;
            StreamReader reader = new StreamReader(fileStream);
            bool methodFound = false;
            while ((reader.Peek() >= 0) && (methodFound == false))
            {
                String s = reader.ReadLine();
                if (s.IndexOf(method) != -1)
                {
                    methodFound = true;
                }
            }
            if (simdIntrinsicsSupported && methodFound)
            {
                Console.WriteLine("Method " + method + " was compiled but should not have been");
                return false;
            }
            // Useful when developing / debugging just to be sure that we reached here:
            // Console.WriteLine(method + ((methodFound) ? " WAS COMPILED" : " WAS NOT COMPILED"));
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Error checking JitLogFile: " + e.Message);
            return false;
        }
    }

    // Check: Check to see Vector<'elementType'>:'method' was recognized as an
    //        intrinsic by the JIT.
    //
    // Arguments:
    //    method - The method name, without its containing type (which is Vector<T>)
    //    elementType - The type with which Vector<T> is instantiated.
    //
    // Return Value:
    //    See the other overload, above.
    //
    // Assumptions:
    //    The JitLog constructor queries Vector.IsHardwareAccelerated to determine
    //    if SIMD intrinsics are enabled, and depends on its correctness.
    //
    // Notes:
    //    It constructs the search string based on the way generic types are currently
    //    dumped by the CLR. If the signature is not provided for the method, it
    //    may result in false matches.
    //
    // Example:
    //    CheckJitLog("op_Addition(struct,struct):struct", "Single");
    //
    public bool Check(String method, String elementType)
    {
        String checkString = "System.Numerics.Vector`1[" + elementType + "][System." + elementType + "]:" + method;
        return Check(checkString);
    }
}
