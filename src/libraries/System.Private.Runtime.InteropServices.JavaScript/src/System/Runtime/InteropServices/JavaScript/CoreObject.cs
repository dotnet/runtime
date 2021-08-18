// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Core objects are the standard built-in objects and functions.
    /// </summary>
    /// <remarks>
    /// These objects are part of the JavaScript environment.  Not to be confused
    /// with objects provided by the host application or the browser context
    /// such as DOM.  For more information about the distinction between the
    /// DOM and core JavaScript, see JavaScript technologies overview:
    /// https://developer.mozilla.org/en-US/docs/Web/JavaScript/JavaScript_technologies_overview
    ///
    /// Core objects are treated differently in the bridge code as they are
    /// guaranteed to be there.
    /// </remarks>
    public abstract class CoreObject : JSObject
    {
        internal CoreObject(IntPtr jsHandle) : base(jsHandle)
        {
        }

        protected CoreObject(string typeName, params object[] _params) : base(typeName, _params)
        {
        }
    }
}
