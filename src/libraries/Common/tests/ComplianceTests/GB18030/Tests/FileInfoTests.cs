// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace GB18030.Tests;

public class FileInfoTests : FileTestBase
{
    protected override void CreateFile(string path) => new FileInfo(path).Create().Dispose();
    protected override void DeleteFile(string path) => new FileInfo(path).Delete();
    protected override void MoveFile(string source, string destination) => new FileInfo(source).MoveTo(destination);
    protected override void CopyFile(string source, string destination) => new FileInfo(source).CopyTo(destination);
}
