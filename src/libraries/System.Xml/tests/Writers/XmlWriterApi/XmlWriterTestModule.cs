// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    //[TestModule(Name="XmlWriter API")]
    public partial class XmlWriterTestModule : CTestModule
    {
        public XmlWriterTestModule() : base()
        {
        }

        protected WriterType writerType1;
        public WriterType WriterType
        {
            get
            {
                return writerType1;
            }
        }

        protected WriterFactory writerFactory1 = null;
        public WriterFactory WriterFactory
        {
            get
            {
                return writerFactory1;
            }
        }

        protected string baselinePath1;
        public string BaselinePath
        {
            get
            {
                return baselinePath1;
            }
        }

        public override int Init(object objParam)
        {
            baselinePath1 = Path.Combine(FilePathUtil.GetTestDataPath(), @"XmlWriter2\");
            string temp = FilePathUtil.GetVariableValue("WriterType").ToUpperInvariant();

            switch (temp)
            {
                case "UTF8WRITER":
                    writerType1 = WriterType.UTF8Writer;
                    break;
                case "UNICODEWRITER":
                    writerType1 = WriterType.UnicodeWriter;
                    break;
                case "CUSTOMWRITER":
                    writerType1 = WriterType.CustomWriter;
                    break;
                case "UTF8WRITERINDENT":
                    writerType1 = WriterType.UTF8WriterIndent;
                    break;
                case "UNICODEWRITERINDENT":
                    writerType1 = WriterType.UnicodeWriterIndent;
                    break;
                case "CHARCHECKINGWRITER":
                    writerType1 = WriterType.CharCheckingWriter;
                    break;
                case "WRAPPEDWRITER":
                    writerType1 = WriterType.WrappedWriter;
                    break;
                default:
                    throw new Exception("Unknown writer type: " + temp);
            }

            writerFactory1 = new XmlCoreTest.Common.WriterFactory(writerType1);
            return base.Init(objParam);
        }
    }
}
