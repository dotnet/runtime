// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.ComHost
{
    /// <summary>
    /// The specified type library path does not exist.
    /// </summary>
    public class TypeLibraryDoesNotExistException : Exception
    {
        public TypeLibraryDoesNotExistException(string path, Exception innerException)
            :base($"Type library '{path}' does not exist.", innerException)
        {
            Path = path;
        }

        public string Path { get; }
    }
}
