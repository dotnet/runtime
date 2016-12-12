//------------------------------------------------------------------------------
// <copyright file="SafeNativeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.Interop {    
    using System;
    using System.Text;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
   
    [
    ComVisible(false), 
    SuppressUnmanagedCodeSecurityAttribute()
    ]
    internal class SafeNativeMethods {
        [DllImport(ExternDll.Oleaut32, PreserveSig=false)]
        public static extern void VariantClear(IntPtr pObject);
        [DllImport(ExternDll.Oleaut32)]
        public static extern void VariantInit(IntPtr pObject);
        [DllImport(ExternDll.Activeds)]
        public static extern bool FreeADsMem(IntPtr pVoid);
        
        public const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
            FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
            FORMAT_MESSAGE_FROM_STRING = 0x00000400,
            FORMAT_MESSAGE_FROM_HMODULE = 0x00000800,
            FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
            FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000,
            FORMAT_MESSAGE_MAX_WIDTH_MASK = 0x000000FF,
            ERROR_MORE_DATA = 234,
            ERROR_SUCCESS = 0;

        [DllImport(ExternDll.Activeds, CharSet=System.Runtime.InteropServices.CharSet.Unicode)]         
        public static extern int ADsGetLastError(out int error, StringBuilder errorBuffer, 
                                                 int errorBufferLength, StringBuilder nameBuffer, int nameBufferLength);

        [DllImport(ExternDll.Activeds, CharSet=System.Runtime.InteropServices.CharSet.Unicode)]         
        public static extern int ADsSetLastError(int error, string errorString, string provider);


        [DllImport(ExternDll.Kernel32, CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int FormatMessageW(int dwFlags, int lpSource, int dwMessageId,
                                                int dwLanguageId, StringBuilder lpBuffer, int nSize, int arguments);

        
        [System.Runtime.InteropServices.ComVisible(false)]
        public class EnumVariant {

            private static readonly object NoMoreValues = new object();
            private Object currentValue = NoMoreValues;
            private IEnumVariant enumerator;

            public EnumVariant(IEnumVariant en) {
                if (en == null)
                    throw new ArgumentNullException("en");
                enumerator = en;
            }

            /// <include file='doc\SafeNativeMethods.uex' path='docs/doc[@for="SafeNativeMethods.EnumVariant.GetNext"]/*' />
            /// <devdoc>
            /// Moves the enumerator to the next value In the list.
            /// </devdoc>
            public bool GetNext() {
                Advance();
                return currentValue != NoMoreValues;
            }

            /// <include file='doc\SafeNativeMethods.uex' path='docs/doc[@for="SafeNativeMethods.EnumVariant.GetValue"]/*' />
            /// <devdoc>
            /// Returns the current value of the enumerator. If GetNext() has never been called,
            /// or if it has been called but it returned false, will throw an exception.
            /// </devdoc>
            public Object GetValue() {
                if (currentValue == NoMoreValues)
                    throw new InvalidOperationException(Res.GetString(Res.DSEnumerator));
                return currentValue;
            }

            /// <include file='doc\SafeNativeMethods.uex' path='docs/doc[@for="SafeNativeMethods.EnumVariant.Reset"]/*' />
            /// <devdoc>
            /// Returns the enumerator to the start of the sequence.
            /// </devdoc>
            public void Reset() {
                enumerator.Reset();
                currentValue = NoMoreValues;
            }

            /// <include file='doc\SafeNativeMethods.uex' path='docs/doc[@for="SafeNativeMethods.EnumVariant.Advance"]/*' />
            /// <devdoc>
            /// Moves the pointer to the next value In the contained IEnumVariant, and
            /// stores the current value In currentValue.
            /// </devdoc>
            private void Advance() {
                currentValue = NoMoreValues;
                IntPtr addr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Variant)));
                try {
                    int[] numRead = new int[] { 0 };
                    SafeNativeMethods.VariantInit(addr);
                    enumerator.Next(1, addr, numRead);
                    
                    try {
                        if (numRead[0] > 0) {
                            currentValue = Marshal.GetObjectForNativeVariant(addr);
                        }
                    }
                    finally {
                        SafeNativeMethods.VariantClear(addr);
                    }
                }
                finally {
                    Marshal.FreeCoTaskMem(addr);
                }
            }
        }

        [ComImport(), Guid("00020404-0000-0000-C000-000000000046"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumVariant {
            
            [SuppressUnmanagedCodeSecurityAttribute()]
             void Next(
                    [In, MarshalAs(UnmanagedType.U4)] 
                     int celt,
                    [In, Out] 
                       IntPtr rgvar,
                    [Out, MarshalAs(UnmanagedType.LPArray)] 
                      int[] pceltFetched);

            [SuppressUnmanagedCodeSecurityAttribute()]
             void Skip(
                    [In, MarshalAs(UnmanagedType.U4)] 
                     int celt);

            [SuppressUnmanagedCodeSecurityAttribute()]
             void Reset();

            [SuppressUnmanagedCodeSecurityAttribute()]
             void Clone(
                    [Out, MarshalAs(UnmanagedType.LPArray)] 
                       IEnumVariant[] ppenum);
        }                   
    }
}
