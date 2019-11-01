// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    // Based on https://github.com/xunit/xunit/blob/bccfcccf26b2c63c90573fe1a17e6572882ef39c/src/xunit.core/Sdk/InlineDataDiscoverer.cs
    public class XmlWriterInlineDataDiscoverer : IDataDiscoverer
    {
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

        private static object[] Prepend(object[] arr, object o)
        {
            List<object> list = new List<object>();
            list.Add(o);
            list.AddRange(arr);
            return list.ToArray();
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

        public virtual IEnumerable<object[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            object[] constructorArgs = dataAttribute.GetConstructorArguments().ToArray();

            if (constructorArgs.Length == 1)
            {
                object[] args = ((IEnumerable<object>)constructorArgs[0] ?? new object[] { null }).ToArray();
                return GenerateTestCases(WriterType.All, args);
            }

            if (constructorArgs.Length == 2)
            {
                WriterType writerTypeFlags = (WriterType)constructorArgs[0];
                object[] args = ((IEnumerable<object>)constructorArgs[1] ?? new object[] { null }).ToArray();
                return GenerateTestCases(writerTypeFlags, args);
            }

            throw new Exception("Invalid args");
        }

        public virtual bool SupportsDiscoveryEnumeration(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            return true;
        }
    }

    // Based on https://github.com/xunit/xunit/blob/bccfcccf26b2c63c90573fe1a17e6572882ef39c/src/xunit.core/InlineDataAttribute.cs
    [DataDiscoverer("System.Xml.Tests.XmlWriterInlineDataDiscoverer", "System.Xml.RW.XmlWriterApi.Tests")]
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

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            return XmlWriterInlineDataDiscoverer.GenerateTestCases(_writerTypeFlags, _data);
        }
    }
}
