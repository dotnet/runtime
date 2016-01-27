// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Methods for Parsing numbers and Strings.
** All methods are implemented in native.
**
** 
===========================================================*/
namespace System {
   
    //This class contains only static members and does not need to be serializable.
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;

    internal static class ParseNumbers {
        internal const int PrintAsI1=0x40;
        internal const int PrintAsI2=0x80;
        internal const int PrintAsI4=0x100;
        internal const int TreatAsUnsigned=0x200;
        internal const int TreatAsI1=0x400;
        internal const int TreatAsI2=0x800;
        internal const int IsTight=0x1000;
        internal const int NoSpace=0x2000;
      
        //
        //
        // NATIVE METHODS
        // For comments on these methods please see $\src\vm\COMUtilNative.cpp
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static long StringToLong(System.String s, int radix, int flags) {
            return StringToLong(s,radix,flags, null);
        }
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public unsafe extern static long StringToLong(System.String s, int radix, int flags, int* currPos);

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static long StringToLong(System.String s, int radix, int flags, ref int currPos) {
            fixed(int * ppos = &currPos) {
                return StringToLong( s, radix, flags, ppos);
            }
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static int StringToInt(System.String s, int radix, int flags) {            
            return StringToInt(s,radix,flags, null);
        }
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]        
        public unsafe extern static int StringToInt(System.String s, int radix, int flags, int* currPos);        

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static int StringToInt(System.String s, int radix, int flags, ref int currPos) {            
            fixed(int * ppos = &currPos) {
                return StringToInt( s, radix, flags, ppos);
            }
        }        
    
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern static String IntToString(int l, int radix, int width, char paddingChar, int flags);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern static String LongToString(long l, int radix, int width, char paddingChar, int flags);
    }
}
