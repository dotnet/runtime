// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace GB18030.Tests;

public class FileTests : FileTestBase
{
    private static readonly byte[] s_expectedBytes = File.ReadAllBytes(TestHelper.s_testDataFilePath);
    private static readonly string s_expectedText = TestHelper.GB18030Encoding.GetString(s_expectedBytes);

    protected override void CreateFile(string path) => File.Create(path).Dispose();
    protected override void DeleteFile(string path) => File.Delete(path);
    protected override void MoveFile(string source, string destination) => File.Move(source, destination);
    protected override void CopyFile(string source, string destination) => File.Copy(source, destination);

    [Fact]
    public void ReadAllText()
    {
        Assert.Equal(s_expectedText, File.ReadAllText(TestHelper.s_testDataFilePath, TestHelper.GB18030Encoding));
    }

    [Fact]
    public void ReadAllLines()
    {
        Assert.Equal(
            s_expectedText.Split([Environment.NewLine], StringSplitOptions.None),
            File.ReadAllLines(TestHelper.s_testDataFilePath, TestHelper.GB18030Encoding));
    }

    [Fact]
    public void WriteAllText()
    {
        string tempFile = Path.Combine(TempDirectory.FullName, Path.GetRandomFileName());
        File.WriteAllText(tempFile, s_expectedText, TestHelper.GB18030Encoding);

        Assert.True(s_expectedBytes.AsSpan().SequenceEqual(File.ReadAllBytes(tempFile)));
    }

    [Fact]
    public void WriteAllLines()
    {
        string tempFile = Path.Combine(TempDirectory.FullName, Path.GetRandomFileName());
        string[] lines = s_expectedText.Split([Environment.NewLine], StringSplitOptions.None);
        File.WriteAllLines(tempFile, lines, TestHelper.GB18030Encoding);

        // WriteAllLines uses TextWriter.WriteLine which concats a newline to each provided line,
        // the result is the expected text with an additional newline at the end.
        byte[] expected = TestHelper.GB18030Encoding.GetBytes(s_expectedText + Environment.NewLine);
        Assert.True(expected.AsSpan().SequenceEqual(File.ReadAllBytes(tempFile)));
    }

    [Fact]
    public void AppendAllText()
    {
        string tempFile = Path.Combine(TempDirectory.FullName, Path.GetRandomFileName());
        const string initialContent = "Initial content: ";
        File.WriteAllText(tempFile, initialContent, TestHelper.GB18030Encoding);
        File.AppendAllText(tempFile, s_expectedText, TestHelper.GB18030Encoding);
        
        byte[] expected = TestHelper.GB18030Encoding.GetBytes(initialContent + s_expectedText);
        Assert.True(expected.AsSpan().SequenceEqual(File.ReadAllBytes(tempFile)));
    }
}
