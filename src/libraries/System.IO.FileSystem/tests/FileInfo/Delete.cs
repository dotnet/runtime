// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_Delete : File_Delete
    {
        public override void Delete(string path)
        {
            new FileInfo(path).Delete();
        }
    }
}
