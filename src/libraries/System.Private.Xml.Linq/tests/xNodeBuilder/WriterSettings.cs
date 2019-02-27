// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeBuilderFunctionalTests : TestModule
    {
        public partial class XNodeBuilderTests : XLinqTestCase
        {
            public partial class Writer_Settings : BridgeHelpers
            {
                //[Variation(Desc = "XDocument: Settings before Close()", Priority = 1)]
                public void var_1()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();

                    TestLog.Compare(w.Settings.Encoding is UTF8Encoding, "Incorrect default value of Encoding");
                    TestLog.Equals(w.Settings.OmitXmlDeclaration, false, "Incorrect default value of OmitXmlDeclaration");
                    TestLog.Equals(w.Settings.NewLineHandling, NewLineHandling.Replace, "Incorrect default value of NewLineHandling");
                    TestLog.Equals(w.Settings.NewLineChars, Environment.NewLine, "Incorrect default value of NewLineChars");
                    TestLog.Equals(w.Settings.Indent, false, "Incorrect default value of Indent");
                    TestLog.Equals(w.Settings.IndentChars, "  ", "Incorrect default value of IndentChars");
                    TestLog.Equals(w.Settings.NewLineOnAttributes, false, "Incorrect default value of NewLineOnAttributes");
                    TestLog.Equals(w.Settings.CloseOutput, false, "Incorrect default value of CloseOutput");
                    TestLog.Equals(w.Settings.CheckCharacters, true, "Incorrect default value of CheckCharacters");
                    TestLog.Equals(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Incorrect default value of ConformanceLevel");
                    w.Dispose();
                }

                //[Variation(Desc = "XElement: Settings before Close()", Priority = 1)]
                public void var_2()
                {
                    XElement d = new XElement("a");
                    XmlWriter w = d.CreateWriter();

                    TestLog.Compare(w.Settings.Encoding is UTF8Encoding, "Incorrect default value of Encoding");
                    TestLog.Equals(w.Settings.OmitXmlDeclaration, false, "Incorrect default value of OmitXmlDeclaration");
                    TestLog.Equals(w.Settings.NewLineHandling, NewLineHandling.Replace, "Incorrect default value of NewLineHandling");
                    TestLog.Equals(w.Settings.NewLineChars, Environment.NewLine, "Incorrect default value of NewLineChars");
                    TestLog.Equals(w.Settings.Indent, false, "Incorrect default value of Indent");
                    TestLog.Equals(w.Settings.IndentChars, "  ", "Incorrect default value of IndentChars");
                    TestLog.Equals(w.Settings.NewLineOnAttributes, false, "Incorrect default value of NewLineOnAttributes");
                    TestLog.Equals(w.Settings.CloseOutput, false, "Incorrect default value of CloseOutput");
                    TestLog.Equals(w.Settings.CheckCharacters, true, "Incorrect default value of CheckCharacters");
                    TestLog.Equals(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Incorrect default value of ConformanceLevel");
                    w.Dispose();
                }

                //[Variation(Desc = "XDocument: Settings after Close()", Priority = 1)]
                public void var_3()
                {
                    XDocument d = new XDocument();
                    XmlWriter w = d.CreateWriter();
                    w.Dispose();
                    TestLog.Compare(w.Settings.Encoding is UTF8Encoding, "Incorrect default value of Encoding");
                    TestLog.Equals(w.Settings.OmitXmlDeclaration, false, "Incorrect default value of OmitXmlDeclaration");
                    TestLog.Equals(w.Settings.NewLineHandling, NewLineHandling.Replace, "Incorrect default value of NewLineHandling");
                    TestLog.Equals(w.Settings.NewLineChars, Environment.NewLine, "Incorrect default value of NewLineChars");
                    TestLog.Equals(w.Settings.Indent, false, "Incorrect default value of Indent");
                    TestLog.Equals(w.Settings.IndentChars, "  ", "Incorrect default value of IndentChars");
                    TestLog.Equals(w.Settings.NewLineOnAttributes, false, "Incorrect default value of NewLineOnAttributes");
                    TestLog.Equals(w.Settings.CloseOutput, false, "Incorrect default value of CloseOutput");
                    TestLog.Equals(w.Settings.CheckCharacters, true, "Incorrect default value of CheckCharacters");
                    TestLog.Equals(w.Settings.ConformanceLevel, ConformanceLevel.Document, "Incorrect default value of ConformanceLevel");
                }

                //[Variation(Desc = "XElement: Settings after Close()", Priority = 1)]
                public void var_4()
                {
                    XElement d = new XElement("a");
                    XmlWriter w = d.CreateWriter();
                    w.Dispose();
                    TestLog.Compare(w.Settings.Encoding is UTF8Encoding, "Incorrect default value of Encoding");
                    TestLog.Equals(w.Settings.OmitXmlDeclaration, false, "Incorrect default value of OmitXmlDeclaration");
                    TestLog.Equals(w.Settings.NewLineHandling, NewLineHandling.Replace, "Incorrect default value of NewLineHandling");
                    TestLog.Equals(w.Settings.NewLineChars, Environment.NewLine, "Incorrect default value of NewLineChars");
                    TestLog.Equals(w.Settings.Indent, false, "Incorrect default value of Indent");
                    TestLog.Equals(w.Settings.IndentChars, "  ", "Incorrect default value of IndentChars");
                    TestLog.Equals(w.Settings.NewLineOnAttributes, false, "Incorrect default value of NewLineOnAttributes");
                    TestLog.Equals(w.Settings.CloseOutput, false, "Incorrect default value of CloseOutput");
                    TestLog.Equals(w.Settings.CheckCharacters, true, "Incorrect default value of CheckCharacters");
                    TestLog.Equals(w.Settings.ConformanceLevel, ConformanceLevel.Fragment, "Incorrect default value of ConformanceLevel");
                }
            }
        }
    }
}
