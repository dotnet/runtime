// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Extensions.DependencyModel
{
    internal sealed class DirectoryWrapper : IDirectory
    {
        public bool Exists([NotNullWhen(true)] string? path)
        {
            return Directory.Exists(path);
        }
    }
}
