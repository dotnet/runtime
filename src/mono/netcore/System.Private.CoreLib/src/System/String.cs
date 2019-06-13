// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System
{
	partial class String
	{
		[NonSerialized]
		int _stringLength;
		[NonSerialized]
		char _firstChar;

		[Intrinsic]
		public static readonly String Empty;

		public int Length => _stringLength;

		[IndexerName ("Chars")]
		public char this [int index] {
			[Intrinsic]
			get {
				if ((uint)index >= _stringLength)
					ThrowHelper.ThrowIndexOutOfRangeException ();

				return Unsafe.Add (ref _firstChar, index);
			}
		}

		public static String Intern (String str)
		{
			if (str == null)
				throw new ArgumentNullException(nameof(str));

			return InternalIntern (str);
		}

		public static String IsInterned (String str)
		{
			if (str == null)
				throw new ArgumentNullException(nameof(str));

			return InternalIsInterned (str);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static String FastAllocateString (int length);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static String InternalIsInterned (String str);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static String InternalIntern (String str);

		// TODO: Should be pointing to Buffer instead
		#region Runtime method-to-ir dependencies

		static unsafe void memset (byte *dest, int val, int len)
		{
			if (len < 8) {
				while (len != 0) {
					*dest = (byte)val;
					++dest;
					--len;
				}
				return;
			}
			if (val != 0) {
				val = val | (val << 8);
				val = val | (val << 16);
			}
			// align to 4
			int rest = (int)dest & 3;
			if (rest != 0) {
				rest = 4 - rest;
				len -= rest;
				do {
					*dest = (byte)val;
					++dest;
					--rest;
				} while (rest != 0);
			}
			while (len >= 16) {
				((int*)dest) [0] = val;
				((int*)dest) [1] = val;
				((int*)dest) [2] = val;
				((int*)dest) [3] = val;
				dest += 16;
				len -= 16;
			}
			while (len >= 4) {
				((int*)dest) [0] = val;
				dest += 4;
				len -= 4;
			}
			// tail bytes
			while (len > 0) {
				*dest = (byte)val;
				dest++;
				len--;
			}
		}

		static unsafe void memcpy (byte *dest, byte *src, int size)
		{
			Buffer.Memcpy (dest, src, size);
		}

		/* Used by the runtime */
		internal static unsafe void bzero (byte *dest, int len) {
			memset (dest, 0, len);
		}

		internal static unsafe void bzero_aligned_1 (byte *dest, int len) {
			((byte*)dest) [0] = 0;
		}

		internal static unsafe void bzero_aligned_2 (byte *dest, int len) {
			((short*)dest) [0] = 0;
		}

		internal static unsafe void bzero_aligned_4 (byte *dest, int len) {
			((int*)dest) [0] = 0;
		}

		internal static unsafe void bzero_aligned_8 (byte *dest, int len) {
			((long*)dest) [0] = 0;
		}

		internal static unsafe void memcpy_aligned_1 (byte *dest, byte *src, int size) {
			((byte*)dest) [0] = ((byte*)src) [0];
		}

		internal static unsafe void memcpy_aligned_2 (byte *dest, byte *src, int size) {
			((short*)dest) [0] = ((short*)src) [0];
		}

		internal static unsafe void memcpy_aligned_4 (byte *dest, byte *src, int size) {
			((int*)dest) [0] = ((int*)src) [0];
		}

		internal static unsafe void memcpy_aligned_8 (byte *dest, byte *src, int size) {
			((long*)dest) [0] = ((long*)src) [0];
		}

		#endregion

		// Certain constructors are redirected to CreateString methods with
		// matching argument list. The this pointer should not be used.
		//
		// TODO: Update runtime to call Ctor directly

		unsafe String CreateString (sbyte* value)
		{
			return Ctor (value);
		}

		unsafe String CreateString (sbyte* value, int startIndex, int length)
		{
			return Ctor (value, startIndex, length);
		}

		unsafe String CreateString (char* value)
		{
			return Ctor (value);
		}

		unsafe String CreateString (char* value, int startIndex, int length)
		{
			return Ctor (value, startIndex, length);
		}

		String CreateString (char[] val, int startIndex, int length)
		{
			return Ctor (val, startIndex, length);
		}

		String CreateString (char [] val)
		{
			return Ctor (val);
		}

		String CreateString (char c, int count)
		{
			return Ctor (c, count);
		}

		unsafe String CreateString (sbyte* value, int startIndex, int length, System.Text.Encoding enc)
		{
			return Ctor (value, startIndex, length, enc);
		}

		String CreateString (ReadOnlySpan<char> value)
		{
			return Ctor (value);
		}
	}
}