// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Tests
{
    public sealed class GetSetAttributes_SafeFileHandle : FileGetSetAttributes
    {
        protected override FileAttributes GetAttributes(string path)
        {
            using var fileHandle =
                File.OpenHandle(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            return File.GetAttributes(fileHandle);
        }

        protected override void SetAttributes(string path, FileAttributes attributes)
        {
            using var fileHandle =
                File.OpenHandle(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            File.SetAttributes(fileHandle, attributes);
        }
    }
}
