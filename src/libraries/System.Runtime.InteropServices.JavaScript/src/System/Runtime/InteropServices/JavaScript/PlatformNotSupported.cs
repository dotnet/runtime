// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    // TODO more after https://github.com/dotnet/runtime/issues/70133
    [SupportedOSPlatform("browser")]
    public class JSObject : IDisposable
    {
        internal JSObject() => throw new PlatformNotSupportedException();
        public bool IsDisposed { get => throw new PlatformNotSupportedException(); }
        public void Dispose() => throw new PlatformNotSupportedException();
    }
    [SupportedOSPlatform("browser")]
    public sealed class JSException : Exception
    {
        public JSException(string msg) => throw new PlatformNotSupportedException();
    }
}
