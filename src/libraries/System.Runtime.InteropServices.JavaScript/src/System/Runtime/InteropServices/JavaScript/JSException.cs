// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Represents an Exception initiated from the JavaScript interop code.
    /// </summary>
    public class JSException : Exception
    {
        public JSException(string msg) : base(msg) { }
    }
}
