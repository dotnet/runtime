// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    // Fake CookieException without serialization to enable unit tests compiling for netstandard 1.3
    public class CookieException : FormatException
    {
        public CookieException() : base() { }
        public CookieException(string message) : base(message) { }
        public CookieException(string message, Exception inner) : base(message, inner) { }
    }
}
