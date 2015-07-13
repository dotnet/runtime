// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_CopyTo_str : File_Copy_str_str
    {
        #region Utilities

        public override void Copy(string source, string dest)
        {
            new FileInfo(source).CopyTo(dest);
        }

        #endregion
    }
}
