// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
internal static partial class Interop
{

    internal static partial class Runtime
    {
        public class AnyRef
        {

            public int JSHandle { get; internal set; }
            internal GCHandle Handle;

            internal AnyRef(int js_handle)
            {
                //Console.WriteLine ($"AnyRef: {js_handle}");
                this.JSHandle = js_handle;
                this.Handle = GCHandle.Alloc(this);
            }

            internal AnyRef(IntPtr js_handle)
            {
                this.JSHandle = (int)js_handle;
                this.Handle = GCHandle.Alloc(this);
            }
        }
    }
}
