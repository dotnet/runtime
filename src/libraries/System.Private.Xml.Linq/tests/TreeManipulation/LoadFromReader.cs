// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XmlDiff;
using CoreXml.Test.XLinq;
using Microsoft.Test.ModuleCore;
using XmlCoreTest.Common;

namespace XLinqTests
{
    public class LoadFromReader : XLinqTestCase
    {
        // Type is CoreXml.Test.XLinq.FunctionalTests+TreeManipulationTests+LoadFromReader
        // Test Case

        #region Constants

        private const string BaseSaveFileName = "baseSave.xml";

        private const string xml = "<PurchaseOrder><Item price=\"100\">Motor<![CDATA[cdata]]><elem>inner text</elem>text<?pi pi pi?></Item></PurchaseOrder>";

        #endregion

        #region Fields

        private readonly XmlDiff _diff;

        private readonly MethodsEnum[] _methods = { MethodsEnum.Load, MethodsEnum.ReadFrom, MethodsEnum.Parse };

        #endregion

        #region Constructors and Destructors

        public LoadFromReader()
        {
            _diff = new XmlDiff();
        }

        #endregion

        #region Enums

        private enum MethodsEnum
        {
            Load,

            ReadFrom,

            Parse
        }

        #endregion

        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(reader_1) { Attribute = new VariationAttribute("Read XDocument") { Priority = 0 } });
            AddChild(new TestVariation(reader_2) { Attribute = new VariationAttribute("Read XElement") { Priority = 0 } });
            AddChild(new TestVariation(reader_12) { Attribute = new VariationAttribute("Read XDocument with string content") { Params = new object[] { "<A>truck</A>" }, Priority = 0 } });
            AddChild(new TestVariation(reader_12) { Attribute = new VariationAttribute("Read XDocument with attribute") { Params = new object[] { "<A attr=\"1\" />" }, Priority = 0 } });
            AddChild(new TestVariation(reader_12) { Attribute = new VariationAttribute("Read XDocument with empty element") { Params = new object[] { "<A />" }, Priority = 0 } });
            AddChild(new TestVariation(reader_12) { Attribute = new VariationAttribute("Read XDocument with empty content") { Params = new object[] { "<A></A>" }, Priority = 0 } });
            AddChild(new TestVariation(reader_12) { Attribute = new VariationAttribute("Read XDocument with namespace and attributes") { Params = new object[] { "<A attr=\"1\" a2=\"a2\" a3=\"a3\" xmlns=\"def\" xmlns:p=\"ns\" p:a3=\"pa3\" />" }, Priority = 2 } });
            AddChild(new TestVariation(reader_12) { Attribute = new VariationAttribute("Read XDocument with string content and empty node") { Params = new object[] { "<X>t0<A />t00</X>" }, Priority = 0 } });
            AddChild(new TestVariation(reader_12) { Attribute = new VariationAttribute("Read XDocument with namespace, CData, PI") { Params = new object[] { "<A xmlns=\"ns0\"><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_12) { Attribute = new VariationAttribute("Read XDocument with string content and non-empty node") { Params = new object[] { "<X>t0<A>truck</A>t00</X>" }, Priority = 0 } });
            AddChild(new TestVariation(reader_12) { Attribute = new VariationAttribute("Read XDocument with namespace") { Params = new object[] { "<a:A xmlns:a=\"a\"><C xmlns:p=\"nsc\" /><B /></a:A>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_12) { Attribute = new VariationAttribute("Read XDocument with attributes") { Params = new object[] { "<A attr=\"1\" a2=\"a2\" a3=\"a3\" />" }, Priority = 0 } });
            AddChild(new TestVariation(reader_12) { Attribute = new VariationAttribute("Read XDocument with namespace") { Params = new object[] { "<A xmlns:p=\"nsc\"><p:C xmlns:a=\"a\"><a:S /></p:C><B /></A>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_12) { Attribute = new VariationAttribute("Read XDocument with namespace, CData") { Params = new object[] { "<A xmlns=\"ns0\"><![CDATA[tralala]]></A>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_14) { Attribute = new VariationAttribute("Read XElement with string content and empty node") { Params = new object[] { "<X>t0<A />t00</X>" }, Priority = 0 } });
            AddChild(new TestVariation(reader_14) { Attribute = new VariationAttribute("Read XElement with namespace, CData, PI") { Params = new object[] { "<A xmlns=\"ns0\"><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_14) { Attribute = new VariationAttribute("Read XElement with namespace and attributes") { Params = new object[] { "<A attr=\"1\" a2=\"a2\" a3=\"a3\" xmlns=\"def\" xmlns:p=\"ns\" p:a3=\"pa3\" />" }, Priority = 2 } });
            AddChild(new TestVariation(reader_14) { Attribute = new VariationAttribute("Read XElement with namespace") { Params = new object[] { "<a:A xmlns:a=\"a\"><C xmlns:p=\"nsc\" /><B /></a:A>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_14) { Attribute = new VariationAttribute("Read XElement with string content") { Params = new object[] { "<A>truck</A>" }, Priority = 0 } });
            AddChild(new TestVariation(reader_14) { Attribute = new VariationAttribute("Read XElement with empty element") { Params = new object[] { "<A />" }, Priority = 0 } });
            AddChild(new TestVariation(reader_14) { Attribute = new VariationAttribute("Read XElement with string content and non-empty node") { Params = new object[] { "<X>t0<A>truck</A>t00</X>" }, Priority = 0 } });
            AddChild(new TestVariation(reader_14) { Attribute = new VariationAttribute("Read XElement with namespace, CData") { Params = new object[] { "<A xmlns=\"ns0\"><![CDATA[tralala]]></A>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_14) { Attribute = new VariationAttribute("Read XElement with namespace") { Params = new object[] { "<A xmlns:p=\"nsc\"><p:C xmlns:a=\"a\"><a:S /></p:C><B /></A>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_14) { Attribute = new VariationAttribute("Read XElement with empty content") { Params = new object[] { "<A></A>" }, Priority = 0 } });
            AddChild(new TestVariation(reader_14) { Attribute = new VariationAttribute("Read XElement with attribute") { Params = new object[] { "<A attr=\"1\" />" }, Priority = 0 } });
            AddChild(new TestVariation(reader_14) { Attribute = new VariationAttribute("Read XElement with attributes") { Params = new object[] { "<A attr=\"1\" a2=\"a2\" a3=\"a3\" />" }, Priority = 0 } });
            AddChild(new TestVariation(reader_15) { Attribute = new VariationAttribute("Read XElement with nwf <Atruck</A>") { Params = new object[] { "<Atruck</A>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_15) { Attribute = new VariationAttribute("Read XElement with nwf empty") { Params = new object[] { "" }, Priority = 2 } });
            AddChild(new TestVariation(reader_15) { Attribute = new VariationAttribute("Read XElement with nwf tab") { Params = new object[] { "\t" }, Priority = 2 } });
            AddChild(new TestVariation(reader_15) { Attribute = new VariationAttribute("Read XElement with nwf EOL") { Params = new object[] { "\n" }, Priority = 2 } });
            AddChild(new TestVariation(reader_15) { Attribute = new VariationAttribute("Read XElement with nwf no closing bracket") { Params = new object[] { "<A >" }, Priority = 2 } });
            AddChild(new TestVariation(reader_15) { Attribute = new VariationAttribute("Read XElement with nwf no opening bracket") { Params = new object[] { "A></A>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_15) { Attribute = new VariationAttribute("Read XElement with nwf missing element") { Params = new object[] { "<q=a/>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_16) { Attribute = new VariationAttribute("Read XDocument with nwf empty") { Params = new object[] { "\n" }, Priority = 2 } });
            AddChild(new TestVariation(reader_16) { Attribute = new VariationAttribute("Read XDocument with nwf empty") { Params = new object[] { "" }, Priority = 2 } });
            AddChild(new TestVariation(reader_16) { Attribute = new VariationAttribute("Read XDocument with nwf no opening bracket") { Params = new object[] { "A></A>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_16) { Attribute = new VariationAttribute("Read XDocument with nwf missing element") { Params = new object[] { "<q=a/>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_16) { Attribute = new VariationAttribute("Read XDocument with nwf empty") { Params = new object[] { "\t" }, Priority = 2 } });
            AddChild(new TestVariation(reader_16) { Attribute = new VariationAttribute("Read XDocument with nwf no closing bracket") { Params = new object[] { "<A >" }, Priority = 2 } });
            AddChild(new TestVariation(reader_16) { Attribute = new VariationAttribute("Read XDocument with nwf <Atruck</A>") { Params = new object[] { "<Atruck</A>" }, Priority = 2 } });
            AddChild(new TestVariation(reader_19) { Attribute = new VariationAttribute("XDocument: Call Read after ReadState = Closed") { Priority = 2 } });
            AddChild(new TestVariation(reader_20) { Attribute = new VariationAttribute("XElement: Call Read after ReadState = Closed") { Priority = 2 } });
            AddChild(new TestVariation(reader_25) { Attribute = new VariationAttribute("XDocument: Null parameters for Load") { Priority = 1 } });
            AddChild(new TestVariation(reader_26) { Attribute = new VariationAttribute("XElement: Null parameters for Load") { Priority = 1 } });
            AddChild(new TestVariation(reader_27) { Attribute = new VariationAttribute("XDocument: Null parameters for Parse, ReadFrom and ReadContentFrom") { Priority = 1 } });
            AddChild(new TestVariation(reader_28) { Attribute = new VariationAttribute("XElement: Null parameters for Parse, ReadFrom and ReadContentFrom") { Priority = 1 } });
        }

        //[Variation(Priority = 0, Desc = "Read XDocument")]

        public void reader_1()
        {
            var doc = new XDocument(new XElement("PurchaseOrder", new XElement("Item", "Motor", new XAttribute("price", "100"), new XCData("cdata"), new XElement("elem", "inner text"), new XText("text"), new XProcessingInstruction("pi", "pi pi"))));

            SaveBaseline(xml);
            LoadURI(doc, BaseSaveFileName, xml);
            LoadTextReader(doc, BaseSaveFileName, xml);
            Parse(doc, xml);
            LoadXmlReader(doc, xml);
            ReadFrom(xml);
        }

        //[Variation(Priority = 0, Desc = "Read XElement")]

        //XDocument
        //[Variation(Priority = 0, Desc = "Read XDocument with empty element", Params = new object[] { "<A />" })]
        //[Variation(Priority = 0, Desc = "Read XDocument with empty content", Params = new object[] { "<A></A>" })]
        //[Variation(Priority = 0, Desc = "Read XDocument with string content", Params = new object[] { "<A>truck</A>" })]
        //[Variation(Priority = 0, Desc = "Read XDocument with string content and empty node", Params = new object[] { "<X>t0<A />t00</X>" })]
        //[Variation(Priority = 0, Desc = "Read XDocument with string content and non-empty node", Params = new object[] { "<X>t0<A>truck</A>t00</X>" })]
        //[Variation(Priority = 2, Desc = "Read XDocument with namespace", Params = new object[] { "<a:A xmlns:a=\"a\"><C xmlns:p=\"nsc\" /><B /></a:A>" })]
        //[Variation(Priority = 2, Desc = "Read XDocument with namespace", Params = new object[] { "<A xmlns:p=\"nsc\"><p:C xmlns:a=\"a\"><a:S /></p:C><B /></A>" })]
        //[Variation(Priority = 2, Desc = "Read XDocument with namespace, CData", Params = new object[] { "<A xmlns=\"ns0\"><![CDATA[tralala]]></A>" })]
        //[Variation(Priority = 2, Desc = "Read XDocument with namespace, CData, PI", Params = new object[] { "<A xmlns=\"ns0\"><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" })]
        //[Variation(Priority = 0, Desc = "Read XDocument with attribute", Params = new object[] { "<A attr=\"1\" />" })]
        //[Variation(Priority = 0, Desc = "Read XDocument with attributes", Params = new object[] { "<A attr=\"1\" a2=\"a2\" a3=\"a3\" />" })]
        //[Variation(Priority = 2, Desc = "Read XDocument with namespace and attributes", Params = new object[] { "<A attr=\"1\" a2=\"a2\" a3=\"a3\" xmlns=\"def\" xmlns:p=\"ns\" p:a3=\"pa3\" />" })]
        public void reader_12()
        {
            var xml = Variation.Params[0] as string;
            SaveBaseline(xml);
            XDocument doc = XDocument.Parse(xml);

            LoadXmlReader(doc, xml);
            LoadURI(doc, BaseSaveFileName, xml);
            LoadTextReader(doc, BaseSaveFileName, xml);
            Parse(doc, xml);
            ReadFrom(xml);
        }

        //XElement
        //[Variation(Priority = 0, Desc = "Read XElement with empty element", Params = new object[] { "<A />" })]
        //[Variation(Priority = 0, Desc = "Read XElement with empty content", Params = new object[] { "<A></A>" })]
        //[Variation(Priority = 0, Desc = "Read XElement with string content", Params = new object[] { "<A>truck</A>" })]
        //[Variation(Priority = 0, Desc = "Read XElement with string content and empty node", Params = new object[] { "<X>t0<A />t00</X>" })]
        //[Variation(Priority = 0, Desc = "Read XElement with string content and non-empty node", Params = new object[] { "<X>t0<A>truck</A>t00</X>" })]
        //[Variation(Priority = 2, Desc = "Read XElement with namespace", Params = new object[] { "<a:A xmlns:a=\"a\"><C xmlns:p=\"nsc\" /><B /></a:A>" })]
        //[Variation(Priority = 2, Desc = "Read XElement with namespace", Params = new object[] { "<A xmlns:p=\"nsc\"><p:C xmlns:a=\"a\"><a:S /></p:C><B /></A>" })]
        //[Variation(Priority = 2, Desc = "Read XElement with namespace, CData", Params = new object[] { "<A xmlns=\"ns0\"><![CDATA[tralala]]></A>" })]
        //[Variation(Priority = 2, Desc = "Read XElement with namespace, CData, PI", Params = new object[] { "<A xmlns=\"ns0\"><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" })]
        //[Variation(Priority = 0, Desc = "Read XElement with attribute", Params = new object[] { "<A attr=\"1\" />" })]
        //[Variation(Priority = 0, Desc = "Read XElement with attributes", Params = new object[] { "<A attr=\"1\" a2=\"a2\" a3=\"a3\" />" })]
        //[Variation(Priority = 2, Desc = "Read XElement with namespace and attributes", Params = new object[] { "<A attr=\"1\" a2=\"a2\" a3=\"a3\" xmlns=\"def\" xmlns:p=\"ns\" p:a3=\"pa3\" />" })]
        public void reader_14()
        {
            var xml = Variation.Params[0] as string;
            SaveBaseline(xml);
            XElement doc = XElement.Parse(xml);

            LoadXmlReader(doc, xml);
            LoadURI(doc, BaseSaveFileName, xml);
            LoadTextReader(doc, BaseSaveFileName, xml);
            Parse(doc, xml);
            ReadFrom(xml);
        }

        //XElement: non-wellformed xml
        //[Variation(Priority = 2, Desc = "Read XElement with nwf empty", Params = new object[] { "" })]
        //[Variation(Priority = 2, Desc = "Read XElement with nwf tab", Params = new object[] { "\t" })]
        //[Variation(Priority = 2, Desc = "Read XElement with nwf EOL", Params = new object[] { "\n" })]
        //[Variation(Priority = 2, Desc = "Read XElement with nwf no closing bracket", Params = new object[] { "<A >" })]
        //[Variation(Priority = 2, Desc = "Read XElement with nwf no opening bracket", Params = new object[] { "A></A>" })]
        //[Variation(Priority = 2, Desc = "Read XElement with nwf missing element", Params = new object[] { "<q=a/>" })]
        //[Variation(Priority = 2, Desc = "Read XElement with nwf <Atruck</A>", Params = new object[] { "<Atruck</A>" })]
        public void reader_15()
        {
            var xml = Variation.Params[0] as string;
            var elem = new XElement("a");

            foreach (MethodsEnum m in _methods)
            {
                using (XmlReader r = XmlReader.Create(new StringReader(xml), null))
                {
                    try
                    {
                        switch (m)
                        {
                            case MethodsEnum.Load:
                                XElement.Load(r);
                                break;
                            case MethodsEnum.ReadFrom:
                                XNode.ReadFrom(r);
                                break;
                            case MethodsEnum.Parse:
                                XElement.Parse(xml);
                                break;
                            default:
                                TestLog.Compare(false, "Unexpected API");
                                break;
                        }
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (Exception)
                    {
                        if (m == MethodsEnum.Load)
                        {
                            TestLog.Compare(r.ReadState, ReadState.Error, "Error in ReadState");
                        }
                        else
                        {
                            TestLog.Compare(r.ReadState, ReadState.Initial, "Error in ReadState");
                        }

                        //
                        // try it again, when reader is in error condition
                        //

                        try
                        {
                            switch (m)
                            {
                                case MethodsEnum.Load:
                                    XElement.Load(r);
                                    break;
                                case MethodsEnum.ReadFrom:
                                    XNode.ReadFrom(r);
                                    break;
                                case MethodsEnum.Parse:
                                    XElement.Parse(xml);
                                    break;
                                default:
                                    TestLog.Compare(false, "Unexpected API");
                                    break;
                            }
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (InvalidOperationException)
                        {
                            if (m == MethodsEnum.Load)
                            {
                                TestLog.Compare(r.ReadState, ReadState.Error, "Error in ReadState");
                            }
                            else
                            {
                                TestLog.Compare(r.ReadState, ReadState.Initial, "Error in ReadState");
                            }
                        }
                        catch (XmlException)
                        {
                            TestLog.Compare(r.ReadState, ReadState.Initial, "Error in ReadState");
                        }
                    }
                }
            }
        }

        //XDocument: non-wellformed xml
        //[Variation(Priority = 2, Desc = "Read XDocument with nwf empty", Params = new object[] { "" })]
        //[Variation(Priority = 2, Desc = "Read XDocument with nwf empty", Params = new object[] { "\t" })]
        //[Variation(Priority = 2, Desc = "Read XDocument with nwf empty", Params = new object[] { "\n" })]
        //[Variation(Priority = 2, Desc = "Read XDocument with nwf no closing bracket", Params = new object[] { "<A >" })]
        //[Variation(Priority = 2, Desc = "Read XDocument with nwf no opening bracket", Params = new object[] { "A></A>" })]
        //[Variation(Priority = 2, Desc = "Read XDocument with nwf missing element", Params = new object[] { "<q=a/>" })]
        //[Variation(Priority = 2, Desc = "Read XDocument with nwf <Atruck</A>", Params = new object[] { "<Atruck</A>" })]
        public void reader_16()
        {
            var xml = Variation.Params[0] as string;
            var doc = new XDocument();

            foreach (MethodsEnum m in _methods)
            {
                using (XmlReader r = XmlReader.Create(new StringReader(xml), null))
                {
                    try
                    {
                        switch (m)
                        {
                            case MethodsEnum.Load:
                                XDocument.Load(r);
                                break;
                            case MethodsEnum.ReadFrom:
                                XNode.ReadFrom(r);
                                break;
                            case MethodsEnum.Parse:
                                XDocument.Parse(xml);
                                break;
                            default:
                                TestLog.Compare(false, "Unexpected API");
                                break;
                        }
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (Exception)
                    {
                        if (m == MethodsEnum.Load)
                        {
                            TestLog.Compare(r.ReadState, ReadState.Error, "Error in ReadState");
                        }
                        else
                        {
                            TestLog.Compare(r.ReadState, ReadState.Initial, "Error in ReadState");
                        }

                        //
                        // try it again, when reader is in error condition
                        //

                        try
                        {
                            switch (m)
                            {
                                case MethodsEnum.Load:
                                    XDocument.Load(r);
                                    break;
                                case MethodsEnum.ReadFrom:
                                    XNode.ReadFrom(r);
                                    break;
                                case MethodsEnum.Parse:
                                    XDocument.Parse(xml);
                                    break;
                                default:
                                    TestLog.Compare(false, "Unexpected API");
                                    break;
                            }
                            throw new TestException(TestResult.Failed, "");
                            ;
                        }
                        catch (InvalidOperationException)
                        {
                            if (m == MethodsEnum.Load)
                            {
                                TestLog.Compare(r.ReadState, ReadState.Error, "Error in ReadState");
                            }
                            else
                            {
                                TestLog.Compare(r.ReadState, ReadState.Initial, "Error in ReadState");
                            }
                        }
                        catch (XmlException)
                        {
                            TestLog.Compare(r.ReadState, ReadState.Initial, "Error in ReadState");
                        }
                    }
                }
            }
        }

        //[Variation(Priority = 2, Desc = "XDocument: Call Read after ReadState = Closed")]
        public void reader_19()
        {
            string xml = "<root/>";
            var doc = new XDocument();

            foreach (MethodsEnum m in _methods)
            {
                using (XmlReader r = XmlReader.Create(new StringReader(xml), null))
                {
                    while (r.Read())
                    {
                    }

                    try
                    {
                        switch (m)
                        {
                            case MethodsEnum.Parse:
                                XDocument.Parse(xml);
                                return; //Parse method does not take reader as a parameter
                            case MethodsEnum.Load:
                                XDocument.Load(r);
                                break;
                            case MethodsEnum.ReadFrom:
                                XNode.ReadFrom(r);
                                break;
                            default:
                                TestLog.Compare(false, "Unexpected API");
                                break;
                        }
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (InvalidOperationException)
                    {
                        TestLog.Compare(r.ReadState, ReadState.EndOfFile, "Error in ReadState");

                        //
                        // try it again, when reader is in error condition
                        //

                        try
                        {
                            switch (m)
                            {
                                case MethodsEnum.Parse:
                                    XDocument.Parse(xml);
                                    return; //Parse method does not take reader as a parameter
                                case MethodsEnum.Load:
                                    XDocument.Load(r);
                                    break;
                                case MethodsEnum.ReadFrom:
                                    XNode.ReadFrom(r);
                                    break;
                                default:
                                    TestLog.Compare(false, "Unexpected API");
                                    break;
                            }
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(r.ReadState, ReadState.EndOfFile, "Error in ReadState");
                        }
                    }
                }
            }
        }

        public void reader_2()
        {
            var doc = new XElement("PurchaseOrder", new XElement("Item", "Motor", new XAttribute("price", "100"), new XCData("cdata"), new XElement("elem", "inner text"), new XText("text"), new XProcessingInstruction("pi", "pi pi")));

            SaveBaseline(xml);
            LoadURI(doc, BaseSaveFileName, xml);
            LoadTextReader(doc, BaseSaveFileName, xml);
            Parse(doc, xml);
            LoadXmlReader(doc, xml);
            ReadFrom(xml);
        }

        //[Variation(Priority = 2, Desc = "XElement: Call Read after ReadState = Closed")]
        public void reader_20()
        {
            string xml = "<root/>";
            var doc = new XElement("a");

            foreach (MethodsEnum m in _methods)
            {
                using (XmlReader r = XmlReader.Create(new StringReader(xml), null))
                {
                    while (r.Read())
                    {
                    }
                    try
                    {
                        switch (m)
                        {
                            case MethodsEnum.Parse:
                                XElement.Parse(xml);
                                return; //Parse method does not take reader as a parameter
                            case MethodsEnum.Load:
                                XElement.Load(r);
                                break;
                            case MethodsEnum.ReadFrom:
                                XNode.ReadFrom(r);
                                break;
                            default:
                                TestLog.Compare(false, "Unexpected API");
                                break;
                        }
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (InvalidOperationException)
                    {
                        TestLog.Compare(r.ReadState, ReadState.EndOfFile, "Error in ReadState");

                        //
                        // try it again, when reader is in error condition
                        //

                        try
                        {
                            switch (m)
                            {
                                case MethodsEnum.Parse:
                                    XElement.Parse(xml);
                                    return; //Parse method does not take reader as a parameter
                                case MethodsEnum.Load:
                                    XElement.Load(r);
                                    break;
                                case MethodsEnum.ReadFrom:
                                    XNode.ReadFrom(r);
                                    break;
                                default:
                                    TestLog.Compare(false, "Unexpected API");
                                    break;
                            }
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (InvalidOperationException)
                        {
                            TestLog.Compare(r.ReadState, ReadState.EndOfFile, "Error in ReadState");
                        }
                    }
                }
            }
        }

        //[Variation(Priority = 1, Desc = "XDocument: Null parameters for Load")]
        public void reader_25()
        {
            try //Load from file
            {
                XDocument.Load((string)null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XDocument.Load((string)null);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }
            try
            {
                XDocument.Load((string)null, LoadOptions.PreserveWhitespace);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XDocument.Load((string)null, LoadOptions.PreserveWhitespace);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }
            try
            {
                XDocument.Load((string)null, LoadOptions.None);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XDocument.Load((string)null, LoadOptions.None);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }

            try //Load from TextReader
            {
                XDocument.Load((TextReader)null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XDocument.Load((TextReader)null);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }
            try
            {
                XDocument.Load((TextReader)null, LoadOptions.PreserveWhitespace);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XDocument.Load((TextReader)null, LoadOptions.PreserveWhitespace);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }
            try
            {
                XDocument.Load((TextReader)null, LoadOptions.None);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XDocument.Load((TextReader)null, LoadOptions.None);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }

            try //Load from XmlReader
            {
                XDocument.Load((XmlReader)null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XDocument.Load((XmlReader)null);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }
        }

        //[Variation(Priority = 1, Desc = "XElement: Null parameters for Load")]
        public void reader_26()
        {
            try //Load from file
            {
                XElement.Load((string)null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                XElement.Load((string)null, LoadOptions.PreserveWhitespace);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                XElement.Load((string)null, LoadOptions.None);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }

            try //Load from TextReader
            {
                XElement.Load((TextReader)null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                XElement.Load((TextReader)null, LoadOptions.PreserveWhitespace);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                XElement.Load((TextReader)null, LoadOptions.None);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }

            try //Load from XmlReader
            {
                XElement.Load((XmlReader)null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }
        }

        //[Variation(Priority = 1, Desc = "XDocument: Null parameters for Parse, ReadFrom and ReadContentFrom")]
        public void reader_27()
        {
            var doc = new XDocument(new XElement("PurchaseOrder"));
            try
            {
                XDocument.Parse(null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XDocument.Parse(null);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }

            try
            {
                XDocument.Parse(null, LoadOptions.PreserveWhitespace);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XDocument.Parse(null, LoadOptions.PreserveWhitespace);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }

            try
            {
                XDocument.Parse(null, LoadOptions.None);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XDocument.Parse(null, LoadOptions.None);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }

            try
            {
                XNode.ReadFrom(null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XNode.ReadFrom(null);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }

            try
            {
                XNode.ReadFrom(null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
                try
                {
                    XNode.ReadFrom(null);
                    throw new TestException(TestResult.Failed, "");
                }
                catch (ArgumentNullException)
                {
                }
            }
        }

        //[Variation(Priority = 1, Desc = "XElement: Null parameters for Parse, ReadFrom and ReadContentFrom")]
        public void reader_28()
        {
            var doc = new XElement(new XElement("PurchaseOrder"));
            try
            {
                XElement.Parse(null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                XElement.Parse(null, LoadOptions.PreserveWhitespace);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                XElement.Parse(null, LoadOptions.None);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                XNode.ReadFrom(null);
                throw new TestException(TestResult.Failed, "");
            }
            catch (ArgumentNullException)
            {
            }
        }

        #endregion

        #region Methods

        private List<XmlReader> CreateXmlReaders(string xml)
        {
            var xmlReaders = new List<XmlReader>();

            xmlReaders.Add(XmlReader.Create(new StringReader(xml)));

            var rsValidationNone = new XmlReaderSettings();
            rsValidationNone.DtdProcessing = DtdProcessing.Ignore;
            xmlReaders.Add(XmlReader.Create(new StringReader(xml), rsValidationNone));
            xmlReaders.Add(new CustomReader(new StringReader(xml), false));

            XDocument doc = XDocument.Parse(xml);
            xmlReaders.Add(doc.CreateReader());

            return xmlReaders;
        }

        private void LoadTextReader(Object doc, string uri, string expectedXml)
        {
            if (doc is XDocument)
            {
                TextReader tr1 = new StreamReader(FilePathUtil.getStream(uri));
                XDocument xdoc = XDocument.Load(tr1);
                string actualXml = xdoc.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XDocument mismatch");
                tr1.Dispose();

                TextReader tr2 = new StreamReader(FilePathUtil.getStream(uri));
                xdoc = XDocument.Load(tr2, LoadOptions.PreserveWhitespace);
                actualXml = xdoc.ToString(SaveOptions.DisableFormatting);
                ValidateXml(actualXml, expectedXml);
                tr2.Dispose();

                TextReader tr3 = new StreamReader(FilePathUtil.getStream(uri));
                xdoc = XDocument.Load(tr3, LoadOptions.None);
                actualXml = xdoc.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XDocument (preserveWhitespace = false) mismatch");
                tr3.Dispose();
            }
            else if (doc is XElement)
            {
                TextReader tr1 = new StreamReader(FilePathUtil.getStream(uri));
                XElement xelem = XElement.Load(tr1);
                string actualXml = xelem.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XElement mismatch");
                tr1.Dispose();

                TextReader tr2 = new StreamReader(FilePathUtil.getStream(uri));
                xelem = XElement.Load(tr2, LoadOptions.PreserveWhitespace);
                actualXml = xelem.ToString(SaveOptions.DisableFormatting);
                ValidateXml(actualXml, expectedXml);
                tr2.Dispose();

                TextReader tr3 = new StreamReader(FilePathUtil.getStream(uri));
                xelem = XElement.Load(tr3, LoadOptions.None);
                actualXml = xelem.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XElement (preserveWhitespace = false) mismatch");
                tr3.Dispose();
            }
            else
            {
                TestLog.Compare(false, "Wrong object");
            }
        }

        private void LoadURI(Object doc, string uri, string expectedXml)
        {
            if (doc is XDocument)
            {
                XDocument xdoc = XDocument.Load(FilePathUtil.getStream(uri));
                string actualXml = xdoc.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XDocument mismatch");

                xdoc = XDocument.Load(FilePathUtil.getStream(uri), LoadOptions.PreserveWhitespace);
                actualXml = xdoc.ToString(SaveOptions.DisableFormatting);
                ValidateXml(actualXml, expectedXml);

                xdoc = XDocument.Load(FilePathUtil.getStream(uri), LoadOptions.None);
                actualXml = xdoc.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XDocument (preserveWhitespace = false) mismatch");
            }
            else if (doc is XElement)
            {
                XElement xelem = XElement.Load(FilePathUtil.getStream(uri));
                string actualXml = xelem.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XElement mismatch");

                xelem = XElement.Load(FilePathUtil.getStream(uri), LoadOptions.PreserveWhitespace);
                actualXml = xelem.ToString(SaveOptions.DisableFormatting);
                ValidateXml(actualXml, expectedXml);

                xelem = XElement.Load(FilePathUtil.getStream(uri), LoadOptions.None);
                actualXml = xelem.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XElement (preserveWhitespace = false) mismatch");
            }
            else
            {
                TestLog.Compare(false, "Wrong object");
            }
        }

        private void LoadXmlReader(Object doc, string expectedXml)
        {
            List<XmlReader> xmlReaders = CreateXmlReaders(expectedXml);
            foreach (XmlReader xmlReader in xmlReaders)
            {
                TestLog.Compare(xmlReader.ReadState, ReadState.Initial, "Error in ReadState");
                if (doc is XDocument)
                {
                    XDocument xdoc = XDocument.Load(xmlReader);
                    string actualXml = xdoc.ToString(SaveOptions.DisableFormatting);
                    TestLog.Compare(actualXml, expectedXml, "XDocument mismatch");
                }
                else if (doc is XElement)
                {
                    XElement xelem = XElement.Load(xmlReader);
                    string actualXml = xelem.ToString(SaveOptions.DisableFormatting);
                    TestLog.Compare(actualXml, expectedXml, "XElement mismatch");
                }
                else
                {
                    TestLog.Compare(false, "Wrong object");
                }
                TestLog.Compare(xmlReader.ReadState, ReadState.EndOfFile, "Error in ReadState");
            }
        }

        private void Parse(Object doc, string expectedXml)
        {
            if (doc is XDocument)
            {
                XDocument xdoc = XDocument.Parse(expectedXml);
                string actualXml = xdoc.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XDocument mismatch");

                xdoc = XDocument.Parse(expectedXml, LoadOptions.PreserveWhitespace);
                actualXml = xdoc.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XDocument mismatch");

                xdoc = XDocument.Parse(expectedXml, LoadOptions.None);
                actualXml = xdoc.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XDocument mismatch");
            }
            else if (doc is XElement)
            {
                XElement xelem = XElement.Parse(expectedXml);
                string actualXml = xelem.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XElement mismatch");

                xelem = XElement.Parse(expectedXml, LoadOptions.PreserveWhitespace);
                actualXml = xelem.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XElement mismatch");

                xelem = XElement.Parse(expectedXml, LoadOptions.None);
                actualXml = xelem.ToString(SaveOptions.DisableFormatting);
                TestLog.Compare(actualXml, expectedXml, "XElement mismatch");
            }
            else
            {
                TestLog.Compare(false, "Wrong object");
            }
        }

        private void ReadFrom(string expectedXml)
        {
            List<XmlReader> xmlReaders = CreateXmlReaders(expectedXml);
            foreach (XmlReader xmlReader in xmlReaders)
            {
                TestLog.Compare(xmlReader.ReadState, ReadState.Initial, "Error in ReadState");
                while (xmlReader.Read())
                {
                    XNode xnode = XNode.ReadFrom(xmlReader);
                    string actualXml = xnode.ToString(SaveOptions.DisableFormatting);
                    TestLog.Compare(actualXml, expectedXml, "XNode mismatch");
                    TestLog.Compare(xmlReader.ReadState, ReadState.EndOfFile, "Error in ReadState");
                }
                while (xmlReader.Read())
                {
                    var xdoc = (XDocument)XNode.ReadFrom(xmlReader);
                    string actualXml = xdoc.ToString(SaveOptions.DisableFormatting);
                    TestLog.Compare(actualXml, expectedXml, "XDocument mismatch");
                    TestLog.Compare(xmlReader.ReadState, ReadState.EndOfFile, "Error in ReadState");
                }
                while (xmlReader.Read())
                {
                    var xelem = (XElement)XNode.ReadFrom(xmlReader);
                    string actualXml = xelem.ToString(SaveOptions.DisableFormatting);
                    TestLog.Compare(actualXml, expectedXml, "XElement mismatch");
                    TestLog.Compare(xmlReader.ReadState, ReadState.EndOfFile, "Error in ReadState");
                }
                TestLog.Compare(xmlReader.ReadState, ReadState.EndOfFile, "Error in ReadState");
            }
        }

        private void SaveBaseline(string xml)
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            sw.Write(xml);
            sw.Flush();
            FilePathUtil.addStream(BaseSaveFileName, ms);
        }

        private void ValidateXml(string actualXml, string expectedXml)
        {
            ValidateXml(actualXml, expectedXml, ConformanceLevel.Document);
        }

        private void ValidateXml(string actualXml, string expectedXml, ConformanceLevel conformanceLevel)
        {
            var rs = new XmlReaderSettings();
            rs.ConformanceLevel = conformanceLevel;

            switch (conformanceLevel)
            {
                case ConformanceLevel.Fragment:
                case ConformanceLevel.Document:
                    using (XmlReader r1 = XmlReader.Create(new StringReader(expectedXml), rs))
                    using (XmlReader r2 = XmlReader.Create(new StringReader(actualXml), rs))
                    {
                        TestLog.Compare(_diff.Compare(r1, r2), "Mismatch");
                    }
                    break;
                default:
                    TestLog.Compare(actualXml, expectedXml, "Mismatch");
                    break;
            }
        }
        #endregion
    }
}
