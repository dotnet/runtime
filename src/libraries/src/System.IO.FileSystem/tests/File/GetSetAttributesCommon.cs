// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Tests
{
    // Concrete class to run common file attributes tests on the File class
    public class File_GetSetAttributesCommon : FileGetSetAttributes
    {
        protected override FileAttributes GetAttributes(string path) => File.GetAttributes(path);
        protected override void SetAttributes(string path, FileAttributes attributes) => File.SetAttributes(path, attributes);
    }
}
