// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.InteropServices.Marshalling;
#endif

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        [LibraryImport(Libraries.Gdi32, EntryPoint = "StartDocW", SetLastError = true)]
        internal static partial int StartDoc(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC, in DOCINFO lpDocInfo);

#if NET
        [NativeMarshalling(typeof(Marshaller))]
#endif
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct DOCINFO
        {
            internal int cbSize = 20;
            internal string? lpszDocName;
            internal string? lpszOutput;
            internal string? lpszDatatype;
            internal int fwType;

            public DOCINFO() { }

#if NET
            [CustomMarshaller(typeof(DOCINFO), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
            public static class Marshaller
            {
                public static Native ConvertToUnmanaged(DOCINFO managed) => new(managed);
                public static void Free(Native native) => native.FreeNative();

                internal struct Native
                {
                    internal int cbSize;
                    internal IntPtr lpszDocName;
                    internal IntPtr lpszOutput;
                    internal IntPtr lpszDatatype;
                    internal int fwType;

                    public Native(DOCINFO docInfo)
                    {
                        cbSize = docInfo.cbSize;
                        lpszDocName = Marshal.StringToCoTaskMemAuto(docInfo.lpszDocName);
                        lpszOutput = Marshal.StringToCoTaskMemAuto(docInfo.lpszOutput);
                        lpszDatatype = Marshal.StringToCoTaskMemAuto(docInfo.lpszDatatype);
                        fwType = docInfo.fwType;
                    }

                    public void FreeNative()
                    {
                        Marshal.FreeCoTaskMem(lpszDocName);
                        Marshal.FreeCoTaskMem(lpszOutput);
                        Marshal.FreeCoTaskMem(lpszDatatype);
                    }
                }
            }
#endif
        }
    }
}
