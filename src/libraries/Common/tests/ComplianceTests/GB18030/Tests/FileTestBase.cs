// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace GB18030.Tests;

public abstract class FileTestBase : IDisposable
{
    protected abstract void CreateFile(string path);
    protected abstract void DeleteFile(string path);
    protected abstract void MoveFile(string source, string destination);
    protected abstract void CopyFile(string source, string destination);

    protected DirectoryInfo TempDirectory { get; }

    public FileTestBase()
    {
        TempDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
    }

    public void Dispose()
    {
        TempDirectory.Delete(true);
    }

    [Theory]
    [MemberData(nameof(TestHelper.NonExceedingPathNameMaxDecodedMemberData), MemberType = typeof(TestHelper))]
    public void Create(string gb18030Line)
    {
        string gb18030Path = Path.Combine(TempDirectory.FullName, gb18030Line);
        CreateFile(gb18030Path);

        var fileInfo = new FileInfo(gb18030Path);
        Assert.True(fileInfo.Exists);
        Assert.Equal(fileInfo.Name, gb18030Line);
    }

    [Theory]
    [MemberData(nameof(TestHelper.NonExceedingPathNameMaxDecodedMemberData), MemberType = typeof(TestHelper))]
    public void Delete(string gb18030Line)
    {
        string gb18030Path = Path.Combine(TempDirectory.FullName, gb18030Line);
        File.Create(gb18030Path).Dispose();
        Assert.True(File.Exists(gb18030Path));

        DeleteFile(gb18030Path);

        Assert.False(File.Exists(gb18030Path));
    }

    [Theory]
    [MemberData(nameof(TestHelper.NonExceedingPathNameMaxDecodedMemberData), MemberType = typeof(TestHelper))]
    public void Move(string gb18030Line)
    {
        string gb18030Path = Path.Combine(TempDirectory.FullName, gb18030Line);
        File.Create(gb18030Path).Dispose();

        string newPath = Path.Combine(TempDirectory.FullName, Path.GetRandomFileName());
        MoveFile(gb18030Path, newPath);
        Assert.True(File.Exists(newPath));
        Assert.False(File.Exists(gb18030Path));

        File.Move(newPath, gb18030Path);
        Assert.True(File.Exists(gb18030Path));
        Assert.False(File.Exists(newPath));
    }

    [Theory]
    [MemberData(nameof(TestHelper.NonExceedingPathNameMaxDecodedMemberData), MemberType = typeof(TestHelper))]
    public void Copy(string gb18030Line)
    {
        ReadOnlySpan<byte> sampleContent = "File_Copy"u8;
        string gb18030Path = Path.Combine(TempDirectory.FullName, gb18030Line);
        File.WriteAllBytes(gb18030Path, sampleContent.ToArray());

        string newPath = Path.Combine(TempDirectory.FullName, Path.GetRandomFileName());
        CopyFile(gb18030Path, newPath);
        Assert.True(File.Exists(newPath));

        File.Delete(gb18030Path);
        Assert.False(File.Exists(gb18030Path));

        CopyFile(newPath, gb18030Path);
        Assert.True(File.Exists(gb18030Path));
        Assert.True(sampleContent.SequenceEqual(File.ReadAllBytes(gb18030Path)));
    }
}
