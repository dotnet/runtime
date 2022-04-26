// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Security;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.Interop
{
    internal static partial class SafeNativeMethods
    {
        [LibraryImport(global::Interop.Libraries.OleAut32)]
        public static partial void VariantInit(IntPtr pObject);

        [LibraryImport(global::Interop.Libraries.Activeds)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool FreeADsMem(IntPtr pVoid);

        public const int
            FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
            FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
            FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000,
            ERROR_MORE_DATA = 234,
            ERROR_SUCCESS = 0;

        [LibraryImport(global::Interop.Libraries.Activeds, StringMarshalling = StringMarshalling.Utf16)]
        public static unsafe partial int ADsGetLastError(out int error, char* errorBuffer, int errorBufferLength, char* nameBuffer, int nameBufferLength);

        [LibraryImport(global::Interop.Libraries.Activeds, StringMarshalling = StringMarshalling.Utf16)]
        public static partial int ADsSetLastError(int error, string? errorString, string? provider);

        public sealed class EnumVariant
        {
            private static readonly object s_noMoreValues = new object();
            private object _currentValue = s_noMoreValues;
            private readonly IEnumVariant _enumerator;

            public EnumVariant(IEnumVariant en)
            {
                ArgumentNullException.ThrowIfNull(en);

                _enumerator = en;
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
                            _currentValue = Marshal.GetObjectForNativeVariant(addr)!;
#pragma warning restore 612, 618
                        }
                    }
                    finally
                    {
                        global::Interop.OleAut32.VariantClear(addr);
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
