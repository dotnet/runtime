// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Safe handle class for Mono.RuntimeGPtrArrayHandle
//
// Authors:
//   Aleksey Kliger <aleksey@xamarin.com>
//   Rodrigo Kumpera <kumpera@xamarin.com>
//
//

using System;

namespace Mono
{
    internal struct SafeGPtrArrayHandle : IDisposable
    {
        private RuntimeGPtrArrayHandle handle;

        internal SafeGPtrArrayHandle(IntPtr ptr)
        {
            handle = new RuntimeGPtrArrayHandle(ptr);
        }

        public void Dispose()
        {
            RuntimeGPtrArrayHandle.DestroyAndFree(ref handle);
        }

        internal int Length
        {
            get
            {
                return handle.Length;
            }
        }

        internal IntPtr this[int i] => handle[i];
    }


}
