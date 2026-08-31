// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.ComHost
{
    /// <summary>
    /// The provided type library file is an invalid format.
    /// </summary>
    public class InvalidTypeLibraryException : Exception
    {
        public InvalidTypeLibraryException(string path)
        {
            Path = path;
        }

        public InvalidTypeLibraryException(string path, Exception innerException)
            :base($"Invalid type library at '{path}'.", innerException)
        {
            Path = path;
        }

        public string Path { get; }
    }
}
