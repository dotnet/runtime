// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCMaxSettings : TCXMLReaderBaseGeneral
    {
        // Type is XmlReaderTest.Common.TCMaxSettings
        // Test Case
        public override void AddChildren()
        {
            // for function v1
            {
                this.AddChild(new CVariation(v1) { Attribute = new Variation("MaxSettings: get default values") { Pri = 0 } });
            }


            // for function v2
            {
                this.AddChild(new CVariation(v2) { Attribute = new Variation("MaxSettings: set, get values") { Pri = 0 } });
            }


            // for function v3
            {
                this.AddChild(new CVariation(v3) { Attribute = new Variation("MaxSettings: set negative, get default values, set 10 and get 10") { Pri = 2 } });
            }


            // for function v9
            {
                this.AddChild(new CVariation(v9) { Attribute = new Variation("MaxCharactersFromEntities: set negative values, get default") { Param = 1, Pri = 2 } });
                this.AddChild(new CVariation(v9) { Attribute = new Variation("MaxCharactersFromEntities: set max negative values, get default") { Param = 2, Pri = 2 } });
            }


            // for function v10
            {
                this.AddChild(new CVariation(v10) { Attribute = new Variation("MaxCharactersInDocument: set negative values, get default") { Param = 1, Pri = 2 } });
                this.AddChild(new CVariation(v10) { Attribute = new Variation("MaxCharactersInDocument: set huge negative values, get default") { Param = 2, Pri = 2 } });
            }


            // for function v20
            {
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<A>truck</A>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<Root><!--\ud812\udd12--></Root>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<X>t0<A>truck</A>t00</X>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc' /><B /></a:A>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S /></p:C><B /></A>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns='def' xmlns:p='ns' p:a3='pa3' />" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<!DOCTYPE doc [  <!ELEMENT doc ANY>  <!ENTITY book 'a&a'>]><doc>&book;</doc>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<!DOCTYPE doc [  <!ELEMENT doc ANY>  <!ENTITY book 'a%a'>]><doc>&book;</doc>  " }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<?xml version='1.0'?><!DOCTYPE test [ <!ELEMENT test ANY> <!ELEMENT a ANY> <!ELEMENT b ANY> <!ELEMENT c ANY> <!ENTITY a '<a>&a;</a>'>]><test>&a;</test>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<!DOCTYPE ROOT [<!ENTITY a '&a;'>]><ROOT att='&a;'/>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<!DOCTYPE root [<!ELEMENT root ANY><!ENTITY e '\ud812\udd12'>]><root>&e;</root>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<!DOCTYPE root[<!ENTITY x 'somevalue'><!ELEMENT root ANY>]><root>value&amp;x;</root>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<root>va&lt;/root&gt;lue</root>" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<Root a='&amp;' b='&amp;#65;' c='&amp;#x43;' />" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<Root a='b&quot;c' />" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<A />" }, Pri = 2 } });
                this.AddChild(new CVariation(v20) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length - 1") { Params = new object[] { "<Root a='                                      &#x9;&#xD;&#xA;' />" }, Pri = 2 } });
            }


            // for function v30
            {
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<Root a='b&quot;c' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<root>va&lt;/root&gt;lue</root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<Root a='                                      &#x9;&#xD;&#xA;' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<Root a='&amp;' b='&amp;#65;' c='&amp;#x43;' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<A />" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<A>truck</A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<Root><!--\ud812\udd12--></Root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<X>t0<A>truck</A>t00</X>" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc' /><B /></a:A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S /></p:C><B /></A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns='def' xmlns:p='ns' p:a3='pa3' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v30) { Attribute = new Variation("MaxEnt not set, MaxDoc = length") { Params = new object[] { "<!DOCTYPE root[<!ENTITY x 'somevalue'><!ELEMENT root ANY>]><root>value&amp;x;</root>" }, Pri = 0 } });
            }


            // for function v40
            {
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<Root a='b&quot;c' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<Root a='                                      &#x9;&#xD;&#xA;' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<X>t0<A>truck</A>t00</X>" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<A>truck</A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<Root><!--\ud812\udd12--></Root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc' /><B /></a:A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S /></p:C><B /></A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns='def' xmlns:p='ns' p:a3='pa3' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<!DOCTYPE root[<!ENTITY x 'somevalue'><!ELEMENT root ANY>]><root>value&amp;x;</root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<root>va&lt;/root&gt;lue</root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<Root a='&amp;' b='&amp;#65;' c='&amp;#x43;' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v40) { Attribute = new Variation("MaxEnt = 1, MaxDoc = length") { Params = new object[] { "<A />" }, Pri = 0 } });
            }


            // for function v50
            {
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<Root><!--\ud812\udd12--></Root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<A />" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<A>truck</A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<X>t0<A>truck</A>t00</X>" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc' /><B /></a:A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S /></p:C><B /></A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns='def' xmlns:p='ns' p:a3='pa3' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<!DOCTYPE root[<!ENTITY x 'somevalue'><!ELEMENT root ANY>]><root>value&amp;x;</root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<root>va&lt;/root&gt;lue</root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<Root a='&amp;' b='&amp;#65;' c='&amp;#x43;' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<Root a='b&quot;c' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v50) { Attribute = new Variation("MaxEnt = 0, MaxDoc = 0") { Params = new object[] { "<Root a='                                      &#x9;&#xD;&#xA;' />" }, Pri = 0 } });
            }


            // for function v60
            {
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<root>va&lt;/root&gt;lue</root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<A attr='1' a2='a2' a3='a3' xmlns='def' xmlns:p='ns' p:a3='pa3' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<A />" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<A>truck</A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<Root><!--\ud812\udd12--></Root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<Root>&#x58341;<Elem>&#xCFE44;</Elem>&#x18022;</Root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<!DOCTYPE root[<!ENTITY x 'somevalue'><!ELEMENT root ANY>]><root>value&amp;x;</root>" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<a:A xmlns:a='a'><C xmlns:p='nsc' /><B /></a:A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<A xmlns:p='nsc'><p:C xmlns:a='a'><a:S /></p:C><B /></A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<A xmlns='ns0'><![CDATA[tralala]]></A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<Root a='&amp;' b='&amp;#65;' c='&amp;#x43;' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<A xmlns='ns0'><![CDATA[ja_a_hele]]><?PI?><X />text<Y /></A>" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<Root a='b&quot;c' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<Root a='                                      &#x9;&#xD;&#xA;' />" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<Root foo='&amp; &lt; &gt; &quot; &apos; &#65;'/>" }, Pri = 0 } });
                this.AddChild(new CVariation(v60) { Attribute = new Variation("MaxEnt not set, MaxDoc not set") { Params = new object[] { "<X>t0<A>truck</A>t00</X>" }, Pri = 0 } });
            }

            // for function v260
            {
                this.AddChild(new CVariation(v260) { Attribute = new Variation("nwf xml: MaxEnt = bigVal, MaxDoc = bigVal") { Params = new object[] { "" }, Pri = 2 } });
                this.AddChild(new CVariation(v260) { Attribute = new Variation("nwf xml: MaxEnt = bigVal, MaxDoc = bigVal") { Params = new object[] { "<root><!--comment \ufffd\ufffd><!--comment \ufffd\ufffd>-->--></root>" }, Pri = 2 } });
                this.AddChild(new CVariation(v260) { Attribute = new Variation("nwf xml: MaxEnt = bigVal, MaxDoc = bigVal") { Params = new object[] { string.Format("<!-- http://www.w3.org is bound to n1 and n2 -->{0}<x xmlns:n1=\"http://www.w3.org\"{0}   xmlns:n2=\"http://www.w3.org\" >{0}   <bad n1:a=\"1\"  n2:a=\"2\" /></x>", Environment.NewLine) }, Pri = 2 } });
                this.AddChild(new CVariation(v260) { Attribute = new Variation("nwf xml: MaxEnt = bigVal, MaxDoc = bigVal") { Params = new object[] { "<!DOCTYPE ROOT [<!ENTITY a '&a;'>]><ROOT att='&a;'/>" }, Pri = 2 } });
                this.AddChild(new CVariation(v260) { Attribute = new Variation("nwf xml: MaxEnt = bigVal, MaxDoc = bigVal") { Params = new object[] { "<?xml version='1.0'?><!DOCTYPE test [ <!ELEMENT test ANY> <!ELEMENT a ANY> <!ELEMENT b ANY> <!ELEMENT c ANY> <!ENTITY a '<a>&a;</a>'>]><test>&a;</test>" }, Pri = 2 } });
                this.AddChild(new CVariation(v260) { Attribute = new Variation("nwf xml: MaxEnt = bigVal, MaxDoc = bigVal") { Params = new object[] { "<?xml version='1.0'?><!DOCTYPE test [ <!ELEMENT test ANY> <!ELEMENT a ANY> <!ELEMENT b ANY> <!ELEMENT c ANY> <!ENTITY a '<a>&amp;</a>'>]><test>&a;<test>" }, Pri = 2 } });
                this.AddChild(new CVariation(v260) { Attribute = new Variation("nwf xml: MaxEnt = bigVal, MaxDoc = bigVal") { Params = new object[] { "<q = 'a'/>" }, Pri = 2 } });
            }


            // for function v270
            {
                this.AddChild(new CVariation(v270) { Attribute = new Variation("nwf xml: MaxEnt = 1, MaxDoc = val") { Params = new object[] { "<!DOCTYPE ROOT [<!ENTITY a '&a;'>]><ROOT att='&a;'/>", 10 }, Pri = 2 } });
                this.AddChild(new CVariation(v270) { Attribute = new Variation("nwf xml: MaxEnt = 1, MaxDoc = val") { Params = new object[] { "<q = 'a'/>", 5 }, Pri = 2 } });
                this.AddChild(new CVariation(v270) { Attribute = new Variation("nwf xml: MaxEnt = 1, MaxDoc = val") { Params = new object[] { "<?xml version='1.0'?><!DOCTYPE test [ <!ELEMENT test ANY> <!ELEMENT a ANY> <!ELEMENT b ANY> <!ELEMENT c ANY> <!ENTITY a '<a>&a;</a>'>]><test>&a;</test>", 25 }, Pri = 2 } });
                this.AddChild(new CVariation(v270) { Attribute = new Variation("nwf xml: MaxEnt = 1, MaxDoc = val") { Params = new object[] { "<?xml version='1.0'?><!DOCTYPE test [ <!ELEMENT test ANY> <!ELEMENT a ANY> <!ELEMENT b ANY> <!ELEMENT c ANY> <!ENTITY a '<a>&amp;</a>'>]><test>&a;<test>", 26 }, Pri = 2 } });
                this.AddChild(new CVariation(v270) { Attribute = new Variation("nwf xml: MaxEnt = 1, MaxDoc = val") { Params = new object[] { "<root><!--comment \ufffd\ufffd><!--comment \ufffd\ufffd>-->--></root>", 18 }, Pri = 2 } });
                this.AddChild(new CVariation(v270) { Attribute = new Variation("nwf xml: MaxEnt = 1, MaxDoc = val") { Params = new object[] { string.Format("<!-- http://www.w3.org is bound to n1 and n2 -->{0}<x xmlns:n1=\"http://www.w3.org\"{0}   xmlns:n2=\"http://www.w3.org\" >{0}   <bad n1:a=\"1\"  n2:a=\"2\" /></x>", Environment.NewLine), 35 }, Pri = 2 } });
            }
        }
    }
}
