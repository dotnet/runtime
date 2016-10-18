// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    // Fake CookieException without serialization to enable unit tests compiling for netstandard 1.3
    public class CookieException : FormatException
    {
        public CookieException() : base() { }
        internal CookieException(string message) : base(message) { }
        internal CookieException(string message, Exception inner) : base(message, inner) { }
    }
}
