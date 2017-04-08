// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Xsl;
using XmlCoreTest.Common;
using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    //[TestCase(Name = "Xml 4th Errata tests for XslCompiledTransform", Params = new object[] { 300 })]
    public class Errata4 : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public Errata4(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        private Random _rand = new Random(unchecked((int)DateTime.Now.Ticks));

        #region private const string xmlDocTemplate = ...

        private const string xmlDocTemplate =
            @"<root>
                <node>{0}</node>
                <{0}>{0}</{0}>
                <{0}:node xmlns:{0}=""ns1"" />
                <node {0}=""attr"" />
                <node {0}:a=""attr"" xmlns:{0}=""ns1""/>
            </root>";

        #endregion private const string xmlDocTemplate = ...

        #region private const string xslStylesheetTemplate = ...

        private const string xslStylesheetTemplate =
            @"<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"">

                <xsl:output indent=""no"" method=""text""/>

                <xsl:template match=""/"">
                    <xsl:apply-templates select=""*/node()"" />
                </xsl:template>

                <!-- <node>{{0}}</node> -->
                <xsl:template match=""node[text() = '{0}']"">
                    <xsl:value-of select=""../node[text() = '{0}']"" />
                </xsl:template>

                <!-- <{{0}}>{{0}}</{{0}}> -->
                <xsl:template match=""{0}"">
                    <xsl:value-of select=""../{0}"" />
                </xsl:template>

                <!-- <{{0}}:node xmlns:{{0}}=""ns1"" /> -->
                <xsl:template match=""{0}:node"" xmlns:{0}=""ns1"">
                    <xsl:value-of select=""substring-before(name(), ':')"" />
                </xsl:template>

                <!-- <node {0}=""attr"" /> -->
                <xsl:template match=""node[@{0}]"">
                    <xsl:value-of select=""local-name(@{0})"" />
                </xsl:template>

                <!-- <node {{0}}:a=""attr"" xmlns:{{0}}=""ns1""/> -->
                <xsl:template match=""node[@{0}:a]"" xmlns:{0}=""ns1"">
                    <xsl:value-of select=""substring-before(name(@{0}:a), ':')"" />
                </xsl:template>

            </xsl:stylesheet>";

        #endregion private const string xslStylesheetTemplate = ...

        #region private const string createElementsXsltAndXpath = ...

        private const string createElementsXsltAndXpath = @" <xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"">
  <xsl:output method=""xml"" indent=""yes""/>

  <xsl:param name=""mode""/>
  <xsl:param name=""char""/>

  <xsl:template match=""/*"">
    <xsl:if test=""$mode='middle'"">
      <xsl:element name=""{concat('mid',$char,'dle')}"">
        <xsl:attribute name=""{concat('mid',$char,'dle')}"">
          <xsl:value-of select=""100""/>
        </xsl:attribute>
      </xsl:element>
    </xsl:if>
    <xsl:if test=""$mode='start'"">
      <xsl:element name=""{concat($char,'start')}"">
        <xsl:attribute name=""{concat($char,'start')}"">
          <xsl:value-of select=""100""/>
        </xsl:attribute>
      </xsl:element>
    </xsl:if>
  </xsl:template>
</xsl:stylesheet>";

        #endregion private const string createElementsXsltAndXpath = ...

        #region private const string createElementsXsltInline = ...

        private const string createElementsXsltInline = @" <xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" xmlns:a=""foo"">
  <xsl:output method=""xml"" indent=""yes""/>
  <xsl:template match=""/*"">
    <xsl:element name=""{0}"">
      <xsl:attribute name=""{0}"">
        <xsl:value-of select=""100""/>
      </xsl:attribute>
    </xsl:element>
  </xsl:template>
</xsl:stylesheet>";

        #endregion private const string createElementsXsltInline = ...

        //[Variation(Priority = 1, Desc = "Crate elment/attribute :: Invalid start name char", Params = new object[] { false, CharType.NCNameStartChar })]
        [InlineData(false, CharType.NameStartChar)]
        //[Variation(Priority = 1, Desc = "Crate elment/attribute :: Invalid name char", Params = new object[] { false, CharType.NCNameChar })]
        [InlineData(false, CharType.NameChar)]
        //[Variation(Priority = 1, Desc = "Crate elment/attribute :: Invalid name CharType.NameStartSurrogateHighChar", Params = new object[] { false, CharType.NameStartSurrogateHighChar })]
        [InlineData(false, CharType.NameStartSurrogateHighChar)]
        //[Variation(Priority = 1, Desc = "Crate elment/attribute :: Invalid name CharType.NameStartSurrogateLowChar", Params = new object[] { false, CharType.NameStartSurrogateLowChar })]
        [InlineData(false, CharType.NameStartSurrogateLowChar)]
        //[Variation(Priority = 1, Desc = "Crate elment/attribute :: Invalid name CharType.NameSurrogateHighChar", Params = new object[] { false, CharType.NameSurrogateHighChar })]
        [InlineData(false, CharType.NameSurrogateHighChar)]
        //[Variation(Priority = 1, Desc = "Crate elment/attribute :: Invalid name CharType.NameSurrogateLowChar", Params = new object[] { false, CharType.NameSurrogateLowChar })]
        [InlineData(false, CharType.NameSurrogateLowChar)]
        // ---
        //[Variation(Priority = 0, Desc = "Crate elment/attribute :: Valid start name char", Params = new object[] { true, CharType.NCNameStartChar })]
        [InlineData(true, CharType.NameStartChar)]
        //[Variation(Priority = 0, Desc = "Crate elment/attribute :: Valid name char", Params = new object[] { true, CharType.NCNameChar })]
        [InlineData(true, CharType.NameChar)]
        // Only Valid for Fifth Edition Xml
        //[Variation(Priority = 0, Desc = "Crate elment/attribute :: Valid name CharType.NameStartSurrogateHighChar", Params = new object[] { true, CharType.NameStartSurrogateHighChar })]
        //[Variation(Priority = 0, Desc = "Crate elment/attribute :: Valid name CharType.NameStartSurrogateLowChar", Params = new object[] { true, CharType.NameStartSurrogateLowChar })]
        //[Variation(Priority = 0, Desc = "Crate elment/attribute :: Valid name CharType.NameSurrogateHighChar", Params = new object[] { true, CharType.NameSurrogateHighChar })]
        //[Variation(Priority = 0, Desc = "Crate elment/attribute :: Valid name CharType.NameSurrogateLowChar", Params = new object[] { true, CharType.NameSurrogateLowChar })]
        [OuterLoop]
        [Theory]
        public void CreateElementsAndAttributesUsingXsltAndXPath(object param0, object param1)
        {
            bool isValidChar = (bool)param0;
            CharType charType = (CharType)param1;
            var startChars = new CharType[] { CharType.NameStartChar, CharType.NameStartSurrogateHighChar, CharType.NameStartSurrogateLowChar };

            string charsToChooseFrom = isValidChar ? UnicodeCharHelper.GetValidCharacters(charType) : UnicodeCharHelper.GetInvalidCharacters(charType);
            Assert.True(charsToChooseFrom.Length > 0);

            foreach (bool enableDebug in new bool[] { /*true,*/ false }) // XSLT debugging not supported in Core
            {
                XslCompiledTransform transf = new XslCompiledTransform(enableDebug);
                using (XmlReader r = XmlReader.Create(new StringReader(createElementsXsltAndXpath))) transf.Load(r);

                for (int i = 0; i < charsToChooseFrom.Length; i++)
                {
                    XsltArgumentList arguments = new XsltArgumentList();
                    arguments.AddParam("mode", "", Array.Exists(startChars, x => x == charType) ? "start" : "middle");
                    string strToInject = GenerateStringToInject(charsToChooseFrom, i, charType);
                    if (strToInject[0] == ':') continue;
                    arguments.AddParam("char", "", strToInject);
                    StringBuilder sb = new StringBuilder();

                    try
                    {
                        using (XmlReader r = XmlReader.Create(new StringReader("<root/>"))) transf.Transform(r, arguments, new StringWriter(sb));
                        Assert.True(isValidChar);
                    }
                    catch (Exception)
                    {
                        if (isValidChar) throw;
                        else continue; //exception expected -> continue
                    }
                }
            }
            return;
        }

        //[Variation(Priority = 1, Desc = "Crate elment/attribute (Inline) :: Invalid start name char", Params = new object[] { false, CharType.NCNameStartChar })]
        [InlineData(false, CharType.NCNameStartChar)]
        //[Variation(Priority = 1, Desc = "Crate elment/attribute (Inline) :: Invalid name char", Params = new object[] { false, CharType.NCNameChar })]
        [InlineData(false, CharType.NCNameChar)]
        //[Variation(Priority = 1, Desc = "Crate elment/attribute (Inline) :: Invalid name CharType.NameStartSurrogateHighChar", Params = new object[] { false, CharType.NameStartSurrogateHighChar })]
        [InlineData(false, CharType.NameStartSurrogateHighChar)]
        //[Variation(Priority = 1, Desc = "Crate elment/attribute (Inline) :: Invalid name CharType.NameStartSurrogateLowChar", Params = new object[] { false, CharType.NameStartSurrogateLowChar })]
        [InlineData(false, CharType.NameStartSurrogateLowChar)]
        //[Variation(Priority = 1, Desc = "Crate elment/attribute (Inline) :: Invalid name CharType.NameSurrogateHighChar", Params = new object[] { false, CharType.NameSurrogateHighChar })]
        [InlineData(false, CharType.NameSurrogateHighChar)]
        //[Variation(Priority = 1, Desc = "Crate elment/attribute (Inline) :: Invalid name CharType.NameSurrogateLowChar", Params = new object[] { false, CharType.NameSurrogateLowChar })]
        [InlineData(false, CharType.NameSurrogateLowChar)]
        // ---
        //[Variation(Priority = 0, Desc = "Crate elment/attribute (Inline) :: Valid start name char", Params = new object[] { true, CharType.NCNameStartChar })]
        [InlineData(true, CharType.NCNameStartChar)]
        //[Variation(Priority = 0, Desc = "Crate elment/attribute (Inline) :: Valid name char", Params = new object[] { true, CharType.NCNameChar })]
        [InlineData(true, CharType.NCNameChar)]
        // Only Valid for Fifth Edition Xml
        //[Variation(Priority = 0, Desc = "Crate elment/attribute (Inline) :: Valid name CharType.NameStartSurrogateHighChar", Params = new object[] { true, CharType.NameStartSurrogateHighChar })]
        //[Variation(Priority = 0, Desc = "Crate elment/attribute (Inline) :: Valid name CharType.NameStartSurrogateLowChar", Params = new object[] { true, CharType.NameStartSurrogateLowChar })]
        //[Variation(Priority = 0, Desc = "Crate elment/attribute (Inline) :: Valid name CharType.NameSurrogateHighChar", Params = new object[] { true, CharType.NameSurrogateHighChar })]
        //[Variation(Priority = 0, Desc = "Crate elment/attribute (Inline) :: Valid name CharType.NameSurrogateLowChar", Params = new object[] { true, CharType.NameSurrogateLowChar })]
        [OuterLoop]
        [Theory]
        public void CreateElementsAndAttributesUsingXsltInline(object param0, object param1)
        {
            int numOfRepeat = 300; // from the test case
            bool isValidChar = (bool)param0;
            CharType charType = (CharType)param1;
            var startChars = new CharType[] { CharType.NameStartChar, CharType.NameStartSurrogateHighChar, CharType.NameStartSurrogateLowChar };

            string charsToChooseFrom = isValidChar ? UnicodeCharHelper.GetValidCharacters(charType) : UnicodeCharHelper.GetInvalidCharacters(charType);
            Assert.True(charsToChooseFrom.Length > 0);

            foreach (bool enableDebug in new bool[] { /*true,*/ false }) // XSLT debugging not supported in Core
            {
                foreach (string name in FuzzNames(!isValidChar, charType, numOfRepeat))
                {
                    XslCompiledTransform transf = new XslCompiledTransform(enableDebug);

                    try
                    {
                        using (XmlReader r = XmlReader.Create(new StringReader(String.Format(createElementsXsltInline, name)))) transf.Load(r);
                        Assert.True(isValidChar);
                    }
                    catch (Exception)
                    {
                        if (isValidChar) throw;
                        else continue; //exception expected -> continue
                    }

                    // if loading of the stylesheet passed, then we should be able to provide
                    StringBuilder sb = new StringBuilder();
                    using (XmlReader r = XmlReader.Create(new StringReader("<root/>"))) transf.Transform(r, null, new StringWriter(sb));
                }
            }
            return;
        }

        private string GenerateStringToInject(string charsToChooseFrom, int position, CharType charType)
        {
            char charToInject = charsToChooseFrom[position];
            switch (charType)
            {
                case CharType.NameStartChar:
                case CharType.NameChar:
                    return new string(charToInject, 1);

                case CharType.NameStartSurrogateHighChar:
                case CharType.NameSurrogateHighChar:
                    return new string(new char[] { charToInject, '\udc00' });

                case CharType.NameStartSurrogateLowChar:
                case CharType.NameSurrogateLowChar:
                    return new string(new char[] { '\udb7f', charToInject });

                default:
                    throw new CTestFailedException("TEST_ISSUE:: CharType not recognized!");
            }
        }

        //[Variation(Priority = 1, Desc = "Invalid start name char", Params = new object[] { false, CharType.NCNameStartChar })]
        [InlineData(false, CharType.NCNameStartChar)]
        //[Variation(Priority = 1, Desc = "Invalid name char", Params = new object[] { false, CharType.NCNameChar })]
        [InlineData(false, CharType.NCNameChar)]
        //[Variation(Priority = 1, Desc = "Invalid name CharType.NameStartSurrogateHighChar", Params = new object[] { false, CharType.NameStartSurrogateHighChar })]
        [InlineData(false, CharType.NameStartSurrogateHighChar)]
        //[Variation(Priority = 1, Desc = "Invalid name CharType.NameStartSurrogateLowChar", Params = new object[] { false, CharType.NameStartSurrogateLowChar })]
        [InlineData(false, CharType.NameStartSurrogateLowChar)]
        //[Variation(Priority = 1, Desc = "Invalid name CharType.NameSurrogateHighChar", Params = new object[] { false, CharType.NameSurrogateHighChar })]
        [InlineData(false, CharType.NameSurrogateHighChar)]
        //[Variation(Priority = 1, Desc = "Invalid name CharType.NameSurrogateLowChar", Params = new object[] { false, CharType.NameSurrogateLowChar })]
        [InlineData(false, CharType.NameSurrogateLowChar)]
        // ---
        //[Variation(Priority = 0, Desc = "Valid start name char", Params = new object[] { true, CharType.NCNameStartChar })]
        [InlineData(true, CharType.NCNameStartChar)]
        //[Variation(Priority = 0, Desc = "Valid name char", Params = new object[] { true, CharType.NCNameChar })]
        [InlineData(true, CharType.NCNameChar)]
        // Only Valid for Fifth Edition Xml
        //[Variation(Priority = 0, Desc = "Valid name CharType.NameStartSurrogateHighChar", Params = new object[] { true, CharType.NameStartSurrogateHighChar })]
        //[Variation(Priority = 0, Desc = "Valid name CharType.NameStartSurrogateLowChar", Params = new object[] { true, CharType.NameStartSurrogateLowChar })]
        //[Variation(Priority = 0, Desc = "Valid name CharType.NameSurrogateHighChar", Params = new object[] { true, CharType.NameSurrogateHighChar })]
        //[Variation(Priority = 0, Desc = "Valid name CharType.NameSurrogateLowChar", Params = new object[] { true, CharType.NameSurrogateLowChar })]
        [Theory]
        public void TestXslTransform(object param0, object param1)
        {
            int numOfRepeat = 300; // from the test case
            bool isValidChar = (bool)param0;
            CharType charType = (CharType)param1;

            foreach (string name in FuzzNames(!isValidChar, charType, numOfRepeat))
            {
                string xsltString = string.Format(xslStylesheetTemplate, name);

                XslCompiledTransform xslt = new XslCompiledTransform();

                try
                {
                    using (XmlReader xr = XmlReader.Create(new StringReader(xsltString)))
                    {
                        xslt.Load(xr, null, new XmlUrlResolver());
                    }
                    Assert.True(isValidChar);
                }
                catch (XsltException)
                {
                    if (isValidChar) throw;
                    else continue; //exception expected -> continue
                }

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(string.Format(xmlDocTemplate, name));

                StringBuilder actualResult = new StringBuilder();

                using (XmlTextWriter xw = new XmlTextWriter(new StringWriter(actualResult)))
                {
                    xslt.Transform(xmlDoc, xw);
                }

                StringBuilder expectedResult = new StringBuilder();
                for (int i = 0; i < xmlDoc.DocumentElement.ChildNodes.Count; i++)
                {
                    expectedResult.Append(name);
                }

                Assert.True(expectedResult.ToString() == actualResult.ToString());
            }

            return;
        }

        private IEnumerable<string> FuzzNames(bool useInvalidCharacters, CharType charType, int namesCount)
        {
            string chars =
                useInvalidCharacters ?
                UnicodeCharHelper.GetInvalidCharacters(charType) :
                UnicodeCharHelper.GetValidCharacters(charType);

            for (int i = 0; i < namesCount; i++)
            {
                yield return GenerateString(chars[_rand.Next(chars.Length)], charType);
            }
        }

        private string GenerateString(char c, CharType charType)
        {
            switch (charType)
            {
                case CharType.NCNameStartChar:
                    return new string(new char[] { c, 'a', 'b' });

                case CharType.NCNameChar:
                    return new string(new char[] { 'a', c, 'b' });

                case CharType.NameStartSurrogateHighChar:
                    return new string(new char[] { c, '\udc00', 'a', 'b' });

                case CharType.NameStartSurrogateLowChar:
                    return new string(new char[] { '\udb7f', c, 'a', 'b' });

                case CharType.NameSurrogateHighChar:
                    return new string(new char[] { 'a', 'b', c, '\udc00' });

                case CharType.NameSurrogateLowChar:
                    return new string(new char[] { 'a', 'b', '\udb7f', c });

                default:
                    throw new CTestFailedException("TEST ISSUE: CharType FAILURE!");
            }
        }

        public new int Init(object objParam)
        {
            return 1;
        }
    }
}