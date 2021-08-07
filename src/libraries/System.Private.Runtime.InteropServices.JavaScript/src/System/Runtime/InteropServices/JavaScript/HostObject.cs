// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Host objects are object supplied by the host environment.
    /// </summary>
    /// <remarks>
    /// These objects are not part of the JavaScript environment and provided by the host application
    /// or the browser context such as DOM.  For more information about the distinction between the
    /// DOM and core JavaScript, see JavaScript technologies overview:
    /// https://developer.mozilla.org/en-US/docs/Web/JavaScript/JavaScript_technologies_overview
    ///
    /// Host objects are treated differently in the bridge code as they are not guaranteed to exist.
    /// </remarks>
    public interface IHostObject
    { }

    public class HostObject : HostObjectBase
    {
        public HostObject(string hostName, params object[] _params) : base(Interop.Runtime.New(hostName, _params))
        { }
    }

    public abstract class HostObjectBase : JSObject, IHostObject
    {
        protected HostObjectBase(int jsHandle) : base(jsHandle, true)
        {
            object result = Interop.Runtime.BindCoreObject(jsHandle, GCHandleValue, out int exception);
            if (exception != 0)
                throw new JSException(SR.Format(SR.HostObjectErrorBinding, result));
        }

        internal HostObjectBase(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }
    }
}
