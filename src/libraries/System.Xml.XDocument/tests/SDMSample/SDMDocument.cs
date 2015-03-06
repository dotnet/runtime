// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class SDMSamplesTests : XLinqTestCase
        {
            public partial class SDM_Document : XLinqTestCase
            {
                /// <summary>
                /// Validate behavior of the default XDocument constructor.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "CreateEmptyDocument")]
                public void CreateEmptyDocument()
                {
                    XDocument doc = new XDocument();

                    Validate.IsNull(doc.Parent);
                    Validate.IsNull(doc.Root);
                    Validate.IsNull(doc.Declaration);
                    Validate.Enumerator(doc.Nodes(), new XNode[0]);
                }

                /// <summary>
                /// Validate behavior of the XDocument constructor that takes content.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "CreateDocumentWithContent")]
                public void CreateDocumentWithContent()
                {
                    XDeclaration declaration = new XDeclaration("1.0", "utf-8", "yes");
                    XComment comment = new XComment("This is a document");
                    XProcessingInstruction instruction = new XProcessingInstruction("doc-target", "doc-data");
                    XElement element = new XElement("RootElement");

                    XDocument doc = new XDocument(declaration, comment, instruction, element);

                    Validate.Enumerator(
                        doc.Nodes(),
                        new XNode[] { comment, instruction, element });
                }

                /// <summary>
                /// Validate behavior of the XDocument copy/clone constructor.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "CreateDocumentCopy")]
                public void CreateDocumentCopy()
                {
                    try
                    {
                        new XDocument((XDocument)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    XDeclaration declaration = new XDeclaration("1.0", "utf-8", "yes");
                    XComment comment = new XComment("This is a document");
                    XProcessingInstruction instruction = new XProcessingInstruction("doc-target", "doc-data");
                    XElement element = new XElement("RootElement");

                    XDocument doc = new XDocument(declaration, comment, instruction, element);

                    XDocument doc2 = new XDocument(doc);

                    IEnumerator e = doc2.Nodes().GetEnumerator();

                    // First node: declaration
                    Validate.IsEqual(doc.Declaration.ToString(), doc2.Declaration.ToString());

                    // Next node: comment
                    Validate.IsEqual(e.MoveNext(), true);
                    Validate.Type(e.Current, typeof(XComment));
                    Validate.IsNotReferenceEqual(e.Current, comment);
                    XComment comment2 = (XComment)e.Current;
                    Validate.IsEqual(comment2.Value, comment.Value);

                    // Next node: processing instruction
                    Validate.IsEqual(e.MoveNext(), true);
                    Validate.Type(e.Current, typeof(XProcessingInstruction));
                    Validate.IsNotReferenceEqual(e.Current, instruction);
                    XProcessingInstruction instruction2 = (XProcessingInstruction)e.Current;
                    Validate.String(instruction2.Target, instruction.Target);
                    Validate.String(instruction2.Data, instruction.Data);

                    // Next node: element.
                    Validate.IsEqual(e.MoveNext(), true);
                    Validate.Type(e.Current, typeof(XElement));
                    Validate.IsNotReferenceEqual(e.Current, element);
                    XElement element2 = (XElement)e.Current;
                    Validate.ElementName(element2, element.Name.ToString());
                    Validate.Count(element2.Nodes(), 0);

                    // Should be end.
                    Validate.IsEqual(e.MoveNext(), false);
                }

                /// <summary>
                /// Validate behavior of the XDocument XmlDeclaration property.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "DocumentXmlDeclaration")]
                public void DocumentXmlDeclaration()
                {
                    XDocument doc = new XDocument();
                    Validate.IsNull(doc.Declaration);

                    XDeclaration dec = new XDeclaration("1.0", "utf-16", "yes");
                    XDocument doc2 = new XDocument(dec);
                    XDeclaration dec2 = doc2.Declaration;
                    Validate.IsReferenceEqual(dec2, dec);

                    doc2.RemoveNodes();
                    Validate.IsNotNull(doc2.Declaration);
                }

                /// <summary>
                /// Validate behavior of the XDocument Root property.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "DocumentRoot")]
                public void DocumentRoot()
                {
                    XDocument doc = new XDocument();
                    Validate.IsNull(doc.Root);

                    XElement e = new XElement("element");
                    doc.Add(e);
                    XElement e2 = doc.Root;
                    Validate.IsReferenceEqual(e2, e);

                    doc.RemoveNodes();
                    doc.Add(new XComment("comment"));
                    Validate.IsNull(doc.Root);
                }

                /// <summary>
                /// Validate behavior of adding string content to a document.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "DocumentAddString")]
                public void DocumentAddString()
                {
                    XDocument doc = new XDocument();

                    try
                    {
                        doc.Add("");
                        Validate.String(doc.ToString(SaveOptions.DisableFormatting), "");
                        doc.Add(" \t\r\n");
                        Validate.String(doc.ToString(SaveOptions.DisableFormatting), " \t\r\n");
                    }
                    catch (Exception ex)
                    {
                        throw new TestException(TestResult.Failed, ex.ToString());
                    }

                    try
                    {
                        doc.Add("a");
                        Validate.ExpectedThrow(typeof(ArgumentException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentException));
                    }

                    try
                    {
                        doc.Add("\tab");
                        Validate.ExpectedThrow(typeof(ArgumentException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentException));
                    }
                }
            }
        }
    }
}