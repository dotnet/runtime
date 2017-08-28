// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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