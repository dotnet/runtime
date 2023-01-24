// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class GetSetAttributes_SafeFileHandle : FileGetSetAttributes
    {
        protected virtual SafeFileHandle OpenFileHandle(string path, FileAccess fileAccess) =>
            File.OpenHandle(
                path,
                FileMode.OpenOrCreate,
                fileAccess,
                FileShare.None);

        protected override bool CanBeReadOnly => false;

        protected override FileAttributes GetAttributes(string path)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.Read);
            return File.GetAttributes(fileHandle);
        }
        
        protected override void SetAttributes(string path, FileAttributes attributes)
        {
            using SafeFileHandle fileHandle = OpenFileHandle(path, FileAccess.ReadWrite);
            File.SetAttributes(fileHandle, attributes);
        }

        [Fact]
        public void NullArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.GetAttributes(default(SafeFileHandle)!));
            Assert.Throws<ArgumentNullException>("fileHandle", static () => File.SetAttributes(default(SafeFileHandle)!, (FileAttributes)0));
        }
    }
}
