// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // TestFiles
    //
    ////////////////////////////////////////////////////////////////
    public class NameTable_TestFiles
    {
        //Data
        private static string s_allNodeTypeDTD = "AllNodeTypes.dtd";
        private static string s_allNodeTypeENT = "AllNodeTypes.ent";
        private static string s_genericXml = "Generic.xml";
        private static string s_bigFileXml = "Big.xml";

        public static void RemoveDataReader(EREADER_TYPE eReaderType)
        {
            switch (eReaderType)
            {
                case EREADER_TYPE.GENERIC:
                    DeleteTestFile(s_allNodeTypeDTD);
                    DeleteTestFile(s_allNodeTypeENT);
                    DeleteTestFile(s_genericXml);
                    break;
                case EREADER_TYPE.BIG_ELEMENT_SIZE:
                    DeleteTestFile(s_bigFileXml);
                    break;
                default:
                    throw new Exception();
            }
        }

        private static Dictionary<EREADER_TYPE, string> s_fileNameMap = null;
        public static string GetTestFileName(EREADER_TYPE eReaderType)
        {
            if (s_fileNameMap == null)
                InitFileNameMap();
            return s_fileNameMap[eReaderType];
        }

        private static void InitFileNameMap()
        {
            if (s_fileNameMap == null)
            {
                s_fileNameMap = new Dictionary<EREADER_TYPE, string>();
            }

            s_fileNameMap.Add(EREADER_TYPE.GENERIC, s_genericXml);
            s_fileNameMap.Add(EREADER_TYPE.BIG_ELEMENT_SIZE, s_bigFileXml);
        }

        public static void CreateTestFile(ref string strFileName, EREADER_TYPE eReaderType)
        {
            strFileName = GetTestFileName(eReaderType);

            switch (eReaderType)
            {
                case EREADER_TYPE.GENERIC:
                    CreateGenericTestFile(strFileName);
                    break;
                case EREADER_TYPE.BIG_ELEMENT_SIZE:
                    CreateBigElementTestFile(strFileName);
                    break;
                default:
                    throw new Exception();
            }
        }

        protected static void DeleteTestFile(string strFileName)
        {
        }

        public static void CreateGenericTestFile(string strFileName)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter tw = new StreamWriter(ms);

            tw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
            tw.WriteLine("<!-- comment1 -->");
            tw.WriteLine("<?PI1_First processing instruction?>");
            tw.WriteLine("<?PI1a?>");
            tw.WriteLine("<?PI1b?>");
            tw.WriteLine("<?PI1c?>");
            tw.WriteLine("<!DOCTYPE root SYSTEM \"AllNodeTypes.dtd\" [");
            tw.WriteLine("<!NOTATION gif SYSTEM \"foo.exe\">");
            tw.WriteLine("<!ELEMENT root ANY>");
            tw.WriteLine("<!ELEMENT elem1 ANY>");
            tw.WriteLine("<!ELEMENT ISDEFAULT ANY>");
            tw.WriteLine("<!ENTITY % e SYSTEM \"AllNodeTypes.ent\">");
            tw.WriteLine("%e;");
            tw.WriteLine("<!ENTITY e1 \"e1foo\">");
            tw.WriteLine("<!ENTITY e2 \"&ext3; e2bar\">");
            tw.WriteLine("<!ENTITY e3 \"&e1; e3bzee \">");
            tw.WriteLine("<!ENTITY e4 \"&e3; e4gee\">");
            tw.WriteLine("<!ATTLIST elem1 child1 CDATA #IMPLIED child2 CDATA \"&e2;\" child3 CDATA #REQUIRED>");
            tw.WriteLine("<!ATTLIST root xmlns:something CDATA #FIXED \"something\" xmlns:my CDATA #FIXED \"my\" xmlns:dt CDATA #FIXED \"urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/\">");
            tw.WriteLine("<!ATTLIST ISDEFAULT d1 CDATA #FIXED \"d1value\">");

            tw.WriteLine("<!ATTLIST MULTISPACES att IDREFS #IMPLIED>");
            tw.WriteLine("<!ELEMENT CATMIXED (#PCDATA)>");

            tw.WriteLine("]>");
            tw.WriteLine("<PLAY>");
            tw.WriteLine("<root xmlns:something=\"something\" xmlns:my=\"my\" xmlns:dt=\"urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/\">");
            tw.WriteLine("<elem1 child1=\"\" child2=\"e1foo e3bzee  e2bar\" child3=\"something\">");
            tw.WriteLine("text node two e1foo text node three");
            tw.WriteLine("</elem1>");
            tw.WriteLine("e1foo e3bzee  e2bar");
            tw.WriteLine("<![CDATA[ This section contains characters that should not be interpreted as markup. For example, characters ', \",");
            tw.WriteLine("<, >, and & are all fine here.]]>");
            tw.WriteLine("<elem2 att1=\"id1\" att2=\"up\" att3=\"attribute3\"> ");
            tw.WriteLine("<a />");
            tw.WriteLine("</elem2>");
            tw.WriteLine("<elem2> ");
            tw.WriteLine("elem2-text1");
            tw.WriteLine("<a refs=\"id2\"> ");
            tw.WriteLine("this-is-a    ");
            tw.WriteLine("</a> ");
            tw.WriteLine("elem2-text2");
            tw.WriteLine("e1foo e3bzee ");
            tw.WriteLine("e1foo e3bzee  e4gee");
            tw.WriteLine("<!-- elem2-comment1-->");
            tw.WriteLine("elem2-text3");
            tw.WriteLine("<b> ");
            tw.WriteLine("this-is-b");
            tw.WriteLine("</b>");
            tw.WriteLine("elem2-text4");
            tw.WriteLine("<?elem2_PI elem2-PI?>");
            tw.WriteLine("elem2-text5");
            tw.WriteLine("</elem2>");
            tw.WriteLine("<elem2 att1=\"id2\"></elem2>");
            tw.WriteLine("</root>");
            tw.Write("<ENTITY1 att1='xxx&lt;xxx&#65;xxx&#x43;xxxe1fooxxx'>xxx&gt;xxx&#66;xxx&#x44;xxxe1fooxxx</ENTITY1>");
            tw.WriteLine("<ENTITY2 att1='xxx&lt;xxx&#65;xxx&#x43;xxxe1fooxxx'>xxx&gt;xxx&#66;xxx&#x44;xxxe1fooxxx</ENTITY2>");
            tw.WriteLine("<ENTITY3 att1='xxx&lt;xxx&#65;xxx&#x43;xxxe1fooxxx'>xxx&gt;xxx&#66;xxx&#x44;xxxe1fooxxx</ENTITY3>");
            tw.WriteLine("<ENTITY4 att1='xxx&lt;xxx&#65;xxx&#x43;xxxe1fooxxx'>xxx&gt;xxx&#66;xxx&#x44;xxxe1fooxxx</ENTITY4>");
            tw.WriteLine("<ENTITY5>e1foo e3bzee </ENTITY5>");
            tw.WriteLine("<ATTRIBUTE1 />");
            tw.WriteLine("<ATTRIBUTE2 a1='a1value' />");
            tw.WriteLine("<ATTRIBUTE3 a1='a1value' a2='a2value' a3='a3value' />");
            tw.WriteLine("<ATTRIBUTE4 a1='' />");
            tw.WriteLine(string.Format("<ATTRIBUTE5 CRLF='x{0}x' CR='x{0}x' LF='x\nx' MS='x     x' TAB='x\tx' />", Environment.NewLine));
            tw.WriteLine(string.Format("<ENDOFLINE1>x{0}x</ENDOFLINE1>", Environment.NewLine));
            tw.WriteLine(string.Format("<ENDOFLINE2>x{0}x</ENDOFLINE2>", Environment.NewLine));
            tw.WriteLine("<ENDOFLINE3>x\nx</ENDOFLINE3>");
            tw.WriteLine(string.Format("<WHITESPACE1>{0}<ELEM />{0}</WHITESPACE1>", Environment.NewLine));
            tw.WriteLine("<WHITESPACE2> <ELEM /> </WHITESPACE2>");
            tw.WriteLine("<WHITESPACE3>\t<ELEM />\t</WHITESPACE3>");
            tw.WriteLine("<SKIP1 /><AFTERSKIP1 />");
            tw.WriteLine("<SKIP2></SKIP2><AFTERSKIP2 />");
            tw.WriteLine("<SKIP3><ELEM1 /><ELEM2>xxx yyy</ELEM2><ELEM3 /></SKIP3><AFTERSKIP3></AFTERSKIP3>");
            tw.WriteLine("<SKIP4><ELEM1 /><ELEM2>xxx<ELEM3 /></ELEM2></SKIP4>");
            tw.WriteLine("<CHARS1>0123456789</CHARS1>");
            tw.WriteLine("<CHARS2>xxx<MARKUP />yyy</CHARS2>");
            tw.WriteLine("<CHARS_ELEM1>xxx<MARKUP />yyy</CHARS_ELEM1>");
            tw.WriteLine("<CHARS_ELEM2><MARKUP />yyy</CHARS_ELEM2>");
            tw.WriteLine("<CHARS_ELEM3>xxx<MARKUP /></CHARS_ELEM3>");
            tw.WriteLine("<CHARS_CDATA1>xxx<![CDATA[yyy]]>zzz</CHARS_CDATA1>");
            tw.WriteLine("<CHARS_CDATA2><![CDATA[yyy]]>zzz</CHARS_CDATA2>");
            tw.WriteLine("<CHARS_CDATA3>xxx<![CDATA[yyy]]></CHARS_CDATA3>");
            tw.WriteLine("<CHARS_PI1>xxx<?PI_CHAR1 yyy?>zzz</CHARS_PI1>");
            tw.WriteLine("<CHARS_PI2><?PI_CHAR2?>zzz</CHARS_PI2>");
            tw.WriteLine("<CHARS_PI3>xxx<?PI_CHAR3 yyy?></CHARS_PI3>");
            tw.WriteLine("<CHARS_COMMENT1>xxx<!-- comment1-->zzz</CHARS_COMMENT1>");
            tw.WriteLine("<CHARS_COMMENT2><!-- comment1-->zzz</CHARS_COMMENT2>");
            tw.WriteLine("<CHARS_COMMENT3>xxx<!-- comment1--></CHARS_COMMENT3>");
            tw.Flush();
            tw.WriteLine("<ISDEFAULT />");
            tw.WriteLine("<ISDEFAULT a1='a1value' />");
            tw.WriteLine("<BOOLEAN1>true</BOOLEAN1>");
            tw.WriteLine("<BOOLEAN2>false</BOOLEAN2>");
            tw.WriteLine("<BOOLEAN3>1</BOOLEAN3>");
            tw.WriteLine("<BOOLEAN4>tRue</BOOLEAN4>");
            tw.WriteLine("<DATETIME>1999-02-22T11:11:11</DATETIME>");
            tw.WriteLine("<DATE>1999-02-22</DATE>");
            tw.WriteLine("<TIME>11:11:11</TIME>");
            tw.WriteLine("<INTEGER>9999</INTEGER>");
            tw.WriteLine("<FLOAT>99.99</FLOAT>");
            tw.WriteLine("<DECIMAL>.09</DECIMAL>");
            tw.WriteLine("<CONTENT><e1 a1='a1value' a2='a2value'><e2 a1='a1value' a2='a2value'><e3 a1='a1value' a2='a2value'>leave</e3></e2></e1></CONTENT>");
            tw.WriteLine("<TITLE><!-- this is a comment--></TITLE>");
            tw.WriteLine("<PGROUP>");
            tw.WriteLine("<ACT0 xmlns:foo=\"http://www.foo.com\" foo:Attr0=\"0\" foo:Attr1=\"1111111101\" foo:Attr2=\"222222202\" foo:Attr3=\"333333303\" foo:Attr4=\"444444404\" foo:Attr5=\"555555505\" foo:Attr6=\"666666606\" foo:Attr7=\"777777707\" foo:Attr8=\"888888808\" foo:Attr9=\"999999909\" />");
            tw.WriteLine("<ACT1 Attr0=\'0\' Attr1=\'1111111101\' Attr2=\'222222202\' Attr3=\'333333303\' Attr4=\'444444404\' Attr5=\'555555505\' Attr6=\'666666606\' Attr7=\'777777707\' Attr8=\'888888808\' Attr9=\'999999909\' />");
            tw.WriteLine("<QUOTE1 Attr0=\"0\" Attr1=\'1111111101\' Attr2=\"222222202\" Attr3=\'333333303\' />");
            tw.WriteLine("<PERSONA>DROMIO OF EPHESUS</PERSONA>");
            tw.WriteLine("<QUOTE2 Attr0=\"0\" Attr1=\"1111111101\" Attr2=\'222222202\' Attr3=\'333333303\' />");
            tw.WriteLine("<QUOTE3 Attr0=\'0\' Attr1=\"1111111101\" Attr2=\'222222202\' Attr3=\"333333303\" />");
            tw.WriteLine("<EMPTY1 />");
            tw.WriteLine("<EMPTY2 val=\"abc\" />");
            tw.WriteLine("<EMPTY3></EMPTY3>");
            tw.WriteLine("<NONEMPTY0></NONEMPTY0>");
            tw.WriteLine("<NONEMPTY1>ABCDE</NONEMPTY1>");
            tw.WriteLine("<NONEMPTY2 val=\"abc\">1234</NONEMPTY2>");
            tw.WriteLine("<ACT2 Attr0=\"10\" Attr1=\"1111111011\" Attr2=\"222222012\" Attr3=\"333333013\" Attr4=\"444444014\" Attr5=\"555555015\" Attr6=\"666666016\" Attr7=\"777777017\" Attr8=\"888888018\" Attr9=\"999999019\" />");
            tw.WriteLine("<GRPDESCR>twin brothers, and sons to Aegeon and Aemilia.</GRPDESCR>");
            tw.WriteLine("</PGROUP>");
            tw.WriteLine("<PGROUP>");
            tw.Flush();
            tw.WriteLine("<XMLLANG0 xml:lang=\"en-US\">What color e1foo is it?</XMLLANG0>");
            tw.Write("<XMLLANG1 xml:lang=\"en-GB\">What color is it?<a><b><c>Language Test</c><PERSONA>DROMIO OF EPHESUS</PERSONA></b></a></XMLLANG1>");
            tw.WriteLine("<NOXMLLANG />");
            tw.WriteLine("<EMPTY_XMLLANG Attr0=\"0\" xml:lang=\"en-US\" />");
            tw.WriteLine("<XMLLANG2 xml:lang=\"en-US\">What color is it?<TITLE><!-- this is a comment--></TITLE><XMLLANG1 xml:lang=\"en-GB\">Testing language<XMLLANG0 xml:lang=\"en-US\">What color is it?</XMLLANG0>haha </XMLLANG1>hihihi</XMLLANG2>");
            tw.WriteLine("<DONEXMLLANG />");
            tw.WriteLine("<XMLSPACE1 xml:space=\'default\'>&lt; &gt;</XMLSPACE1>");
            tw.Write("<XMLSPACE2 xml:space=\'preserve\'>&lt; &gt;<a><!-- comment--><b><?PI1a?><c>Space Test</c><PERSONA>DROMIO OF SYRACUSE</PERSONA></b></a></XMLSPACE2>");
            tw.WriteLine("<NOSPACE />");
            tw.WriteLine("<EMPTY_XMLSPACE Attr0=\"0\" xml:space=\'default\' />");
            tw.WriteLine("<XMLSPACE2A xml:space=\'default\'>&lt; <XMLSPACE3 xml:space=\'preserve\'>  &lt; &gt; <XMLSPACE4 xml:space=\'default\'>  &lt; &gt;  </XMLSPACE4> test </XMLSPACE3> &gt;</XMLSPACE2A>");
            tw.WriteLine("<GRPDESCR>twin brothers, and attendants on the two Antipholuses.</GRPDESCR>");
            tw.WriteLine("<DOCNAMESPACE>");
            tw.WriteLine("<NAMESPACE0 xmlns:bar=\"1\"><bar:check>Namespace=1</bar:check></NAMESPACE0>");
            tw.WriteLine("<NAMESPACE1 xmlns:bar=\"1\"><a><b><c><d><bar:check>Namespace=1</bar:check><bar:check2></bar:check2></d></c></b></a></NAMESPACE1>");
            tw.WriteLine("<NONAMESPACE>Namespace=\"\"</NONAMESPACE>");
            tw.WriteLine("<EMPTY_NAMESPACE bar:Attr0=\"0\" xmlns:bar=\"1\" />");
            tw.WriteLine("<EMPTY_NAMESPACE1 Attr0=\"0\" xmlns=\"14\" />");
            tw.WriteLine("<EMPTY_NAMESPACE2 Attr0=\"0\" xmlns=\"14\"></EMPTY_NAMESPACE2>");
            tw.WriteLine("<NAMESPACE2 xmlns:bar=\"1\"><a><b><c xmlns:bar=\"2\"><d><bar:check>Namespace=2</bar:check></d></c></b></a></NAMESPACE2>");
            tw.WriteLine("<NAMESPACE3 xmlns=\"1\"><a xmlns:a=\"2\" xmlns:b=\"3\" xmlns:c=\"4\"><b xmlns:d=\"5\" xmlns:e=\"6\" xmlns:f='7'><c xmlns:d=\"8\" xmlns:e=\"9\" xmlns:f=\"10\">");
            tw.WriteLine("<d xmlns:g=\"11\" xmlns:h=\"12\"><check>Namespace=1</check><testns xmlns=\"100\"><empty100 /><check100>Namespace=100</check100></testns><check1>Namespace=1</check1><d:check8>Namespace=8</d:check8></d></c><d:check5>Namespace=5</d:check5></b></a>");
            tw.WriteLine("<a13 a:check=\"Namespace=13\" xmlns:a=\"13\" /><check14 xmlns=\"14\">Namespace=14</check14></NAMESPACE3>");
            tw.WriteLine("<NONAMESPACE>Namespace=\"\"</NONAMESPACE>");
            tw.WriteLine("<NONAMESPACE1 Attr1=\"one\" xmlns=\"1000\">Namespace=\"\"</NONAMESPACE1>");
            tw.WriteLine("</DOCNAMESPACE>");
            tw.WriteLine("</PGROUP>");
            tw.WriteLine("<GOTOCONTENT>some text<![CDATA[cdata info]]></GOTOCONTENT>");
            tw.WriteLine("<SKIPCONTENT att1=\"\">  <!-- comment1--> \n <?PI_SkipContent instruction?></SKIPCONTENT>");
            tw.WriteLine("<MIXCONTENT>  <!-- comment1-->some text<?PI_SkipContent instruction?><![CDATA[cdata info]]></MIXCONTENT>");
            tw.WriteLine("<A att=\"123\">1<B>2<C>3<D>4<E>5<F>6<G>7<H>8<I>9<J>10");
            tw.WriteLine("<A1 att=\"456\">11<B1>12<C1>13<D1>14<E1>15<F1>16<G1>17<H1>18<I1>19<J1>20");
            tw.WriteLine("<A2 att=\"789\">21<B2>22<C2>23<D2>24<E2>25<F2>26<G2>27<H2>28<I2>29<J2>30");
            tw.WriteLine("<A3 att=\"123\">31<B3>32<C3>33<D3>34<E3>35<F3>36<G3>37<H3>38<I3>39<J3>40");
            tw.WriteLine("<A4 att=\"456\">41<B4>42<C4>43<D4>44<E4>45<F4>46<G4>47<H4>48<I4>49<J4>50");
            tw.WriteLine("<A5 att=\"789\">51<B5>52<C5>53<D5>54<E5>55<F5>56<G5>57<H5>58<I5>59<J5>60");
            tw.WriteLine("<A6 att=\"123\">61<B6>62<C6>63<D6>64<E6>65<F6>66<G6>67<H6>68<I6>69<J6>70");
            tw.WriteLine("<A7 att=\"456\">71<B7>72<C7>73<D7>74<E7>75<F7>76<G7>77<H7>78<I7>79<J7>80");
            tw.WriteLine("<A8 att=\"789\">81<B8>82<C8>83<D8>84<E8>85<F8>86<G8>87<H8>88<I8>89<J8>90");
            tw.WriteLine("<A9 att=\"123\">91<B9>92<C9>93<D9>94<E9>95<F9>96<G9>97<H9>98<I9>99<J9>100");
            tw.WriteLine("<A10 att=\"123\">101<B10>102<C10>103<D10>104<E10>105<F10>106<G10>107<H10>108<I10>109<J10>110");
            tw.WriteLine("</J10>109</I10>108</H10>107</G10>106</F10>105</E10>104</D10>103</C10>102</B10>101</A10>");
            tw.WriteLine("</J9>99</I9>98</H9>97</G9>96</F9>95</E9>94</D9>93</C9>92</B9>91</A9>");
            tw.WriteLine("</J8>89</I8>88</H8>87</G8>86</F8>85</E8>84</D8>83</C8>82</B8>81</A8>");
            tw.WriteLine("</J7>79</I7>78</H7>77</G7>76</F7>75</E7>74</D7>73</C7>72</B7>71</A7>");
            tw.WriteLine("</J6>69</I6>68</H6>67</G6>66</F6>65</E6>64</D6>63</C6>62</B6>61</A6>");
            tw.WriteLine("</J5>59</I5>58</H5>57</G5>56</F5>55</E5>54</D5>53</C5>52</B5>51</A5>");
            tw.WriteLine("</J4>49</I4>48</H4>47</G4>46</F4>45</E4>44</D4>43</C4>42</B4>41</A4>");
            tw.WriteLine("</J3>39</I3>38</H3>37</G3>36</F3>35</E3>34</D3>33</C3>32</B3>31</A3>");
            tw.WriteLine("</J2>29</I2>28</H2>27</G2>26</F2>25</E2>24</D2>23</C2>22</B2>21</A2>");
            tw.WriteLine("</J1>19</I1>18</H1>17</G1>16</F1>15</E1>14</D1>13</C1>12</B1>11</A1>");
            tw.Write("</J>9</I>8</H>7</G>6</F>5</E>4</D>3</C>2</B>1</A>");
            tw.WriteLine("<EMPTY4 val=\"abc\"></EMPTY4>");
            tw.WriteLine("<COMPLEX>Text<!-- comment --><![CDATA[cdata]]></COMPLEX>");
            tw.WriteLine("<DUMMY />");
            tw.WriteLine(string.Format("<MULTISPACES att=' {0} \t {0}{0}  n1  {0} \t {0}{0}  n2  {0} \t {0}{0} ' />", Environment.NewLine));
            tw.WriteLine("<CAT>AB<![CDATA[CD]]> </CAT>");
            tw.WriteLine("<CATMIXED>AB<![CDATA[CD]]> </CATMIXED>");

            tw.WriteLine("<VALIDXMLLANG0 xml:lang=\"a\" />");
            tw.WriteLine("<VALIDXMLLANG1 xml:lang=\"\" />");
            tw.WriteLine("<VALIDXMLLANG2 xml:lang=\"ab-cd-\" />");
            tw.WriteLine("<VALIDXMLLANG3 xml:lang=\"a b-cd\" />");


            tw.Write("</PLAY>");
            tw.Flush();
            FilePathUtil.addStream(strFileName, ms);
        }

        public static void CreateBigElementTestFile(string strFileName)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter tw = new StreamWriter(ms);

            string str = new string('Z', (1 << 20) - 1);
            tw.WriteLine("<Root>");
            tw.Write("<");
            tw.Write(str);
            tw.WriteLine("X />");
            tw.Flush();

            tw.Write("<");
            tw.Write(str);
            tw.WriteLine("Y />");
            tw.WriteLine("</Root>");

            tw.Flush();
            FilePathUtil.addStream(strFileName, ms);
        }
    }
}

