// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using XmlCoreTest.Common;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace System.Xml.XmlWriterApiTests
{
    // Based on https://github.com/xunit/xunit/blob/bccfcccf26b2c63c90573fe1a17e6572882ef39c/src/xunit.core/InlineDataAttribute.cs
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class XmlWriterInlineDataAttribute : DataAttribute
    {
        private readonly object[] _data;
        WriterType _writerTypeFlags;

        public XmlWriterInlineDataAttribute(params object[] data)
        {
            _data = data;
            _writerTypeFlags = WriterType.All;
        }

        public XmlWriterInlineDataAttribute(WriterType writerTypeFlag, params object[] data)
        {
            _data = data;
            _writerTypeFlags = writerTypeFlag;
        }

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod, DisposalTracker disposalTracker)
        {
            var testCases = new List<ITheoryDataRow>();
            foreach (object[] testCase in GenerateTestCases(_writerTypeFlags, _data))
            {
                testCases.Add(new TheoryDataRow(testCase));
            }
            return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(testCases);
        }

        public static IEnumerable<object[]> GenerateTestCases(WriterType writerTypeFlags, object[] args)
        {
            bool noAsyncFlag = writerTypeFlags.HasFlag(WriterType.NoAsync);
            bool asyncFlag = writerTypeFlags.HasFlag(WriterType.Async);

            if (!noAsyncFlag && !asyncFlag)
            {
                // flags for writers specified directly, none of those would mean no tests should be run
                // this is likely not what was meant
                noAsyncFlag = true;
                asyncFlag = true;
            }

            foreach (WriterType writerType in GetWriterTypes(writerTypeFlags))
            {
                if (noAsyncFlag)
                    yield return Prepend(args, new XmlWriterUtils(writerType, async: false)).ToArray();

                if (asyncFlag)
                    yield return Prepend(args, new XmlWriterUtils(writerType, async: true)).ToArray();
            }
        }

        private static IEnumerable<WriterType> GetWriterTypes(WriterType writerTypeFlags)
        {
            if (writerTypeFlags.HasFlag(WriterType.UTF8Writer))
                yield return WriterType.UTF8Writer;

            if (writerTypeFlags.HasFlag(WriterType.UnicodeWriter))
                yield return WriterType.UnicodeWriter;

            if (writerTypeFlags.HasFlag(WriterType.CustomWriter))
                yield return WriterType.CustomWriter;

            if (writerTypeFlags.HasFlag(WriterType.CharCheckingWriter))
                yield return WriterType.CharCheckingWriter;

            if (writerTypeFlags.HasFlag(WriterType.UTF8WriterIndent))
                yield return WriterType.UTF8WriterIndent;

            if (writerTypeFlags.HasFlag(WriterType.UnicodeWriterIndent))
                yield return WriterType.UnicodeWriterIndent;

            if (writerTypeFlags.HasFlag(WriterType.WrappedWriter))
                yield return WriterType.WrappedWriter;
        }

        private static object[] Prepend(object[] arr, object o)
        {
            List<object> list = new List<object>();
            list.Add(o);
            list.AddRange(arr);
            return list.ToArray();
        }

        public override bool SupportsDiscoveryEnumeration()
        {
            return true;
        }
    }
}
