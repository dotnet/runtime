// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.DirectoryServices
{
    internal static partial class SafeNativeMethods
    {
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
                    global::Interop.OleAut32.VariantInit(addr);
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
