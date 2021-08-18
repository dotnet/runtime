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

    public class HostObject : JSObject, IHostObject
    {
        public HostObject(string typeName, params object[] _params) : base(typeName, _params)
        { }
    }
}
