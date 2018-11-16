// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using SafeHandlesTests;

namespace SafeHandlesTests{
    public class Helper
    {
        public const int N = 3;
        public const int ReturnValue = 123;

        /// <summary>
        /// Creates and returns a new SFH_NoCloseHandle
        /// </summary>
        /// <returns></returns>
        public static SFH_NoCloseHandle NewSFH_NoCloseHandle()
        {
            String lpFileName = "C.txt";
            DesiredAccess dwDesiredAccess = DesiredAccess.GENERIC_WRITE;
            ShareMode dwShareMode = ShareMode.FILE_SHARE_WRITE;
            IntPtr lpSecurityAttributes = IntPtr.Zero;
            CreationDisposition dwCreationDisposition = CreationDisposition.CREATE_ALWAYS;
            FlagsAndAttributes dwFlagsAndAttributes = FlagsAndAttributes.None;
            IntPtr hTemplateFile = IntPtr.Zero;

            //create the handle
            SFH_NoCloseHandle hnd = SFH_NoCloseHandle.CreateFile(lpFileName, dwDesiredAccess, dwShareMode,
                lpSecurityAttributes, dwCreationDisposition,
                dwFlagsAndAttributes, hTemplateFile);

            return hnd;
        }

        /// <summary>
        /// Creates and returns a new ChildSFH_NoCloseHandle
        /// </summary>
        /// <returns></returns>
        public static ChildSFH_NoCloseHandle NewChildSFH_NoCloseHandle()
        {
            String lpFileName = "D.txt";
            DesiredAccess dwDesiredAccess = DesiredAccess.GENERIC_WRITE;
            ShareMode dwShareMode = ShareMode.FILE_SHARE_WRITE;
            IntPtr lpSecurityAttributes = IntPtr.Zero;
            CreationDisposition dwCreationDisposition = CreationDisposition.CREATE_ALWAYS;
            FlagsAndAttributes dwFlagsAndAttributes = FlagsAndAttributes.None;
            IntPtr hTemplateFile = IntPtr.Zero;

            //create the handle
            ChildSFH_NoCloseHandle hnd = ChildSFH_NoCloseHandle.CreateChildSafeFileHandle(lpFileName, dwDesiredAccess, dwShareMode,
                lpSecurityAttributes, dwCreationDisposition,
                dwFlagsAndAttributes, hTemplateFile);

            return hnd;
        }

        /// <summary>
        /// Creates and returns a new ChildSafeFileHandle
        /// </summary>
        /// <returns></returns>
        public static ChildSafeFileHandle NewChildSFH()
        {
            String lpFileName = "B.txt";
            DesiredAccess dwDesiredAccess = DesiredAccess.GENERIC_WRITE;
            ShareMode dwShareMode = ShareMode.FILE_SHARE_WRITE;
            IntPtr lpSecurityAttributes = IntPtr.Zero;
            CreationDisposition dwCreationDisposition = CreationDisposition.CREATE_ALWAYS;
            FlagsAndAttributes dwFlagsAndAttributes = FlagsAndAttributes.None;
            IntPtr hTemplateFile = IntPtr.Zero;

            //create the handle
            ChildSafeFileHandle hnd = ChildSafeFileHandle.CreateChildSafeFileHandle(lpFileName, dwDesiredAccess, dwShareMode,
                lpSecurityAttributes, dwCreationDisposition,
                dwFlagsAndAttributes, hTemplateFile);

            return hnd;
        }

        /// <summary>
        /// Creates and returns a new SafeFileHandle
        /// </summary>
        /// <returns></returns>
        public static SafeFileHandle NewSFH()
        {
            String lpFileName = "A.txt";
            DesiredAccess dwDesiredAccess = DesiredAccess.GENERIC_WRITE;
            ShareMode dwShareMode = ShareMode.FILE_SHARE_WRITE;
            IntPtr lpSecurityAttributes = IntPtr.Zero;
            CreationDisposition dwCreationDisposition = CreationDisposition.CREATE_ALWAYS;
            FlagsAndAttributes dwFlagsAndAttributes = FlagsAndAttributes.None;
            IntPtr hTemplateFile = IntPtr.Zero;

            //create the handle
            SafeFileHandle hnd = SafeFileHandle.CreateFile(lpFileName, dwDesiredAccess, dwShareMode,
                lpSecurityAttributes, dwCreationDisposition,
                dwFlagsAndAttributes, hTemplateFile);

            return hnd;
        }

        /// <summary>
        /// Returns the Int32 value associated with a SFH_NoCloseHandle
        /// </summary>
        /// <returns></returns>
        public static Int32 SHInt32(SFH_NoCloseHandle hnd)
        {
            IntPtr hndIntPtr = hnd.DangerousGetHandle(); //get the IntPtr associated with hnd
            return hndIntPtr.ToInt32(); //return the 32-bit value associated with hnd
        }

        /// <summary>
        /// Returns the Int32 value associated with a ChildSFH_NoCloseHandle
        /// </summary>
        /// <returns></returns>
        public static Int32 SHInt32(ChildSFH_NoCloseHandle hnd)
        {
            IntPtr hndIntPtr = hnd.DangerousGetHandle(); //get the IntPtr associated with hnd
            return hndIntPtr.ToInt32(); //return the 32-bit value associated with hnd
        }

        /// <summary>
        /// Returns the Int32 value associated with a ChildSafeFileHandle
        /// </summary>
        /// <returns></returns>
        public static Int32 SHInt32(ChildSafeFileHandle hnd)
        {
            IntPtr hndIntPtr = hnd.DangerousGetHandle(); //get the IntPtr associated with hnd
            return hndIntPtr.ToInt32(); //return the 32-bit value associated with hnd
        }

        /// <summary>
        /// Returns the Int32 value associated with a SafeFileHandle
        /// </summary>
        /// <returns></returns>
        public static Int32 SHInt32(SafeFileHandle hnd)
        {
            IntPtr hndIntPtr = hnd.DangerousGetHandle(); //get the IntPtr associated with hnd
            return hndIntPtr.ToInt32(); //return the 32-bit value associated with hnd
        }

        /// <summary>
        /// Returns the Int32 value associated with a SafeHandle
        /// </summary>
        /// <returns></returns>
        public static Int32 SHInt32(SafeHandle hnd)
        {
            IntPtr hndIntPtr = hnd.DangerousGetHandle(); //get the IntPtr associated with hnd
            return hndIntPtr.ToInt32(); //return the 32-bit value associated with hnd
        }

        /// <summary>
        /// Returns true if SH subclass (SFH_NoCloseHandle) value has changed else returns false
        /// </summary>
        /// <returns></returns>
        public static bool IsChanged(SFH_NoCloseHandle hnd)
        {
            Int32 hndInt32 = SHInt32(hnd); //get the 32-bit value associated with hnd
            if (hndInt32 == ReturnValue)
                return true;
            return false;
        }

        /// <summary>
        /// Returns true if ChildSFH_NoCloseHandle value has changed else returns false
        /// </summary>
        /// <returns></returns>
        public static bool IsChanged(ChildSFH_NoCloseHandle hnd)
        {
            Int32 hndInt32 = SHInt32(hnd); //get the 32-bit value associated with hnd
            if (hndInt32 == ReturnValue)
                return true;
            return false;
        }

        /// <summary>
        /// Returns true if SafeFileHandle subclass value has changed else returns false
        /// </summary>
        /// <returns></returns>
        public static bool IsChanged(ChildSafeFileHandle hnd)
        {
            Int32 hndInt32 = SHInt32(hnd); //get the 32-bit value associated with hnd
            if (hndInt32 == ReturnValue)
                return true;
            return false;
        }

        /// <summary>
        /// Returns true if SH subclass value has changed else returns false
        /// </summary>
        /// <returns></returns>
        public static bool IsChanged(SafeFileHandle hnd)
        {
            Int32 hndInt32 = SHInt32(hnd); //get the 32-bit value associated with hnd
            if (hndInt32 == ReturnValue)
                return true;
            return false;
        }

        /// <summary>
        /// Returns true if SH value has changed else returns false
        /// </summary>
        /// <returns></returns>
        public static bool IsChanged(SafeHandle hnd)
        {
            Int32 hndInt32 = SHInt32(hnd); //get the 32-bit value associated with hnd
            if (hndInt32 == ReturnValue)
                return true;
            return false;
        }

        /// <summary>
        /// Creates a new StructWithManySHFlds; Fills in arrInt32s with the 32-bit values
        /// of the handle flds of the struct
        /// </summary>
        /// <returns>StructWithManySHFlds</returns>
        public static StructWithManySHFlds NewStructWithManySHFlds(ref Int32[] arrInt32s)
        {
            arrInt32s = new Int32[15]; //size corresponds to the number of flds
            StructWithManySHFlds s = new StructWithManySHFlds();
            s.hnd1 = NewSFH(); //get a new SH
            arrInt32s[0] = SHInt32(s.hnd1);
            s.hnd2 = NewSFH(); //get a new SH
            arrInt32s[1] = SHInt32(s.hnd2);
            s.hnd3 = NewChildSFH(); //get a new SH
            arrInt32s[2] = SHInt32(s.hnd3);
            s.hnd4 = NewSFH(); //get a new SH
            arrInt32s[3] = SHInt32(s.hnd4);
            s.hnd5 = NewSFH(); //get a new SH
            arrInt32s[4] = SHInt32(s.hnd5);
            s.hnd6 = NewChildSFH(); //get a new SH
            arrInt32s[5] = SHInt32(s.hnd6);
            s.hnd7 = NewSFH(); //get a new SH
            arrInt32s[6] = SHInt32(s.hnd7);
            s.hnd8 = NewSFH(); //get a new SH
            arrInt32s[7] = SHInt32(s.hnd8);
            s.hnd9 = NewChildSFH(); //get a new SH
            arrInt32s[8] = SHInt32(s.hnd9);
            s.hnd10 = NewSFH(); //get a new SH
            arrInt32s[9] = SHInt32(s.hnd10);
            s.hnd11 = NewSFH(); //get a new SH
            arrInt32s[10] = SHInt32(s.hnd11);
            s.hnd12 = NewChildSFH(); //get a new SH
            arrInt32s[11] = SHInt32(s.hnd12);
            s.hnd13 = NewSFH(); //get a new SH
            arrInt32s[12] = SHInt32(s.hnd13);
            s.hnd14 = NewSFH(); //get a new SH
            arrInt32s[13] = SHInt32(s.hnd14);
            s.hnd15 = NewChildSFH(); //get a new SH
            arrInt32s[14] = SHInt32(s.hnd15);

            return s;
        }

        /// <summary>
        /// Returns true if any of the handle flds has changed else returns false
        /// </summary>
        public static bool IsChangedStructWithManySHFlds(StructWithManySHFlds s, Int32[] arrInt32s)
        {
            if (SHInt32(s.hnd1) != arrInt32s[0] || SHInt32(s.hnd2) != arrInt32s[1] || SHInt32(s.hnd3) != arrInt32s[2] ||
                SHInt32(s.hnd4) != arrInt32s[3] || SHInt32(s.hnd5) != arrInt32s[4] || SHInt32(s.hnd6) != arrInt32s[5] ||
                SHInt32(s.hnd7) != arrInt32s[6] || SHInt32(s.hnd8) != arrInt32s[7] || SHInt32(s.hnd9) != arrInt32s[8] ||
                SHInt32(s.hnd10) != arrInt32s[9] || SHInt32(s.hnd11) != arrInt32s[10] || SHInt32(s.hnd12) != arrInt32s[11] ||
                SHInt32(s.hnd13) != arrInt32s[12] || SHInt32(s.hnd14) != arrInt32s[13] || SHInt32(s.hnd15) != arrInt32s[14])
                return true;
            return false;
        }

    } //end of class Helper
}
