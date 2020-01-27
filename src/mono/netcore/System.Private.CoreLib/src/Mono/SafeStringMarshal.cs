//
// Safe wrapper for a string and its UTF8 encoding
//
// Authors:
//   Aleksey Kliger <aleksey@xamarin.com>
//   Rodrigo Kumpera <kumpera@xamarin.com>
//
// Copyright 2016 Dot net foundation.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

namespace Mono  {
	internal struct SafeStringMarshal : IDisposable {
		readonly string str;
		IntPtr marshaled_string;

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static IntPtr StringToUtf8_icall (ref string str);

		public static IntPtr StringToUtf8 (string str)
		{
			return StringToUtf8_icall (ref str);
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static void GFree (IntPtr ptr);

		public SafeStringMarshal (string str) {
			this.str = str;
			this.marshaled_string = IntPtr.Zero;
		}

		public IntPtr Value {
			get {
				if (marshaled_string == IntPtr.Zero && str != null)
					marshaled_string = StringToUtf8 (str);
				return marshaled_string;
			}
		}

		public void Dispose () {
			if (marshaled_string != IntPtr.Zero) {
				GFree (marshaled_string);
				marshaled_string = IntPtr.Zero;
			}
		}
	}
}
