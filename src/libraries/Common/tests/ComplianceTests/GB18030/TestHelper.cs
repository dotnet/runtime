// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Globalization.Tests;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace GB18030.Tests;

public static class TestHelper
{
    // New Code Points in existing ranges
    internal static IEnumerable<int> CjkNewCodePoints { get; } = CreateRange(0x9FF0, 0x9FFF);
    internal static IEnumerable<int> CjkExtensionANewCodePoints { get; } = CreateRange(0x4DB6, 0x4DBF);
    internal static IEnumerable<int> CjkExtensionBNewCodePoints { get; } = CreateRange(0x2A6D7, 0x2A6DF);
    internal static IEnumerable<int> CjkExtensionCNewCodePoints { get; } = CreateRange(0x2B735, 0x2B739);

    // New ranges
    internal static IEnumerable<int> CjkExtensionG { get; } = CreateRange(0x30000, 0x3134A);
    internal static IEnumerable<int> CjkExtensionH { get; } = CreateRange(0x31350, 0x323AF);
    internal static IEnumerable<int> CjkExtensionI { get; } = CreateRange(0x2EBF0, 0x2EE5D);

    private static IEnumerable<int> CreateRange(int first, int last) => Enumerable.Range(first, last - first + 1);

    private static IEnumerable<CharUnicodeInfoTestCase> s_gb18030CharUnicodeInfo { get; } = GetGB18030CharUnicodeInfo();
    private static IEnumerable<CharUnicodeInfoTestCase> GetGB18030CharUnicodeInfo()
    {
        const int CodePointsTotal = 9793; // Make sure a Unicode version downgrade doesn't make us lose coverage.

        var ret = CharUnicodeInfoTestData.TestCases.Where(tc => IsInGB18030Range(tc.CodePoint)).ToArray();
        Assert.Equal(CodePointsTotal, ret.Length);
        return ret;

        static bool IsInGB18030Range(int codePoint)
            => (codePoint >= 0x9FF0 && codePoint <= 0x9FFF) ||
            (codePoint >= 0x4DB6 && codePoint <= 0x4DBF) ||
            (codePoint >= 0x2A6D7 && codePoint <= 0x2A6DF) ||
            (codePoint >= 0x2B735 && codePoint <= 0x2B739) ||
            (codePoint >= 0x30000 && codePoint <= 0x3134A) ||
            (codePoint >= 0x31350 && codePoint <= 0x323AF) ||
            (codePoint >= 0x2EBF0 && codePoint <= 0x2EE5D);
    }

    internal static CultureInfo[] Cultures { get; } = [
        CultureInfo.CurrentCulture,
        CultureInfo.InvariantCulture,
        new CultureInfo("zh-CN")];

    internal static CompareOptions[] CompareOptions { get; } = [
        System.Globalization.CompareOptions.None,
        System.Globalization.CompareOptions.IgnoreCase];

    internal static StringComparison[] NonOrdinalStringComparisons { get; } = [
        StringComparison.CurrentCulture,
        StringComparison.CurrentCultureIgnoreCase,
        StringComparison.InvariantCulture,
        StringComparison.InvariantCultureIgnoreCase];

    // Detects whether the current platform has up-to-date GB18030 support. Some older
    // platforms (e.g. Windows Nano Server) lack it: they don't carry collation weights for
    // the GB18030-2022 code points and can't set the GB18030 console code page. On such
    // platforms U+4DBC (a CJK Extension A code point that GB18030-2022 covers) is treated as
    // ignorable in linguistic comparisons, so IndexOf finds it at index 0 instead of -1.
    public static bool IsGB18030Supported { get; } =
        "abc".IndexOf("\u4DBC", StringComparison.InvariantCulture) < 0;

    internal static string TestDataResourceName { get; } = "Level3+Amendment_Test_Data_for_Mid_to_High_Volume_cases.txt";

    internal static byte[] TestDataFileBytes { get; } = ReadTestDataFileBytes();

    private static byte[] ReadTestDataFileBytes()
    {
        using Stream stream = typeof(TestHelper).Assembly.GetManifestResourceStream(TestDataResourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource '{TestDataResourceName}'.");

        // The manifest resource stream is seekable and reports the exact resource size, so read the
        // whole thing into a right-sized buffer instead of growing a MemoryStream.
        byte[] bytes = new byte[stream.Length];
        int totalRead = 0;
        while (totalRead < bytes.Length)
        {
            int read = stream.Read(bytes, totalRead, bytes.Length - totalRead);
            if (read == 0)
                throw new EndOfStreamException($"Expected {bytes.Length} bytes from '{TestDataResourceName}' but only read {totalRead}.");
            totalRead += read;
        }

        // The data file is checked in as LF, but 'text=auto' materializes it as CRLF on Windows
        // checkouts and it is then embedded verbatim, so the embedded bytes depend on the build OS.
        // This broke the WASM legs: built on Windows (CRLF) but run on mono/WASM where the parser's
        // LF record delimiters weren't found, throwing from the static constructor and failing every
        // test. Normalize to LF so the data is identical regardless of build/run OS. Dropping every
        // 0x0D is safe: it can only be a CR, as it never appears inside a multi-byte GB18030 sequence.
        return bytes.Where(b => b != (byte)'\r').ToArray();
    }

    private static Encoding? s_gb18030Encoding;
    internal static Encoding GB18030Encoding
    {
        get
        {
            if (s_gb18030Encoding is null)
            {
#if !NETFRAMEWORK
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
                s_gb18030Encoding = Encoding.GetEncoding("gb18030");
            }

            return s_gb18030Encoding;
        }
    }

    private static readonly IEnumerable<byte[]> s_encodedTestData = GetTestData().ToArray();

    internal static IEnumerable<string> DecodedTestData { get; } = s_encodedTestData.Select(data => GB18030Encoding.GetString(data)).ToArray();

    private static readonly IEnumerable<string> s_splitNewLineDecodedTestData = DecodedTestData.SelectMany(
        data => data.Split(['\n'], StringSplitOptions.RemoveEmptyEntries)).ToArray();

    internal static IEnumerable<string> NonExceedingPathNameMaxDecodedTestData { get; } =
        s_splitNewLineDecodedTestData.SelectMany<string, string>(
        (data) =>
        {
            const int MaxPathSegmentName = 128;
            Encoding fileSystemEncoding = PlatformDetection.IsWindows ? Encoding.Unicode : Encoding.UTF8;

            if (fileSystemEncoding.GetByteCount(data) <= MaxPathSegmentName)
                return [data];

            List<string> result = new();
            string current = string.Empty;
            foreach (string element in GetTextElements(data))
            {
                if (fileSystemEncoding.GetByteCount(current) > MaxPathSegmentName)
                {
                    result.Add(current);
                    current = string.Empty;
                }
                current += element;
            }
            result.Add(current);
            return result;
        }).ToArray();

    public static IEnumerable<object[]> EncodedMemberData { get; } = s_encodedTestData.Select(data => new object[] { data }).ToArray();
    public static IEnumerable<object[]> DecodedMemberData { get; } = DecodedTestData.Select(data => new object[] { data }).ToArray();
    public static IEnumerable<object[]> NonExceedingPathNameMaxDecodedMemberData { get; } = NonExceedingPathNameMaxDecodedTestData.Select(data => new object[] { data }).ToArray();
    public static IEnumerable<object[]> GB18030CharUnicodeInfoMemberData { get; } = s_gb18030CharUnicodeInfo.Select(data => new object[] { data }).ToArray();

    private static IEnumerable<byte[]> GetTestData()
    {
        // The test data uses LF as its record separator (see ReadTestDataFileBytes, which normalizes
        // the embedded resource to LF). Parse against '\n' explicitly so the record boundaries never
        // depend on Environment.NewLine, which varies by runtime (LF on Unix/WASM, CRLF on Windows).
        byte[] startDelimiter = GB18030Encoding.GetBytes(":\n");
        byte[] endDelimiter = GB18030Encoding.GetBytes("\n\n");

        // Instead of inlining the data in source, parse the test data from an embedded resource to prevent encoding issues.
        ReadOnlyMemory<byte> testFileBytes = TestDataFileBytes;

        while (testFileBytes.Length > 0)
        {
            int start = testFileBytes.Span.IndexOf(startDelimiter);
            if (start == -1)
            {
                // Every record is introduced by its start delimiter, so anything left here means the
                // data file was modified unexpectedly. Fail loudly rather than silently running fewer cases.
                throw new InvalidDataException($"Unexpected trailing content in '{TestDataResourceName}'; expected a '{{label}}:' record delimiter.");
            }

            testFileBytes = testFileBytes.Slice(start + startDelimiter.Length);

            int end = testFileBytes.Span.IndexOf(endDelimiter);
            if (end == -1)
                end = testFileBytes.Length;

            yield return testFileBytes.Slice(0, end).ToArray();

            testFileBytes = testFileBytes.Slice(end);
        }

        // Add a few additional test cases to exercise test correctness.
        yield return GB18030Encoding.GetBytes("aaa");
        yield return GB18030Encoding.GetBytes("abc");
        yield return GB18030Encoding.GetBytes("𫓧𫓧");
    }

    internal static IEnumerable<string> GetTextElements(string input)
    {
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(input);
        while (enumerator.MoveNext())
        {
            yield return enumerator.GetTextElement();
        }
    }
}
