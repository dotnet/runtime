// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.ComHost
{
    /// <summary>
    /// The provided resource id for the type library is unsupported.
    /// </summary>
    public class InvalidTypeLibraryIdException : Exception
    {
        public InvalidTypeLibraryIdException(string path, int id)
        {
            Path = path;
            Id = id;
        }

        public string Path { get; }

        public int Id { get; }
    }
}
