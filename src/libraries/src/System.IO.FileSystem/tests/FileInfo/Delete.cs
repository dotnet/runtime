// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_Delete : File_Delete
    {
        public override void Delete(string path)
        {
            new FileInfo(path).Delete();
        }
    }
}
