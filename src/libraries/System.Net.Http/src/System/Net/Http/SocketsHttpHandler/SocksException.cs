// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.Http
{
    internal sealed class SocksException : IOException
    {
        public SocksException(string message) : base(message) { }

        public SocksException(string message, Exception innerException) : base(message, innerException) { }
    }
}
