// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class PropertiesTests : XLinqTestCase
        {
            public partial class ImplicitConversionsElem : XLinqTestCase
            {
                // these tests are duplication of the 
                // valid cases
                //   - direct value
                //   - concat value
                //          - concat of text
                //          - text & CData
                //          - concat from children
                //   - removing, adding and modifying nodes sanity

                // invalid cases (sanity, pri2)

                //[Variation(Priority = 0, Desc = "String - direct", Params = new object[] { @"<A>text</A>", @"text" })]
                //[Variation(Priority = 0, Desc = "String - concat two text nodes", Params = new object[] { @"<A>text1<B/>text2</A>", @"text1text2" })]
                //[Variation(Priority = 0, Desc = "String - concat text and CData", Params = new object[] { @"<A>text1<B/><![CDATA[text2]]></A>", @"text1text2" })]
                //[Variation(Priority = 0, Desc = "String - concat from children", Params = new object[] { @"<A><B>text1<D><![CDATA[text2]]></D></B><C>text3</C></A>", @"text1text2text3" })]
                //[Variation(Priority = 0, Desc = "String - empty", Params = new object[] { @"<A><B><D></D></B><C></C></A>", @"" })]
                //[Variation(Priority = 0, Desc = "String - empty with CDATA", Params = new object[] { @"<A><B><D><![CDATA[]]></D></B><C></C></A>", @"" })]
                public void StringConvert()
                {
                    string xml = Variation.Params[0] as string;
                    string expText = Variation.Params[1] as string;
                    XElement elem = XElement.Parse(xml);
                    TestLog.Compare((string)elem, expText, "text is not as expected");
                }

                //[Variation(Priority = 0, Desc = "bool - direct - true", Params = new object[] { @"<A>true</A>", true })]
                //[Variation(Priority = 0, Desc = "bool - direct - 1", Params = new object[] { @"<A>1</A>", true })]
                //[Variation(Priority = 0, Desc = "bool - direct - false", Params = new object[] { @"<A>false</A>", false })]
                //[Variation(Priority = 0, Desc = "bool - direct - 0 ", Params = new object[] { @"<A>0</A>", false })]
                //[Variation(Priority = 1, Desc = "bool - concat two text nodes - true", Params = new object[] { @"<A>tr<B/>ue</A>", true })]
                //[Variation(Priority = 1, Desc = "bool - concat two text nodes - false", Params = new object[] { @"<A>f<B/>alse</A>", false })]
                //[Variation(Priority = 1, Desc = "bool - concat text and CData - true", Params = new object[] { @"<A>tru<B/><![CDATA[e]]></A>", true })]
                //[Variation(Priority = 1, Desc = "bool - concat text and CData - false", Params = new object[] { @"<A>fal<B/><![CDATA[se]]></A>", false })]
                //[Variation(Priority = 1, Desc = "bool - concat from children - true", Params = new object[] { @"<A><B>t<D><![CDATA[ru]]></D></B><C>e</C></A>", true })]
                //[Variation(Priority = 1, Desc = "bool - concat from children - 1", Params = new object[] { @"<A> <B><D><![CDATA[1]]></D></B><C> </C></A>", true })]
                //[Variation(Priority = 1, Desc = "bool - concat from children - false", Params = new object[] { @"<A><B>fa<D><![CDATA[l]]></D></B><C>se</C></A>", false })]
                //[Variation(Priority = 1, Desc = "bool - concat from children - 0", Params = new object[] { @"<A><B> <D><![CDATA[ ]]></D></B><C>0</C></A>", false })]
                public void BoolConvert()
                {
                    string xml = Variation.Params[0] as string;
                    bool expBool = (bool)Variation.Params[1];
                    XElement elem = XElement.Parse(xml);
                    TestLog.Compare((bool)elem, expBool, "bool is not as expected");
                }

                //[Variation(Priority = 2, Desc = "bool - Invalid - empty", Params = new object[] { @"<A></A>" })]
                //[Variation(Priority = 2, Desc = "bool - Invalid - capital T", Params = new object[] { @"<A>True</A>" })]
                //[Variation(Priority = 2, Desc = "bool? - Invalid - some other", Params = new object[] { @"<A>2</A>" })]
                public void BoolConvertInvalid()
                {
                    string xml = Variation.Params[0] as string;
                    XElement elem = XElement.Parse(xml);
                    try
                    {
                        bool r = (bool)elem;
                    }
                    catch (FormatException fe)
                    {
                        TestLog.WriteLineIgnore(fe.Message);
                    }
                }

                //[Variation(Priority = 0, Desc = "bool? - direct - true", Params = new object[] { @"<A>true</A>", true })]
                //[Variation(Priority = 0, Desc = "bool? - direct - 1", Params = new object[] { @"<A>1</A>", true })]
                //[Variation(Priority = 0, Desc = "bool? - direct - false", Params = new object[] { @"<A>false</A>", false })]
                //[Variation(Priority = 0, Desc = "bool? - direct - 0 ", Params = new object[] { @"<A>0</A>", false })]
                //[Variation(Priority = 1, Desc = "bool? - concat two text nodes - true", Params = new object[] { @"<A>tr<B/>ue</A>", true })]
                //[Variation(Priority = 1, Desc = "bool? - concat two text nodes - false", Params = new object[] { @"<A>f<B/>alse</A>", false })]
                //[Variation(Priority = 1, Desc = "bool? - concat text and CData - true", Params = new object[] { @"<A>tru<B/><![CDATA[e]]></A>", true })]
                //[Variation(Priority = 1, Desc = "bool? - concat text and CData - false", Params = new object[] { @"<A>fal<B/><![CDATA[se]]></A>", false })]
                //[Variation(Priority = 1, Desc = "bool? - concat from children - true", Params = new object[] { @"<A><B>t<D><![CDATA[ru]]></D></B><C>e</C></A>", true })]
                //[Variation(Priority = 1, Desc = "bool? - concat from children - 1", Params = new object[] { @"<A> <B><D><![CDATA[1]]></D></B><C> </C></A>", true })]
                //[Variation(Priority = 1, Desc = "bool? - concat from children - false", Params = new object[] { @"<A><B>fa<D><![CDATA[l]]></D></B><C>se</C></A>", false })]
                //[Variation(Priority = 1, Desc = "bool? - concat from children - 0", Params = new object[] { @"<A><B> <D><![CDATA[ ]]></D></B><C>0</C></A>", false })]
                public void BoolQConvert()
                {
                    string xml = Variation.Params[0] as string;
                    bool? expBool = (bool?)Variation.Params[1];
                    XElement elem = XElement.Parse(xml);
                    TestLog.Compare((bool?)elem, expBool, "bool is not as expected");
                }

                //[Variation(Priority = 2, Desc = "bool? - Invalid - empty", Params = new object[] { @"<A a='1'></A>" })]
                //[Variation(Priority = 2, Desc = "bool? - Invalid - capital T", Params = new object[] { @"<A a='1'>True</A>" })]
                //[Variation(Priority = 2, Desc = "bool? - Invalid - space inside", Params = new object[] { @"<A a='1'>tr<B/> ue</A>" })]
                //[Variation(Priority = 2, Desc = "bool? - Invalid - some other", Params = new object[] { @"<A a='1'>2</A>" })]
                public void BoolQConvertInvalid()
                {
                    string xml = Variation.Params[0] as string;
                    XElement elem = XElement.Parse(xml);
                    try
                    {
                        bool? r = (bool?)elem;
                    }
                    catch (FormatException fe)
                    {
                        TestLog.WriteLineIgnore(fe.Message);
                    }
                }

                //[Variation(Priority = 2, Desc = "bool? - Null")]
                public void BoolQConvertNull()
                {
                    XElement elem = null;
                    bool? r = (bool?)elem;
                    TestLog.Compare(r == null, "result is null");
                }

                //[Variation(Priority = 0, Desc = "int - direct", Params = new object[] { @"<A a='1'>10</A>", 10 })]
                //[Variation(Priority = 0, Desc = "int - concat two text nodes", Params = new object[] { @"<A a='1'>1<B/>7</A>", 17 })]
                //[Variation(Priority = 0, Desc = "int - concat text and CData", Params = new object[] { @"<A a='1'>-<B/><![CDATA[21]]></A>", -21 })]
                //[Variation(Priority = 0, Desc = "int - concat from children", Params = new object[] { @"<A a='1'><B>-<D><![CDATA[12]]></D></B><C>0</C></A>", -120 })]
                public void IntConvert()
                {
                    string xml = Variation.Params[0] as string;
                    int exp = (int)Variation.Params[1];
                    XElement elem = XElement.Parse(xml);
                    TestLog.Compare((int)elem, exp, "int is not as expected");
                }

                //[Variation(Priority = 2, Desc = "int - Invalid - empty", Params = new object[] { @"<A a='1'></A>" })]
                //[Variation(Priority = 2, Desc = "int - Invalid - space inside", Params = new object[] { @"<A a='1'>2<B/> 1</A>" })]
                //[Variation(Priority = 2, Desc = "int - Invalid - some other", Params = new object[] { @"<A a='1'>X</A>" })]
                public void IntConvertInvalid()
                {
                    string xml = Variation.Params[0] as string;
                    XElement elem = XElement.Parse(xml);
                    try
                    {
                        int r = (int)elem;
                    }
                    catch (FormatException fe)
                    {
                        TestLog.WriteLineIgnore(fe.Message);
                    }
                }


                //[Variation(Priority = 0, Desc = "int? - direct", Params = new object[] { @"<A a='1'>10</A>", 10 })]
                //[Variation(Priority = 0, Desc = "int? - concat two text nodes", Params = new object[] { @"<A a='1'>1<B/>7</A>", 17 })]
                //[Variation(Priority = 0, Desc = "int? - concat text and CData", Params = new object[] { @"<A a='1'>-<B/><![CDATA[21]]></A>", -21 })]
                //[Variation(Priority = 0, Desc = "int? - concat from children", Params = new object[] { @"<A a='1'><B>-<D><![CDATA[12]]></D></B><C>0</C></A>", -120 })]
                public void IntQConvert()
                {
                    string xml = Variation.Params[0] as string;
                    int? exp = (int?)Variation.Params[1];
                    XElement elem = XElement.Parse(xml);
                    TestLog.Compare((int?)elem, exp, "int? is not as expected");
                }

                //[Variation(Priority = 2, Desc = "int? - Invalid - empty", Params = new object[] { @"<A a='1'></A>" })]
                //[Variation(Priority = 2, Desc = "int? - Invalid - space inside", Params = new object[] { @"<A a='1'>2<B/> 1</A>" })]
                //[Variation(Priority = 2, Desc = "int? - Invalid - some other", Params = new object[] { @"<A a='1'>X</A>" })]
                public void IntQConvertInvalid()
                {
                    string xml = Variation.Params[0] as string;
                    XElement elem = XElement.Parse(xml);
                    try
                    {
                        int? r = (int?)elem;
                    }
                    catch (FormatException fe)
                    {
                        TestLog.WriteLineIgnore(fe.Message);
                    }
                }

                //[Variation(Priority = 2, Desc = "int? - Null")]
                public void IntQConvertNull()
                {
                    XElement elem = null;
                    int? r = (int?)elem;
                    TestLog.Compare(r == null, "result is null");
                }

                //[Variation(Priority = 0, Desc = "uint - direct", Params = new object[] { @"<A a='1'>10</A>", 10 })]
                //[Variation(Priority = 0, Desc = "uint - concat two text nodes", Params = new object[] { @"<A a='1'>1<B/>7</A>", 17 })]
                //[Variation(Priority = 0, Desc = "uint - concat text and CData", Params = new object[] { @"<A a='1'><B/><![CDATA[21]]></A>", 21 })]
                //[Variation(Priority = 0, Desc = "uint - concat from children", Params = new object[] { @"<A a='1'><B><D><![CDATA[12]]></D></B><C>0</C></A>", 120 })]
                public void UIntConvert()
                {
                    string xml = Variation.Params[0] as string;
                    uint exp = (uint)((int)Variation.Params[1]);
                    XElement elem = XElement.Parse(xml);
                    TestLog.Compare((uint)elem == exp, "uint is not as expected");
                }

                //[Variation(Priority = 2, Desc = "uint - Invalid - empty", Params = new object[] { @"<A a='1'></A>" })]
                //[Variation(Priority = 2, Desc = "uint - Invalid - space inside", Params = new object[] { @"<A a='1'>2<B/> 1</A>" })]
                //[Variation(Priority = 2, Desc = "uint - Invalid - negative", Params = new object[] { @"<A a='1'>-<B/>1</A>" })]
                //[Variation(Priority = 2, Desc = "uint - Invalid - some other", Params = new object[] { @"<A a='1'>X</A>" })]
                public void UIntConvertInvalid()
                {
                    string xml = Variation.Params[0] as string;
                    XElement elem = XElement.Parse(xml);
                    try
                    {
                        uint r = (uint)elem;
                    }
                    catch (FormatException fe)
                    {
                        TestLog.WriteLineIgnore(fe.Message);
                    }
                }

                //[Variation(Priority = 0, Desc = "uint? - direct", Params = new object[] { @"<A a='1'>10</A>", 10 })]
                //[Variation(Priority = 0, Desc = "uint? - concat two text nodes", Params = new object[] { @"<A a='1'>1<B/>7</A>", 17 })]
                //[Variation(Priority = 0, Desc = "uint? - concat text and CData", Params = new object[] { @"<A a='1'><B/><![CDATA[21]]></A>", 21 })]
                //[Variation(Priority = 0, Desc = "uint? - concat from children", Params = new object[] { @"<A a='1'><B><D><![CDATA[12]]></D></B><C>0</C></A>", 120 })]
                public void UIntQConvert()
                {
                    string xml = Variation.Params[0] as string;
                    uint? exp = (uint?)((int)Variation.Params[1]);
                    XElement elem = XElement.Parse(xml);
                    TestLog.Compare((uint?)elem, exp, "uint? is not as expected");
                }

                //[Variation(Priority = 2, Desc = "uint? - Invalid - empty", Params = new object[] { @"<A a='1'></A>" })]
                //[Variation(Priority = 2, Desc = "uint? - Invalid - space inside", Params = new object[] { @"<A a='1'>2<B/> 1</A>" })]
                //[Variation(Priority = 2, Desc = "uint? - Invalid - negative", Params = new object[] { @"<A a='1'>-<B/>1</A>" })]
                //[Variation(Priority = 2, Desc = "uint? - Invalid - some other", Params = new object[] { @"<A a='1'>X</A>" })]
                public void UIntQConvertInvalid()
                {
                    string xml = Variation.Params[0] as string;
                    XElement elem = XElement.Parse(xml);
                    try
                    {
                        uint? r = (uint?)elem;
                    }
                    catch (FormatException fe)
                    {
                        TestLog.WriteLineIgnore(fe.Message);
                    }
                }

                //[Variation(Priority = 2, Desc = "uint? - Null")]
                public void UIntQConvertNull()
                {
                    XElement elem = null;
                    uint? r = (uint?)elem;
                    TestLog.Compare(r == null, "result is null");
                }
            }
        }
    }
}