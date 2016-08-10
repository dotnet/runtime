// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.IO;

namespace System.Xml.Tests
{
    // Note: Skipping these tests as they try to load a dll which references System.dll
    ////[TestCase(Name = "Classname Tests", Desc = "This Testcase maps to test variations described in 'Classname Functional Tests'")]
    //public class XsltcTestClassname : XsltcTestCaseBase
    //{
    //    private ITestOutputHelper _output;
    //    public XsltcTestClassname(ITestOutputHelper output) : base(output)
    //    {
    //        _output = output;
    //    }

    //    //[Variation("1", Desc = "Exercise the basic use case, a simple valid classname", Pri = 1, Params = new object[] { "/class:MyClass cnt1.xsl", "MyClass", "cnt1.dll", "cnt1.txt" })]
    //    [InlineData("/class:MyClass cnt1.xsl", "MyClass", "cnt1.dll", "cnt1.txt")]
    //    //[Variation("2", Desc = "Exercise the basic use case, a fully qualified classname", Pri = 1, Params = new object[] { "/class:XmlCoreTest.MyClass cnt2.xsl", "XmlCoreTest.MyClass", "cnt2.dll", "cnt2.txt" })]
    //    [InlineData("/class:XmlCoreTest.MyClass cnt2.xsl", "XmlCoreTest.MyClass", "cnt2.dll", "cnt2.txt")]
    //    //[Variation("4", Desc = "Exercise a C# reserved keyword", Pri = 1, Params = new object[] { "/class:class cnt4.xsl", "class", "cnt4.dll", "cnt4.txt" })]
    //    [InlineData("/class:class cnt4.xsl", "class", "cnt4.dll", "cnt4.txt")]
    //    //[Variation("5", Desc = "Exercise an MSIL reserved keyword", Pri = 1, Params = new object[] { "/class:hidebysig cnt5.xsl", "hidebysig", "cnt5.dll", "cnt5.txt" })]
    //    [InlineData("/class:hidebysig cnt5.xsl", "hidebysig", "cnt5.dll", "cnt5.txt")]
    //    //[Variation("6", Desc = "Exercise case sensitivity", Pri = 1, Params = new object[] { "/class:MyStyleSheEt cnt6.xsl", "MyStyleSheEt", "cnt6.dll", "cnt6.txt" })]
    //    [InlineData("/class:MyStyleSheEt cnt6.xsl", "MyStyleSheEt", "cnt6.dll", "cnt6.txt")]
    //    //[Variation("7", Desc = "Exercise wildcards", Pri = 1, Params = new object[] { "/class:AB? cnt7.xsl", "AB?", "cnt7.dll", "cnt7.txt" })]
    //    [InlineData("/class:AB? cnt7.xsl", "AB?", "cnt7.dll", "cnt7.txt")]
    //    //[Variation("8", Desc = "Exercise same classname, one in defautl NS, one in fully-qualified NS", Pri = 1, Params = new object[] { "/class:myclass cnt8a.xsl /class:myns.myclass cnt8b.xsl", "myclass", "cnt8a.dll", "cnt8.txt" })]
    //    [InlineData("/class:myclass cnt8a.xsl /class:myns.myclass cnt8b.xsl", "myclass", "cnt8a.dll", "cnt8.txt")]
    //    //[Variation("9", Desc = "Exercise same classname, different namespaces", Pri = 1, Params = new object[] { "/class:ns1.myclass cnt9a.xsl /class:ns2.myclass cnt9b.xsl", "ns1.myclass", "cnt9a.dll", "cnt9.txt" })]
    //    [InlineData("/class:ns1.myclass cnt9a.xsl /class:ns2.myclass cnt9b.xsl", "ns1.myclass", "cnt9a.dll", "cnt9.txt")]
    //    //[Variation("10", Desc = "Exercise same namespace, different classnames", Pri = 1, Params = new object[] { "/class:ns.myclass1 cnt10a.xsl /class:ns.myclass2 cnt10b.xsl", "ns.myclass2", "cnt10a.dll", "cnt10.txt" })]
    //    [InlineData("/class:ns.myclass1 cnt10a.xsl /class:ns.myclass2 cnt10b.xsl", "ns.myclass2", "cnt10a.dll", "cnt10.txt")]
    //    //[Variation("11", Desc = "Exercise inner class classname", Pri = 2, Params = new object[] { "/class:myclass1 cnt11a.xsl /class:myclass1.myclass2 cnt11b.xsl", "myclass1", "cnt11a.dll", "cnt11.txt" })]
    //    [InlineData("/class:myclass1 cnt11a.xsl /class:myclass1.myclass2 cnt11b.xsl", "myclass1", "cnt11a.dll", "cnt11.txt")]
    //    //[Variation("12", Desc = "Exercise inner class classname ordered", Pri = 1, Params = new object[] { "/class:myclass1.myclass2 cnt12b.xsl /class:myclass1 cnt12a.xsl ", "myclass1", "cnt12b.dll", "cnt12.txt" })]
    //    [InlineData("/class:myclass1.myclass2 cnt12b.xsl /class:myclass1 cnt12a.xsl ", "myclass1", "cnt12b.dll", "cnt12.txt")]
    //    //[Variation("14", Desc = "Exercise case sensitivity-2", Pri = 1, Params = new object[] { "/class:MyStyleSheEt cnt14.xsl /class:MystylesheeT cnt14b.xsl", "MyStyleSheEt", "cnt14.dll", "cnt14.txt" })]
    //    [InlineData("/class:MyStyleSheEt cnt14.xsl /class:MystylesheeT cnt14b.xsl", "MyStyleSheEt", "cnt14.dll", "cnt14.txt")]
    //    //[Variation("16", Desc = "Exercise stylesheet name with multiple dots for default classname ", Pri = 2, Params = new object[] { "ns1.ns2.cnt16.xsl", "ns1.ns2.cnt16", "ns1.ns2.cnt16.dll", "cnt16.txt" })]
    //    [InlineData("ns1.ns2.cnt16.xsl", "ns1.ns2.cnt16", "ns1.ns2.cnt16.dll", "cnt16.txt")]
    //    //[Variation("17", Desc = "Exercise stylesheet name with multiple adjacent dots for default classname", Pri = 2, Params = new object[] { "ns1..cnt17.xsl", "ns1..cnt17", "ns1..cnt17.dll", "cnt17.txt" })]
    //    [InlineData("ns1..cnt17.xsl", "ns1..cnt17", "ns1..cnt17.dll", "cnt17.txt")]
    //    //[Variation("19", Desc = "Exercise a stylesheet with invalid c# classname", Pri = 1, Params = new object[] { "2-(1)cnt19.xsl", "2-(1)cnt19", "2-(1)cnt19.dll", "cnt19.txt" })]
    //    [InlineData("2-(1)cnt19.xsl", "2-(1)cnt19", "2-(1)cnt19.dll", "cnt19.txt")]
    //    //[Variation("21", Desc = "Exercise keyword case sensitivity", Pri = 1, Params = new object[] { "/CLasS:cnt21 cnt21.xsl", "cnt21", "cnt21.dll", "cnt21.txt" })]
    //    [InlineData("/CLasS:cnt21 cnt21.xsl", "cnt21", "cnt21.dll", "cnt21.txt")]
    //    //[Variation("22", Desc = "Exercise a space containing classname", Pri = 2, Params = new object[] { "/class:\"cn t22\" cnt22.xsl", "cn t22", "cnt22.dll", "cnt22.txt" })]
    //    [InlineData("/class:\"cn t22\" cnt22.xsl", "cn t22", "cnt22.dll", "cnt22.txt")]
    //    //[Variation("23", Desc = "Exercise short form", Pri = 1, Params = new object[] { "-c:myclass cnt23.xsl", "myclass", "cnt23.dll", "cnt23.txt" })]
    //    [InlineData("-c:myclass cnt23.xsl", "myclass", "cnt23.dll", "cnt23.txt")]
    //    //[Variation("25", Desc = "Exercise two classnames differentiated only by Cyrillic E", Pri = 1, Params = new object[] { "/class:\u0400cnt25\u0400 cnt25a.xsl /class:cnt25 cnt25b.xsl", "\u0400cnt25\u0400", "cnt25a.dll", "cnt25.txt" })]
    //    [InlineData("/class:\u0400cnt25\u0400 cnt25a.xsl /class:cnt25 cnt25b.xsl", "\u0400cnt25\u0400", "cnt25a.dll", "cnt25.txt")]
    //    //[Variation("26", Desc = "Exercise two classnames differentiated only by zero width non breaking space and zero width non joiner", Pri = 1, Params = new object[] { "/class:\uFEFFcnt26\u200B cnt26a.xsl /class:cnt26 cnt26b.xsl", "\uFEFFcnt26\u200B", "cnt26a.dll", "cnt26.txt" })]
    //    [InlineData("/class:\uFEFFcnt26\u200B cnt26a.xsl /class:cnt26 cnt26b.xsl", "\uFEFFcnt26\u200B", "cnt26a.dll", "cnt26.txt")]
    //    //[Variation("27", Desc = "Exercise two classnames differentiated only by latin a combined with ring above and \u00e5", Pri = 1, Params = new object[] { "/class:\u0061\u030acnt27 cnt27a.xsl /class:\u00e5cnt27 cnt27b.xsl", "\u0061\u030acnt27", "cnt27a.dll", "cnt27.txt" })]
    //    [InlineData("/class:\u0061\u030acnt27 cnt27a.xsl /class:\u00e5cnt27 cnt27b.xsl", "\u0061\u030acnt27", "cnt27a.dll", "cnt27.txt")]
    //    //[Variation("28", Desc = "Exercise two classnames differentiated only by latin small y and IPA block phoenetic y with load", Pri = 1, Params = new object[] { "/class:\u0079cnt28 cnt28a.xsl /class:\u028Fcnt28 cnt28b.xsl", "\u0079cnt28", "cnt28a.dll", "cnt28.txt" })]
    //    [InlineData("/class:\u0079cnt28 cnt28a.xsl /class:\u028Fcnt28 cnt28b.xsl", "\u0079cnt28", "cnt28a.dll", "cnt28.txt")]
    //    //[Variation("29", Desc = "Exercise classnames containing (0x03C2), (0x03A3) and (0x03C3), where 2 lower case characters have the same upper case character", Pri = 1, Params = new object[] { "/class:\u03C2\u03A3\u03C3cnt29 cnt29a.xsl /class:\u03A3\u03C2\u03C3cnt29 cnt29b.xsl", "\u03C2\u03A3\u03C3cnt29", "cnt29a.dll", "cnt29.txt" })]
    //    [InlineData("/class:\u03C2\u03A3\u03C3cnt29 cnt29a.xsl /class:\u03A3\u03C2\u03C3cnt29 cnt29b.xsl", "\u03C2\u03A3\u03C3cnt29", "cnt29a.dll", "cnt29.txt")]
    //    [Theory]
    //    public void Var1(object param0, object param1, object param2, object param3)
    //    {
    //        String cmdLine = param0.ToString();
    //        String asmName = param2.ToString();
    //        bool asmCreated = true;
    //        String typeName = param1.ToString();
    //        String pdbName = Path.ChangeExtension(param2.ToString(), ".pdb");
    //        bool pdbCreated = false;
    //        String baselineFile = param3.ToString();

    //        VerifyTest(cmdLine, asmName, asmCreated, typeName, pdbName, pdbCreated, baselineFile, _createFromInputFile);
    //    }

    //    //[Variation("3", Desc = "Exercise an invalid case, empty/no argument", Pri = 1, Params = new object[] { "/class: cnt3.xsl", "cnt3.txt" })]
    //    [InlineData("/class: cnt3.xsl", "cnt3.txt")]
    //    //[Variation("13", Desc = "Exercise an invalid case, class specified without colon", Pri = 1, Params = new object[] { "/class cnt13.xsl", "cnt13.txt" })]
    //    [InlineData("/class cnt13.xsl", "cnt13.txt")]
    //    //[Variation("15", Desc = "Exercise classname conflict, 2 stylesheets one with default classname and one specifying the same", Pri = 2, Params = new object[] { "cnt15a.xsl /class:cnt15a cnt15b.xsl", "cnt15.txt" })]
    //    [InlineData("cnt15a.xsl /class:cnt15a cnt15b.xsl", "cnt15.txt")]
    //    //[Variation("18", Desc = "Exercise the basic use case, 2 stylesheets with different extensions", Pri = 2, Params = new object[] { "cnt18.xsl cnt18.xslt", "cnt18.txt" })]
    //    [InlineData("cnt18.xsl cnt18.xslt", "cnt18.txt")]
    //    //[Variation("20", Desc = "Exercise a very long classname", Pri = 2, Params = new object[] { "/class:cnt2001234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789 cnt20.xsl", "cnt20.txt", "EnglishOnly" })]
    //    //[InlineData("/class:cnt2001234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789 cnt20.xsl", "cnt20.txt"/*, "EnglishOnly"*/)]
    //    //[Variation("24", Desc = "Exercise multiple /class flags for one stylesheet", Pri = 1, Params = new object[] { "/class:A /class:B cnt24.xsl", "cnt24.txt" })]
    //    [InlineData("/class:A /class:B cnt24.xsl", "cnt24.txt")]
    //    [Theory]
    //    public void Var2(object param0, object param1)
    //    {
    //        if (ShouldSkip(new object[] { param0, param1 }))
    //        {
    //            return; //TEST_SKIPPED;
    //        }
    //        String cmdLine = param0.ToString();
    //        String baselineFile = param1.ToString();

    //        VerifyTest(cmdLine, baselineFile, _createFromInputFile);
    //    }
    //}
}