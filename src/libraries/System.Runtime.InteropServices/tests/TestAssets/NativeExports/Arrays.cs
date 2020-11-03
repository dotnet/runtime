using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NativeExports
{
    public static unsafe class Arrays
    {
        [UnmanagedCallersOnly(EntryPoint = "sum_int_array")]
        public static int Sum(int* values, int numValues)
        {
            if (values == null)
            {
                return -1;
            }

            int sum = 0;
            for (int i = 0; i < numValues; i++)
            {
                sum += values[i];
            }
            return sum;
        }

        [UnmanagedCallersOnly(EntryPoint = "sum_int_array_ref")]
        public static int SumInArray(int** values, int numValues)
        {
            if (*values == null)
            {
                return -1;
            }

            int sum = 0;
            for (int i = 0; i < numValues; i++)
            {
                sum += (*values)[i];
            }
            return sum;
        }

        [UnmanagedCallersOnly(EntryPoint = "duplicate_int_array")]
        public static void Duplicate(int** values, int numValues)
        {
            int* newArray = (int*)Marshal.AllocCoTaskMem(sizeof(int) * numValues);
            new Span<int>(*values, numValues).CopyTo(new Span<int>(newArray, numValues));
            Marshal.FreeCoTaskMem((IntPtr)(*values));
            *values = newArray;
        }

        [UnmanagedCallersOnly(EntryPoint = "create_range_array")]
        public static int* CreateRange(int start, int end, int* numValues)
        {
            if (start >= end)
            {
                *numValues = 0;
                return null;
            }

            *numValues = end - start;

            int* retVal = (int*)Marshal.AllocCoTaskMem(sizeof(int) * (*numValues));
            for (int i = start; i < end; i++)
            {
                retVal[i - start] = i;
            }

            return retVal;
        }
        
        [UnmanagedCallersOnly(EntryPoint = "sum_string_lengths")]
        public static int SumStringLengths(ushort** strArray)
        {
            if (strArray == null)
            {
                return 0;
            }
            int length = 0;
            for (int i = 0; (nint)strArray[i] != 0; i++)
            {
                length += new string((char*)strArray[i]).Length;
            }
            return length;
        }

        [UnmanagedCallersOnly(EntryPoint = "reverse_strings")]
        public static void ReverseStrings(ushort*** strArray, int* numValues)
        {
            if (*strArray == null)
            {
                *numValues = 0;
                return;
            }
            List<IntPtr> newStrings = new List<IntPtr>();
            for (int i = 0; (nint)(*strArray)[i] != 0; i++)
            {
                newStrings.Add((IntPtr)Strings.Reverse((*strArray)[i]));
            }
            newStrings.Add(IntPtr.Zero);
            Marshal.FreeCoTaskMem((IntPtr)(*strArray));
            *strArray = (ushort**)Marshal.AllocCoTaskMem(sizeof(ushort*) * newStrings.Count);
            CollectionsMarshal.AsSpan(newStrings).CopyTo(new Span<IntPtr>((IntPtr*)(*strArray), newStrings.Count));
            *numValues = newStrings.Count;
        }

        [UnmanagedCallersOnly(EntryPoint = "get_long_bytes")]
        public static byte* GetLongBytes(long l)
        {
            const int NumBytesInLong = sizeof(long);

            byte* bytes = (byte*)Marshal.AllocCoTaskMem(NumBytesInLong);
            MemoryMarshal.Write(new Span<byte>(bytes, NumBytesInLong), ref l);
            return bytes;
        }
        
        [UnmanagedCallersOnly(EntryPoint = "append_int_to_array")]
        public static void Append(int** values, int numOriginalValues, int newValue)
        {
            int* newArray = (int*)Marshal.AllocCoTaskMem(sizeof(int) * (numOriginalValues + 1));
            new Span<int>(*values, numOriginalValues).CopyTo(new Span<int>(newArray, numOriginalValues));
            newArray[numOriginalValues] = newValue;
            *values = newArray;
        }
    }
}