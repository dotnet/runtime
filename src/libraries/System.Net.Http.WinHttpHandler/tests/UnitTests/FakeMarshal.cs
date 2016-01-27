// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.WinHttpHandlerUnitTests;

namespace System.Net.Http
{
    public static class Marshal
    {
        public static int GetLastWin32Error()
        {
            if (TestControl.LastWin32Error != 0)
            {
                return TestControl.LastWin32Error;
            }

            return System.Runtime.InteropServices.Marshal.GetLastWin32Error();
        }

        public static IntPtr AllocHGlobal(int cb)
        {
            return System.Runtime.InteropServices.Marshal.AllocHGlobal(cb);
        }

        public static void FreeHGlobal(IntPtr hglobal)
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(hglobal);
        }

        public static string PtrToStringUni(IntPtr ptr)
        {
            return System.Runtime.InteropServices.Marshal.PtrToStringUni(ptr);
        }

        public static IntPtr StringToHGlobalUni(string s)
        {
            return System.Runtime.InteropServices.Marshal.StringToHGlobalUni(s);
        }

        public static void Copy(IntPtr source, byte[] destination, int startIndex, int length)
        {
            System.Runtime.InteropServices.Marshal.Copy(source, destination, startIndex, length);
        }

        public static int SizeOf<T>()
        {
            return System.Runtime.InteropServices.Marshal.SizeOf<T>();
        }

        public static IntPtr UnsafeAddrOfPinnedArrayElement<T>(T[] arr, int index)
        {
            return System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(arr, index);
        }
        
        public static T PtrToStructure<T>(IntPtr ptr)
        {
            return System.Runtime.InteropServices.Marshal.PtrToStructure<T>(ptr);
        }

        public static void WriteByte(IntPtr ptr, int ofs, byte val)
        {
            System.Runtime.InteropServices.Marshal.WriteByte(ptr, ofs, val);
        }

        public static string PtrToStringAnsi(IntPtr ptr, int len)
        {
            return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr, len);
        }

        public static void StructureToPtr<T>(T structure, IntPtr ptr, bool fDeleteOld)
        {
            System.Runtime.InteropServices.Marshal.StructureToPtr<T>(structure, ptr, fDeleteOld);
        }
        
        public static int SizeOf<T>(T structure)
        {
            return System.Runtime.InteropServices.Marshal.SizeOf<T>(structure);
        }

        public static int ReadInt32(IntPtr ptr)
        {
            return System.Runtime.InteropServices.Marshal.ReadInt32(ptr);
        }
    }
}
