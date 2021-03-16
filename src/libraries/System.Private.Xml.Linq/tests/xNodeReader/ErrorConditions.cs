// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class XNodeReaderFunctionalTests : TestModule
    {
        public partial class XNodeReaderTests : XLinqTestCase
        {
            public partial class ErrorConditions : BridgeHelpers
            {
                //[Variation(Desc = "IsStartElement")]
                public void Variation3()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            TestLog.Compare(r.IsStartElement(null, null), false, "Error");
                            TestLog.Compare(r.IsStartElement(null), false, "Error");
                            TestLog.Compare(r.IsStartElement("", ""), false, "Error");
                            TestLog.Compare(r.IsStartElement(""), false, "Error");
                        }
                    }
                }

                //[Variation(Desc = "LookupNamespace")]
                public void Variation4()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            TestLog.Compare(r.LookupNamespace(""), null, "Error");
                            TestLog.Compare(r.LookupNamespace(null), null, "Error");
                        }
                    }
                }

                //[Variation(Desc = "MoveToAttribute")]
                public void Variation5()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.MoveToAttribute(-100000);
                            r.MoveToAttribute(-1);
                            r.MoveToAttribute(0);
                            r.MoveToAttribute(100000);
                            TestLog.Compare(r.MoveToAttribute(null), false, "Error");
                            TestLog.Compare(r.MoveToAttribute(null, null), false, "Error");
                            TestLog.Compare(r.MoveToAttribute(""), false, "Error");
                            TestLog.Compare(r.MoveToAttribute("", ""), false, "Error");
                        }
                    }
                }

                //[Variation(Desc = "Other APIs")]
                public void Variation6()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.MoveToContent();
                            TestLog.Compare(r.MoveToElement(), false, "Error");
                            TestLog.Compare(r.MoveToFirstAttribute(), false, "Error");
                            TestLog.Compare(r.MoveToNextAttribute(), false, "Error");
                            TestLog.Compare(r.ReadAttributeValue(), false, "Error");
                            r.Read();
                            TestLog.Compare(r.ReadInnerXml(), "", "Error");
                            r.ReadOuterXml();
                            r.ResolveEntity();
                            r.Skip();
                        }
                    }
                }

                //[Variation(Desc = "ReadContentAs(null, null)")]
                public void Variation7()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.MoveToContent();
                            try
                            {
                                r.ReadContentAs(null, null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException)
                            {
                                try
                                {
                                    r.ReadContentAs(null, null);
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (ArgumentNullException) { }
                                catch (InvalidOperationException) { }
                            }
                            catch (InvalidOperationException)
                            {
                                try
                                {
                                    r.ReadContentAs(null, null);
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadContentAsBase64")]
                public void Variation8()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.MoveToContent();
                            try
                            {
                                r.ReadContentAsBase64(null, 0, 0);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (NotSupportedException)
                            {
                                try
                                {
                                    r.ReadContentAsBase64(null, 0, 0);
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (NotSupportedException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadContentAsBinHex")]
                public void Variation9()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.MoveToContent();
                            try
                            {
                                r.ReadContentAsBinHex(null, 0, 0);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (NotSupportedException)
                            {
                                try
                                {
                                    r.ReadContentAsBinHex(null, 0, 0);
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (NotSupportedException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadContentAsBoolean")]
                public void Variation10()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.MoveToContent();
                            try
                            {
                                r.ReadContentAsBoolean();
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (XmlException)
                            {
                                try
                                {
                                    r.ReadContentAsBoolean();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (InvalidOperationException) { }
                            }
                            catch (InvalidOperationException)
                            {
                                try
                                {
                                    r.ReadContentAsBoolean();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadContentAsDateTimeOffset")]
                public void Variation11b()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.MoveToContent();
                            try
                            {
                                r.ReadContentAsDateTimeOffset();
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (XmlException)
                            {
                                try
                                {
                                    r.ReadContentAsDateTimeOffset();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (InvalidOperationException) { }
                            }
                            catch (InvalidOperationException)
                            {
                                try
                                {
                                    r.ReadContentAsDateTimeOffset();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadContentAsDecimal")]
                public void Variation12()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.MoveToContent();
                            try
                            {
                                r.ReadContentAsDecimal();
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (XmlException)
                            {
                                try
                                {
                                    r.ReadContentAsDecimal();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (InvalidOperationException) { }
                            }
                            catch (InvalidOperationException)
                            {
                                try
                                {
                                    r.ReadContentAsDecimal();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadContentAsDouble")]
                public void Variation13()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.MoveToContent();
                            try
                            {
                                r.ReadContentAsDouble();
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (XmlException)
                            {
                                try
                                {
                                    r.ReadContentAsDouble();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (InvalidOperationException) { }
                            }
                            catch (InvalidOperationException)
                            {
                                try
                                {
                                    r.ReadContentAsDouble();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadContentAsFloat")]
                public void Variation14()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.MoveToContent();
                            try
                            {
                                r.ReadContentAsFloat();
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (XmlException)
                            {
                                try
                                {
                                    r.ReadContentAsFloat();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (InvalidOperationException) { }
                            }
                            catch (InvalidOperationException)
                            {
                                try
                                {
                                    r.ReadContentAsFloat();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadContentAsInt")]
                public void Variation15()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.MoveToContent();
                            try
                            {
                                r.ReadContentAsInt();
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (XmlException)
                            {
                                try
                                {
                                    r.ReadContentAsInt();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (InvalidOperationException) { }
                            }
                            catch (InvalidOperationException)
                            {
                                try
                                {
                                    r.ReadContentAsInt();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadContentAsLong")]
                public void Variation16()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.MoveToContent();
                            try
                            {
                                r.ReadContentAsLong();
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (XmlException)
                            {
                                try
                                {
                                    r.ReadContentAsLong();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (InvalidOperationException) { }
                            }
                            catch (InvalidOperationException)
                            {
                                try
                                {
                                    r.ReadContentAsLong();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAs(null, null)")]
                public void Variation17()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadElementContentAs(null, null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException)
                            {
                                try
                                {
                                    r.ReadElementContentAs(null, null, null, null);
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (ArgumentNullException) { }
                            }
                            catch (InvalidOperationException)
                            {
                                try
                                {
                                    r.ReadElementContentAs(null, null, null, null);
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (ArgumentNullException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAsBase64")]
                public void Variation18()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadElementContentAsBase64(null, 0, 0);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (NotSupportedException)
                            {
                                try
                                {
                                    r.ReadElementContentAsBase64(null, 0, 0);
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (NotSupportedException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAsBinHex")]
                public void Variation19()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadElementContentAsBinHex(null, 0, 0);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (NotSupportedException)
                            {
                                try
                                {
                                    r.ReadElementContentAsBinHex(null, 0, 0);
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (NotSupportedException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAsBoolean")]
                public void Variation20()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadElementContentAsBoolean(null, null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException)
                            {
                                try
                                {
                                    r.ReadElementContentAsBoolean();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (FormatException) { }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAsDecimal")]
                public void Variation22()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadElementContentAsDecimal(null, null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException)
                            {
                                try
                                {
                                    r.ReadElementContentAsDecimal();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (FormatException) { }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAsDouble")]
                public void Variation23()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadElementContentAsDouble(null, null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException)
                            {
                                try
                                {
                                    r.ReadElementContentAsDouble();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (FormatException) { }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAsFloat")]
                public void Variation24()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadElementContentAsFloat(null, null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException)
                            {
                                try
                                {
                                    r.ReadElementContentAsFloat();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (FormatException) { }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAsInt")]
                public void Variation25()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadElementContentAsInt(null, null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException)
                            {
                                try
                                {
                                    r.ReadElementContentAsInt();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (FormatException) { }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAsLong")]
                public void Variation26()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadElementContentAsLong(null, null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException)
                            {
                                try
                                {
                                    r.ReadElementContentAsLong();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                                catch (FormatException) { }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAsObject")]
                public void Variation27()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadElementContentAsLong(null, null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException)
                            {
                                try
                                {
                                    r.ReadElementContentAsLong(null, null);
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (ArgumentNullException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAsString")]
                public void Variation28()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadElementContentAsString(null, null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException)
                            {
                                try
                                {
                                    r.ReadElementContentAsString(null, null);
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (ArgumentNullException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadStartElement")]
                public void Variation30()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadStartElement(null, null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (XmlException)
                            {
                                try
                                {
                                    r.ReadStartElement(null);
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (XmlException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadToDescendant(null)")]
                public void Variation31()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            TestLog.Compare(r.ReadToDescendant(null, null), false, "Incorrect value returned");
                        }
                    }
                }

                //[Variation(Desc = "ReadToDescendant(String.Empty)")]
                public void Variation32()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            TestLog.Compare(r.ReadToDescendant("", ""), false, "Incorrect value returned");
                        }
                    }
                }

                //[Variation(Desc = "ReadToFollowing(null)")]
                public void Variation33()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            TestLog.Compare(r.ReadToFollowing(null, null), false, "Incorrect value returned");
                        }
                    }
                }

                //[Variation(Desc = "ReadToFollowing(String.Empty)")]
                public void Variation34()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            TestLog.Compare(r.ReadToFollowing("", ""), false, "Incorrect value returned");
                        }
                    }
                }

                //[Variation(Desc = "ReadToNextSibling(null)")]
                public void Variation35()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            TestLog.Compare(r.ReadToNextSibling(null, null), false, "Incorrect value returned");
                        }
                    }
                }

                //[Variation(Desc = "ReadToNextSibling(String.Empty)")]
                public void Variation36()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            TestLog.Compare(r.ReadToNextSibling("", ""), false, "Incorrect value returned");
                        }
                    }
                }

                //[Variation(Desc = "ReadValueChunk")]
                public void Variation37()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            r.Read();
                            try
                            {
                                r.ReadValueChunk(null, 0, 0);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (NotSupportedException)
                            {
                                try
                                {
                                    r.ReadValueChunk(null, 0, 0);
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (NotSupportedException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAsObject")]
                public void Variation38()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            while (r.Read()) ;
                            try
                            {
                                r.ReadElementContentAsObject();
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (InvalidOperationException)
                            {
                                try
                                {
                                    r.ReadElementContentAsObject();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //[Variation(Desc = "ReadElementContentAsString")]
                public void Variation39()
                {
                    foreach (XNode n in GetXNodeR())
                    {
                        using (XmlReader r = n.CreateReader())
                        {
                            while (r.Read()) ;
                            try
                            {
                                r.ReadElementContentAsString();
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (InvalidOperationException)
                            {
                                try
                                {
                                    r.ReadElementContentAsString();
                                    throw new TestException(TestResult.Failed, "");
                                }
                                catch (InvalidOperationException) { }
                            }
                        }
                    }
                }

                //GetXNode List
                public List<XNode> GetXNodeR()
                {
                    List<XNode> xNode = new List<XNode>();

                    xNode.Add(new XDocument(new XDocumentType("root", "", "", "<!ELEMENT root ANY>"), new XElement("root")));
                    xNode.Add(new XElement("elem1"));
                    xNode.Add(new XText("text1"));
                    xNode.Add(new XComment("comment1"));
                    xNode.Add(new XProcessingInstruction("pi1", "pi1pi1pi1pi1pi1"));
                    xNode.Add(new XCData("cdata cdata"));
                    xNode.Add(new XDocumentType("dtd1", "dtd1dtd1dtd1", "dtd1dtd1", "dtd1dtd1dtd1dtd1"));
                    return xNode;
                }
            }
        }
    }
}
