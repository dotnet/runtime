// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Security;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.Interop
{
#pragma warning disable BCL0015 // CoreFxPort
    internal class SafeNativeMethods
    {
        [DllImport(ExternDll.Oleaut32, PreserveSig = false)]
        public static extern void VariantClear(IntPtr pObject);

        [DllImport(ExternDll.Oleaut32)]
        public static extern void VariantInit(IntPtr pObject);

        [DllImport(ExternDll.Activeds)]
        public static extern bool FreeADsMem(IntPtr pVoid);

        public const int
            FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
            FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
            FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000,
            ERROR_MORE_DATA = 234,
            ERROR_SUCCESS = 0;

        [DllImport(ExternDll.Activeds, CharSet = CharSet.Unicode)]
        public static extern int ADsGetLastError(out int error, StringBuilder errorBuffer,
                                                 int errorBufferLength, StringBuilder nameBuffer, int nameBufferLength);

        [DllImport(ExternDll.Activeds, CharSet = CharSet.Unicode)]
        public static extern int ADsSetLastError(int error, string errorString, string provider);

        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Unicode)]
        public static extern int FormatMessageW(int dwFlags, int lpSource, int dwMessageId,
                                                int dwLanguageId, StringBuilder lpBuffer, int nSize, int arguments);

        public class EnumVariant
        {
            private static readonly object s_noMoreValues = new object();
            private object _currentValue = s_noMoreValues;
            private IEnumVariant _enumerator;

            public EnumVariant(IEnumVariant en)
            {
                _enumerator = en ?? throw new ArgumentNullException(nameof(en));
            }

            /// <devdoc>
            /// Moves the enumerator to the next value In the list.
            /// </devdoc>
            public bool GetNext()
            {
                Advance();
                return _currentValue != s_noMoreValues;
            }

            /// <devdoc>
            /// Returns the current value of the enumerator. If GetNext() has never been called,
            /// or if it has been called but it returned false, will throw an exception.
            /// </devdoc>
            public object GetValue()
            {
                if (_currentValue == s_noMoreValues)
                {
                    throw new InvalidOperationException(SR.DSEnumerator);
                }

                return _currentValue;
            }

            /// <devdoc>
            /// Returns the enumerator to the start of the sequence.
            /// </devdoc>
            public void Reset()
            {
                _enumerator.Reset();
                _currentValue = s_noMoreValues;
            }

            /// <devdoc>
            /// Moves the pointer to the next value In the contained IEnumVariant, and
            /// stores the current value In currentValue.
            /// </devdoc>
            private void Advance()
            {
                _currentValue = s_noMoreValues;
                IntPtr addr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Variant)));
                try
                {
                    int[] numRead = new int[] { 0 };
                    VariantInit(addr);
                    _enumerator.Next(1, addr, numRead);

                    try
                    {
                        if (numRead[0] > 0)
                        {
#pragma warning disable 612, 618
                            _currentValue = Marshal.GetObjectForNativeVariant(addr);
#pragma warning restore 612, 618
                        }
                    }
                    finally
                    {
                        VariantClear(addr);
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(addr);
                }
            }
        }

        [ComImport]
        [Guid("00020404-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumVariant
        {
            void Next([In, MarshalAs(UnmanagedType.U4)] int celt,
                      [In, Out] IntPtr rgvar,
                      [Out, MarshalAs(UnmanagedType.LPArray)] int[] pceltFetched);

            void Skip([In, MarshalAs(UnmanagedType.U4)] int celt);

            void Reset();

            void Clone([Out, MarshalAs(UnmanagedType.LPArray)] IEnumVariant[] ppenum);
        }
    }
}
