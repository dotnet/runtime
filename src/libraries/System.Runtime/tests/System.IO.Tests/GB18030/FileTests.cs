// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace GB18030.Tests;

public class FileTests : FileTestBase
{
    private static readonly byte[] s_expectedBytes = TestHelper.TestDataFileBytes;
    private static readonly string s_expectedText = TestHelper.GB18030Encoding.GetString(s_expectedBytes);

    protected override void CreateFile(string path) => File.Create(path).Dispose();
    protected override void DeleteFile(string path) => File.Delete(path);
    protected override void MoveFile(string source, string destination) => File.Move(source, destination);
    protected override void CopyFile(string source, string destination) => File.Copy(source, destination);

    [Fact]
    public void ReadAllText()
    {
        string tempFile = Path.Combine(TempDirectory.FullName, Path.GetRandomFileName());
        File.WriteAllBytes(tempFile, s_expectedBytes);

        Assert.Equal(s_expectedText, File.ReadAllText(tempFile, TestHelper.GB18030Encoding));
    }

    [Fact]
    public void ReadAllLines()
    {
        string tempFile = Path.Combine(TempDirectory.FullName, Path.GetRandomFileName());
        File.WriteAllBytes(tempFile, s_expectedBytes);

        Assert.Equal(
            SplitLines(s_expectedText),
            File.ReadAllLines(tempFile, TestHelper.GB18030Encoding));
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
        string[] lines = SplitLines(s_expectedText);
        File.WriteAllLines(tempFile, lines, TestHelper.GB18030Encoding);

        // WriteAllLines uses TextWriter.WriteLine which concats Environment.NewLine to each provided line,
        // so the expected content is the lines rejoined with Environment.NewLine plus a trailing one.
        byte[] expected = TestHelper.GB18030Encoding.GetBytes(string.Join(Environment.NewLine, lines) + Environment.NewLine);
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

    // Splits text the same way File.ReadAllLines does: on '\n', trimming an optional trailing '\r',
    // so comparisons are robust regardless of whether the data uses LF or CRLF line endings.
    private static string[] SplitLines(string text) =>
        text.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
}
