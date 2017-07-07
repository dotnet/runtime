// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class XNodeBuilderTests : XLinqTestCase
        {
            public partial class XObjectBuilderTest : BridgeHelpers
            {
                //[Variation(Priority = 2, Desc = "LookupPrefix(null)")]
                public void var_1()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            string prefix = w.LookupPrefix(null);
                        }
                        catch (ArgumentNullException e)
                        {
                            CompareParamName(e.ParamName, "ns", "prefix mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                string prefix = w.LookupPrefix(null);
                            }
                            catch (ArgumentNullException ae)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                CompareParamName(ae.ParamName, "ns", "prefix mismatch");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteAttributes(null, true)", Param = true)]
                //[Variation(Priority = 2, Desc = "WriteAttributes(null, false)", Param = false)]
                public void var_2()
                {
                    XDocument doc = new XDocument();
                    bool defattr = (bool)Variation.Param;
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteAttributes(null, defattr);
                        }
                        catch (ArgumentNullException e)
                        {
                            CompareParamName(e.ParamName, "reader", "mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                                w.WriteAttributes(null, defattr);
                            }
                            catch (ArgumentNullException ae)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                                CompareParamName(ae.ParamName, "reader", "mismatch");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteAttributeString(null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteAttributeString(String.Empty)", Param = "")]
                public void var_3()
                {
                    XDocument doc = new XDocument();
                    string name = (string)Variation.Param;
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteAttributeString(name, null);
                        }
                        catch (ArgumentException e)
                        {
                            CompareParamName(e.ParamName, null, "mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteAttributeString(name, null);
                            }
                            catch (ArgumentException)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                CompareParamName(e.ParamName, null, "mismatch");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteBase64(null)")]
                public void var_4()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteBase64(null, 0, 0);
                        }
                        catch (ArgumentNullException e)
                        {
                            CompareParamName(e.ParamName, "buffer", "mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteBase64(null, 0, 0);
                            }
                            catch (ArgumentNullException ae)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                CompareParamName(ae.ParamName, "buffer", "mismatch");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteBinHex(null)")]
                public void var_5()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        w.WriteStartDocument();
                        w.WriteStartElement("a");
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Element, "Error");
                            w.WriteBinHex(null, 0, 0);
                        }
                        catch (ArgumentNullException e)
                        {
                            CompareParamName(e.ParamName, "buffer", "mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteBinHex(null, 0, 0);
                            }
                            catch (InvalidOperationException)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteCData(null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteCData('')", Param = "")]
                public void var_6()
                {
                    string param = (string)Variation.Param;
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteCData(param);
                        }
                        catch (InvalidOperationException)
                        {
                            try
                            {
                                w.WriteCData(param);
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteChars(null)")]
                public void var_7()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteChars(null, 0, 0);
                        }
                        catch (ArgumentNullException)
                        {
                            try
                            {
                                w.WriteChars(null, 0, 0);
                            }
                            catch (ArgumentNullException)
                            {
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "Other APIs")]
                public void var_8()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                    w.WriteComment(null);
                    TestLog.Compare(w.Equals(null), false, "Error");
                    w.WriteComment("");
                    w.LookupPrefix("");
                    TestLog.Compare(w.Equals(""), false, "Error");
                    TestLog.Compare(w.WriteState, WriteState.Prolog, "Error");
                    w.Flush();
                    w.Dispose();
                    TestLog.Compare(w.WriteState, WriteState.Closed, "Error");
                    w.Flush();
                    w.Dispose();
                    TestLog.Compare(w.WriteState, WriteState.Closed, "Error");
                }

                //[Variation(Priority = 2, Desc = "WriteDocType(null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteDocType('')", Param = "")]
                public void var_9()
                {
                    string param = (string)Variation.Param;
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteDocType(param, param, param, param);
                        }
                        catch (ArgumentException)
                        {
                            try
                            {
                                w.WriteDocType(param, param, param, param);
                            }
                            catch (ArgumentException) { return; }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteElementString(null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteElementString(String.Empty)", Param = "")]
                public void var_10()
                {
                    XDocument doc = new XDocument();
                    string name = (string)Variation.Param;
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteElementString(name, null, null, null);
                        }
                        catch (ArgumentException e)
                        {
                            CompareParamName(e.ParamName, null, "mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteElementString(name, null, null, null);
                            }
                            catch (ArgumentException ae)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                CompareParamName(ae.ParamName, null, "mismatch");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteEndAttribute()")]
                public void var_11()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteEndAttribute();
                        }
                        catch (InvalidOperationException)
                        {
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteEndAttribute();
                            }
                            catch (InvalidOperationException)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteEndDocument()")]
                public void var_12()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteEndDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteEndDocument();
                            }
                            catch (InvalidOperationException)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteEndElement()")]
                public void var_13()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteEndElement();
                        }
                        catch (InvalidOperationException)
                        {
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteEndElement();
                            }
                            catch (InvalidOperationException)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteEntityRef(null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteEntityRef('')", Param = "")]
                public void var_14()
                {
                    string param = (string)Variation.Param;
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteEntityRef(param);
                        }
                        catch (ArgumentException)
                        {
                            try
                            {
                                w.WriteEntityRef(param);
                            }
                            catch (ArgumentException)
                            {
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteFullEndElement()")]
                public void var_15()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteFullEndElement();
                        }
                        catch (InvalidOperationException)
                        {
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteFullEndElement();
                            }
                            catch (InvalidOperationException)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteName(null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteName('')", Param = "")]
                public void var_16()
                {
                    string param = (string)Variation.Param;
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteName(param);
                        }
                        catch (ArgumentException e)
                        {
                            CompareParamName(e.ParamName, "name", "mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                                w.WriteName(param);
                            }
                            catch (ArgumentException ae)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                                CompareParamName(ae.ParamName, "name", "mismatch");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteNmToken(null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteNmToken('')", Param = "")]
                public void var_17()
                {
                    string param = (string)Variation.Param;
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteNmToken(param);
                        }
                        catch (ArgumentException e)
                        {
                            CompareParamName(e.ParamName, null, "mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                                w.WriteNmToken(param);
                            }
                            catch (ArgumentException ae)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                                CompareParamName(ae.ParamName, null, "mismatch");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteNode(null)", Params = new object[] { "reader", true })]
                //[Variation(Priority = 2, Desc = "WriteNode(null)", Params = new object[] { "reader", false })]
                //[Variation(Priority = 2, Desc = "WriteNode(null)", Params = new object[] { "navigator", true })]
                //[Variation(Priority = 2, Desc = "WriteNode(null)", Params = new object[] { "navigator", false })]
                public void var_18()
                {
                    string param1 = (string)Variation.Params[0];
                    bool param2 = (bool)Variation.Params[1];
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            switch (param1)
                            {
                                case ("reader"):
                                    w.WriteNode((XmlReader)null, param2);
                                    break;
                            }
                        }
                        catch (ArgumentNullException e)
                        {
                            switch (param1)
                            {
                                case ("reader"):
                                    CompareParamName(e.ParamName, "reader", "mismatch");
                                    break;
                                case ("navigator"):
                                    CompareParamName(e.ParamName, "navigator", "mismatch");
                                    break;
                            }
                            try
                            {
                                switch (param1)
                                {
                                    case ("reader"):
                                        w.WriteNode((XmlReader)null, param2);
                                        break;
                                }
                            }
                            catch (ArgumentNullException)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                                switch (param1)
                                {
                                    case ("reader"):
                                        CompareParamName(e.ParamName, "reader", "mismatch");
                                        break;
                                }
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteProcessingInstruction(null, null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteProcessingInstruction('', '')", Param = "")]
                public void var_19()
                {
                    string param = (string)Variation.Param;
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteProcessingInstruction(param, param);
                        }
                        catch (ArgumentException e)
                        {
                            CompareParamName(e.ParamName, null, "mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteProcessingInstruction(param, param);
                            }
                            catch (ArgumentException ae)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                CompareParamName(ae.ParamName, null, "mismatch");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteQualifiedName(null, null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteQualifiedName('', '')", Param = "")]
                public void var_20()
                {
                    string param = (string)Variation.Param;
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteQualifiedName(param, param);
                        }
                        catch (ArgumentException e)
                        {
                            CompareParamName(e.ParamName, null, "mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteQualifiedName(param, param);
                            }
                            catch (ArgumentException ae)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                CompareParamName(ae.ParamName, null, "mismatch");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteRaw(null, 0, 0)")]
                public void var_21()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteRaw(null, 0, 0);
                        }
                        catch (ArgumentNullException e)
                        {
                            CompareParamName(e.ParamName, "buffer", "mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteRaw(null, 0, 0);
                            }
                            catch (ArgumentNullException ae)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                CompareParamName(ae.ParamName, "buffer", "mismatch");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteStartAttribute(null, null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteStartAttribute('', '')", Param = "")]
                public void var_22()
                {
                    XDocument doc = new XDocument();
                    string param = (string)Variation.Param;
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteStartAttribute(param, param);
                        }
                        catch (ArgumentException e)
                        {
                            CompareParamName(e.ParamName, null, "mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteStartAttribute(param, param);
                            }
                            catch (ArgumentException ae)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                CompareParamName(ae.ParamName, null, "mismatch");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteStartElement(null, null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteStartElement('', '')", Param = "")]
                public void var_23()
                {
                    XDocument doc = new XDocument();
                    string param = (string)Variation.Param;
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteStartElement(param);
                        }
                        catch (ArgumentException e)
                        {
                            CompareParamName(e.ParamName, null, "mismatch");
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteStartElement(param);
                            }
                            catch (ArgumentException ae)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                CompareParamName(ae.ParamName, null, "mismatch");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteStartDocument()", Param = 1)]
                //[Variation(Priority = 2, Desc = "WriteStartDocument(true)", Param = 2)]
                //[Variation(Priority = 2, Desc = "WriteStartDocument(false)", Param = 3)]
                public void var_24()
                {
                    XDocument doc = new XDocument();
                    XmlWriter w = CreateWriter(doc);
                    int param = (int)Variation.Param;
                    switch (param)
                    {
                        case 1: w.WriteStartDocument(); break;
                        case 2: w.WriteStartDocument(true); break;
                        case 3: w.WriteStartDocument(false); break;
                        default: throw new TestException(TestResult.Failed, "");
                    }
                    TestLog.Compare(w.WriteState, WriteState.Prolog, "Error");
                    w.WriteStartElement("a");
                    TestLog.Compare(w.WriteState, WriteState.Element, "Error");
                    w.Dispose();
                    TestLog.Compare(w.WriteState, WriteState.Closed, "Error");
                    if (doc.ToString() != "<a />")
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteString(null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteString('')", Param = "")]
                public void var_25()
                {
                    XDocument doc = new XDocument();
                    string param = (string)Variation.Param;
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteString(param);
                        }
                        catch (InvalidOperationException)
                        {
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteString(param);
                            }
                            catch (InvalidOperationException)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                if (param == null)
                                    throw;
                                else
                                    return;
                            }
                        }
                    }
                    if (param == null)
                        return;
                    else
                        throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteValue(true)", Param = true)]
                //[Variation(Priority = 2, Desc = "WriteValue(false)", Param = false)]
                public void var_26()
                {
                    bool param = (bool)Variation.Param;
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteValue(param);
                        }
                        catch (InvalidOperationException)
                        {
                            try
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                w.WriteValue(param);
                            }
                            catch (InvalidOperationException)
                            {
                                TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                                return;
                            }
                        }
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "WriteWhitespace(null)", Param = null)]
                //[Variation(Priority = 2, Desc = "WriteWhitespace('')", Param = "")]
                public void var_27()
                {
                    string param = (string)Variation.Param;
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        w.WriteWhitespace(param);
                        TestLog.Compare(w.WriteState, WriteState.Prolog, "Error");
                    }
                }

                //[Variation(Priority = 2, Desc = "EntityRef after Document should error - PROLOG")]
                public void var_28()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteStartDocument();
                            w.WriteEntityRef("ent");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                w.WriteEntityRef("ent");
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "EntityRef after Document should error - EPILOG")]
                public void var_29()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteStartDocument();
                            TestLog.Compare(w.WriteState, WriteState.Prolog, "Error");
                            w.WriteStartElement("Root");
                            TestLog.Compare(w.WriteState, WriteState.Element, "Error");
                            w.WriteEndElement();
                            TestLog.Compare(w.WriteState, WriteState.Content, "Error");
                            w.WriteEntityRef("ent");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                w.WriteEntityRef("ent");
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "CharEntity after Document should error - PROLOG")]
                public void var_30()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteStartDocument();
                            w.WriteCharEntity('\uD23E');
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                w.WriteCharEntity('\uD23E');
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "CharEntity after Document should error - EPILOG")]
                public void var_31()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteStartDocument();
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            w.WriteCharEntity('\uD23E');
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                w.WriteCharEntity('\uD23E');
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "SurrogateCharEntity after Document should error - PROLOG")]
                public void var_32()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteStartDocument();
                            w.WriteSurrogateCharEntity('\uDF41', '\uD920');
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                w.WriteSurrogateCharEntity('\uDF41', '\uD920');
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "SurrogateCharEntity after Document should error - EPILOG")]
                public void var_33()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteStartDocument();
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            w.WriteSurrogateCharEntity('\uDF41', '\uD920');
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                w.WriteSurrogateCharEntity('\uDF41', '\uD920');
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "Attribute after Document should error - PROLOG")]
                public void var_34()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteStartDocument();
                            w.WriteStartAttribute("attr", "");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                w.WriteStartAttribute("attr", "");
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "Attribute after Document should error - EPILOG")]
                public void var_35()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            TestLog.Compare(w.WriteState, WriteState.Start, "Error");
                            w.WriteStartDocument();
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            w.WriteStartAttribute("attr", "");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                w.WriteStartAttribute("attr", "");
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "CDATA after Document should error - PROLOG")]
                public void var_36()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            TestLog.Compare(w.WriteState, WriteState.Prolog, "Error");
                            w.WriteCData("Invalid");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                w.WriteCData("Invalid");
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                }

                //[Variation(Priority = 2, Desc = "CDATA after Document should error - EPILOG")]
                public void var_37()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartDocument();
                            w.WriteStartElement("Root");
                            w.WriteEndElement();
                            TestLog.Compare(w.WriteState, WriteState.Content, "Error");
                            w.WriteCData("Invalid");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                w.WriteCData("Invalid");
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "Element followed by Document should error")]
                public void var_38()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            TestLog.Compare(w.WriteState, WriteState.Element, "Error");
                            w.WriteStartDocument();
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                w.WriteStartDocument();
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "Element followed by DocType should error")]
                public void var_39()
                {
                    XDocument doc = new XDocument();
                    using (XmlWriter w = CreateWriter(doc))
                    {
                        try
                        {
                            w.WriteStartElement("Root");
                            TestLog.Compare(w.WriteState, WriteState.Element, "Error");
                            w.WriteDocType("Test", null, null, "");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                            try
                            {
                                w.WriteDocType("Test", null, null, "");
                            }
                            catch (InvalidOperationException) { return; }
                        }
                    }
                    TestLog.WriteLine("Did not throw exception");
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "WriteBase64")]
                public void Variation41()
                {
                    int bufferSize = 4096;
                    byte[] buffer = new byte[bufferSize];
                    int readByte = 1;
                    XDocument d = new XDocument();
                    XmlWriter xw = d.CreateWriter();
                    TestLog.Compare(xw.WriteState, WriteState.Start, "Error");
                    xw.WriteStartDocument(true);
                    TestLog.Compare(xw.WriteState, WriteState.Prolog, "Error");
                    xw.WriteStartElement("root");
                    TestLog.Compare(xw.WriteState, WriteState.Element, "Error");
                    try
                    {
                        xw.WriteBase64(buffer, 0, readByte);
                    }
                    catch (NotSupportedException)
                    {
                        TestLog.Compare(xw.WriteState, WriteState.Error, "Error");
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Desc = "WriteEntityRef")]
                public void Variation42()
                {
                    XElement d = new XElement("a");
                    XmlWriter w = d.CreateWriter();
                    try
                    {
                        w.WriteEntityRef("ent");
                    }
                    catch (NotSupportedException)
                    {
                        TestLog.Compare(w.WriteState, WriteState.Error, "Error");
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                private void CompareParamName(string actual, string expected, string message)
                {
                    if (PlatformDetection.IsNetNative) // ILC optimization sets ParamName always to null.
                        return;

                    TestLog.Compare(actual, expected, message);
                }
            }
        }
    }
}
