// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace GB18030.Tests;

public abstract class DirectoryTestBase : IDisposable
{
    protected abstract void CreateDirectory(string path);
    protected abstract void DeleteDirectory(string path, bool recursive);
    protected abstract void MoveDirectory(string source, string destination);

    protected DirectoryInfo TempDirectory { get; }

    public DirectoryTestBase()
    {
        TempDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
    }

    [Theory]
    [MemberData(nameof(TestHelper.NonExceedingPathNameMaxDecodedMemberData), MemberType = typeof(TestHelper))]
    public void Create(string gb18030Line)
    {
        string gb18030Path = Path.Combine(TempDirectory.FullName, gb18030Line);
        CreateDirectory(gb18030Path);

        var dirInfo = new DirectoryInfo(gb18030Path);
        Assert.True(dirInfo.Exists);
        Assert.Equal(gb18030Line, dirInfo.Name);
    }

    public static IEnumerable<object[]> Delete_MemberData() =>
        TestHelper.NonExceedingPathNameMaxDecodedTestData.SelectMany(testData =>
        new int[] { 0, 2, 8 }.Select(recurseLevel => new object[] { testData, recurseLevel }));

    [Theory]
    [MemberData(nameof(Delete_MemberData))]
    public void Delete(string gb18030Line, int recurseLevel)
    {
        string firstPath = Path.Combine(TempDirectory.FullName, gb18030Line);
        string nestedDirPath = Path.Combine(firstPath, Path.Combine(Enumerable.Repeat(gb18030Line, recurseLevel).ToArray()));
        Assert.True(recurseLevel > 0 || firstPath.Equals(nestedDirPath));

        Directory.CreateDirectory(nestedDirPath);
        Assert.True(Directory.Exists(nestedDirPath));

        DeleteDirectory(firstPath, recursive: recurseLevel > 0);

        Assert.False(Directory.Exists(firstPath));
    }

    [Theory]
    [MemberData(nameof(TestHelper.NonExceedingPathNameMaxDecodedMemberData), MemberType = typeof(TestHelper))]
    public void Move(string gb18030Line)
    {
        string gb18030Path = Path.Combine(TempDirectory.FullName, gb18030Line);
        Directory.CreateDirectory(gb18030Path);
        Assert.True(Directory.Exists(gb18030Path));

        string newPath = Path.Combine(TempDirectory.FullName, Path.GetRandomFileName());
        MoveDirectory(gb18030Path, newPath);
        Assert.True(Directory.Exists(newPath));
        Assert.False(Directory.Exists(gb18030Path));

        MoveDirectory(newPath, gb18030Path);
        Assert.True(Directory.Exists(gb18030Path));
        Assert.False(Directory.Exists(newPath));
    }

    public void Dispose()
    {
        TempDirectory.Delete(true);
    }
}
