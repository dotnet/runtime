// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public class TCStandAlone
    {
        //[Variation(id=1, Desc="StartDocument(bool standalone = true)", Pri=0)]
        [Fact]
        public void standalone_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.CloseOutput = false;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartDocument(true);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.WriteEndDocument();
            w.Dispose();

            bool ret = false;

            if (WriterType == WriterType.UnicodeWriter)
                ret = CompareReader("<?xml version=\"1.0\" encoding=\"unicode\" standalone=\"yes\"?><Root />");
            else
                ret = CompareReader("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><Root />");

            Assert.True(ret);
        }

        //[Variation(id=2, Desc="StartDocument(bool standalone = false)", Pri=0)]
        [Fact]
        public void standalone_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.CloseOutput = false;

            XmlWriter w = CreateWriter(wSettings);
            w.WriteStartDocument(false);
            w.WriteStartElement("Root");
            w.WriteEndElement();
            w.WriteEndDocument();
            w.Dispose();

            bool ret = false;

            if (WriterType == WriterType.UnicodeWriter)
                ret = CompareReader("<?xml version=\"1.0\" encoding=\"unicode\" standalone=\"no\"?><Root />");
            else
                ret = CompareReader("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?><Root />");

            Assert.True(ret);
        }
    }
}
