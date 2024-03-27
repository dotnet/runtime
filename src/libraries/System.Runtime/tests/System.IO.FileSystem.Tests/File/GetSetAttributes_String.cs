// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Tests
{
    // Concrete class to run common file attributes tests on the File class
    public sealed class File_GetSetAttributes_String : FileGetSetAttributes
    {
        protected override FileAttributes GetAttributes(string path) => File.GetAttributes(path);
        protected override void SetAttributes(string path, FileAttributes attributes) => File.SetAttributes(path, attributes);
        protected override bool CanBeReadOnly => true;
    }
}
