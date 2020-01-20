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
		static bool IsPrimitiveTypeArray (Array array) => array.IsPrimitive ();

		internal static unsafe void Memcpy (byte* dest, byte* src, int len) => Memmove (dest, src, (nuint) len);

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern unsafe void __Memmove (byte* dest, byte* src, nuint len);

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern void BulkMoveWithWriteBarrier (ref byte dmem, ref byte smem, nuint size);

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static unsafe void _ZeroMemory(ref byte b, nuint byteLength)
		{
			fixed (byte* bytePointer = &b) {
				__ZeroMemory (bytePointer, byteLength);
			}
		}
		
		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern unsafe void __ZeroMemory (void* p, nuint byteLength);
	}
}
