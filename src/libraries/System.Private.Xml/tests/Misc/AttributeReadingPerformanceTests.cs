// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.XPath;
using Xunit;

namespace System.Xml.Tests
{
    public abstract class AttributeReadingPerformanceTests
    {
        // This should match the value in XmlTextReaderImpl
        private const int MaxAttrDuplWalkCount = 64;

        private const int SmallN = 2_000;
        private const int LargeN = 20_000;
        private const double SizeRatio = (double)LargeN / SmallN;
        private const double MaxRatioMultiplier = 4;
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);

        protected abstract void ReadFully(string xml, CancellationToken ct);

        [Fact]
        public void AttributeDuplicatesCheck_LongUris_SameLocalName_AboveThreshold()
        {
            // Exercises the HashSet duplicate-check path (number of attributes >= threshold)
            AssertLinearScaling(
                n => GenerateDoc(n, attrCount: MaxAttrDuplWalkCount, longUris: true, distinctLocalNames: false));
        }

        [Fact]
        public void AttributeDuplicatesCheck_ShortUris_SameLocalName_BelowThreshold()
        {
            // Pairwise walk path (number of attributes < threshold)
            AssertLinearScaling(
                n => GenerateDoc(n, attrCount: MaxAttrDuplWalkCount - 1, longUris: false, distinctLocalNames: false));
        }

        [Fact]
        public void AttributeDuplicatesCheck_LongUris_DistinctLocalNames_AboveThreshold()
        {
            // Distinct localNames starting with same letter (to bypass some optimizations)
            AssertLinearScaling(
                n => GenerateDoc(n, attrCount: MaxAttrDuplWalkCount, longUris: true, distinctLocalNames: true));
        }

        [Fact]
        public void AttributeDuplicatesCheck_LongUris_SameLocalName_WellAboveThreshold()
        {
            // Larger amount of attributes — well above the threshold
            AssertLinearScaling(
                n => GenerateDoc(n, attrCount: MaxAttrDuplWalkCount * 4, longUris: true, distinctLocalNames: false));
        }

        [Fact]
        public void AttributeDuplicatesCheck_ShortUris_DistinctLocalNames_BelowThreshold()
        {
            // Below threshold with distinct localNames
            AssertLinearScaling(
                n => GenerateDoc(n, attrCount: MaxAttrDuplWalkCount - 1, longUris: false, distinctLocalNames: true));
        }

        // We're doing full string.Equals on DEBUG on top of Ref.Equal which makes it quadratic.
#if !DEBUG
        [Fact]
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

            ReadFully(generateDoc(SmallN), ct);

            long smallTime = MeasureRead(generateDoc(SmallN), ct);
            long largeTime = MeasureRead(generateDoc(LargeN), ct);

            double maxAllowed = SizeRatio * MaxRatioMultiplier;
            double actualRatio = (double)largeTime / Math.Max(smallTime, 1);

            Assert.True(actualRatio <= maxAllowed,
                $"Scaling ratio {actualRatio:F1}x exceeded {maxAllowed:F1}x limit " +
                $"(input grew {SizeRatio:F0}x). " +
                $"Small ({SmallN}): {smallTime} ms, Large ({LargeN}): {largeTime} ms.");
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
