// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Represents an Exception initiated from the JavaScript interop code.
    /// </summary>
    [SupportedOSPlatform("browser")] // @kg: Do we really need to platform-lock JSException?
    public sealed class JSException : Exception
    {
        // TODO after https://github.com/dotnet/runtime/issues/70133

        public JSException(string msg) : base(msg)
        {
        }

        public override string ToString()
        {
            // skip the expensive stack trace
            return Message;
        }
    }
}
