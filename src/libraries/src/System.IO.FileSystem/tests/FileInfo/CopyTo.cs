// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_CopyTo_str : File_Copy_str_str
    {
        public override void Copy(string source, string dest)
        {
            new FileInfo(source).CopyTo(dest);
        }
    }

    public class FileInfo_CopyTo_str_b : File_Copy_str_str_b
    {
        public override void Copy(string source, string dest)
        {
            new FileInfo(source).CopyTo(dest, false);
        }

        public override void Copy(string source, string dest, bool overwrite)
        {
            new FileInfo(source).CopyTo(dest, overwrite);
        }
    }
}
