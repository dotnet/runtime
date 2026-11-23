// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using Xunit;

namespace System.Xml.Tests
{
    public class AttributeValueScanningTests
    {
        private delegate int AttributeScanner(ReadOnlySpan<char> value);
        private delegate int TextScanner(ReadOnlySpan<char> value, ref int orChars);

        [DynamicDependency("FindAttributeValueEnd", "System.Xml.XmlCharType", "System.Private.Xml")]
        private static AttributeScanner GetScanner() =>
            typeof(XmlReader).Assembly.GetType("System.Xml.XmlCharType", throwOnError: true)!
                .GetMethod("FindAttributeValueEnd", BindingFlags.Static | BindingFlags.NonPublic)!
                .CreateDelegate<AttributeScanner>();

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(127)]
        [DynamicDependency("FindTextEnd", "System.Xml.XmlCharType", "System.Private.Xml")]
        public void TextScanner_AllCharactersWhitespaceAndBounds(int length)
        {
            TextScanner scan = typeof(XmlReader).Assembly.GetType("System.Xml.XmlCharType", throwOnError: true)!
                .GetMethod("FindTextEnd", BindingFlags.Static | BindingFlags.NonPublic)!
                .CreateDelegate<TextScanner>();
            foreach (PoisonPagePlacement placement in new[] { PoisonPagePlacement.Before, PoisonPagePlacement.After })
            {
                using BoundedMemory<char> memory = BoundedMemory.Allocate<char>(length, placement);
                Span<char> value = memory.Span;
                value.Fill(' ');
                int accumulator = 0;
                Assert.Equal(length, scan(value, ref accumulator));
                Assert.True(accumulator <= 0x20);
                if (length == 0)
                    continue;

                foreach (int offset in new[] { 0, length / 2, length - 1 })
                {
                    for (int c = 0; c <= char.MaxValue; c++)
                    {
                        char ch = (char)c;
                        value[offset] = ch;
                        bool allowed = XmlConvert.IsXmlChar(ch) && ch is not ('\n' or '\r' or '\t' or '<' or '&' or ']');
                        accumulator = 0;
                        Assert.Equal(allowed ? length : offset, scan(value, ref accumulator));
                        Assert.Equal(allowed && ch != ' ', accumulator > 0x20);
                        accumulator = 0xFF;
                        Assert.Equal(allowed ? length : offset, scan(value, ref accumulator));
                        Assert.True(accumulator > 0x20);
                    }
                    value[offset] = ' ';
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(127)]
        public void AttributeScanner_AllCharactersAndBounds(int length)
        {
            AttributeScanner scan = GetScanner();
            foreach (PoisonPagePlacement placement in new[] { PoisonPagePlacement.Before, PoisonPagePlacement.After })
            {
                using BoundedMemory<char> memory = BoundedMemory.Allocate<char>(length, placement);
                Span<char> value = memory.Span;
                value.Fill('x');
                Assert.Equal(length, scan(value));
                if (length == 0)
                    continue;

                foreach (int offset in new[] { 0, length / 2, length - 1 })
                {
                    for (int c = 0; c <= char.MaxValue; c++)
                    {
                        char ch = (char)c;
                        value[offset] = ch;
                        bool allowed = XmlConvert.IsXmlChar(ch) && ch is not ('\n' or '\r' or '\t' or '<' or '>' or '&' or '\'' or '"');
                        Assert.Equal(allowed ? length : offset, scan(value));
                    }
                    value[offset] = 'x';
                }
            }
        }

        public static IEnumerable<object[]> AttributeBoundaryCases()
        {
            foreach (int length in new[] { 0, 15, 16, 17, 31, 32, 33, 63, 64, 65, 4095, 4096, 4097 })
            {
                foreach (bool async in new[] { false, true })
                {
                    yield return new object[] { length, async };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AttributeBoundaryCases))]
        public async Task AttributeValues_UnicodeEntitiesAndNormalization(int length, bool async)
        {
            string prefix = new string('x', length);
            foreach ((string raw, string expected) in new[]
            {
                ("", ""), ("' > \u007f", "' > \u007f"), ("\u00e9\u4e2d\ud83d\ude42", "\u00e9\u4e2d\ud83d\ude42"),
                ("&lt;&amp;&quot;&#x9;&#xA;&#xD;", "<&\"\t\n\r"), ("\r\n\t\n\r", "    ")
            })
            {
                string xml = $"<root value=\"{prefix}{raw}{prefix}\"/>";
                using FragmentedAttributeReader source = new(xml);
                using XmlReader reader = XmlReader.Create(source, new XmlReaderSettings { Async = async });
                Assert.True(async ? await reader.ReadAsync() : reader.Read());
                Assert.Equal(prefix + expected + prefix, reader.GetAttribute("value"));
                Assert.True(reader.IsEmptyElement);
                Assert.False(async ? await reader.ReadAsync() : reader.Read());
            }
        }

        [Theory]
        [MemberData(nameof(AttributeBoundaryCases))]
        public async Task AttributeValues_InvalidCharacters(int length, bool async)
        {
            foreach (string invalid in new[] { "\0", "\u0001", "\ud800", "\udc00", "\ufffe", "\uffff", "<", "&missing;" })
            {
                using FragmentedAttributeReader source = new($"<root value=\"{new string('x', length)}{invalid}\"/>");
                using XmlReader reader = XmlReader.Create(source, new XmlReaderSettings { Async = async });
                if (async)
                    await Assert.ThrowsAsync<XmlException>(async () => { while (await reader.ReadAsync()) { } });
                else
                    Assert.Throws<XmlException>(() => { while (reader.Read()) { } });
            }
        }

        public static IEnumerable<object[]> TextBoundaryCases()
        {
            foreach (int length in new[] { 0, 15, 16, 17, 31, 32, 33, 63, 64, 65, 2045, 2046, 4095, 4096, 4097 })
            {
                foreach (bool async in new[] { false, true })
                {
                    yield return new object[] { length, async };
                }
            }
        }

        [Theory]
        [MemberData(nameof(TextBoundaryCases))]
        public async Task TextValues_ClassificationAndNormalization(int length, bool async)
        {
            // Whitespace-only values reach the 4096 character whitespace lookahead limit at length 2046 and are then reported as text.
            const int MinWhitespaceLookaheadCount = 4096;
            foreach ((string raw, string expected, bool isWhitespace) in new[]
            {
                ("\r\n\t\n\r", "\n\t\n\n", true),
                ("&lt;&amp;&gt;", "<&>", false),
                ("\u00e9\u4e2d\ud83d\ude42", "\u00e9\u4e2d\ud83d\ude42", false),
                ("] ] >", "] ] >", false),
                ("x", "x", false)
            })
            {
                string prefix = string.Create(length, 0, static (span, _) =>
                {
                    for (int i = 0; i < span.Length; i++)
                    {
                        span[i] = " \t\n"[i % 3];
                    }
                });
                string expectedValue = prefix + expected + prefix;
                XmlNodeType expectedNodeType = isWhitespace && expectedValue.Length < MinWhitespaceLookaheadCount ? XmlNodeType.Whitespace : XmlNodeType.Text;
                using FragmentedAttributeReader source = new($"<root>{prefix}{raw}{prefix}</root>");
                using XmlReader reader = XmlReader.Create(source, new XmlReaderSettings { Async = async });
                Assert.True(async ? await reader.ReadAsync() : reader.Read());
                Assert.True(async ? await reader.ReadAsync() : reader.Read());
                Assert.Equal(expectedNodeType, reader.NodeType);
                Assert.Equal(expectedValue, reader.Value);
                Assert.True(async ? await reader.ReadAsync() : reader.Read());
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
                Assert.False(async ? await reader.ReadAsync() : reader.Read());
            }
        }

        [Theory]
        [InlineData(15, false)]
        [InlineData(16, false)]
        [InlineData(64, false)]
        [InlineData(65, false)]
        [InlineData(1000, false)]
        [InlineData(15, true)]
        [InlineData(16, true)]
        [InlineData(64, true)]
        [InlineData(65, true)]
        [InlineData(1000, true)]
        public async Task TextValues_SignificantWhitespace(int length, bool async)
        {
            string whitespace = string.Create(length, 0, static (span, _) =>
            {
                for (int i = 0; i < span.Length; i++)
                {
                    span[i] = "\t \n "[i % 4];
                }
            });
            using FragmentedAttributeReader source = new($"<root xml:space=\"preserve\"><a/>{whitespace}<b/>{whitespace}x</root>");
            using XmlReader reader = XmlReader.Create(source, new XmlReaderSettings { Async = async });
            Assert.True(async ? await reader.ReadAsync() : reader.Read());
            Assert.True(async ? await reader.ReadAsync() : reader.Read());
            Assert.True(async ? await reader.ReadAsync() : reader.Read());
            Assert.Equal(XmlNodeType.SignificantWhitespace, reader.NodeType);
            Assert.Equal(whitespace, reader.Value);
            Assert.True(async ? await reader.ReadAsync() : reader.Read());
            Assert.True(async ? await reader.ReadAsync() : reader.Read());
            Assert.Equal(XmlNodeType.Text, reader.NodeType);
            Assert.Equal(whitespace + "x", reader.Value);
        }

        private sealed class FragmentedAttributeReader(string xml) : StringReader(xml)
        {
            public override int Read(char[] buffer, int index, int count) => base.Read(buffer, index, Math.Min(count, 17));

            public override async Task<int> ReadAsync(char[] buffer, int index, int count)
            {
                await Task.Yield();
                return Read(buffer, index, count);
            }

            public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return base.Read(buffer.Span.Slice(0, Math.Min(buffer.Length, 17)));
            }
        }
    }

    public abstract class AttributeReadingPerformanceTests
    {
        // This should match the value in XmlTextReaderImpl
        private const int MaxAttrDuplWalkCount = 64;

        private const int SmallN = 2_000;
        private const int LargeN = 20_000;
        private const double SizeRatio = (double)LargeN / SmallN;
        private const double MaxRatioMultiplier = 5;
        // Floor for the small-side divisor: below this, wall-clock noise dominates the reading.
        private const long MinBaselineMs = 30;
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);

        protected abstract void ReadFully(string xml, CancellationToken ct);

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInterpreter))]
        public void AttributeDuplicatesCheck_LongUris_SameLocalName_AboveThreshold()
        {
            // Exercises the HashSet duplicate-check path (number of attributes >= threshold)
            AssertLinearScaling(
                n => GenerateDoc(n, attrCount: MaxAttrDuplWalkCount, longUris: true, distinctLocalNames: false));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInterpreter))]
        public void AttributeDuplicatesCheck_ShortUris_SameLocalName_BelowThreshold()
        {
            // Pairwise walk path (number of attributes < threshold)
            AssertLinearScaling(
                n => GenerateDoc(n, attrCount: MaxAttrDuplWalkCount - 1, longUris: false, distinctLocalNames: false));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInterpreter))]
        public void AttributeDuplicatesCheck_LongUris_DistinctLocalNames_AboveThreshold()
        {
            // Distinct localNames starting with same letter (to bypass some optimizations)
            AssertLinearScaling(
                n => GenerateDoc(n, attrCount: MaxAttrDuplWalkCount, longUris: true, distinctLocalNames: true));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInterpreter))]
        public void AttributeDuplicatesCheck_LongUris_SameLocalName_WellAboveThreshold()
        {
            // Larger amount of attributes — well above the threshold
            AssertLinearScaling(
                n => GenerateDoc(n, attrCount: MaxAttrDuplWalkCount * 4, longUris: true, distinctLocalNames: false));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInterpreter))]
        public void AttributeDuplicatesCheck_ShortUris_DistinctLocalNames_BelowThreshold()
        {
            // Below threshold with distinct localNames
            AssertLinearScaling(
                n => GenerateDoc(n, attrCount: MaxAttrDuplWalkCount - 1, longUris: false, distinctLocalNames: true));
        }

        // We're doing full string.Equals on DEBUG on top of Ref.Equal which makes it quadratic.
#if !DEBUG
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInterpreter))]
        public void AttributeDuplicatesCheck_LongUris_SameLocalName_BelowThreshold()
        {
            // Pairwise walk path with long URIs — only valid in Release
            AssertLinearScaling(
                n => GenerateDoc(n, attrCount: MaxAttrDuplWalkCount - 1, longUris: true, distinctLocalNames: false));
        }
#endif

        private void AssertLinearScaling(Func<int, string> generateDoc)
        {
            using CancellationTokenSource cts = new(s_timeout);
            CancellationToken ct = cts.Token;

            // Warm up JIT and caches.
            ReadFully(generateDoc(SmallN), ct);

            long smallTime = MeasureRead(generateDoc(SmallN), ct);
            long largeTime = MeasureRead(generateDoc(LargeN), ct);

            double maxAllowed = SizeRatio * MaxRatioMultiplier;
            long baseline = Math.Max(smallTime, MinBaselineMs);
            double actualRatio = (double)largeTime / baseline;

            Assert.True(actualRatio <= maxAllowed,
                $"Scaling ratio {actualRatio:F1}x exceeded {maxAllowed:F1}x limit " +
                $"(input grew {SizeRatio:F0}x). " +
                $"Small ({SmallN}): {smallTime} ms (baseline {baseline} ms), " +
                $"Large ({LargeN}): {largeTime} ms.");
        }

        private long MeasureRead(string doc, CancellationToken ct)
        {
            Stopwatch sw = Stopwatch.StartNew();
            ReadFully(doc, ct);
            return sw.ElapsedMilliseconds;
        }

        private static string GenerateDoc(int n, int attrCount, bool longUris, bool distinctLocalNames)
        {
            int childCount = n / 2;
            string garbageText = longUris ? new string('x', childCount) : "ns";

            StringBuilder child = new();
            child.Append("<child ");
            for (int i = attrCount - 1; i >= 0; i--)
            {
                if (distinctLocalNames)
                    child.Append($" x{i:X4}:a{i:X4}=\"\"");
                else
                    child.Append($" x{i:X4}:a=\"\"");
            }
            child.Append("/>");
            string childElement = child.ToString();

            StringBuilder sb = new();
            sb.Append("<parent ");
            for (int i = 0; i < attrCount; i++)
            {
                sb.Append($" xmlns:x{i:X4}=\"{garbageText}{i:X4}\"");
            }
            sb.Append('>');
            for (int i = 0; i < childCount; i++)
            {
                sb.Append(childElement);
            }
            sb.Append("</parent>");

            return sb.ToString();
        }
    }

    // XmlReader.Create wraps XmlTextReaderImpl in XmlAsyncCheckReader
    public class AttributeReadingPerformanceTests_XmlReaderCreate : AttributeReadingPerformanceTests
    {
        protected override void ReadFully(string xml, CancellationToken ct)
        {
            using XmlReader xr = XmlReader.Create(new StringReader(xml));
            while (xr.Read()) { ct.ThrowIfCancellationRequested(); }
        }
    }

    // XmlNodeReader reads from a pre-parsed DOM tree (XmlDocument).
    // Construction is not timed — only the read traversal.
    public class AttributeReadingPerformanceTests_XmlNodeReader : AttributeReadingPerformanceTests
    {
        protected override void ReadFully(string xml, CancellationToken ct)
        {
            XmlDocument doc = new();
            doc.LoadXml(xml);
            using XmlReader xr = new XmlNodeReader(doc);
            while (xr.Read()) { ct.ThrowIfCancellationRequested(); }
        }
    }

    // XPathNavigatorReader reads from an XPathDocument's XPath data model
    public class AttributeReadingPerformanceTests_XPathNavigatorReader : AttributeReadingPerformanceTests
    {
        protected override void ReadFully(string xml, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            XPathDocument doc = new(new StringReader(xml));
            ct.ThrowIfCancellationRequested();
            using XmlReader xr = doc.CreateNavigator().ReadSubtree();
            while (xr.Read()) { ct.ThrowIfCancellationRequested(); }
        }
    }

    // XmlReader.Create(XmlReader, settings) wraps a reader in XmlSubtreeReader
    public class AttributeReadingPerformanceTests_WrappedReader : AttributeReadingPerformanceTests
    {
        protected override void ReadFully(string xml, CancellationToken ct)
        {
            using XmlReader inner = XmlReader.Create(new StringReader(xml));
            using XmlReader xr = XmlReader.Create(inner, new XmlReaderSettings());
            while (xr.Read()) { ct.ThrowIfCancellationRequested(); }
        }
    }
}
