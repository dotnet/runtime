// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

#if BIT64
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
#endif

namespace System
{
	partial class Buffer
	{
		[MethodImpl (MethodImplOptions.InternalCall)]
		public static extern void BlockCopy (Array src, int srcOffset, Array dst, int dstOffset, int count);

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern int _ByteLength (Array array);

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern bool IsPrimitiveTypeArray (Array array);

		internal static unsafe void Memcpy (byte* dest, byte* src, int len) => Memmove (dest, src, (nuint) len);

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern unsafe void __Memmove (byte* dest, byte* src, nuint len);
	}
}
