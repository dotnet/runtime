// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using Xunit;

namespace System.Xml.XmlReaderTests
{
    public class ReadCharsTests
    {
        [Fact]
        public static void ReadCharsInLoop()
        {
            string xml = GenerateTestXml(out string expected);

            // Load the reader with the xml.  Ignore white space.
            using (XmlTextReader reader = new XmlTextReader(new StringReader(xml)))
            {
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.DtdProcessing = DtdProcessing.Ignore;

                // Set variables used by ReadChars.
                int charbuffersize = 10;
                char[] buffer = new char[charbuffersize];

                // Parse the xml.  Read the element content
                // using the ReadChars method.
                reader.MoveToContent();
                StringBuilder sb = new StringBuilder();
                int iCnt;
                while ((iCnt = reader.ReadChars(buffer, 0, charbuffersize)) > 0)
                {
                    // Append the buffer contents.
                    sb.Append(buffer, 0, iCnt);
                    // Clear the buffer.
                    Array.Clear(buffer, 0, charbuffersize);
                }

                Assert.Equal(expected, sb.ToString());
            }
        }

        private static string GenerateTestXml(out string expected)
        {
            StringBuilder fullXml = new StringBuilder();
            StringBuilder expectedXml = new StringBuilder();

            fullXml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
            fullXml.AppendLine("<!-- comment1 -->");
            fullXml.AppendLine("<?PI1_First processing instruction?>");
            fullXml.AppendLine("<?PI1a?>");
            fullXml.AppendLine("<?PI1b?>");
            fullXml.AppendLine("<?PI1c?>");
            fullXml.AppendLine("<!DOCTYPE root SYSTEM \"AllNodeTypes.dtd\" [");
            fullXml.AppendLine("<!NOTATION gif SYSTEM \"foo.exe\">");
            fullXml.AppendLine("<!ELEMENT root ANY>");
            fullXml.AppendLine("<!ELEMENT elem1 ANY>");
            fullXml.AppendLine("<!ELEMENT ISDEFAULT ANY>");
            fullXml.AppendLine("<!ENTITY % e SYSTEM \"AllNodeTypes.ent\">");
            fullXml.AppendLine("%e;");
            fullXml.AppendLine("<!ENTITY e1 \"e1foo\">");
            fullXml.AppendLine("<!ENTITY e2 \"&ext3; e2bar\">");
            fullXml.AppendLine("<!ENTITY e3 \"&e1; e3bzee \">");
            fullXml.AppendLine("<!ENTITY e4 \"&e3; e4gee\">");
            fullXml.AppendLine("<!ATTLIST elem1 child1 CDATA #IMPLIED child2 CDATA \"&e2;\" child3 CDATA #REQUIRED>");
            fullXml.AppendLine("<!ATTLIST root xmlns:something CDATA #FIXED \"something\" xmlns:my CDATA #FIXED \"my\" xmlns:dt CDATA #FIXED \"urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/\">");
            fullXml.AppendLine("<!ATTLIST ISDEFAULT d1 CDATA #FIXED \"d1value\">");

            fullXml.AppendLine("<!ATTLIST MULTISPACES att IDREFS #IMPLIED>");
            fullXml.AppendLine("<!ELEMENT CATMIXED (#PCDATA)>");

            fullXml.AppendLine("]>");

            //add starting tag
            fullXml.AppendLine("<PLAY>");

            expectedXml.AppendLine("<root xmlns:something=\"something\" xmlns:my=\"my\" xmlns:dt=\"urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/\">");
            expectedXml.AppendLine("<elem1 child1=\"\" child2=\"e1foo e3bzee  e2bar\" child3=\"something\">");
            expectedXml.AppendLine("text node two e1foo text node three");
            expectedXml.AppendLine("</elem1>");
            expectedXml.AppendLine("e1foo e3bzee  e2bar");
            expectedXml.AppendLine("<![CDATA[ This section contains characters that should not be interpreted as markup. For example, characters ', \",");
            expectedXml.AppendLine("<, >, and & are all fine here.]]>");
            expectedXml.AppendLine("<elem2 att1=\"id1\" att2=\"up\" att3=\"attribute3\"> ");
            expectedXml.AppendLine("<a />");
            expectedXml.AppendLine("</elem2>");
            expectedXml.AppendLine("<elem2> ");
            expectedXml.AppendLine("elem2-text1");
            expectedXml.AppendLine("<a refs=\"id2\"> ");
            expectedXml.AppendLine("this-is-a    ");
            expectedXml.AppendLine("</a> ");
            expectedXml.AppendLine("elem2-text2");
            expectedXml.AppendLine("e1foo e3bzee ");
            expectedXml.AppendLine("e1foo e3bzee  e4gee");
            expectedXml.AppendLine("<!-- elem2-comment1-->");
            expectedXml.AppendLine("elem2-text3");
            expectedXml.AppendLine("<b> ");
            expectedXml.AppendLine("this-is-b");
            expectedXml.AppendLine("</b>");
            expectedXml.AppendLine("elem2-text4");
            expectedXml.AppendLine("<?elem2_PI elem2-PI?>");
            expectedXml.AppendLine("elem2-text5");
            expectedXml.AppendLine("</elem2>");
            expectedXml.AppendLine("<elem2 att1=\"id2\"></elem2>");
            expectedXml.AppendLine("</root>");
            expectedXml.Append("<ENTITY1 att1='xxx&lt;xxx&#65;xxx&#x43;xxxe1fooxxx'>xxx&gt;xxx&#66;xxx&#x44;xxxe1fooxxx</ENTITY1>");
            expectedXml.AppendLine("<ENTITY2 att1='xxx&lt;xxx&#65;xxx&#x43;xxxe1fooxxx'>xxx&gt;xxx&#66;xxx&#x44;xxxe1fooxxx</ENTITY2>");
            expectedXml.AppendLine("<ENTITY3 att1='xxx&lt;xxx&#65;xxx&#x43;xxxe1fooxxx'>xxx&gt;xxx&#66;xxx&#x44;xxxe1fooxxx</ENTITY3>");
            expectedXml.AppendLine("<ENTITY4 att1='xxx&lt;xxx&#65;xxx&#x43;xxxe1fooxxx'>xxx&gt;xxx&#66;xxx&#x44;xxxe1fooxxx</ENTITY4>");
            expectedXml.AppendLine("<ENTITY5>e1foo e3bzee </ENTITY5>");
            expectedXml.AppendLine("<ATTRIBUTE1 />");
            expectedXml.AppendLine("<ATTRIBUTE2 a1='a1value' />");
            expectedXml.AppendLine("<ATTRIBUTE3 a1='a1value' a2='a2value' a3='a3value' />");
            expectedXml.AppendLine("<ATTRIBUTE4 a1='' />");
            expectedXml.AppendLine(string.Format("<ATTRIBUTE5 CRLF='x{0}x' CR='x{0}x' LF='x\nx' MS='x     x' TAB='x\tx' />", Environment.NewLine));
            expectedXml.AppendLine(string.Format("<ENDOFLINE1>x{0}x</ENDOFLINE1>", Environment.NewLine));
            expectedXml.AppendLine(string.Format("<ENDOFLINE2>x{0}x</ENDOFLINE2>", Environment.NewLine));
            expectedXml.AppendLine("<ENDOFLINE3>x\nx</ENDOFLINE3>");
            expectedXml.AppendLine(string.Format("<WHITESPACE1>{0}<ELEM />{0}</WHITESPACE1>", Environment.NewLine));
            expectedXml.AppendLine("<WHITESPACE2> <ELEM /> </WHITESPACE2>");
            expectedXml.AppendLine("<WHITESPACE3>\t<ELEM />\t</WHITESPACE3>");
            expectedXml.AppendLine("<SKIP1 /><AFTERSKIP1 />");
            expectedXml.AppendLine("<SKIP2></SKIP2><AFTERSKIP2 />");
            expectedXml.AppendLine("<SKIP3><ELEM1 /><ELEM2>xxx yyy</ELEM2><ELEM3 /></SKIP3><AFTERSKIP3></AFTERSKIP3>");
            expectedXml.AppendLine("<SKIP4><ELEM1 /><ELEM2>xxx<ELEM3 /></ELEM2></SKIP4>");
            expectedXml.AppendLine("<CHARS1>0123456789</CHARS1>");
            expectedXml.AppendLine("<CHARS2>xxx<MARKUP />yyy</CHARS2>");
            expectedXml.AppendLine("<CHARS_ELEM1>xxx<MARKUP />yyy</CHARS_ELEM1>");
            expectedXml.AppendLine("<CHARS_ELEM2><MARKUP />yyy</CHARS_ELEM2>");
            expectedXml.AppendLine("<CHARS_ELEM3>xxx<MARKUP /></CHARS_ELEM3>");
            expectedXml.AppendLine("<CHARS_CDATA1>xxx<![CDATA[yyy]]>zzz</CHARS_CDATA1>");
            expectedXml.AppendLine("<CHARS_CDATA2><![CDATA[yyy]]>zzz</CHARS_CDATA2>");
            expectedXml.AppendLine("<CHARS_CDATA3>xxx<![CDATA[yyy]]></CHARS_CDATA3>");
            expectedXml.AppendLine("<CHARS_PI1>xxx<?PI_CHAR1 yyy?>zzz</CHARS_PI1>");
            expectedXml.AppendLine("<CHARS_PI2><?PI_CHAR2?>zzz</CHARS_PI2>");
            expectedXml.AppendLine("<CHARS_PI3>xxx<?PI_CHAR3 yyy?></CHARS_PI3>");
            expectedXml.AppendLine("<CHARS_COMMENT1>xxx<!-- comment1-->zzz</CHARS_COMMENT1>");
            expectedXml.AppendLine("<CHARS_COMMENT2><!-- comment1-->zzz</CHARS_COMMENT2>");
            expectedXml.AppendLine("<CHARS_COMMENT3>xxx<!-- comment1--></CHARS_COMMENT3>");
            expectedXml.AppendLine("<ISDEFAULT />");
            expectedXml.AppendLine("<ISDEFAULT a1='a1value' />");
            expectedXml.AppendLine("<BOOLEAN1>true</BOOLEAN1>");
            expectedXml.AppendLine("<BOOLEAN2>false</BOOLEAN2>");
            expectedXml.AppendLine("<BOOLEAN3>1</BOOLEAN3>");
            expectedXml.AppendLine("<BOOLEAN4>tRue</BOOLEAN4>");
            expectedXml.AppendLine("<DATETIME>1999-02-22T11:11:11</DATETIME>");
            expectedXml.AppendLine("<DATE>1999-02-22</DATE>");
            expectedXml.AppendLine("<TIME>11:11:11</TIME>");
            expectedXml.AppendLine("<INTEGER>9999</INTEGER>");
            expectedXml.AppendLine("<FLOAT>99.99</FLOAT>");
            expectedXml.AppendLine("<DECIMAL>.09</DECIMAL>");
            expectedXml.AppendLine("<CONTENT><e1 a1='a1value' a2='a2value'><e2 a1='a1value' a2='a2value'><e3 a1='a1value' a2='a2value'>leave</e3></e2></e1></CONTENT>");
            expectedXml.AppendLine("<TITLE><!-- this is a comment--></TITLE>");
            expectedXml.AppendLine("<PGROUP>");
            expectedXml.AppendLine("<ACT0 xmlns:foo=\"http://www.foo.com\" foo:Attr0=\"0\" foo:Attr1=\"1111111101\" foo:Attr2=\"222222202\" foo:Attr3=\"333333303\" foo:Attr4=\"444444404\" foo:Attr5=\"555555505\" foo:Attr6=\"666666606\" foo:Attr7=\"777777707\" foo:Attr8=\"888888808\" foo:Attr9=\"999999909\" />");
            expectedXml.AppendLine("<ACT1 Attr0=\'0\' Attr1=\'1111111101\' Attr2=\'222222202\' Attr3=\'333333303\' Attr4=\'444444404\' Attr5=\'555555505\' Attr6=\'666666606\' Attr7=\'777777707\' Attr8=\'888888808\' Attr9=\'999999909\' />");
            expectedXml.AppendLine("<QUOTE1 Attr0=\"0\" Attr1=\'1111111101\' Attr2=\"222222202\" Attr3=\'333333303\' />");
            expectedXml.AppendLine("<PERSONA>DROMIO OF EPHESUS</PERSONA>");
            expectedXml.AppendLine("<QUOTE2 Attr0=\"0\" Attr1=\"1111111101\" Attr2=\'222222202\' Attr3=\'333333303\' />");
            expectedXml.AppendLine("<QUOTE3 Attr0=\'0\' Attr1=\"1111111101\" Attr2=\'222222202\' Attr3=\"333333303\" />");
            expectedXml.AppendLine("<EMPTY1 />");
            expectedXml.AppendLine("<EMPTY2 val=\"abc\" />");
            expectedXml.AppendLine("<EMPTY3></EMPTY3>");
            expectedXml.AppendLine("<NONEMPTY0></NONEMPTY0>");
            expectedXml.AppendLine("<NONEMPTY1>ABCDE</NONEMPTY1>");
            expectedXml.AppendLine("<NONEMPTY2 val=\"abc\">1234</NONEMPTY2>");
            expectedXml.AppendLine("<ACT2 Attr0=\"10\" Attr1=\"1111111011\" Attr2=\"222222012\" Attr3=\"333333013\" Attr4=\"444444014\" Attr5=\"555555015\" Attr6=\"666666016\" Attr7=\"777777017\" Attr8=\"888888018\" Attr9=\"999999019\" />");
            expectedXml.AppendLine("<GRPDESCR>twin brothers, and sons to Aegeon and Aemilia.</GRPDESCR>");
            expectedXml.AppendLine("</PGROUP>");
            expectedXml.AppendLine("<PGROUP>");
            expectedXml.AppendLine("<XMLLANG0 xml:lang=\"en-US\">What color e1foo is it?</XMLLANG0>");
            expectedXml.Append("<XMLLANG1 xml:lang=\"en-GB\">What color is it?<a><b><c>Language Test</c><PERSONA>DROMIO OF EPHESUS</PERSONA></b></a></XMLLANG1>");
            expectedXml.AppendLine("<NOXMLLANG />");
            expectedXml.AppendLine("<EMPTY_XMLLANG Attr0=\"0\" xml:lang=\"en-US\" />");
            expectedXml.AppendLine("<XMLLANG2 xml:lang=\"en-US\">What color is it?<TITLE><!-- this is a comment--></TITLE><XMLLANG1 xml:lang=\"en-GB\">Testing language<XMLLANG0 xml:lang=\"en-US\">What color is it?</XMLLANG0>haha </XMLLANG1>hihihi</XMLLANG2>");
            expectedXml.AppendLine("<DONEXMLLANG />");
            expectedXml.AppendLine("<XMLSPACE1 xml:space=\'default\'>&lt; &gt;</XMLSPACE1>");
            expectedXml.Append("<XMLSPACE2 xml:space=\'preserve\'>&lt; &gt;<a><!-- comment--><b><?PI1a?><c>Space Test</c><PERSONA>DROMIO OF SYRACUSE</PERSONA></b></a></XMLSPACE2>");
            expectedXml.AppendLine("<NOSPACE />");
            expectedXml.AppendLine("<EMPTY_XMLSPACE Attr0=\"0\" xml:space=\'default\' />");
            expectedXml.AppendLine("<XMLSPACE2A xml:space=\'default\'>&lt; <XMLSPACE3 xml:space=\'preserve\'>  &lt; &gt; <XMLSPACE4 xml:space=\'default\'>  &lt; &gt;  </XMLSPACE4> test </XMLSPACE3> &gt;</XMLSPACE2A>");
            expectedXml.AppendLine("<GRPDESCR>twin brothers, and attendants on the two Antipholuses.</GRPDESCR>");
            expectedXml.AppendLine("<DOCNAMESPACE>");
            expectedXml.AppendLine("<NAMESPACE0 xmlns:bar=\"1\"><bar:check>Namespace=1</bar:check></NAMESPACE0>");
            expectedXml.AppendLine("<NAMESPACE1 xmlns:bar=\"1\"><a><b><c><d><bar:check>Namespace=1</bar:check><bar:check2></bar:check2></d></c></b></a></NAMESPACE1>");
            expectedXml.AppendLine("<NONAMESPACE>Namespace=\"\"</NONAMESPACE>");
            expectedXml.AppendLine("<EMPTY_NAMESPACE bar:Attr0=\"0\" xmlns:bar=\"1\" />");
            expectedXml.AppendLine("<EMPTY_NAMESPACE1 Attr0=\"0\" xmlns=\"14\" />");
            expectedXml.AppendLine("<EMPTY_NAMESPACE2 Attr0=\"0\" xmlns=\"14\"></EMPTY_NAMESPACE2>");
            expectedXml.AppendLine("<NAMESPACE2 xmlns:bar=\"1\"><a><b><c xmlns:bar=\"2\"><d><bar:check>Namespace=2</bar:check></d></c></b></a></NAMESPACE2>");
            expectedXml.AppendLine("<NAMESPACE3 xmlns=\"1\"><a xmlns:a=\"2\" xmlns:b=\"3\" xmlns:c=\"4\"><b xmlns:d=\"5\" xmlns:e=\"6\" xmlns:f='7'><c xmlns:d=\"8\" xmlns:e=\"9\" xmlns:f=\"10\">");
            expectedXml.AppendLine("<d xmlns:g=\"11\" xmlns:h=\"12\"><check>Namespace=1</check><testns xmlns=\"100\"><empty100 /><check100>Namespace=100</check100></testns><check1>Namespace=1</check1><d:check8>Namespace=8</d:check8></d></c><d:check5>Namespace=5</d:check5></b></a>");
            expectedXml.AppendLine("<a13 a:check=\"Namespace=13\" xmlns:a=\"13\" /><check14 xmlns=\"14\">Namespace=14</check14></NAMESPACE3>");
            expectedXml.AppendLine("<NONAMESPACE>Namespace=\"\"</NONAMESPACE>");
            expectedXml.AppendLine("<NONAMESPACE1 Attr1=\"one\" xmlns=\"1000\">Namespace=\"\"</NONAMESPACE1>");
            expectedXml.AppendLine("</DOCNAMESPACE>");
            expectedXml.AppendLine("</PGROUP>");
            expectedXml.AppendLine("<GOTOCONTENT>some text<![CDATA[cdata info]]></GOTOCONTENT>");
            expectedXml.AppendLine("<SKIPCONTENT att1=\"\">  <!-- comment1--> \n <?PI_SkipContent instruction?></SKIPCONTENT>");
            expectedXml.AppendLine("<MIXCONTENT>  <!-- comment1-->some text<?PI_SkipContent instruction?><![CDATA[cdata info]]></MIXCONTENT>");
            expectedXml.AppendLine("<A att=\"123\">1<B>2<C>3<D>4<E>5<F>6<G>7<H>8<I>9<J>10");
            expectedXml.AppendLine("<A1 att=\"456\">11<B1>12<C1>13<D1>14<E1>15<F1>16<G1>17<H1>18<I1>19<J1>20");
            expectedXml.AppendLine("<A2 att=\"789\">21<B2>22<C2>23<D2>24<E2>25<F2>26<G2>27<H2>28<I2>29<J2>30");
            expectedXml.AppendLine("<A3 att=\"123\">31<B3>32<C3>33<D3>34<E3>35<F3>36<G3>37<H3>38<I3>39<J3>40");
            expectedXml.AppendLine("<A4 att=\"456\">41<B4>42<C4>43<D4>44<E4>45<F4>46<G4>47<H4>48<I4>49<J4>50");
            expectedXml.AppendLine("<A5 att=\"789\">51<B5>52<C5>53<D5>54<E5>55<F5>56<G5>57<H5>58<I5>59<J5>60");
            expectedXml.AppendLine("<A6 att=\"123\">61<B6>62<C6>63<D6>64<E6>65<F6>66<G6>67<H6>68<I6>69<J6>70");
            expectedXml.AppendLine("<A7 att=\"456\">71<B7>72<C7>73<D7>74<E7>75<F7>76<G7>77<H7>78<I7>79<J7>80");
            expectedXml.AppendLine("<A8 att=\"789\">81<B8>82<C8>83<D8>84<E8>85<F8>86<G8>87<H8>88<I8>89<J8>90");
            expectedXml.AppendLine("<A9 att=\"123\">91<B9>92<C9>93<D9>94<E9>95<F9>96<G9>97<H9>98<I9>99<J9>100");
            expectedXml.AppendLine("<A10 att=\"123\">101<B10>102<C10>103<D10>104<E10>105<F10>106<G10>107<H10>108<I10>109<J10>110");
            expectedXml.AppendLine("</J10>109</I10>108</H10>107</G10>106</F10>105</E10>104</D10>103</C10>102</B10>101</A10>");
            expectedXml.AppendLine("</J9>99</I9>98</H9>97</G9>96</F9>95</E9>94</D9>93</C9>92</B9>91</A9>");
            expectedXml.AppendLine("</J8>89</I8>88</H8>87</G8>86</F8>85</E8>84</D8>83</C8>82</B8>81</A8>");
            expectedXml.AppendLine("</J7>79</I7>78</H7>77</G7>76</F7>75</E7>74</D7>73</C7>72</B7>71</A7>");
            expectedXml.AppendLine("</J6>69</I6>68</H6>67</G6>66</F6>65</E6>64</D6>63</C6>62</B6>61</A6>");
            expectedXml.AppendLine("</J5>59</I5>58</H5>57</G5>56</F5>55</E5>54</D5>53</C5>52</B5>51</A5>");
            expectedXml.AppendLine("</J4>49</I4>48</H4>47</G4>46</F4>45</E4>44</D4>43</C4>42</B4>41</A4>");
            expectedXml.AppendLine("</J3>39</I3>38</H3>37</G3>36</F3>35</E3>34</D3>33</C3>32</B3>31</A3>");
            expectedXml.AppendLine("</J2>29</I2>28</H2>27</G2>26</F2>25</E2>24</D2>23</C2>22</B2>21</A2>");
            expectedXml.AppendLine("</J1>19</I1>18</H1>17</G1>16</F1>15</E1>14</D1>13</C1>12</B1>11</A1>");
            expectedXml.Append("</J>9</I>8</H>7</G>6</F>5</E>4</D>3</C>2</B>1</A>");
            expectedXml.AppendLine("<EMPTY4 val=\"abc\"></EMPTY4>");
            expectedXml.AppendLine("<COMPLEX>Text<!-- comment --><![CDATA[cdata]]></COMPLEX>");
            expectedXml.AppendLine("<DUMMY />");
            expectedXml.AppendLine(string.Format("<MULTISPACES att=' {0} \t {0}{0}  n1  {0} \t {0}{0}  n2  {0} \t {0}{0} ' />", Environment.NewLine));
            expectedXml.AppendLine("<CAT>AB<![CDATA[CD]]> </CAT>");
            expectedXml.AppendLine("<CATMIXED>AB<![CDATA[CD]]> </CATMIXED>");

            expectedXml.AppendLine("<VALIDXMLLANG0 xml:lang=\"a\" />");
            expectedXml.AppendLine("<VALIDXMLLANG1 xml:lang=\"\" />");
            expectedXml.AppendLine("<VALIDXMLLANG2 xml:lang=\"ab-cd-\" />");
            expectedXml.AppendLine("<VALIDXMLLANG3 xml:lang=\"a b-cd\" />");

            //append expected xml
            fullXml.AppendLine(expectedXml.ToString());

            //add end tag
            fullXml.Append("</PLAY>");

            //quote expected string with Environment.NewLine
            expected = Environment.NewLine + expectedXml.ToString() + Environment.NewLine;

            return fullXml.ToString();
        }
    }
}
