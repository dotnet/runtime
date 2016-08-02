// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.InternalAbstractions;

namespace Microsoft.Extensions.EnvironmentAbstractions
{
    internal class DirectoryWrapper: IDirectory
    {
        public bool Exists(string path)
        {
            return Directory.Exists(path);
        }

        public ITemporaryDirectory CreateTemporaryDirectory()
        {
            return new TemporaryDirectory();
        }
    }
}