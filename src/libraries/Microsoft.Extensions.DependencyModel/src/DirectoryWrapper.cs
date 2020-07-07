// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.Extensions.DependencyModel
{
    internal class DirectoryWrapper: IDirectory
    {
        public bool Exists(string path)
        {
            return Directory.Exists(path);
        }
    }
}