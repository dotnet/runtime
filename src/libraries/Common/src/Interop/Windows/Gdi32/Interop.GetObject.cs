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
        [LibraryImport(Libraries.Gdi32, EntryPoint = "GetObjectW", SetLastError = true)]
        internal static partial int GetObject(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hObject, int nSize, ref BITMAP bm);

        [LibraryImport(Libraries.Gdi32, EntryPoint = "GetObjectW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int GetObject(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hObject, int nSize, ref Interop.User32.LOGFONT lf);

        internal static unsafe int GetObject(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hObject, ref Interop.User32.LOGFONT lp)
            => GetObject(hObject, sizeof(Interop.User32.LOGFONT), ref lp);

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAP
        {
            public uint bmType;
            public uint bmWidth;
            public uint bmHeight;
            public uint bmWidthBytes;
            public ushort bmPlanes;
            public ushort bmBitsPixel;
            public IntPtr bmBits;
        }
    }
}
