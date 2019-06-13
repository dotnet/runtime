// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

#if BIT64
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
#endif

namespace System.Runtime
{
	static class RuntimeImports
	{
		internal static unsafe void RhZeroMemory (ref byte b, nuint byteLength)
		{
			fixed (byte* bytePointer = &b) {
				ZeroMemory (bytePointer, byteLength);
			}
		}

		internal static unsafe void RhZeroMemory (IntPtr p, UIntPtr byteLength) => ZeroMemory ((void*) p, (nuint) byteLength);

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern unsafe void ZeroMemory (void* p, nuint byteLength);

		[MethodImpl (MethodImplOptions.InternalCall)]
		internal extern static void RhBulkMoveWithWriteBarrier (ref byte dmem, ref byte smem, nuint size);
	}
}
