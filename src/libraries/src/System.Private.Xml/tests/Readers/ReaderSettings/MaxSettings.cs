// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    [TestCase(Name = "MaxCharacters Settings", Desc = "MaxCharacters Settings")]
    public partial class TCMaxSettings : TCXMLReaderBaseGeneral
    {
        private long _defaultCharsEnt = (long)1e7;  // By default, entity resolving is limited to 10 million characters (On full .NET the default used to be zero (=unlimited) as LegacyXmlSettings was enabled)
        private long _defaultCharsDoc = 0;
        private long _maxVal = long.MaxValue;
        private long _bigVal = 100000;
        private string _path = Path.Combine(FilePathUtil.GetStandardPath(), @"xml10\entityexpansion\");
        private string _path2 = Path.Combine(FilePathUtil.GetStandardPath(), @"XML10\ms_xml\");

        private string[] _skipFiles =
        {
            "billionlaughs",
            "extent_4.xml",
            "surrogate_parameterentities_2.xml"
        };

        private bool IsSkipped(string uri)
        {
            if (!uri.EndsWith(".xml"))
            {
                return true;
            }
            for (int i = 0; i < _skipFiles.Length; i++)
            {
                if (uri.Contains(_skipFiles[i]))
                    return true;
            }
            return false;
        }

        [Variation(Pri = 0, Desc = "MaxSettings: get default values")]
        public int v1()
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            CError.Compare(rs.MaxCharactersFromEntities, _defaultCharsEnt, "Error");
            CError.Compare(rs.MaxCharactersInDocument, _defaultCharsDoc, "Error");

            using (XmlReader r = ReaderHelper.Create(new StringReader("<foo/>")))
            {
                CError.Compare(r.Settings.MaxCharactersFromEntities, _defaultCharsEnt, "Error");
                CError.Compare(r.Settings.MaxCharactersInDocument, _defaultCharsDoc, "Error");
            }
            return TEST_PASS;
        }

        [Variation(Pri = 0, Desc = "MaxSettings: set, get values")]
        public int v2()
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.MaxCharactersFromEntities = 1;
            rs.MaxCharactersInDocument = 1;
            CError.Compare((int)rs.MaxCharactersFromEntities, 1, "Error");
            CError.Compare((int)rs.MaxCharactersInDocument, 1, "Error");
            return TEST_PASS;
        }

        [Variation(Pri = 2, Desc = "MaxSettings: set negative, get default values, set 10 and get 10")]
        public int v3()
        {
            string xml = "<a/>";
            XmlReaderSettings rs = new XmlReaderSettings();
            try
            {
                rs.MaxCharactersFromEntities = -1;
                return TEST_FAIL;
            }
            catch (ArgumentOutOfRangeException) { }
            try
            {
                rs.MaxCharactersInDocument = -1;
                return TEST_FAIL;
            }
            catch (ArgumentOutOfRangeException) { }
            CError.Compare(rs.MaxCharactersFromEntities, _defaultCharsEnt, "Error");
            CError.Compare(rs.MaxCharactersInDocument, _defaultCharsDoc, "Error");

            rs.MaxCharactersFromEntities = 10;
            rs.MaxCharactersInDocument = 10;
            using (XmlReader r = ReaderHelper.Create(new StringReader(xml), rs))
            {
                while (r.Read()) ;
                CError.Compare((int)r.Settings.MaxCharactersFromEntities, 10, "Error");
                CError.Compare((int)r.Settings.MaxCharactersInDocument, 10, "Error");
            }
            return TEST_PASS;
        }

        //[Variation(Pri = 2, Desc = "MaxCharactersFromEntities: set negative values, get default", Param = 1)]
        //[Variation(Pri = 2, Desc = "MaxCharactersFromEntities: set max negative values, get default", Param = 2)]
        public int v9()
        {
            long param = -1;
            if ((int)CurVariation.Param == 2)
            {
                param = -_maxVal;
            }

            XmlReaderSettings rs = new XmlReaderSettings();
            try
            {
                rs.MaxCharactersFromEntities = param;
            }
            catch (ArgumentOutOfRangeException)
            {
                try
                {
                    rs.MaxCharactersFromEntities = param;
                }
                catch (ArgumentOutOfRangeException)
                {
                    CError.Compare(rs.MaxCharactersFromEntities, _defaultCharsEnt, "Error");
                    CError.Compare(rs.MaxCharactersInDocument, _defaultCharsDoc, "Error");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        //[Variation(Pri = 2, Desc = "MaxCharactersInDocument: set negative values, get default", Param = 1)]
        //[Variation(Pri = 2, Desc = "MaxCharactersInDocument: set huge negative values, get default", Param = 2)]
        public int v10()
        {
            long param = -1;
            if ((int)CurVariation.Param == 2)
            {
                param = -_maxVal;
            }

            XmlReaderSettings rs = new XmlReaderSettings();
            try
            {
                rs.MaxCharactersInDocument = param;
            }
            catch (ArgumentOutOfRangeException)
            {
                try
                {
                    rs.MaxCharactersInDocument = param;
                }
                catch (ArgumentOutOfRangeException)
                {
                    CError.Compare(rs.MaxCharactersFromEntities, _defaultCharsEnt, "Error");
                    CError.Compare(rs.MaxCharactersInDocument, _defaultCharsDoc, "Error");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<A />"})]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<A>truck</A>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<Root><!--\uD812\uDD12--></Root>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<X>t0<A>truck</A>t00</X>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc' /><B /></a:A>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S /></p:C><B /></A>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns='def' xmlns:p='ns' p:a3='pa3' />" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<!DOCTYPE doc [  <!ELEMENT doc ANY>  <!ENTITY book 'a&a'>]><doc>&book;</doc>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<!DOCTYPE doc [  <!ELEMENT doc ANY>  <!ENTITY book 'a%a'>]><doc>&book;</doc>  " })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<?xml version='1.0'?><!DOCTYPE test [ <!ELEMENT test ANY> <!ELEMENT a ANY> <!ELEMENT b ANY> <!ELEMENT c ANY> <!ENTITY a '<a>&a;</a>'>]><test>&a;</test>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<!DOCTYPE ROOT [<!ENTITY a '&a;'>]><ROOT att='&a;'/>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<!DOCTYPE root [<!ELEMENT root ANY><!ENTITY e '\uD812\uDD12'>]><root>&e;</root>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<!DOCTYPE root[<!ENTITY x 'somevalue'><!ELEMENT root ANY>]><root>value&amp;x;</root>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<root>va&lt;/root&gt;lue</root>" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<Root a='&amp;' b='&amp;#65;' c='&amp;#x43;' />" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<Root a='b&quot;c' />" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<Root a='                                      &#x9;&#xD;&#xA;' />" })]
        //[Variation(Pri = 2, Desc = "MaxEnt = 1, MaxDoc = length - 1", Params = new object[] { "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>" })]
        public int v20()
        {
            string xml = CurVariation.Params[0] as string;
            int val = xml.Length - 1;

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.MaxCharactersFromEntities = 1;
            rs.MaxCharactersInDocument = val;
            try
            {
                XmlReader reader = ReaderHelper.Create(new StringReader(xml), rs);
                if (AsyncUtil.IsAsyncEnabled)
                    reader.Read();
            }
            catch (XmlException)
            {
                CError.Compare((int)rs.MaxCharactersFromEntities, 1, "Error");
                CError.Compare((int)rs.MaxCharactersInDocument, val, "Error");
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<A />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<A>truck</A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<Root><!--\uD812\uDD12--></Root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<X>t0<A>truck</A>t00</X>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc' /><B /></a:A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S /></p:C><B /></A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns='def' xmlns:p='ns' p:a3='pa3' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<!DOCTYPE root[<!ENTITY x 'somevalue'><!ELEMENT root ANY>]><root>value&amp;x;</root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<root>va&lt;/root&gt;lue</root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<Root a='&amp;' b='&amp;#65;' c='&amp;#x43;' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<Root a='b&quot;c' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<Root a='                                      &#x9;&#xD;&#xA;' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc = length", Params = new object[] { "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>" })]
        public int v30()
        {
            string xml = CurVariation.Params[0] as string;
            int val = xml.Length;

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.MaxCharactersInDocument = val;
            rs.DtdProcessing = DtdProcessing.Ignore;
            using (XmlReader reader = ReaderHelper.Create(new StringReader(xml), rs))
            {
                while (reader.Read()) ;
                CError.Compare(reader.Settings.MaxCharactersFromEntities, _defaultCharsEnt, "Error");
                CError.Compare((int)reader.Settings.MaxCharactersInDocument, val, "Error");
            }
            return TEST_PASS;
        }

        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<A />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<A>truck</A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<Root><!--\uD812\uDD12--></Root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<X>t0<A>truck</A>t00</X>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc' /><B /></a:A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S /></p:C><B /></A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns='def' xmlns:p='ns' p:a3='pa3' />" })]      
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<!DOCTYPE root[<!ENTITY x 'somevalue'><!ELEMENT root ANY>]><root>value&amp;x;</root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<root>va&lt;/root&gt;lue</root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<Root a='&amp;' b='&amp;#65;' c='&amp;#x43;' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<Root a='b&quot;c' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<Root a='                                      &#x9;&#xD;&#xA;' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 1, MaxDoc = length", Params = new object[] { "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>" })]
        public int v40()
        {
            string xml = CurVariation.Params[0] as string;
            int val = xml.Length;

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.MaxCharactersFromEntities = 1;
            rs.MaxCharactersInDocument = val;
            rs.DtdProcessing = DtdProcessing.Ignore;

            using (XmlReader reader = ReaderHelper.Create(new StringReader(xml), rs))
            {
                while (reader.Read()) ;
                CError.Compare((int)reader.Settings.MaxCharactersFromEntities, 1, "Error");
                CError.Compare((int)reader.Settings.MaxCharactersInDocument, val, "Error");
            }
            return TEST_PASS;
        }

        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<A />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<A>truck</A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<Root><!--\uD812\uDD12--></Root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<X>t0<A>truck</A>t00</X>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc' /><B /></a:A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S /></p:C><B /></A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns='def' xmlns:p='ns' p:a3='pa3' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<!DOCTYPE root[<!ENTITY x 'somevalue'><!ELEMENT root ANY>]><root>value&amp;x;</root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<root>va&lt;/root&gt;lue</root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<Root a='&amp;' b='&amp;#65;' c='&amp;#x43;' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<Root a='b&quot;c' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<Root a='                                      &#x9;&#xD;&#xA;' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt = 0, MaxDoc = 0", Params = new object[] { "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>" })]
        public int v50()
        {
            string xml = CurVariation.Params[0] as string;

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.MaxCharactersFromEntities = 0;
            rs.MaxCharactersInDocument = 0;
            rs.DtdProcessing = DtdProcessing.Ignore;

            using (XmlReader reader = ReaderHelper.Create(new StringReader(xml), rs))
            {
                while (reader.Read()) ;
                CError.Compare((int)reader.Settings.MaxCharactersFromEntities, 0, "Error");
                CError.Compare((int)reader.Settings.MaxCharactersInDocument, 0, "Error");
            }
            return TEST_PASS;
        }

        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<A />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<A>truck</A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<Root><!--\uD812\uDD12--></Root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<X>t0<A>truck</A>t00</X>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc' /><B /></a:A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S /></p:C><B /></A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns='def' xmlns:p='ns' p:a3='pa3' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<!DOCTYPE root[<!ENTITY x 'somevalue'><!ELEMENT root ANY>]><root>value&amp;x;</root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<root>va&lt;/root&gt;lue</root>" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<Root a='&amp;' b='&amp;#65;' c='&amp;#x43;' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<Root a='b&quot;c' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<Root a='                                      &#x9;&#xD;&#xA;' />" })]
        //[Variation(Pri = 0, Desc = "MaxEnt not set, MaxDoc not set", Params = new object[] { "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>" })]
        public int v60()
        {
            string xml = CurVariation.Params[0] as string;
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = DtdProcessing.Ignore;

            using (XmlReader reader = ReaderHelper.Create(new StringReader(xml), rs))
            {
                while (reader.Read()) ;
                CError.Compare((long)reader.Settings.MaxCharactersFromEntities, _defaultCharsEnt, "Error");
                CError.Compare((long)reader.Settings.MaxCharactersInDocument, _defaultCharsDoc, "Error");
            }
            return TEST_PASS;
        }

        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = bigVal, MaxDoc = bigVal", Params = new object[] { "<!DOCTYPE ROOT [<!ENTITY a '&a;'>]><ROOT att='&a;'/>" })]
        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = bigVal, MaxDoc = bigVal", Params = new object[] { "<?xml version='1.0'?><!DOCTYPE test [ <!ELEMENT test ANY> <!ELEMENT a ANY> <!ELEMENT b ANY> <!ELEMENT c ANY> <!ENTITY a '<a>&a;</a>'>]><test>&a;</test>" })]
        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = bigVal, MaxDoc = bigVal", Params = new object[] { "" })]
        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = bigVal, MaxDoc = bigVal", Params = new object[] { "<?xml version='1.0'?><!DOCTYPE test [ <!ELEMENT test ANY> <!ELEMENT a ANY> <!ELEMENT b ANY> <!ELEMENT c ANY> <!ENTITY a '<a>&amp;</a>'>]><test>&a;<test>" })]
        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = bigVal, MaxDoc = bigVal", Params = new object[] { "<q = 'a'/>" })]
        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = bigVal, MaxDoc = bigVal", Params = new object[] { string.Format("<!-- http://www.w3.org is bound to n1 and n2 -->{0}<x xmlns:n1=\"http://www.w3.org\"{0}   xmlns:n2=\"http://www.w3.org\" >{0}   <bad n1:a=\"1\"  n2:a=\"2\" /></x>", Environment.NewLine) })]
        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = bigVal, MaxDoc = bigVal", Params = new object[] { "<root><!--comment \uD812><!--comment \uD812>-->--></root>" })]
        public int v260()
        {
            string xml = CurVariation.Params[0] as string;

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = DtdProcessing.Ignore;
            rs.MaxCharactersFromEntities = _bigVal;
            rs.MaxCharactersInDocument = _bigVal;
            using (XmlReader reader = ReaderHelper.Create(new StringReader(xml), rs))
            {
                try
                {
                    while (reader.Read()) ;
                }
                catch (XmlException)
                {
                    CError.Compare(reader.Settings.MaxCharactersFromEntities, _bigVal, "Error");
                    CError.Compare(reader.Settings.MaxCharactersInDocument, _bigVal, "Error");
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = 1, MaxDoc = val", Params = new object[] { "<!DOCTYPE ROOT [<!ENTITY a '&a;'>]><ROOT att='&a;'/>", 10 })]
        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = 1, MaxDoc = val", Params = new object[] { "<?xml version='1.0'?><!DOCTYPE test [ <!ELEMENT test ANY> <!ELEMENT a ANY> <!ELEMENT b ANY> <!ELEMENT c ANY> <!ENTITY a '<a>&a;</a>'>]><test>&a;</test>", 25 })]     
        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = 1, MaxDoc = val", Params = new object[] { "<?xml version='1.0'?><!DOCTYPE test [ <!ELEMENT test ANY> <!ELEMENT a ANY> <!ELEMENT b ANY> <!ELEMENT c ANY> <!ENTITY a '<a>&amp;</a>'>]><test>&a;<test>", 26 })]
        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = 1, MaxDoc = val", Params = new object[] { "<q = 'a'/>", 5 })]
        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = 1, MaxDoc = val", Params = new object[] { string.Format("<!-- http://www.w3.org is bound to n1 and n2 -->{0}<x xmlns:n1=\"http://www.w3.org\"{0}   xmlns:n2=\"http://www.w3.org\" >{0}   <bad n1:a=\"1\"  n2:a=\"2\" /></x>", Environment.NewLine), 35 })]
        //[Variation(Pri = 2, Desc = "nwf xml: MaxEnt = 1, MaxDoc = val", Params = new object[] { "<root><!--comment \uD812><!--comment \uD812>-->--></root>", 18 })]
        public int v270()
        {
            string xml = CurVariation.Params[0] as string;
            int val = (int)CurVariation.Params[1];

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.MaxCharactersFromEntities = 1;
            rs.MaxCharactersInDocument = val;
            try
            {
                using (XmlReader reader = ReaderHelper.Create(new StringReader(xml), rs))
                {
                    while (reader.Read()) ;
                }
            }
            catch (XmlException)
            {
                CError.Compare((int)rs.MaxCharactersFromEntities, 1, "Error");
                CError.Compare((int)rs.MaxCharactersInDocument, val, "Error");
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        private string _xml = @"<!DOCTYPE r [<!ENTITY e SYSTEM '" + Path.Combine(FilePathUtil.GetTestDataPath(), "XmlReader", "ent.ent") + @"'>]><r>&e;</r>";
    }
}


internal class OneByteStream : Stream
{
    private Stream _inner;

    public OneByteStream(string fileName)
    {
        _inner = FilePathUtil.getStream(fileName);
    }

    public override bool CanRead
    {
        get { return true; }
    }

    public override bool CanSeek
    {
        get { throw new Exception("The method or operation is not implemented."); }
    }

    public override bool CanWrite
    {
        get { throw new Exception("The method or operation is not implemented."); }
    }

    public override void Flush()
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public override long Length
    {
        get { throw new Exception("The method or operation is not implemented."); }
    }

    public override long Position
    {
        get
        {
            throw new Exception("The method or operation is not implemented.");
        }
        set
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _inner.Read(buffer, offset, 1);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public override void SetLength(long value)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new Exception("The method or operation is not implemented.");
    }
}
