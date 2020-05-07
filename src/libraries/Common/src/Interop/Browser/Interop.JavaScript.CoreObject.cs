// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal static partial class Interop
{
    internal static partial class JavaScript
    {
        /// <summary>
        /// Core objects are the standard built-in objects and functions.  These
        /// objects are part of the JavaScript environment.  Not to be confused
        /// with objects provided by the host application or the browser context
        /// such as DOM.  For more information about the distinction between the
        /// DOM and core JavaScript, see JavaScript technologies overview:
        /// https://developer.mozilla.org/en-US/docs/Web/JavaScript/JavaScript_technologies_overview
        ///
        /// Core objects are treated differently in the bridge code as they are
        /// guaranteed to be there.
        /// </summary>
        public abstract class CoreObject : JSObject
        {
            protected CoreObject(int jsHandle) : base(jsHandle)
            {
                var result = Runtime.BindCoreObject(jsHandle, (int)(IntPtr)Handle, out int exception);
                if (exception != 0)
                    throw new JSException($"CoreObject Error binding: {result.ToString()}");

            }

            internal CoreObject(IntPtr js_handle) : base(js_handle)
            { }
        }
    }
}
