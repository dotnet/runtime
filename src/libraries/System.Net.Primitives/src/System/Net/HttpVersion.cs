// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    public static class HttpVersion
    {
        /// <summary>Defines a <see cref="Version"/> instance that indicates an unknown version of HTTP.</summary>
        public static readonly Version Unknown = new Version(0, 0);
        /// <summary>Defines a <see cref="Version"/> instance for HTTP 1.0.</summary>
        public static readonly Version Version10 = new Version(1, 0);
        /// <summary>Defines a <see cref="Version"/> instance for HTTP 1.1.</summary>
        public static readonly Version Version11 = new Version(1, 1);
        /// <summary>Defines a <see cref="Version"/> instance for HTTP 2.0.</summary>
        public static readonly Version Version20 = new Version(2, 0);
        /// <summary>Defines a <see cref="Version"/> instance for HTTP 3.0.</summary>
        public static readonly Version Version30 = new Version(3, 0);
    }
}
