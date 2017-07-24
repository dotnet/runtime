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
        public static IEnumerable<object[]> GenerateTestCases(TestCaseUtilsImplementation implementation, WriterType writerTypeFlags, object[] args)
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
                    yield return args.Prepend(CreateTestUtils(implementation, writerType, async: false)).ToArray();

                if (asyncFlag)
                    yield return args.Prepend(CreateTestUtils(implementation, writerType, async: true)).ToArray();
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

        private static object CreateTestUtils(TestCaseUtilsImplementation implementation, WriterType writerType, bool async)
        {
            switch (implementation)
            {
                case TestCaseUtilsImplementation.XmlFactoryWriter:
                    return new XmlFactoryWriterTestCaseUtils(writerType, async: async);
            }

            throw new Exception("Invalid implementation");
        }

        public virtual IEnumerable<object[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            object[] constructorArgs = dataAttribute.GetConstructorArguments().ToArray();

            if (constructorArgs.Length == 2)
            {
                TestCaseUtilsImplementation implementation = (TestCaseUtilsImplementation)constructorArgs[0];
                object[] args = ((IEnumerable<object>)constructorArgs[1] ?? new object[] { null }).ToArray();
                return GenerateTestCases(implementation, WriterType.All, args);
            }

            if (constructorArgs.Length == 3)
            {
                TestCaseUtilsImplementation implementation = (TestCaseUtilsImplementation)constructorArgs[0];
                WriterType writerTypeFlags = (WriterType)constructorArgs[1];
                object[] args = ((IEnumerable<object>)constructorArgs[2] ?? new object[] { null }).ToArray();
                return GenerateTestCases(implementation, writerTypeFlags, args);
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
        TestCaseUtilsImplementation _implementation;
        WriterType _writerTypeFlags;

        public XmlWriterInlineDataAttribute(TestCaseUtilsImplementation implementation, params object[] data)
        {
            _data = data;
            _implementation = implementation;
            _writerTypeFlags = WriterType.All;
        }

        public XmlWriterInlineDataAttribute(TestCaseUtilsImplementation implementation, WriterType writerTypeFlag, params object[] data)
        {
            _data = data;
            _implementation = implementation;
            _writerTypeFlags = writerTypeFlag;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            return XmlWriterInlineDataDiscoverer.GenerateTestCases(_implementation, _writerTypeFlags, _data);
        }
    }

    public enum TestCaseUtilsImplementation
    {
        XmlFactoryWriter
    }
}
