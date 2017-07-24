using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace System.Xml.Tests
{
    // Based on https://github.com/xunit/xunit/blob/bccfcccf26b2c63c90573fe1a17e6572882ef39c/src/xunit.core/Sdk/InlineDataDiscoverer.cs
    public class XmlWriterInlineDataDiscoverer : IDataDiscoverer
    {
        public static IEnumerable<object[]> GenerateTestCases(object[] args)
        {
            yield return args.Prepend("test1").ToArray();
            yield return args.Prepend("test2").ToArray();
            yield return args.Prepend("test3").ToArray();
        }

        public virtual IEnumerable<object[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            var args = ((IEnumerable<object>)dataAttribute.GetConstructorArguments().Single() ?? new object[] { null }).ToArray();
            return GenerateTestCases(args);
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
        readonly object[] data;

        public XmlWriterInlineDataAttribute(params object[] data)
        {
            this.data = data;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            return XmlWriterInlineDataDiscoverer.GenerateTestCases(data);
        }
    }

    // new XmlCoreTest.Common.WriterFactory(writerType1);
    [Flags]
    public enum WriterImplementation
    {
        All = UTF8Writer | UnicodeWriter | CustomWriter | CharCheckingWriter | UTF8WriterIndent | UnicodeWriterIndent | WrappedWriter,
        UTF8Writer = 1,
        UnicodeWriter = 2,
        CustomWriter = 4,
        CharCheckingWriter = 8,
        UTF8WriterIndent = 16,
        UnicodeWriterIndent = 32,
        WrappedWriter = 64
    }


    //public virtual XmlWriter CreateWriter()
    //{
    //    return this.XmlWriterTestModule.WriterFactory.CreateWriter();
    //}

    //public virtual XmlWriter CreateWriter(XmlWriterSettings s)
    //{
    //    return this.XmlWriterTestModule.WriterFactory.CreateWriter(s);
    //}

    //public virtual XmlReader GetReader()
    //{
    //    return this.XmlWriterTestModule.WriterFactory.GetReader();
    //}
}
