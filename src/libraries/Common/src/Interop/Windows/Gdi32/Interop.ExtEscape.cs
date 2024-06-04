// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
#if NET
using System.Runtime.InteropServices.Marshalling;
#endif

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        internal const int QUERYESCSUPPORT = 8;
        internal const int CHECKJPEGFORMAT = 4119;
        internal const int CHECKPNGFORMAT = 4120;

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial int ExtEscape(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC, int nEscape, int cbInput, ref int inData, int cbOutput, out int outData);

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial int ExtEscape(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC, int nEscape, int cbInput, byte[] inData, int cbOutput, out int outData);
    }
}
