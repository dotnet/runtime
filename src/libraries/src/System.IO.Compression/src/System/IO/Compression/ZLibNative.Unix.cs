// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            internal IntPtr nextIn;     //Bytef    *next_in;  /* next input byte */
            internal UIntPtr availIn;   //uInt     avail_in;  /* number of bytes available at next_in */
            internal UIntPtr totalIn;   //uLong    total_in;  /* total nb of input bytes read so far */

            internal IntPtr nextOut;    //Bytef    *next_out; /* next output byte should be put there */
            internal UIntPtr availOut;  //uInt     avail_out; /* remaining free space at next_out */
            internal UIntPtr totalOut;  //uLong    total_out; /* total nb of bytes output so far */

            internal IntPtr msg;        //char     *msg;      /* last error message, NULL if no error */

            internal IntPtr state;      //struct internal_state FAR *state; /* not visible by applications */

            internal IntPtr zalloc;     //alloc_func zalloc;  /* used to allocate the internal state */
            internal IntPtr zfree;      //free_func  zfree;   /* used to free the internal state */
            internal IntPtr opaque;     //voidpf   opaque;    /* private data object passed to zalloc and zfree */

            internal int dataType;      //int      data_type; /* best guess about the data type: binary or text */
            internal UIntPtr adler;     //uLong    adler;     /* adler32 value of the uncompressed data */
            internal UIntPtr reserved;  //uLong    reserved;  /* reserved for future use */
        }

        /// <summary>Casts a uint to a native zlib uLong.</summary>
        private static UIntPtr CastUInt32ToNativeuLong(uint value)
        {
            // Several members of ZStream are defined by zlib as uLong, which is 32-bits on
            // Windows, but 32-bits on 32-bit Unix and 64-bits on 64-bit Unix.  The consuming code
            // works in terms of a 32-bit value, so we need to cast back and forth in
            // a platform-agnostic way, hence we have a PAL helper on Unix and one on Windows.
            return (UIntPtr)value;
        }
    }
}
