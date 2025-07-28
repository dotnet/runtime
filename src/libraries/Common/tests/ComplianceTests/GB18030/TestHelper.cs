// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace GB18030.Tests;

public static class TestHelper
{
    internal static CultureInfo[] s_cultureInfos = [
        CultureInfo.CurrentCulture,
        CultureInfo.InvariantCulture,
        new CultureInfo("zh-CN")];

    internal static CompareOptions[] s_compareOptions = [
        CompareOptions.None,
        CompareOptions.IgnoreCase,
        CompareOptions.Ordinal,
        CompareOptions.OrdinalIgnoreCase];

    internal static readonly StringComparison[] s_ordinalStringComparisons = [
        StringComparison.Ordinal,
        StringComparison.OrdinalIgnoreCase];

    internal static readonly StringComparison[] s_nonOrdinalStringComparisons = [
        StringComparison.CurrentCulture,
        StringComparison.CurrentCultureIgnoreCase,
        StringComparison.InvariantCulture,
        StringComparison.InvariantCultureIgnoreCase];

    internal static readonly StringComparison[] s_allStringComparisons = [
        .. s_ordinalStringComparisons,
        .. s_nonOrdinalStringComparisons];

    internal static string s_testDataFilePath = Path.Combine(AppContext.BaseDirectory, "GB18030", "Level3+Amendment_Test_Data_for_Mid_to_High_Volume_cases.txt");

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

    private static readonly IEnumerable<byte[]> s_encodedTestData = GetTestData();
    internal static readonly IEnumerable<string> s_decodedTestData = s_encodedTestData.Select(data => GB18030Encoding.GetString(data));
    private static readonly IEnumerable<string> s_splitNewLineDecodedTestData = s_decodedTestData.SelectMany(
        data => data.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries));

    internal static readonly IEnumerable<string> s_nonExceedingPathNameMaxDecodedTestData =
        s_splitNewLineDecodedTestData.SelectMany<string, string>(
        (data) =>
        {
            const int MaxPathSegmentName = 255;
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
        });

    public static IEnumerable<object[]> EncodedTestData { get; } = s_encodedTestData.Select(data => new object[] { data });
    public static IEnumerable<object[]> DecodedTestData { get; } = s_decodedTestData.Select(data => new object[] { data });
    public static IEnumerable<object[]> NonExceedingPathNameMaxDecodedTestData { get; } = s_nonExceedingPathNameMaxDecodedTestData.Select(data => new object[] { data });

    private static IEnumerable<byte[]> GetTestData()
    {
        byte[] startDelimiter = GB18030Encoding.GetBytes($":{Environment.NewLine}");
        byte[] endDelimiter = GB18030Encoding.GetBytes($"{Environment.NewLine}{Environment.NewLine}");

        // Instead of inlining the data in source, parse the test data from the file to prevent encoding issues.
        ReadOnlyMemory<byte> testFileBytes = File.ReadAllBytes(s_testDataFilePath);

        while (testFileBytes.Length > 0)
        {
            int start = testFileBytes.Span.IndexOf(startDelimiter);
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
