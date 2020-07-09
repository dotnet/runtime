// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.IO.Compression
{
    internal static partial class ZLibNative
    {
        /// <summary>
        /// ZLib stream descriptor data structure
        /// Do not construct instances of <code>ZStream</code> explicitly.
        /// Always use <code>ZLibNative.DeflateInit2_</code> or <code>ZLibNative.InflateInit2_</code> instead.
        /// Those methods will wrap this structure into a <code>SafeHandle</code> and thus make sure that it is always disposed correctly.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct ZStream
        {
            internal void Init()
            {
            }

            internal IntPtr nextIn;  //Bytef    *next_in;  /* next input byte */
            internal IntPtr nextOut; //Bytef    *next_out; /* next output byte should be put there */

            internal IntPtr msg;     //char     *msg;      /* last error message, NULL if no error */

            private readonly IntPtr internalState;    //internal state that is not visible to managed code

            internal uint availIn;   //uInt     avail_in;  /* number of bytes available at next_in */
            internal uint availOut;  //uInt     avail_out; /* remaining free space at next_out */
        }
    }
}
