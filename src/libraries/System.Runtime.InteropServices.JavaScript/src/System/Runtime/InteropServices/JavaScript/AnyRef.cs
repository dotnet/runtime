// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices.JavaScript
{
    public class AnyRef
    {
        public int JSHandle { get; internal set; }
        internal GCHandle Handle;

        internal AnyRef(int jsHandle)
        {
            JSHandle = jsHandle;
            Handle = GCHandle.Alloc(this);
        }

        internal AnyRef(IntPtr jsHandle)
        {
            JSHandle = (int)jsHandle;
            Handle = GCHandle.Alloc(this);
        }
    }
}
