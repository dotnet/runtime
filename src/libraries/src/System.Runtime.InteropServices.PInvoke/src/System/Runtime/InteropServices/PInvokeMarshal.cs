// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    public static partial class PInvokeMarshal
    {
        public static readonly int SystemDefaultCharSize = Marshal.SystemDefaultCharSize;
        public static readonly int SystemMaxDBCSCharSize = Marshal.SystemMaxDBCSCharSize;
        public static System.IntPtr AllocateMemory(int sizeInBytes)
        {
            return Marshal.AllocCoTaskMem(sizeInBytes);
        }
        public static void DestroyStructure(System.IntPtr ptr, System.Type structureType)
        {
            Marshal.DestroyStructure(ptr, structureType);
        }
        public static void DestroyStructure<T>(System.IntPtr ptr)
        {
            Marshal.DestroyStructure<T>(ptr);
        }
        public static void FreeMemory(System.IntPtr ptr)
        {
            Marshal.FreeCoTaskMem(ptr);
        }
        public static System.Delegate GetDelegateForFunctionPointer(System.IntPtr ptr, System.Type delegateType)
        {
            return Marshal.GetDelegateForFunctionPointer(ptr, delegateType);
        }
        public static TDelegate GetDelegateForFunctionPointer<TDelegate>(System.IntPtr ptr)
        {
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr);
        }
        public static System.IntPtr GetFunctionPointerForDelegate(System.Delegate d)
        {
            return Marshal.GetFunctionPointerForDelegate(d);
        }
        public static System.IntPtr GetFunctionPointerForDelegate<TDelegate>(TDelegate d)
        {
            return Marshal.GetFunctionPointerForDelegate<TDelegate>(d);
        }
        public static int GetLastError()
        {
            return Marshal.GetLastWin32Error();
        }
        public static System.IntPtr OffsetOf(System.Type type, string fieldName)
        {
            return Marshal.OffsetOf(type, fieldName);
        }
        public static System.IntPtr OffsetOf<T>(string fieldName)
        {
            return Marshal.OffsetOf<T>(fieldName);
        }
        public static string PtrToStringAnsi(System.IntPtr ptr)
        {
            return Marshal.PtrToStringAnsi(ptr);
        }
        public static string PtrToStringAnsi(System.IntPtr ptr, int len)
        {
            return Marshal.PtrToStringAnsi(ptr, len);
        }
        public static string PtrToStringUTF16(System.IntPtr ptr)
        {
            return Marshal.PtrToStringUni(ptr);
        }
        public static string PtrToStringUTF16(System.IntPtr ptr, int len)
        {
            return Marshal.PtrToStringUni(ptr, len);
        }
        public static void PtrToStructure(System.IntPtr ptr, object structure)
        {
            Marshal.PtrToStructure(ptr, structure);
        }
        public static object PtrToStructure(System.IntPtr ptr, System.Type structureType)
        {
            return Marshal.PtrToStructure(ptr, structureType);
        }
        public static T PtrToStructure<T>(System.IntPtr ptr)
        {
            return Marshal.PtrToStructure<T>(ptr);
        }
        public static void PtrToStructure<T>(System.IntPtr ptr, T structure)
        {
            Marshal.PtrToStructure<T>(ptr, structure);
        }
        public static System.IntPtr ReallocateMemory(System.IntPtr ptr, int sizeInBytes)
        {
            return Marshal.ReAllocCoTaskMem(ptr, sizeInBytes);
        }
        public static int SizeOf(object structure)
        {
            return Marshal.SizeOf(structure);
        }
        public static int SizeOf(System.Type type)
        {
            return Marshal.SizeOf(type);
        }
        public static int SizeOf<T>()
        {
            return Marshal.SizeOf<T>();
        }
        public static System.IntPtr StringToAllocatedMemoryAnsi(string s)
        {
            return Marshal.StringToCoTaskMemAnsi(s);
        }
        public static System.IntPtr StringToAllocatedMemoryUTF16(string s)
        {
            return Marshal.StringToCoTaskMemUni(s);
        }
        public static void StructureToPtr(object structure, System.IntPtr ptr, bool fDeleteOld)
        {
            Marshal.StructureToPtr(structure, ptr, fDeleteOld);
        }
        public static void StructureToPtr<T>(T structure, System.IntPtr ptr, bool fDeleteOld)
        {
            Marshal.StructureToPtr<T>(structure, ptr, fDeleteOld);
        }
        public static System.IntPtr UnsafeAddrOfPinnedArrayElement(System.Array arr, int index)
        {
            return Marshal.UnsafeAddrOfPinnedArrayElement(arr, index);
        }
        public static System.IntPtr UnsafeAddrOfPinnedArrayElement<T>(T[] arr, int index)
        {
            return Marshal.UnsafeAddrOfPinnedArrayElement<T>(arr, index);
        }
        public static void ZeroFreeMemoryAnsi(System.IntPtr s)
        {
            Marshal.ZeroFreeCoTaskMemAnsi(s);
        }
        public static void ZeroFreeMemoryUTF16(System.IntPtr s)
        {
            Marshal.ZeroFreeCoTaskMemUnicode(s);
        }
    }
}
