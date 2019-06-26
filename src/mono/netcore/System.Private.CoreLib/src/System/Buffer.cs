// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

#if BIT64
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
#endif

namespace System
{
	partial class Buffer
	{
		public static void BlockCopy (Array src, int srcOffset, Array dst, int dstOffset, int count)
		{
			if (src == null)
				throw new ArgumentNullException (nameof (src));
			if (dst == null)
				throw new ArgumentNullException ("dst");

			if (srcOffset < 0)
				throw new ArgumentOutOfRangeException (nameof (srcOffset), SR.ArgumentOutOfRange_MustBeNonNegInt32);
			if (dstOffset < 0)
				throw new ArgumentOutOfRangeException (nameof (dstOffset), SR.ArgumentOutOfRange_MustBeNonNegInt32);
			if (count < 0)
				throw new ArgumentOutOfRangeException (nameof (count), SR.ArgumentOutOfRange_MustBeNonNegInt32);
			if (!IsPrimitiveTypeArray (src))
				throw new ArgumentException (SR.Arg_MustBePrimArray, nameof (src));
			if (!IsPrimitiveTypeArray (dst))
				throw new ArgumentException (SR.Arg_MustBePrimArray, nameof (dst));

			var uCount = (nuint) count;
			var uSrcOffset = (nuint) srcOffset;
			var uDstOffset = (nuint) dstOffset;

			var uSrcLen = (nuint) ByteLength (src);
			var uDstLen = (nuint) ByteLength (dst);

			if (uSrcLen < uSrcOffset + uCount)
				throw new ArgumentException (SR.Argument_InvalidOffLen, "");
			if (uDstLen < uDstOffset + uCount)
				throw new ArgumentException (SR.Argument_InvalidOffLen, "");

			if (uCount != 0) {
				unsafe {
					fixed (byte* pSrc = &src.GetRawArrayData (), pDst = &dst.GetRawArrayData ()) {
						Memmove (pDst + uDstOffset, pSrc + uSrcOffset, uCount);
					}
				}
			}
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern int _ByteLength (Array array);

		static bool IsPrimitiveTypeArray (Array array)
		{
			// TODO: optimize			
			return array.GetType ().GetElementType ()!.IsPrimitive;
		}

		internal static unsafe void Memcpy (byte* dest, byte* src, int len) => Memmove (dest, src, (nuint) len);

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern unsafe void __Memmove (byte* dest, byte* src, nuint len);
	}
}
