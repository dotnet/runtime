// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58707", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowserOnWindows), nameof(PlatformDetection.IsMonoAOT))]
    public class FileInfo_CopyTo_str : File_Copy_str_str
    {
        protected override void Copy(string source, string dest)
        {
            new FileInfo(source).CopyTo(dest);
        }
    }

    public class FileInfo_CopyTo_str_b : File_Copy_str_str_b
    {
        protected override void Copy(string source, string dest)
        {
            new FileInfo(source).CopyTo(dest, false);
        }

        protected override void Copy(string source, string dest, bool overwrite)
        {
            new FileInfo(source).CopyTo(dest, overwrite);
        }
    }
}
