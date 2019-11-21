//
// Safe handle class for Mono.RuntimeGPtrArrayHandle
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

namespace Mono {
	internal struct SafeGPtrArrayHandle : IDisposable {
		RuntimeGPtrArrayHandle handle;

		internal SafeGPtrArrayHandle (IntPtr ptr)
		{
			handle = new RuntimeGPtrArrayHandle (ptr);
		}

		public void Dispose () {
			RuntimeGPtrArrayHandle.DestroyAndFree (ref handle);
		}

		internal int Length {
			get {
				return handle.Length;
			}
		}

		internal IntPtr this[int i] => handle[i];
	}


}
