// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    //[TestModule(Name="XmlWriter API")]
    public class XmlWriterTestModule
    {
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

        [Theory]
        [XmlWriterInlineData("abc")]
        public void TestTest(string test, string a)
        {
            Assert.StartsWith("test", test);
            Assert.Equal("abc", a);
        }
    }
}
