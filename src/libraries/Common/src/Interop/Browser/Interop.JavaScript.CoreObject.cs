// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
internal static partial class Interop
{
    internal static partial class JavaScript
    {
        public abstract class CoreObject : JSObject
        {

            protected CoreObject(int js_handle) : base(js_handle)
            {
                var result = Runtime.BindCoreObject(js_handle, (int)(IntPtr)Handle, out int exception);
                if (exception != 0)
                    throw new JSException($"CoreObject Error binding: {result.ToString()}");

            }

            internal CoreObject(IntPtr js_handle) : base(js_handle)
            { }
        }
    }
}
