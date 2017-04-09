// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;
using XmlCoreTest.Common;
using Xunit;

namespace XLinqTests
{
    public class SaveWithFileName
    {
        [Fact]
        public void XDocumentSaveToFile()
        {
            SerializeWithSaveOptions(SerializeXDocumentSaveToFile, testXElement: false, testXDocument: true);
        }

        [Fact]
        public void XDocumentSave()
        {
            string markup = "<e> <e2 /> </e>";

            using (TempFile temp = TempFile.Create())
            {
                XDocument d = XDocument.Parse(markup, LoadOptions.PreserveWhitespace);
                d.Save(temp.Path);

                temp.AssertExists();
                Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + markup, NormalizeNewLines(temp.ReadAllText()));
            }
        }

        [Fact]
        public void XDocumentSave_SaveOptions()
        {
            string markup = "<e> <e2 /> </e>";
            using (TempFile temp = TempFile.Create())
            {
                XDocument d = XDocument.Parse(markup, LoadOptions.PreserveWhitespace);
                d.Save(temp.Path, SaveOptions.DisableFormatting);

                temp.AssertExists();
                Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + markup, temp.ReadAllText());
            }
        }

        [Fact]
        public void XDocumentSave_NullParameter()
        {
            Assert.Throws<ArgumentNullException>(() => new XDocument().Save((string)null));
            Assert.Throws<ArgumentNullException>(() => new XDocument().Save((string)null, SaveOptions.DisableFormatting));
            Assert.Throws<ArgumentNullException>(() => new XDocument().Save((string)null, SaveOptions.None));
        }

        [Fact]
        public void XElementSaveToFile()
        {
            SerializeWithSaveOptions(SerializeXElementSaveToFile, testXElement: true, testXDocument: false);
        }

        [Fact]
        public void XElementSave()
        {
            string markup = "<e a=\"value\"> <e2 /> </e>";
            using (TempFile temp = TempFile.Create())
            {
                XElement e = XElement.Parse(markup, LoadOptions.PreserveWhitespace);
                e.Save(temp.Path);

                temp.AssertExists();
                Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + markup, NormalizeNewLines(temp.ReadAllText()));
            }
        }

        [Fact]
        public void XElementSave_SaveOptions()
        {
            string markup = "<e a=\"value\"> <e2 /> </e>";
            using (TempFile temp = TempFile.Create())
            {
                XElement e = XElement.Parse(markup, LoadOptions.PreserveWhitespace);
                e.Save(temp.Path, SaveOptions.DisableFormatting);

                temp.AssertExists();
                Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + markup, temp.ReadAllText());
            }
        }

        [Fact]
        public void XElementSave_NullParameter()
        {
            Assert.Throws<ArgumentNullException>(() => new XElement("e").Save((string)null));
            Assert.Throws<ArgumentNullException>(() => new XElement("e").Save((string)null, SaveOptions.DisableFormatting));
            Assert.Throws<ArgumentNullException>(() => new XElement("e").Save((string)null, SaveOptions.None));
        }

        [Fact]
        public void XStreamingElementSave_NullParameter()
        {
            Assert.Throws<ArgumentNullException>(() => new XStreamingElement("e").Save((string)null));
            Assert.Throws<ArgumentNullException>(() => new XStreamingElement("e").Save((string)null, SaveOptions.DisableFormatting));
            Assert.Throws<ArgumentNullException>(() => new XStreamingElement("e").Save((string)null, SaveOptions.None));
        }

        [Fact]
        public void XStreamingElementSave()
        {
            string markup = "<e a=\"value\"> <!--comment--> <e2> <![CDATA[cdata]]> </e2> <?pi target?> </e>";
            using (TempFile temp = TempFile.Create())
            {
                XElement e = XElement.Parse(markup, LoadOptions.PreserveWhitespace);
                XStreamingElement e2 = new XStreamingElement(e.Name, e.Attributes(), e.Nodes());
                e2.Save(temp.Path);

                temp.AssertExists();
                Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + markup, NormalizeNewLines(temp.ReadAllText()));
            }
        }

        [Fact]
        public void XStreamingElementSave_SaveOptions()
        {
            string markup = "<e a=\"value\"> <!--comment--> <e2> <![CDATA[cdata]]> </e2> <?pi target?> </e>";
            using (TempFile temp = TempFile.Create())
            {
                XElement e = XElement.Parse(markup, LoadOptions.PreserveWhitespace);
                XStreamingElement e2 = new XStreamingElement(e.Name, e.Attributes(), e.Nodes());
                e2.Save(temp.Path, SaveOptions.DisableFormatting);

                temp.AssertExists();
                Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + markup, temp.ReadAllText());
            }
        }

        //
        // helpers
        //
        private static string SerializeXDocumentSaveToFile(XNode node)
        {
            string result;
            using (TempFile temp = TempFile.Create())
            {
                ((XDocument)node).Save(temp.Path);
                using (TextReader tr = new StreamReader(temp.Path))
                {
                    result = StripOffXmlDeclaration(tr.ReadToEnd());
                }

                temp.AssertExists();
            }
            return result;
        }

        private static string SerializeXElementSaveToFile(XNode node)
        {
            string result;
            using (TempFile temp = TempFile.Create())
            {
                ((XElement)node).Save(temp.Path);
                using (TextReader tr = new StreamReader(temp.Path))
                {
                    result = StripOffXmlDeclaration(tr.ReadToEnd());
                }

                temp.AssertExists();
            }
            return result;
        }

        private delegate string SerializeNode(XNode node);

        private static void SerializeWithSaveOptions(SerializeNode serialize, bool testXElement, bool testXDocument)
        {
            // Test both options at once as they don't really collide
            SaveOptions so = SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces;

            XElement root = XElement.Parse("<root xmlns:a='uri'><child xmlns:a='uri'><baby xmlns:a='uri'>text</baby></child></root>");
            XElement child = root.Element("child");
            XElement baby = child.Element("baby");
            XNode text = baby.FirstNode;

            // Verify that without annotation the output gets indented and the duplicate ns decls are not removed
            if (testXElement)
            {
                Assert.Equal(NormalizeNewLines(serialize(child)), "<child xmlns:a=\"uri\">  <baby xmlns:a=\"uri\">text</baby></child>");
            }

            // Now add annotation to the leaf element node
            // Even though it's in effect the output should stay the same (as there is only one namespace decl and mixed content).
            baby.AddAnnotation(so);

            if (testXElement)
            {
                Assert.Equal(serialize(baby), "<baby xmlns:a=\"uri\">text</baby>");
            }

            // Now add annotation to the middle node
            child.AddAnnotation(so);

            if (testXElement)
            {
                // Verify that the options are applied correctly
                Assert.Equal(NormalizeNewLines(serialize(child)), "<child xmlns:a=\"uri\"><baby>text</baby></child>");
                // Verify that the root node is not affected as we don't look for the annotation among descendants
                Assert.Equal(NormalizeNewLines(serialize(root)), "<root xmlns:a=\"uri\">  <child xmlns:a=\"uri\">    <baby xmlns:a=\"uri\">text</baby>  </child></root>");
            }

            // And now add the annotation to the root and remove it from the child to test that we can correctly skip over a node
            root.AddAnnotation(so);
            child.RemoveAnnotations(typeof(SaveOptions));

            if (testXElement)
            {
                // Verify that the options are still applied to child
                Assert.Equal(serialize(child), "<child xmlns:a=\"uri\"><baby>text</baby></child>");
                // And they should be also applied to the root now
                Assert.Equal(serialize(root), "<root xmlns:a=\"uri\"><child><baby>text</baby></child></root>");
            }

            // Add a document node above it all to test that it works on non-XElement as well
            XDocument doc = new XDocument(root);
            // Add the annotation to the doc and remove it from the root
            doc.AddAnnotation(so);
            root.RemoveAnnotations(typeof(SaveOptions));

            // Options should still apply to root as well as the doc
            if (testXElement)
            {
                Assert.Equal(serialize(root), "<root xmlns:a=\"uri\"><child><baby>text</baby></child></root>");
            }

            if (testXDocument)
            {
                Assert.Equal(serialize(doc), "<root xmlns:a=\"uri\"><child><baby>text</baby></child></root>");
            }
        }

        private static string StripOffXmlDeclaration(string s)
        {
            if (s.StartsWith("<?xml "))
            {
                s = s.Substring(s.IndexOf('>') + 1);

                if (s.StartsWith("\r\n"))
                {
                    s = s.Substring(2);
                }
            }
            return s;
        }

        private static string NormalizeNewLines(string s)
        {
            s = s.Replace("\n", "");
            s = s.Replace("\r", "");
            return s;
        }
    }
}
