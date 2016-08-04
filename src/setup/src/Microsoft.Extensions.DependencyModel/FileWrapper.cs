// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Extensions.DependencyModel
{
    internal class FileWrapper: IFile
    {
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public Stream OpenFile(
            string path,
            FileMode fileMode,
            FileAccess fileAccess,
            FileShare fileShare,
            int bufferSize,
            FileOptions fileOptions)
        {
            return new FileStream(path, fileMode, fileAccess, fileShare, bufferSize, fileOptions);
        }

        public void CreateEmptyFile(string path)
        {
            try
            {
                var emptyFile = File.Create(path);
                if (emptyFile != null)
                {
                    emptyFile.Dispose();
                }
            }
            catch { }
        }
    }
}