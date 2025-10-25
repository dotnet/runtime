// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GB18030.Tests;

public class DirectoryTests : DirectoryTestBase
{
    protected override void CreateDirectory(string path) => Directory.CreateDirectory(path);
    protected override void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);
    protected override void MoveDirectory(string source, string destination) => Directory.Move(source, destination);

    [Theory]
    [MemberData(nameof(TestHelper.NonExceedingPathNameMaxDecodedMemberData), MemberType = typeof(TestHelper))]
    public void EnumerateFileSystemEntries(string gb18030Line)
    {
        string rootDir = TempDirectory.FullName;
        List<string> expected = [];

        string gb18030Dir = Path.Combine(rootDir, gb18030Line);
        Directory.CreateDirectory(gb18030Dir);
        expected.Add(gb18030Dir);

        string gb18030File = Path.Combine(rootDir, gb18030Line + ".txt");
        File.Create(gb18030File).Dispose();
        expected.Add(gb18030File);

        Assert.Equivalent(expected, Directory.EnumerateFileSystemEntries(rootDir));
    }
}
