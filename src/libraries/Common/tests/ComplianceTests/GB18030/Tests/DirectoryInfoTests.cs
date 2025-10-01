// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GB18030.Tests;

public class DirectoryInfoTests : DirectoryTestBase
{
    protected override void CreateDirectory(string path) => new DirectoryInfo(path).Create();
    protected override void DeleteDirectory(string path, bool recursive) => new DirectoryInfo(path).Delete(recursive);
    protected override void MoveDirectory(string source, string destination) => new DirectoryInfo(source).MoveTo(destination);

    [Theory]
    [MemberData(nameof(TestHelper.NonExceedingPathNameMaxDecodedMemberData), MemberType = typeof(TestHelper))]
    public void CreateSubdirectory(string gb18030Line)
    {
        var subDirInfo = TempDirectory.CreateSubdirectory(gb18030Line);

        Assert.True(subDirInfo.Exists);
        Assert.Equal(gb18030Line, subDirInfo.Name);
        Assert.Equal(Path.Combine(TempDirectory.FullName, gb18030Line), subDirInfo.FullName);
    }

    [Theory]
    [MemberData(nameof(TestHelper.NonExceedingPathNameMaxDecodedMemberData), MemberType = typeof(TestHelper))]
    public void EnumerateFileSystemInfos(string gb18030Line)
    {
        string rootDir = TempDirectory.FullName;
        List<FileSystemInfo> expected = [];

        string gb18030Dir = Path.Combine(rootDir, gb18030Line);
        var dirInfo = new DirectoryInfo(gb18030Dir);
        dirInfo.Create();
        expected.Add(dirInfo);

        string gb18030File = Path.Combine(rootDir, gb18030Line + ".txt");
        var fileInfo = new FileInfo(gb18030File);
        fileInfo.Create().Dispose();
        expected.Add(fileInfo);

        Assert.Equivalent(expected, new DirectoryInfo(rootDir).EnumerateFileSystemInfos());
    }
}
