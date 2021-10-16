// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Tests
{
    // Concrete class to run common file attributes tests on the File class
    public class File_GetSetAttributesCommon : FileGetSetAttributes
    {
        protected override FileAttributes GetAttributes(string path) => File.GetAttributes(path);
        protected override FileAttributes GetAttributes(SafeFileHandle fileHandle) => File.GetAttributes(fileHandle);
        protected override void SetAttributes(string path, FileAttributes attributes) => File.SetAttributes(path, attributes);
        protected override void SetAttributes(SafeFileHandle fileHandle, FileAttributes attributes) => File.SetAttributes(fileHandle, attributes);
    }
}
