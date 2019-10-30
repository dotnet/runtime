// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.Extensions.DependencyModel
{
    internal interface IFile
    {
        bool Exists(string path);

        string ReadAllText(string path);

        Stream OpenRead(string path);

        Stream OpenFile(
            string path,
            FileMode fileMode,
            FileAccess fileAccess,
            FileShare fileShare,
            int bufferSize,
            FileOptions fileOptions);

        void CreateEmptyFile(string path);
    }
}