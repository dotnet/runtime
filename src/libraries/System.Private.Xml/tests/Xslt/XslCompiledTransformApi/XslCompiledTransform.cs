// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Text;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public class ReflectionTestCaseBase : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public ReflectionTestCaseBase(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        public static MethodInfo GetInstanceMethod(Type type, string methName)
        {
            MethodInfo methInfo = type.GetMethod(methName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(methInfo != null, "Instance method " + type.Name + "." + methName + " not found");
            return methInfo;
        }

        public static MethodInfo GetStaticMethod(Type type, string methName)
        {
            MethodInfo methInfo = type.GetMethod(methName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(methInfo != null, "Static method " + type.Name + "." + methName + " not found");
            return methInfo;
        }

        protected string scriptTestPath = null;

        protected string ScriptTestPath
        {
            get
            {
                if (scriptTestPath == null) //not thread safe
                {
                    scriptTestPath = _standardTests;

                    if (!scriptTestPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        scriptTestPath += Path.DirectorySeparatorChar;

                    scriptTestPath += "Scripting" + Path.DirectorySeparatorChar;
                }

                return scriptTestPath;
            }
        }

        protected MethodInfo AMethodInfo
        {
            get
            {
                string asmPath = Path.Combine(Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "xsltc", "precompiled"), "bftBaseLine.dll");
                string type = "bftBaseLine";

                Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(asmPath);
                Type t = asm.GetType(type);

                return GetStaticMethod(t, "Execute");
            }
        }

        protected void WLoad(XslCompiledTransform instance, MethodInfo meth, byte[] bytes, Type[] types)
        {
            instance.Load(meth, bytes, types);
        }
    }

    //[TestCase(Name = "Load(MethodInfo, ByteArray, TypeArray) tests", Desc = "This testcase tests private Load method via Reflection. This method is used by sharepoint")]
    public class CLoadMethInfoTest : ReflectionTestCaseBase
    {
        private ITestOutputHelper _output;
        public CLoadMethInfoTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Load(MethodInfo = null, ByteArray, TypeArray)", Pri = 1)]
        [Fact]
        public void Var1()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            try
            {
                WLoad(xslt, (MethodInfo)null, new byte[5], new Type[5]);
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());

                if (e is ArgumentNullException || e.InnerException is ArgumentNullException)
                    return;

                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Load(MethodInfo, ByteArray = null, TypeArray)", Pri = 1)]
        [Fact]
        public void Var2()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            try
            {
                WLoad(xslt, AMethodInfo, null, new Type[5]);
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());

                if (e is ArgumentException || e.InnerException is ArgumentException)
                    return;

                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }
    }

    //[TestCase(Name = "Null argument tests", Desc = "This testcase passes NULL arguments to all XslCompiledTransform methods")]
    public class CNullArgumentTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CNullArgumentTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Load(string = null)", Pri = 1)]
        [Fact]
        public void Var0()
        {
            try
            {
                new XslCompiledTransform().Load((string)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Load(IXPathNavigable = null)", Pri = 1)]
        [Fact]
        public void Var1()
        {
            try
            {
                new XslCompiledTransform().Load((IXPathNavigable)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Load(XmlReader = null)", Pri = 1)]
        [Fact]
        public void Var2()
        {
            try
            {
                new XslCompiledTransform().Load((XmlReader)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Load(IXPathNavigable = null, XmlResolver = null)", Pri = 1)]
        [Fact]
        public void Var3()
        {
            try
            {
                new XslCompiledTransform().Load((IXPathNavigable)null, XsltSettings.TrustedXslt, (XmlResolver)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Load(XmlReader = null, XmlResolver = null)", Pri = 1)]
        [Fact]
        public void Var4()
        {
            try
            {
                new XslCompiledTransform().Load((XmlReader)null, XsltSettings.TrustedXslt, (XmlResolver)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Load(IXPathNavigable = null, XmlResolver = null)", Pri = 1)]
        [Fact]
        public void Var5()
        {
            try
            {
                new XslCompiledTransform().Load((IXPathNavigable)null, XsltSettings.TrustedXslt, (XmlResolver)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Load(XmlReader = null, XmlResolver = null)", Pri = 1)]
        [Fact]
        public void Var6()
        {
            try
            {
                new XslCompiledTransform().Load((XmlReader)null, XsltSettings.TrustedXslt, (XmlResolver)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Transform(IXPathNavigable = null, XsltArgumentList = null)", Pri = 1)]
        [Fact]
        public void Var7()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                StringWriter sw = new StringWriter();
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, sw);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Transform(IXPathNavigable = null, XsltArgumentList = null)", Pri = 1)]
        [Fact]
        public void Var8()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                StringWriter sw = new StringWriter();
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, sw);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Transform(IXPathNavigable = null, XsltArgumentList = null, TextWriter = null)", Pri = 1)]
        [Fact]
        public void Var9()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, (TextWriter)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Transform(IXPathNavigable = null, XsltArgumentList = null, TextWriter = null)", Pri = 1)]
        [Fact]
        public void Var10()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, (TextWriter)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Transform(IXPathNavigable = null, XsltArgumentList = null, Stream = null)", Pri = 1)]
        [Fact]
        public void Var11()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, (Stream)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Transform(IXPathNavigable = null, XsltArgumentList = null, Stream = null)", Pri = 1)]
        [Fact]
        public void Var12()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, (Stream)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Transform(IXPathNavigable = null, XsltArgumentList = null, XmlWriter = null)", Pri = 1)]
        [Fact]
        public void Var13()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, (XmlWriter)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }

        //[Variation("Transform(IXPathNavigable = null, XsltArgumentList = null, XmlWriter = null)", Pri = 1)]
        [Fact]
        public void Var14()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, (XmlWriter)null);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Did not throw ArgumentNullException");
            }
            Assert.True(false);
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Resolver - Integrity              */
    /***********************************************************/

    //[TestCase(Name = "XslCompiledTransform.XmlResolver : Reader, Reader", Desc = "READER,READER")]
    //[TestCase(Name = "XslCompiledTransform.XmlResolver : Reader, Stream", Desc = "READER,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.XmlResolver : Reader, Writer", Desc = "READER,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.XmlResolver : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    //[TestCase(Name = "XslCompiledTransform.XmlResolver : URI, Reader", Desc = "URI,READER")]
    //[TestCase(Name = "XslCompiledTransform.XmlResolver : URI, Stream", Desc = "URI,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.XmlResolver : URI, Writer", Desc = "URI,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.XmlResolver : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    //[TestCase(Name = "XslCompiledTransform.XmlResolver : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    //[TestCase(Name = "XslCompiledTransform.XmlResolver : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.XmlResolver : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.XmlResolver : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CXmlResolverTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CXmlResolverTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }
        //[Variation(id = 1, Desc = "Set XmlResolver property to null, load style sheet with import/include, should not affect transform")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void XmlResolver1(XslInputType xslInputType, ReaderType readerType)
        {
            try
            {
                LoadXSL("XmlResolver_Main.xsl", xslInputType, readerType, null);
                Assert.True(false);
            }
            catch (XsltException e1)
            {
                _output.WriteLine(e1.Message);
                return;
            }
            catch (ArgumentNullException e2)
            {
                _output.WriteLine(e2.Message);
                if (xslInputType == XslInputType.URI)
                    return;
                else
                {
                    _output.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + xslInputType + "'");
                    Assert.True(false);
                }
            }
        }

        //[Variation(id = 2, Desc = "Set XmlResolver property to null, load style sheet with document function, should not resolve during load")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void XmlResolver2(XslInputType xslInputType, ReaderType readerType)
        {
            try
            {
                LoadXSL("XmlResolver_Main.xsl", xslInputType, readerType, null);
                _output.WriteLine("No exception was thrown");
                Assert.True(false);
            }
            catch (XsltException e1)
            {
                _output.WriteLine(e1.Message);
                return;
            }
            catch (ArgumentNullException e2)
            {
                _output.WriteLine(e2.Message);
                if (xslInputType == XslInputType.URI)
                    return;
                else
                {
                    _output.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + xslInputType + "'");
                    Assert.True(false);
                }
            }
        }

        //[Variation(id = 3, Desc = "Default XmlResolver, load style sheet with document function, should resolve during transform", Pri = 1, Param = "DefaultResolver.txt")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51911", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        [InlineData("DefaultResolver.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void XmlResolver3(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            AppContext.SetSwitch("Switch.System.Xml.AllowDefaultResolver", true);

            string Baseline = Path.Combine("baseline", (string)param);

            if (LoadXSL("xmlResolver_document_function.xsl", xslInputType, readerType) == 1)
            {
                if (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1)
                {
                    VerifyResult(Baseline, _strOutFile);
                    return;
                }
            }
            else
            {
                _output.WriteLine("Problem loading stylesheet with document function and default resolver!");
                Assert.True(false);
            }
            Assert.True(false);
        }

        //[Variation(id = 7, Desc = "document() has absolute URI", Pri = 0)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void XmlResolver7(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            AppContext.SetSwitch("Switch.System.Xml.AllowDefaultResolver", true);

            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>123</result>";
            string fileName = GetType().Name + "_" + Path.GetRandomFileName();
            string testFile = Path.Combine(Path.GetTempPath(), fileName);
            string xmlFile = FullFilePath(fileName);

            // copy file on the local machine
            try
            {
                File.Copy(xmlFile, testFile, true);
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Could not copy file to local. Some other issues prevented this test from running");
                return; //TEST_SKIPPED;
            }
            finally
            {
                if (File.Exists(testFile))
                {
                    File.SetAttributes(testFile, FileAttributes.Normal);
                    File.Delete(testFile);
                }
            }

            // copy file on the local machine (this is now done with createAPItestfiles.js, see Oasys scenario.)
            if (LoadXSL("xmlResolver_document_function_absolute_uri.xsl", xslInputType, readerType) == 1)
            {
                if (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1)
                {
                    VerifyResult(expected);
                    return;
                }
                else
                {
                    _output.WriteLine("Failed to resolve document function with absolute URI.");
                    Assert.True(false);
                }
            }
            else
            {
                _output.WriteLine("Failed to load style sheet!");
                Assert.True(false);
            }
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Load - Integrity                  */
    /***********************************************************/

    //[TestCase(Name = "XslCompiledTransform.Load() - Integrity : Reader, Reader", Desc = "READER,READER")]
    //[TestCase(Name = "XslCompiledTransform.Load() - Integrity : Reader, Stream", Desc = "READER,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Load() - Integrity : Reader, Writer", Desc = "READER,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Load() - Integrity : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    //[TestCase(Name = "XslCompiledTransform.Load() - Integrity : URI, Reader", Desc = "URI,READER")]
    //[TestCase(Name = "XslCompiledTransform.Load() - Integrity : URI, Stream", Desc = "URI,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Load() - Integrity : URI, Writer", Desc = "URI,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Load() - Integrity : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    //[TestCase(Name = "XslCompiledTransform.Load() - Integrity : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    //[TestCase(Name = "XslCompiledTransform.Load() - Integrity : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Load() - Integrity : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Load() - Integrity : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CLoadTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CLoadTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation(id = 1, Desc = "Call Load with null value")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadGeneric1(XslInputType xslInputType, ReaderType readerType)
        {
            try
            {
                LoadXSL(null, xslInputType, readerType);
            }
            catch (System.ArgumentException)
            {
                return;
            }
            _output.WriteLine("Exception not generated for null parameter name");
            Assert.True(false);
        }

        //[Variation(id = 2, Desc = "Load with valid, then invalid, then valid again")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadGeneric2(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                try
                {
                    LoadXSL("IDontExist.xsl", xslInputType, readerType);
                }
                catch (System.IO.FileNotFoundException)
                {
                    try
                    {
                        Transform((string) "fruits.xml", (OutputType) outputType, navType);
                    }
                    catch (System.InvalidOperationException e)
                    {
                        CheckExpectedError(e, "System.xml", "Xslt_NoStylesheetLoaded", new string[] { "" });
                        return;
                    }
                }
            }
            else
            {
                _output.WriteLine("Failed to load style sheet!");
                Assert.True(false);
            }
            Assert.True(false);
        }

        //[Variation(id = 3, Desc = "Load an invalid, then a valid and transform", Param = "showParam.txt")]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadGeneric3(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            try
            {
                LoadXSL("IDontExist.xsl", xslInputType, readerType);
            }
            catch (System.IO.FileNotFoundException)
            {
                if ((LoadXSL("showParam.xsl", xslInputType, readerType) == 1) && (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1))
                {
                    VerifyResult(Baseline, _strOutFile);
                    return;
                }
            }
            _output.WriteLine("Exception not generated for non-existent file name");
            Assert.True(false);
        }

        //[Variation(id = 4, Desc = "Call several overloaded functions", Param = "showParam.txt")]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadGeneric4(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            if (xslInputType != XslInputType.Reader)
                LoadXSL("showParamLongName.xsl", XslInputType.Reader, readerType, new XmlUrlResolver());
            if (xslInputType != XslInputType.URI)
                LoadXSL("showParamLongName.xsl", XslInputType.URI, readerType, new XmlUrlResolver());
            if (xslInputType != XslInputType.Navigator)
                LoadXSL("showParamLongName.xsl", XslInputType.Navigator, readerType, new XmlUrlResolver());

            if ((LoadXSL("showParam.xsl", xslInputType, readerType) == 0) || (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 0))
                Assert.True(false);

            VerifyResult(Baseline, _strOutFile);

            if (xslInputType != XslInputType.Navigator)
                LoadXSL("showParamLongName.xsl", XslInputType.Navigator, readerType, new XmlUrlResolver());
            if (xslInputType != XslInputType.URI)
                LoadXSL("showParamLongName.xsl", XslInputType.URI, readerType, new XmlUrlResolver());
            if (xslInputType != XslInputType.Reader)
                LoadXSL("showParamLongName.xsl", XslInputType.Reader, readerType, new XmlUrlResolver());

            if ((LoadXSL("showParam.xsl", xslInputType, readerType) == 1) && (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1))
            {
                VerifyResult(Baseline, _strOutFile);
                return;
            }

            Assert.True(false);
        }

        //[Variation(id = 5, Desc = "Call same overloaded Load() many times then transform", Param = "showParam.txt")]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadGeneric5(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            for (int i = 0; i < 100; i++)
            {
                if (LoadXSL("showParam.xsl", xslInputType, readerType) != 1)
                {
                    _output.WriteLine("Failed to load stylesheet showParam.xsl on the {0} attempt", i);
                    Assert.True(false);
                }
            }
            if ((LoadXSL("showParam.xsl", xslInputType, readerType) == 1) && (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1))
            {
                VerifyResult(Baseline, _strOutFile);
                return;
            }

            Assert.True(false);
        }

        //[Variation(id = 6, Desc = "Call load with non-existing stylesheet")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadGeneric6(XslInputType xslInputType, ReaderType readerType)
        {
            try
            {
                LoadXSL("IDontExist.xsl", xslInputType, readerType);
            }
            catch (System.IO.FileNotFoundException)
            {
                return;
            }
            _output.WriteLine("Exception not generated for non-existent file parameter name");
            Assert.True(false);
        }

        //[Variation(id = 7, Desc = "Verify that style sheet is closed properly after Load - Shared Read Access")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadGeneric7(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            FileStream s2;
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result><fruit>Apple</fruit><fruit>orange</fruit></result>";

            // check immediately after load and after transform
            if (LoadXSL("XmlResolver_Main.xsl", xslInputType, readerType) == 1)
            {
                s2 = new FileStream(FullFilePath("XmlResolver_Main.xsl"), FileMode.Open, FileAccess.Read, FileShare.Read);
                s2.Dispose();
                if (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1)
                {
                    VerifyResult(expected);
                    s2 = new FileStream(FullFilePath("XmlResolver_Main.xsl"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    s2.Dispose();
                    return;
                }
            }
            Assert.True(false);
        }

        //[Variation(id = 9, Desc = "Verify that included files are closed properly after Load - Read Access")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadGeneric9(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            FileStream s2;
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result><fruit>Apple</fruit><fruit>orange</fruit></result>";

            // check immediately after load and after transform
            if (LoadXSL("XmlResolver_Main.xsl", xslInputType, readerType) == 1)
            {
                s2 = new FileStream(FullFilePath("XmlResolver_Sub.xsl"), FileMode.Open, FileAccess.Read);
                s2.Dispose();
                if (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1)
                {
                    VerifyResult(expected);
                    s2 = new FileStream(FullFilePath("XmlResolver_Include.xsl"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    s2.Dispose();
                    return;
                }
            }
            _output.WriteLine("Appeared to not close file properly after loading.");
            Assert.True(false);
        }

        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadGeneric11(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            if (navType.ToString() == "DataDocument")
                // Skip the test for DataDocument
                return;
            else
            {
                string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><Book>
			Name
		</Book>";

                if (LoadXSL("books_entity_ref.xsl", XslInputType.Reader, readerType, new XmlUrlResolver()) != 1)
                {
                    _output.WriteLine("Failed to load stylesheet books_entity_ref.xsl");
                    Assert.True(false);
                }
                if ((LoadXSL("books_entity_ref.xsl", xslInputType, readerType) == 1) && (Transform((string) "books_entity_ref.xml", (OutputType) outputType, navType) == 1))
                {
                    VerifyResult(expected);
                    return;
                }
                Assert.True(false);
            }
        }

        //[Variation(id = 12, Desc = "Load with invalid stylesheet and verify that file is closed properly")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadGeneric12(XslInputType xslInputType, ReaderType readerType)
        {
            Stream strmTemp;

            try
            {
                int i = LoadXSL("xslt_error.xsl", xslInputType, readerType);
            }
            catch (XsltException)
            {
                // Try to open the xsl file
                try
                {
                    strmTemp = new FileStream(FullFilePath("xslt_error.xsl"), FileMode.Open, FileAccess.Read);
                }
                catch (Exception ex)
                {
                    _output.WriteLine("Did not close stylesheet properly after load");
                    _output.WriteLine(ex.Message);
                    Assert.True(false);
                }
                return;
            }
            _output.WriteLine("Did not throw compile exception for stylesheet");
            Assert.True(false);
        }
    }

    /**************************************************************************/
    /*          XslCompiledTransform.Load(XmlResolver) - Integrity   */
    /**************************************************************************/

    //[TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Reader, Reader", Desc = "READER,READER")]
    //[TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Reader, Stream", Desc = "READER,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Reader, Writer", Desc = "READER,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    //[TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    //[TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    //[TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : URI, Reader", Desc = "URI,READER")]
    //[TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : URI, Stream", Desc = "URI,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : URI, Writer", Desc = "URI,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    public class CLoadXmlResolverTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CLoadXmlResolverTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Call Load with null source value and null resolver")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadGeneric1(XslInputType xslInputType, ReaderType readerType)
        {
            try
            {
                LoadXSL_Resolver(null, xslInputType, readerType, null);
            }
            catch (System.ArgumentNullException e)
            {
                _output.WriteLine(e.ToString());
                return;
            }
            _output.WriteLine("Passing null argument should have thrown ArgumentNullException");
            Assert.True(false);
        }

        //[Variation("Call Load with null source value and valid resolver")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadGeneric2(XslInputType xslInputType, ReaderType readerType)
        {
            try
            {
                LoadXSL_Resolver(null, xslInputType, readerType, new XmlUrlResolver());
            }
            catch (System.ArgumentNullException e)
            {
                _output.WriteLine(e.ToString());
                return;
            }
            _output.WriteLine("Passing null stylesheet should have thrown ArgumentNullException");
            Assert.True(false);
        }

        //[Variation("Call Load with null XmlResolver, style sheet does not have include/import, URI should throw ArgumentNullException and the rest shouldn't error", Param = "showParam.txt")]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadGeneric3(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            try
            {
                LoadXSL_Resolver("showParam.xsl", xslInputType, readerType, null);
                Transform((string) "fruits.xml", (OutputType) outputType, navType);
                VerifyResult(Baseline, _strOutFile);
                return;
            }
            catch (ArgumentNullException e)
            {
                _output.WriteLine(e.ToString());
                if (xslInputType == XslInputType.URI)
                    return;
                else
                {
                    _output.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + xslInputType + "'");
                    Assert.True(false);
                }
            }
        }

        //[Variation("Call Load with null XmlResolver and stylesheet has import/include, URI should throw ArgumentNullException, rest throw XsltException")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadGeneric4(XslInputType xslInputType, ReaderType readerType)
        {
            try
            {
                LoadXSL_Resolver("XmlResolver_Main.xsl", xslInputType, readerType, null);
                _output.WriteLine("No exception was thrown when a null resolver is passed");
                Assert.True(false);
            }
            catch (XsltException e1)
            {
                _output.WriteLine(e1.Message);
                return;
            }
            catch (ArgumentNullException e2)
            {
                _output.WriteLine(e2.Message);
                if (xslInputType == XslInputType.URI)
                    return;
                else
                {
                    _output.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + xslInputType + "'");
                    Assert.True(false);
                }
            }
        }

        //[Variation("Call Load with null custom resolver and style sheet has import/include, should throw Exception")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadGeneric5(XslInputType xslInputType, ReaderType readerType)
        {
            CustomNullResolver myResolver = new CustomNullResolver(_output);

            try
            {
                LoadXSL_Resolver("XmlResolver_Main.xsl", xslInputType, readerType, myResolver);
                _output.WriteLine("No exception is thrown");
                Assert.True(false);
            }
            catch (XsltException e1)
            {
                _output.WriteLine(e1.Message);
                return;
            }
            catch (ArgumentNullException e2)
            {
                _output.WriteLine(e2.Message);
                if (xslInputType == XslInputType.URI)
                    return;
                else
                {
                    _output.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + xslInputType + "'");
                    Assert.True(false);
                }
            }
            catch (XmlException e3)
            {
                _output.WriteLine(e3.Message);
                if (xslInputType == XslInputType.URI)
                    return;
                else
                {
                    _output.WriteLine("XmlException is not supposed to be thrown for the input type '" + xslInputType + "'");
                    Assert.True(false);
                }
            }
        }

        //[Variation("Call Load with null custom resolver and style sheet has no import/include, should error for URI only", Param = "ShowParam.txt")]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadGeneric6(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            CustomNullResolver myResolver = new CustomNullResolver(_output);
            try
            {
                LoadXSL_Resolver("showParam.xsl", xslInputType, readerType, myResolver);
                Transform((string) "fruits.xml", (OutputType) outputType, navType);
                VerifyResult(Baseline, _strOutFile);
                return;
            }
            catch (ArgumentNullException e)
            {
                _output.WriteLine(e.ToString());
                if (xslInputType == XslInputType.URI)
                    return;
                else
                {
                    _output.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + xslInputType + "'");
                    Assert.True(false);
                }
            }
            catch (XsltException e3)
            {
                _output.WriteLine(e3.Message);
                if (xslInputType == XslInputType.URI)
                    return;
                else
                {
                    _output.WriteLine("XmlException is not supposed to be thrown for the input type '" + xslInputType + "'");
                    Assert.True(false);
                }
            }
        }

        //[Variation("Style sheet has import/include, call Load first with custom null resolver and then default resolver, should not fail", Param = "XmlResolverTestMain.txt")]
        [InlineData("XmlResolverTestMain.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("XmlResolverTestMain.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("XmlResolverTestMain.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("XmlResolverTestMain.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("XmlResolverTestMain.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("XmlResolverTestMain.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("XmlResolverTestMain.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("XmlResolverTestMain.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("XmlResolverTestMain.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadGeneric7(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result><fruit>Apple</fruit><fruit>orange</fruit></result>";

            string Baseline = Path.Combine("baseline", (string)param);
            CustomNullResolver myResolver = new CustomNullResolver(_output);

            try
            {
                LoadXSL_Resolver("XmlResolver_Main.xsl", xslInputType, readerType, myResolver);
            }
            //For input types != URI
            catch (System.Xml.Xsl.XsltException e1)
            {
                // The lovely thing about this test is that the stylesheet, XmlResolver_Main.xsl, has an include. GetEntity is therefore called twice. We need to have both these
                // checks here to ensure that both the XmlResolver_Main.xsl and XmlResolver_Include.xsl GetEntity() calls are handled.
                try
                {
                    CheckExpectedError(e1, "System.Xml", "Xslt_CannotLoadStylesheet", new string[] { new Uri(Uri.UriSchemeFile + Uri.SchemeDelimiter + Path.GetFullPath(FullFilePath("XmlResolver_Include.xsl"))).ToString(), "null" });
                }
                catch (Xunit.Sdk.TrueException)
                {
                    CheckExpectedError(e1, "System.Xml", "Xslt_CannotLoadStylesheet", new string[] { new Uri(Uri.UriSchemeFile + Uri.SchemeDelimiter + Path.GetFullPath(FullFilePath("XmlResolver_Main.xsl"))).ToString(), "null" });
                }

                if (LoadXSL("XmlResolver_Main.xsl", xslInputType, readerType) == 1)
                {
                    if (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1)
                    {
                        VerifyResult(expected);
                        return;
                    }
                    else
                        Assert.True(false);
                }
                else
                {
                    _output.WriteLine("Failed to load stylesheet using default resolver");
                    Assert.True(false);
                }
            }

            //For URI
            catch (System.ArgumentNullException e2)
            {
                try
                {
                    CheckExpectedError(e2, "System.Xml", "Xslt_CannotLoadStylesheet", new string[] { new Uri(Uri.UriSchemeFile + Uri.SchemeDelimiter + Path.GetFullPath(FullFilePath("XmlResolver_Include.xsl"))).ToString(), "null" });
                }
                catch (Xunit.Sdk.TrueException)
                {
                    CheckExpectedError(e2, "System.Xml", "Xslt_CannotLoadStylesheet", new string[] { new Uri(Uri.UriSchemeFile + Uri.SchemeDelimiter + Path.GetFullPath(FullFilePath("XmlResolver_Main.xsl"))).ToString(), "null" });
                }

                if (LoadXSL("XmlResolver_Main.xsl", xslInputType, readerType) == 1)
                {
                    if (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1)
                    {
                        VerifyResult(Baseline, _strOutFile);
                        return;
                    }
                    else
                        Assert.True(false);
                }
                else
                {
                    _output.WriteLine("Failed to load stylesheet using default resolver");
                    Assert.True(false);
                }
            }
            catch (XmlException e3)
            {
                _output.WriteLine(e3.Message);
                if (xslInputType == XslInputType.URI)
                    return;
                else
                {
                    _output.WriteLine("XmlException is not supposed to be thrown for the input type '" + xslInputType + "'");
                    Assert.True(false);
                }
            }

            Assert.True(false);
        }

        //[Variation("Style sheet has import/include, call Load first with default resolver and then with custom null resolver, should fail", Param = "XmlResolverTestMain.txt")]
        [InlineData("XmlResolverTestMain.txt", XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData("XmlResolverTestMain.txt", XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData("XmlResolverTestMain.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadGeneric8(object param, XslInputType xslInputType, ReaderType readerType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            CustomNullResolver myResolver = new CustomNullResolver(_output);

            if ((LoadXSL("XmlResolver_Main.xsl", xslInputType, readerType) == 1))
            {
                try
                {
                    LoadXSL_Resolver("XmlResolver_Main.xsl", xslInputType, readerType, myResolver);
                }
                catch (System.Xml.Xsl.XsltException e1)
                {
                    // The lovely thing about this test is that the stylesheet, XmlResolver_Main.xsl, has an include. GetEntity is therefore called twice. We need to have both these
                    // checks here to ensure that both the XmlResolver_Main.xsl and XmlResolver_Include.xsl GetEntity() calls are handled.
                    // Yes, this is effetively the same test as LoadGeneric7, in that we use the NullResolver to return null from a GetEntity call.
                    try
                    {
                        CheckExpectedError(e1, "System.Xml", "Xslt_CannotLoadStylesheet", new string[] { new Uri(Uri.UriSchemeFile + Uri.SchemeDelimiter + Path.GetFullPath(FullFilePath("XmlResolver_Include.xsl"))).ToString(), "null" });
                        return;
                    }
                    catch (Xunit.Sdk.TrueException)
                    {
                        CheckExpectedError(e1, "System.Xml", "Xslt_CannotLoadStylesheet", new string[] { new Uri(Uri.UriSchemeFile + Uri.SchemeDelimiter + Path.GetFullPath(FullFilePath("XmlResolver_Main.xsl"))).ToString(), "null" });
                        return;
                    }
                }
                catch (ArgumentNullException e2)
                {
                    _output.WriteLine(e2.Message);
                    if (xslInputType == XslInputType.URI)
                        return;
                    else
                    {
                        _output.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + xslInputType + "'");
                        Assert.True(false);
                    }
                }
                catch (XmlException e3)
                {
                    _output.WriteLine(e3.Message);
                    if (xslInputType == XslInputType.URI)
                        return;
                    else
                    {
                        _output.WriteLine("XmlException is not supposed to be thrown for the input type '" + xslInputType + "'");
                        Assert.True(false);
                    }
                }
                _output.WriteLine("No exception generated when loading with an invalid resolver after loading with valid resolver");
                Assert.True(false);
            }
            _output.WriteLine("Could not load style sheet with default resolver");
            Assert.True(false);
        }

        //[Variation("Load with resolver with credentials, then load XSL that does not need cred.")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadGeneric9(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result><fruit>Apple</fruit><fruit>orange</fruit></result>";

            if ((LoadXSL_Resolver("XmlResolver_Main.xsl", xslInputType, readerType, GetDefaultCredResolver()) == 1))
            {
                if ((LoadXSL("XmlResolver_Main.xsl", xslInputType, readerType) == 1) && (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1))
                {
                    VerifyResult(expected);
                    return;
                }
            }
            else
            {
                _output.WriteLine("Failed to load!");
                Assert.True(false);
            }
            Assert.True(false);
        }

        //[Variation("Call Load with null Resolver, file does not exist")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadGeneric11(XslInputType xslInputType, ReaderType readerType)
        {
            try
            {
                LoadXSL_Resolver("IDontExist.xsl", xslInputType, readerType, null);
                _output.WriteLine("No exception was thrown");
                Assert.True(false);
            }
            catch (FileNotFoundException e1)
            {
                _output.WriteLine(e1.Message);
                return;
            }
            catch (ArgumentNullException e2)
            {
                _output.WriteLine(e2.Message);
                if (xslInputType == XslInputType.URI)
                    return;
                else
                {
                    _output.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + xslInputType + "'");
                    Assert.True(false);
                }
            }
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Load(Url, Resolver)               */
    /***********************************************************/

    //[TestCase(Name = "XslCompiledTransform.Load(Url, Resolver) : URI, Reader", Desc = "URI,READER")]
    //[TestCase(Name = "XslCompiledTransform.Load(Url, Resolver) : URI, Stream", Desc = "URI,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Load(Url, Resolver) : URI, Writer", Desc = "URI,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Load(Url, Resolver) : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    public class CLoadUrlResolverTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CLoadUrlResolverTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Basic check for usage of credentials on resolver, load XSL that needs cred. with correct resolver", Param = "XmlResolverTestMain.txt")]
        [InlineData("XmlResolverTestMain.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("XmlResolverTestMain.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("XmlResolverTestMain.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadUrlResolver1(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            // XsltResolverTestMain.xsl is placed in IIS virtual directory
            // which requires integrated Windows NT authentication
            string Baseline = Path.Combine("baseline", (string)param);
            if ((LoadXSL_Resolver(Path.Combine("XmlResolver", "XmlResolverTestMain.xsl"), xslInputType, readerType, GetDefaultCredResolver()) == 1) &&
                (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1))
            {
                VerifyResult(Baseline, _strOutFile);
                return;
            }

            Assert.True(false);
        }

        //[Variation("Load XSL that needs cred. with null resolver, should fail", Param = "XmlResolverTestMain.txt")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadUrlResolver2(XslInputType xslInputType, ReaderType readerType)
        {
            try
            {
                LoadXSL_Resolver(Path.Combine("XmlResolver", "XmlResolverTestMain.xsl"), xslInputType, readerType, null);
            }
            catch (XsltException e)
            {
                _output.WriteLine(e.ToString());
                return;
            }
            _output.WriteLine("Passing null resolver should have thrown XsltException");
            Assert.True(false);
        }

        //[Variation("Call Load with null source value")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadUrlResolver3(XslInputType xslInputType, ReaderType readerType)
        {
            try
            {
                LoadXSL_Resolver(null, xslInputType, readerType, new XmlUrlResolver());
            }
            catch (ArgumentNullException e)
            {
                _output.WriteLine(e.ToString());
                return;
            }
            _output.WriteLine("Passing null stylesheet parameter should have thrown ArgumentNullException");
            Assert.True(false);
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Load(Url)                         */
    /***********************************************************/

    //[TestCase(Name = "XslCompiledTransform.Load(Url) Integrity : URI, Stream", Desc = "URI,STREAM")]
    public class CLoadStringTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CLoadStringTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Call Load with an invalid uri")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadUrl1(XslInputType xslInputType, ReaderType readerType)
        {
            try
            {
                LoadXSL("IDontExist.xsl", XslInputType.URI, readerType, new XmlUrlResolver());
                _output.WriteLine("No exception was thrown");
                Assert.True(false);
            }
            catch (FileNotFoundException e1)
            {
                _output.WriteLine(e1.Message);
                if (xslInputType == XslInputType.URI)
                    return;
                else
                {
                    _output.WriteLine("FileNotFoundException is supposed to be thrown");
                    Assert.True(false);
                }
            }
            catch (ArgumentNullException e2)
            {
                _output.WriteLine(e2.Message);
                if (xslInputType == XslInputType.URI)
                    return;
                else
                {
                    _output.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + xslInputType + "'");
                    Assert.True(false);
                }
            }
        }

        //[Variation("Load file with empty string")]
        [InlineData(ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadUrl2(ReaderType readerType)
        {
            try
            {
                LoadXSL(szEmpty, XslInputType.URI, readerType, new XmlUrlResolver());
            }
            catch (System.ArgumentException)
            {
                return;
            }
            _output.WriteLine("Exception not generated for an empty string filename");
            Assert.True(false);
        }

        //[Variation("Load with \".\"")]
        [InlineData(ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadUrl3(ReaderType readerType)
        {
            try
            {
                LoadXSL(".", XslInputType.URI, readerType, new XmlUrlResolver());
            }
            catch (System.UnauthorizedAccessException)
            {
                return;
            }
            _output.WriteLine("Exception not generated for non-existent file parameter name");
            Assert.True(false);
        }

        //[Variation("Load with \"..\"")]
        [InlineData(ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadUrl(ReaderType readerType)
        {
            try
            {
                LoadXSL("..", XslInputType.URI, readerType, new XmlUrlResolver());
            }
            catch (System.UnauthorizedAccessException)
            {
                return;
            }
            _output.WriteLine("Exception not generated for non-existent file parameter name");
            Assert.True(false);
        }

        //[Variation("Load with \"\\\\\"")]
        [PlatformSpecific(TestPlatforms.Windows)] //Not an invalid path on Unix
        [InlineData(ReaderType.XmlValidatingReader)]
        [Theory]
        public void LoadUrl5(ReaderType readerType)
        {
            try
            {
                LoadXSL("    ", XslInputType.URI, readerType, new XmlUrlResolver());
            }
            catch (System.ArgumentException)
            {
                return;
            }
            _output.WriteLine("Exception not generated for non-existent file parameter name");
            Assert.True(false);
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Load(IXPathNavigable)             */
    /***********************************************************/

    //[TestCase(Name = "XslCompiledTransform .Load(IXPathNavigable) : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CLoadXPathNavigableTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CLoadXPathNavigableTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Basic Verification Test", Param = "showParam.txt")]
        [InlineData("showParam.txt", OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadNavigator1(object param, OutputType outputType, NavType navType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            xslt = new XslCompiledTransform();
            string _strXslFile = "showParam.xsl";

            _strXslFile = FullFilePath(_strXslFile);
            _output.WriteLine("Compiling {0}", _strXslFile);

            XmlReader xrLoad = XmlReader.Create(_strXslFile);
            XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
            xrLoad.Dispose();
            xslt.Load(xdTemp);

            if (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1)
            {
                VerifyResult(Baseline, _strOutFile);
                return;
            }
            Assert.True(false);
        }

        //[Variation("Create Navigator and navigate away from root", Param = "showParam.txt")]
        [InlineData("showParam.txt", OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadNavigator2(object param, OutputType outputType, NavType navType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            xslt = new XslCompiledTransform();
            XmlReader xrLoad = XmlReader.Create(FullFilePath("showParam.xsl"));
            XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
            xrLoad.Dispose();
            XPathNavigator xP = ((IXPathNavigable)xdTemp).CreateNavigator();

            xP.MoveToNext();
            xslt.Load(xP);

            if (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1)
            {
                VerifyResult(Baseline, _strOutFile);
                return;
            }
            Assert.True(false);
        }

        //[Variation("Basic check for usage of credentials on resolver, load XSL that needs cred. with correct resolver", Param = "XmlResolverTestMain.txt")]
        [InlineData("XmlResolverTestMain.txt", OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void LoadNavigator3(object param, OutputType outputType, NavType navType)
        {
            xslt = new XslCompiledTransform();
            string Baseline = Path.Combine("baseline", (string)param);
            XmlReader xrLoad = XmlReader.Create(FullFilePath(Path.Combine("XmlResolver", "XmlResolverTestMain.xsl")));

            XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
            XPathNavigator xP = ((IXPathNavigable)xdTemp).CreateNavigator();

            xslt.Load(xP, XsltSettings.TrustedXslt, GetDefaultCredResolver());
            if (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1)
            {
                VerifyResult(Baseline, _strOutFile);
                return;
            }
            Assert.True(false);
        }

        //[Variation("Regression case for bug 80768")]
        [Fact]
        public void LoadNavigator4()
        {
            var e = Assert.ThrowsAny<XsltException>(() =>
            {
                string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><out>You are safe</out>";

                xslt = new XslCompiledTransform();
                XmlReader xrLoad = XmlReader.Create(FullFilePath("Bug80768.xsl"));
                XPathDocument xd = new XPathDocument(xrLoad, XmlSpace.Preserve);

                xslt.Load(xd, XsltSettings.TrustedXslt, new XmlUrlResolver());

                FileStream fs = new FileStream(_strOutFile, FileMode.Create, FileAccess.ReadWrite);
                XPathNavigator xn = new MyNavigator(FullFilePath("foo.xml"));
                xslt.Transform(xn, null, fs);
                fs.Dispose();

                VerifyResult(expected);
            });

            Assert.Equal("Compiling JScript/CSharp scripts is not supported", e.InnerException.Message);
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Load(Reader)                      */
    /***********************************************************/

    //[TestCase(Name = "XslCompiledTransform.Load(Reader) : Reader, Stream", Desc = "READER,STREAM")]
    public class CLoadReaderTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CLoadReaderTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Basic Verification Test", Param = "showParam.txt")]
        [InlineData("showParam.txt", OutputType.Stream, NavType.XPathDocument)]
        [Theory]
        public void LoadXmlReader1(object param, OutputType outputType, NavType navType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            bool fTEST_FAIL = false;
            xslt = new XslCompiledTransform();

            XmlReaderSettings xrs = new XmlReaderSettings();
#pragma warning disable 0618
            xrs.ProhibitDtd = false;
#pragma warning restore 0618
            XmlReader xrTemp = XmlReader.Create(FullFilePath("showParam.xsl"));

            try
            {
                xslt.Load(xrTemp);
            }
            catch (Exception ex)
            {
                fTEST_FAIL = true;
                throw (ex);
            }
            finally
            {
                xrTemp.Dispose();
            }
            if (fTEST_FAIL)
                Assert.True(false);
            if (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1)
            {
                VerifyResult(Baseline, _strOutFile);
                return;
            }
            Assert.True(false);
        }

        //[Variation("Calling with a closed reader, should throw exception")]
        [Fact]
        public void LoadXmlReader2()
        {
            xslt = new XslCompiledTransform();

            XmlReaderSettings xrs = new XmlReaderSettings();
#pragma warning disable 0618
            xrs.ProhibitDtd = false;
#pragma warning restore 0618
            XmlReader xrTemp = XmlReader.Create(FullFilePath("showParam.xsl"), xrs);
            xrTemp.Dispose();

            try
            {
                xslt.Load(xrTemp);
            }
            catch (System.Xml.Xsl.XsltException e)
            {
                CheckExpectedError(e, "system.xml", "Xslt_WrongStylesheetElement", new string[] { "" });
                return;
            }
            _output.WriteLine("No exception thrown for a loading a closed reader!");
            Assert.True(false);
        }

        //[Variation("Verify Reader isn't closed after Load")]
        [Fact]
        public void LoadXmlReader3()
        {
            bool fTEST_FAIL = false;
            xslt = new XslCompiledTransform();

            XmlReaderSettings xrs = new XmlReaderSettings();
#pragma warning disable 0618
            xrs.ProhibitDtd = false;
#pragma warning restore 0618
            XmlReader xrTemp = XmlReader.Create(FullFilePath("showParam.xsl"), xrs);

            try
            {
                xslt.Load(xrTemp);
            }
            catch (Exception ex)
            {
                fTEST_FAIL = true;
                throw (ex);
            }
            finally
            {
                if (!fTEST_FAIL || (xrTemp.ReadState != ReadState.Closed))
                    fTEST_FAIL = false;
                xrTemp.Dispose();
            }
            if (fTEST_FAIL)
            {
                _output.WriteLine("Appear to have accidentally closed the Reader");
                Assert.True(false);
            }
            return;
        }

        //[Variation("Verify position of node in Reader is at EOF after Load")]
        [Fact]
        public void LoadXmlReader4()
        {
            bool fTEST_FAIL = false;
            xslt = new XslCompiledTransform();

            XmlReaderSettings xrs = new XmlReaderSettings();
#pragma warning disable 0618
            xrs.ProhibitDtd = false;
#pragma warning restore 0618
            XmlReader xrTemp = XmlReader.Create(FullFilePath("showParam.xsl"), xrs);
            try
            {
                xslt.Load(xrTemp);
            }
            catch (Exception ex)
            {
                fTEST_FAIL = true;
                throw (ex);
            }
            finally
            {
                if (!fTEST_FAIL && (!xrTemp.EOF))
                    fTEST_FAIL = false;
                xrTemp.Dispose();
            }
            if (fTEST_FAIL)
            {
                _output.WriteLine("Reader does not appear to be at the end of file.");
                Assert.True(false);
            }
            return;
        }

        //[Variation("Load with reader position at EOF, should throw exception")]
        [Fact]
        public void LoadXmlReader5()
        {
            bool fTEST_FAIL = false;
            xslt = new XslCompiledTransform();

            XmlReaderSettings xrs = new XmlReaderSettings();
#pragma warning disable 0618
            xrs.ProhibitDtd = false;
#pragma warning restore 0618
            XmlReader xrTemp = XmlReader.Create(FullFilePath("showParam.xsl"), xrs);
            xslt.Load(xrTemp);
            try
            {
                xslt.Load(xrTemp);  // should now be at end and should give exception
                fTEST_FAIL = true;
            }
            catch (System.Xml.Xsl.XsltException e)
            {
                CheckExpectedError(e, "system.xml", "Xslt_WrongStylesheetElement", new string[] { "" });
            }
            finally
            {
                xrTemp.Dispose();
            }
            if (fTEST_FAIL)
                Assert.True(false);
            return;
        }

        //[Variation("Load with NULL reader, should throw System.ArgumentNullException")]
        [Fact]
        public void LoadXmlReader6()
        {
            xslt = new XslCompiledTransform();

            XmlTextReader xrTemp = null;

            try
            {
                xslt.Load(xrTemp);  // should now be at end and should give exception
            }
            catch (System.ArgumentNullException)
            {
                return;
            }
            _output.WriteLine("Failed to throw System.ArgumentNullException for NULL reader input");
            Assert.True(false);
        }

        //[Variation("Basic check for usage of credentials on resolver, load XSL that needs cred. with correct resolver", Param = "XmlResolverTestMain.txt")]
        [InlineData("XmlResolverTestMain.txt", OutputType.Stream, NavType.XPathDocument)]
        [Theory]
        public void LoadXmlReader7(object param, OutputType outputType, NavType navType)
        {
            xslt = new XslCompiledTransform();
            string Baseline = Path.Combine("baseline", (string)param);
            XmlReader xrLoad = XmlReader.Create(FullFilePath(Path.Combine("XmlResolver", "XmlResolverTestMain.xsl")));

            xslt.Load(xrLoad, XsltSettings.TrustedXslt, GetDefaultCredResolver());
            xrLoad.Dispose();

            if (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1)
            {
                VerifyResult(Baseline, _strOutFile);
                return;
            }
            Assert.True(false);
        }

        //[Variation("bug 380138 NRE during XSLT compilation")]
        [Fact]
        public void Bug380138()
        {
            string xsl = @"<?xml version=""1.0"" encoding=""utf-8""?>
    <xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'
        xmlns:ms='urn:schemas-microsoft-com:xslt' exclude-result-prefixes='ms'>
      <xsl:template match='asf'><xsl:value-of select=""ms:namespace-uri('ms:b')""/></xsl:template>
    </xsl:stylesheet>";
            XslCompiledTransform xslt = new XslCompiledTransform();
            try
            {
                xslt.Load(XmlReader.Create(new StringReader(xsl)));
            }
            catch (NullReferenceException nre)
            {
                _output.WriteLine(nre.Message);
                Assert.True(false);
            }

            return;
        }
    }

    public class SimpleWrapperNavigator : XPathNavigator
    {
        private XPathNavigator _innerNavigator;
        private XmlNameTable _nt;

        public SimpleWrapperNavigator(XPathNavigator nav)
        {
            _innerNavigator = nav;
            _nt = new NameTable();
        }

        public SimpleWrapperNavigator(XPathNavigator nav, XmlNameTable nt)
        {
            _innerNavigator = nav;
            _nt = nt;
        }

        public override string BaseURI
        {
            get { return _innerNavigator.BaseURI; }
        }

        public override XPathNavigator Clone()
        {
            return new SimpleWrapperNavigator(_innerNavigator.Clone(), _nt);
        }

        public override bool IsEmptyElement
        {
            get { return _innerNavigator.IsEmptyElement; }
        }

        public override bool IsSamePosition(XPathNavigator other)
        {
            if (other is SimpleWrapperNavigator)
            {
                return _innerNavigator.IsSamePosition((other as SimpleWrapperNavigator)._innerNavigator);
            }
            else
            {
                return _innerNavigator.IsSamePosition(other);
            }
        }

        public override string LocalName
        {
            get { return _nt.Add(_innerNavigator.LocalName); }
        }

        public override bool MoveTo(XPathNavigator other)
        {
            SimpleWrapperNavigator nav = other as SimpleWrapperNavigator;
            if (nav != null)
            {
                return _innerNavigator.MoveTo(nav._innerNavigator);
            }
            return false;
        }

        public override bool MoveToFirstAttribute()
        {
            return _innerNavigator.MoveToFirstAttribute();
        }

        public override bool MoveToFirstChild()
        {
            return _innerNavigator.MoveToFirstChild();
        }

        public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
        {
            return _innerNavigator.MoveToFirstNamespace(namespaceScope);
        }

        public override bool MoveToId(string id)
        {
            return _innerNavigator.MoveToId(id);
        }

        public override bool MoveToNext()
        {
            return _innerNavigator.MoveToNext();
        }

        public override bool MoveToNextAttribute()
        {
            return _innerNavigator.MoveToNextAttribute();
        }

        public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
        {
            return _innerNavigator.MoveToNextNamespace(namespaceScope);
        }

        public override bool MoveToParent()
        {
            return _innerNavigator.MoveToParent();
        }

        public override bool MoveToPrevious()
        {
            return _innerNavigator.MoveToPrevious();
        }

        public override string Name
        {
            get { return _innerNavigator.Name; }
        }

        public override XmlNameTable NameTable
        {
            get { return _nt; }
        }

        public override string NamespaceURI
        {
            get { return _nt.Add(_innerNavigator.NamespaceURI); }
        }

        public override XPathNodeType NodeType
        {
            get { return _innerNavigator.NodeType; }
        }

        public override string Prefix
        {
            get { return _nt.Add(_innerNavigator.Prefix); }
        }

        public override string Value
        {
            get { return _innerNavigator.Value; }
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Transform - Integrity     */
    /***********************************************************/

    //[TestCase(Name = "XslCompiledTransform.Transform() Integrity : Reader , Reader", Desc = "READER,READER")]
    //[TestCase(Name = "XslCompiledTransform.Transform() Integrity : Reader, Stream", Desc = "READER,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Transform() Integrity : Reader, Writer", Desc = "READER,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Transform() Integrity : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    //[TestCase(Name = "XslCompiledTransform.Transform() Integrity : URI, Reader", Desc = "URI,READER")]
    //[TestCase(Name = "XslCompiledTransform.Transform() Integrity : URI, Stream", Desc = "URI,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Transform() Integrity : URI, Writer", Desc = "URI,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Transform() Integrity : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    //[TestCase(Name = "XslCompiledTransform.Transform() Integrity : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    //[TestCase(Name = "XslCompiledTransform.Transform() Integrity : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Transform() Integrity : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Transform() Integrity : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CTransformTestGeneric : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CTransformTestGeneric(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Basic Verification Test", Param = "showParam.txt")]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void TransformGeneric1(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            if ((LoadXSL("showParam.xsl", xslInputType, readerType) == 1) && (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1))
            {
                VerifyResult(Baseline, _strOutFile);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Load and Transform multiple times", Param = "showParam.txt")]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void TransformGeneric2(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            for (int i = 0; i < 5; i++)
            {
                if ((LoadXSL("showParam.xsl", xslInputType, readerType) != 1) || (Transform((string) "fruits.xml", (OutputType) outputType, navType) != 1))
                    Assert.True(false);

                VerifyResult(Baseline, _strOutFile);
            }
            return;
        }

        //[Variation("Load once, Transform many times", Param = "showParam.txt")]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void TransformGeneric3(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                for (int i = 0; i < 100; i++)
                {
                    if (Transform((string) "fruits.xml", (OutputType) outputType, navType) != 1)
                    {
                        _output.WriteLine("Test failed to transform after {0} iterations", i);
                        Assert.True(false);
                    }
                    VerifyResult(Baseline, _strOutFile);
                }
                return;
            }
            Assert.True(false);
        }

        //[Variation("Call Transform without loading")]
        [InlineData(OutputType.Stream, NavType.XPathDocument)]
        [InlineData(OutputType.Writer, NavType.XPathDocument)]
        [InlineData(OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void TransformGeneric4(OutputType outputType, NavType navType)
        {
            xslt = new XslCompiledTransform();
            try
            {
                Transform((string) "fruits.xml", (OutputType) outputType, navType);
            }
            catch (System.InvalidOperationException e)
            {
                CheckExpectedError(e, "system.xml", "Xslt_NoStylesheetLoaded", new string[] { "" });
                return;
            }
            _output.WriteLine("Exception not given for a transform that didn't have a Load method instantiated");
            Assert.True(false);
        }

        //[Variation("Closing XSL and XML files used in transform, Read access")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void TransformGeneric5(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            FileStream s2;

            if ((LoadXSL("showParam.xsl", xslInputType, readerType) == 1) && (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1))
            {
                s2 = new FileStream(FullFilePath("showParam.xsl"), FileMode.Open, FileAccess.Read);
                s2.Dispose();

                s2 = new FileStream(FullFilePath("fruits.xml"), FileMode.Open, FileAccess.Read);
                s2.Dispose();

                return;
            }
            _output.WriteLine("Encountered errors performing transform and could not verify if files were closed");
            Assert.True(false);
        }

        //[Variation("Bug20003707 - InvalidProgramException for 2.0 stylesheets in forwards-compatible mode")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void TransformGeneric7(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            try
            {
                LoadXSL("ForwardComp.xsl", xslInputType, readerType);
                Transform((string) "data.xml", (OutputType) outputType, navType);
            }
            catch (XsltException e)
            {
                CheckExpectedError(e, "system.xml", "XPath_ScientificNotation", new string[] { "" });
                return;
            }
            _output.WriteLine("XsltException (XPath_ScientificNotation) was expected");
            Assert.True(false);
        }

        //[Variation("Bug382506 - Loading stylesheet from custom navigator with enableDebug = true causes ArgumentOutOfRangeException")]
        [Fact]
        public void TransformGeneric8()
        {
            xslt = new XslCompiledTransform();
            xslt.Load(new SimpleWrapperNavigator(new XPathDocument(FullFilePath("CustomNav.xsl")).CreateNavigator()));

            return;
        }

        //[Variation("Bug378293 - Incorrect error message when an attribute is added to a root node")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void TransformGeneric9(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            try
            {
                LoadXSL("RootNodeAtt.xsl", xslInputType, readerType);
                Transform((string) "data.xml", (OutputType) outputType, navType);
            }
            catch (XsltException e)
            {
                CheckExpectedError(e, "system.xml", "XmlIl_BadXmlState", new string[] { "Attribute", "Root" });
                return;
            }
            _output.WriteLine("XslTransformException (XmlIl_BadXmlState) was expected");
            Assert.True(false);
        }

        //[Variation("Bug349757 - document() function does not work when stylesheet was loaded from a stream or reader or constructed DOM")]
        [Fact]
        public void TransformGeneric10()
        {
            xslt = new XslCompiledTransform();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<doc xsl:version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>" + "<xsl:copy-of select='document(\"test.xml\")'/>" + "</doc>");

            xslt.Load(doc, XsltSettings.TrustedXslt, new XmlUrlResolver());
            return;
        }

        //[Variation("Bug369463 - Invalid XPath exception in forward compatibility mode should render lineNumber linePosition")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void TransformGeneric11(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            try
            {
                LoadXSL("Bug369463.xsl", xslInputType, readerType);
                Transform((string) "data.xml", (OutputType) outputType, navType);
            }
            catch (XsltException e)
            {
                CheckExpectedError(e, "system.xml", "XPath_UnexpectedToken", new string[] { "+" });
                return;
            }
            _output.WriteLine("XslException (XPath_UnexpectedToken) was expected");
            Assert.True(false);
        }
    }

    /*************************************************************/
    /*          XslCompiledTransform(Resolver) - Integrity       */
    /*************************************************************/

    //[TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Reader, Reader", Desc = "READER,READER")]
    //[TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Reader, Stream", Desc = "READER,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Reader, Writer", Desc = "READER,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    //[TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : URI, Reader", Desc = "URI,READER")]
    //[TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : URI, Stream", Desc = "URI,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : URI, Writer", Desc = "URI,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    //[TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    //[TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    //[TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CTransformResolverTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CTransformResolverTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Pass null XmlResolver, load style sheet with import/include, should not affect transform")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void XmlResolver1(XslInputType xslInputType , ReaderType readerType, OutputType outputType, NavType navType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result><fruit>Apple</fruit><fruit>orange</fruit></result>";

            try
            {
                if (LoadXSL("XmlResolver_Main.xsl", xslInputType, readerType) == 1)
                {
                    if (TransformResolver("fruits.xml", outputType, navType, null) == 1)
                    {
                        VerifyResult(expected);
                        return;
                    }
                    else
                        Assert.True(false);
                }
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }
            Assert.True(false);
        }

        //[Variation("Pass null XmlResolver, load style sheet with document function, should not resolve during transform", Param = "xmlResolver_document_function.txt")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51911", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        [InlineData("xmlResolver_document_function.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("xmlResolver_document_function.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("xmlResolver_document_function.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("xmlResolver_document_function.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("xmlResolver_document_function.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("xmlResolver_document_function.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("xmlResolver_document_function.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("xmlResolver_document_function.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("xmlResolver_document_function.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void XmlResolver2(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            AppContext.SetSwitch("Switch.System.Xml.AllowDefaultResolver", true);

            // "xmlResolver_document_function.xsl" contains
            // <xsl:for-each select="document('xmlResolver_document_function.xml')//elem">
            string Baseline = Path.Combine("baseline", (string)param);

            if (LoadXSL("xmlResolver_document_function.xsl", xslInputType, readerType) == 1)
            {
                if (TransformResolver("fruits.xml", outputType, navType, null) == 1)
                {
                    VerifyResult(Baseline, _strOutFile);
                    return;
                }
            }
            else
            {
                _output.WriteLine("Problem loading stylesheet!");
                Assert.True(false);
            }
            Assert.True(false);
        }

        //[Variation("Default XmlResolver, load style sheet with document function, should resolve during transform", Param = "DefaultResolver.txt")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51911", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        [InlineData("DefaultResolver.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData("DefaultResolver.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void XmlResolver3(object param, XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            AppContext.SetSwitch("Switch.System.Xml.AllowDefaultResolver", true);

            // "xmlResolver_document_function.xsl" contains
            // <xsl:for-each select="document('xmlResolver_document_function.xml')//elem">
            string Baseline = Path.Combine("baseline", (string)param);
            if (LoadXSL("xmlResolver_document_function.xsl", xslInputType, readerType) == 1)
            {
                if (Transform((string) "fruits.xml", (OutputType) outputType, navType) == 1)
                {
                    VerifyResult(Baseline, _strOutFile);
                    return;
                }
            }
            else
            {
                _output.WriteLine("Problem loading stylesheet with document function and default resolver!");
                Assert.True(false);
            }
            Assert.True(false);
        }

        //[Variation("document() has absolute URI")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void XmlResolver5(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            AppContext.SetSwitch("Switch.System.Xml.AllowDefaultResolver", true);

            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result>123</result>";
            string fileName = GetType().Name + "_" + Path.GetRandomFileName();
            string testFile = Path.Combine(Path.GetTempPath(), fileName);
            string xmlFile = FullFilePath(fileName);

            // copy file on the local machine
            try
            {
                File.Copy(xmlFile, testFile, true);
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                _output.WriteLine("Could not copy file to local. Some other issues prevented this test from running");
                return; //TEST_SKIPPED;
            }
            finally
            {
                if (File.Exists(testFile))
                {
                    File.SetAttributes(testFile, FileAttributes.Normal);
                    File.Delete(testFile);
                }
            }

            if (LoadXSL("xmlResolver_document_function_absolute_uri.xsl", xslInputType, readerType) == 1)
            {
                if (TransformResolver("fruits.xml", outputType, navType, new XmlUrlResolver()) == 1)
                {
                    VerifyResult(expected);
                    return;
                }
                else
                {
                    _output.WriteLine("Failed to resolve document function with absolute URI.");
                    Assert.True(false);
                }
            }
            else
            {
                _output.WriteLine("Failed to load style sheet!");
                Assert.True(false);
            }
        }

        //[Variation("Pass null resolver but stylesheet doesn't have any include/imports")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.Writer, NavType.XPathDocument)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void XmlResolver7(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            LoadXSL("Bug382198.xsl", xslInputType, readerType);
            // Pass null
            TransformResolver("fruits.xml", outputType, navType, null);
            return;
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Transform - (String, String)                    */
    /***********************************************************/

    //[TestCase(Name = "XslCompiledTransform.Transform(String, String) : Reader , String", Desc = "READER,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Transform(String, String) : URI, String", Desc = "URI,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Transform(String, String) : Navigator, String", Desc = "NAVIGATOR,STREAM")]
    public class CTransformStrStrTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CTransformStrStrTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Basic Verification Test", Param = "showParam.txt")]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr1(object param, XslInputType xslInputType, ReaderType readerType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            string szFullFilename = FullFilePath("fruits.xml");

            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                xslt.Transform(szFullFilename, _strOutFile);
                VerifyResult(Baseline, _strOutFile);
                return;
            }
            Assert.True(false);
        }

        //[Variation("Input is null")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr2(XslInputType xslInputType, ReaderType readerType)
        {
            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                try
                {
                    xslt.Transform(null, _strOutFile);
                }
                catch (System.ArgumentException)
                { return; }
            }
            _output.WriteLine("Exception not generated for null input filename");
            Assert.True(false);
        }

        //[Variation("Output file is null")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr3(XslInputType xslInputType, ReaderType readerType)
        {
            string szFullFilename = FullFilePath("fruits.xml");

            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                try
                {
                    xslt.Transform(szFullFilename, (string)null);
                }
                catch (System.ArgumentException)
                {
                    return;
                }
            }
            _output.WriteLine("Exception not generated for null output filename");
            Assert.True(false);
        }

        //[Variation("Input is nonexisting file")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr4(XslInputType xslInputType, ReaderType readerType)
        {
            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                try
                {
                    xslt.Transform("IDontExist.xsl", _strOutFile);
                }
                catch (System.IO.FileNotFoundException)
                {
                    return;
                }
            }
            _output.WriteLine("Exception not generated for invalid input file");
            Assert.True(false);
        }

        //[Variation("Output file is invalid")]
        [PlatformSpecific(TestPlatforms.Windows)] //Output file name is valid on Unix
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr5(XslInputType xslInputType, ReaderType readerType)
        {
            string szFullFilename = FullFilePath("fruits.xml");

            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                try
                {
                    xslt.Transform(szFullFilename, szInvalid);
                }
                catch (System.ArgumentException)
                {
                    return;
                }
            }
            _output.WriteLine("Exception not generated for invalid output destination");
            Assert.True(false);
        }

        //[Variation("Input is empty string")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr6(XslInputType xslInputType, ReaderType readerType)
        {
            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                try
                {
                    xslt.Transform(szEmpty, _strOutFile);
                }
                catch (System.ArgumentException)
                {
                    return;
                }
            }
            _output.WriteLine("Exception not generated for empty string input file");
            Assert.True(false);
        }

        //[Variation("Output file is empty string")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr7(XslInputType xslInputType, ReaderType readerType)
        {
            string szFullFilename = FullFilePath("fruits.xml");

            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                try
                {
                    xslt.Transform(szFullFilename, szEmpty);
                }
                catch (System.ArgumentException)
                {
                    return;
                }
            }
            _output.WriteLine("Exception not generated for empty output file name");
            Assert.True(false);
        }

        //[Variation("Call Transform many times", Param = "showParam.txt")]
        [InlineData("showParam.txt", XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData("showParam.txt", XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData("showParam.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr8(object param, XslInputType xslInputType, ReaderType readerType)
        {
            string Baseline = Path.Combine("baseline", (string)param);
            string szFullFilename = FullFilePath("fruits.xml");

            for (int i = 0; i < 50; i++)
            {
                if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
                {
                    xslt.Transform(szFullFilename, _strOutFile);
                    VerifyResult(Baseline, _strOutFile);
                    //_output.WriteLine("Failed to process Load after calling {0} times", i);
                }
            }
            return;
        }

        //[Variation("Call without loading")]
        [Fact]
        public void TransformStrStr9()
        {
            xslt = new XslCompiledTransform();
            SetExpectedError("Xslt_NoStylesheetLoaded");
            try
            {
                xslt.Transform(FullFilePath("fruits.xml"), _strOutFile);
            }
            catch (System.InvalidOperationException e)
            {
                CheckExpectedError(e, "System.xml", "Xslt_NoStylesheetLoaded", new string[] { "" });
                return;
            }
            _output.WriteLine("Exception attempting a transform without loading an XSL file");
            Assert.True(false);
        }

        //[Variation("Output to unreachable destination")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr10(XslInputType xslInputType, ReaderType readerType)
        {
            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                try
                {
                    xslt.Transform("fruits.xml", "http://www.IdontExist.com/index.xml");
                }
                catch (System.Exception e)
                {
                    _output.WriteLine(e.ToString());
                    return;
                }
            }
            _output.WriteLine("Exception not generated for invalid output destination");
            Assert.True(false);
        }

        //[Variation("Input filename is \'.\', \'..\', and \'\\\\\'")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr11(XslInputType xslInputType, ReaderType readerType)
        {
            int iCount = 0;
            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                SetExpectedError("Xml_ResolveUrl");
                try
                {
                    xslt.Transform("..", _strOutFile);
                }
                catch (System.Exception)
                {
                    iCount++;
                }

                try
                {
                    xslt.Transform(".", _strOutFile);
                }
                catch (System.Exception)
                {
                    iCount++;
                }

                try
                {
                    xslt.Transform("\\\\", _strOutFile);
                }
                catch (System.Exception)
                {
                    iCount++;
                }
            }

            if (iCount.Equals(3))
                return;

            _output.WriteLine("Exception not generated for invalid input sources");
            Assert.True(false);
        }

        //[Variation("Output filename is \'.\' and \'..\'")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr12(XslInputType xslInputType, ReaderType readerType)
        {
            string szFullFilename = FullFilePath("fruits.xml");
            int iCount = 0;
            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                try
                {
                    xslt.Transform(szFullFilename, "..");
                }
                catch (System.Exception)
                {
                    iCount++;
                }

                try
                {
                    xslt.Transform(szFullFilename, ".");
                }
                catch (System.Exception)
                {
                    iCount++;
                }
            }

            if (iCount.Equals(2))
                return;
            _output.WriteLine("Exception not generated for invalid ouput destinations");
            Assert.True(false);
        }

        //[Variation("Output filename is \'\\\\\'")]
        [PlatformSpecific(TestPlatforms.Windows)]  // Invalid path specific to Windows
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr12_win(XslInputType xslInputType, ReaderType readerType)
        {
            string szFullFilename = FullFilePath("fruits.xml");

            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                    Assert.Throws<System.ArgumentException>(() => xslt.Transform(szFullFilename, "    "));
                    return;
            }

            _output.WriteLine("Exception not generated for invalid ouput destinations");
            Assert.True(false);
        }

        //[Variation("Closing files after transform")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStr13(XslInputType xslInputType, ReaderType readerType)
        {
            string szFullFilename = FullFilePath("fruits.xml");
            Stream strmTemp;

            if (LoadXSL("showParam.xsl", xslInputType, readerType) == 1)
            {
                xslt.Transform(szFullFilename, _strOutFile);
                StreamReader fs = null;

                // check if I can open and close the xml file
                fs = new StreamReader(new FileStream(szFullFilename, FileMode.Open, FileAccess.Read));
                fs.Dispose();

                strmTemp = new FileStream(szFullFilename, FileMode.Open, FileAccess.Read);
                strmTemp.Dispose();

                // check if I can open and close the output file
                fs = new StreamReader(new FileStream(_strOutFile, FileMode.Open, FileAccess.Read));
                fs.Dispose();

                strmTemp = new FileStream(_strOutFile, FileMode.Open, FileAccess.Read);
                strmTemp.Dispose();

                return;
            }
            Assert.True(false);
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Transform - (String, String, Resolver)          */
    /***********************************************************/

    //[TestCase(Name = "XslCompiledTransform.Transform(String, String, Resolver) : Reader , String", Desc = "READER,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Transform(String, String, Resolver) : URI, String", Desc = "URI,STREAM")]
    //[TestCase(Name = "XslCompiledTransform.Transform(String, String, Resolver) : Navigator, String", Desc = "NAVIGATOR,STREAM")]
    public class CTransformStrStrResolverTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CTransformStrStrResolverTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Pass null XmlResolver to Transform, load style sheet with import/include, should not affect transform")]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStrResolver1(XslInputType xslInputType, ReaderType readerType)
        {
            string szFullFilename = FullFilePath("fruits.xml");
            string expected = @"<result>
  <fruit>Apple</fruit>
  <fruit>orange</fruit>
</result>";

            if (LoadXSL("XmlResolver_Main.xsl", xslInputType, readerType, new XmlUrlResolver()) == 1)
            {
                XmlTextReader xr = new XmlTextReader(szFullFilename);
                XmlTextWriter xw = new XmlTextWriter("out.xml", Encoding.Unicode);
                xslt.Transform(xr, null, xw, null);
                xr.Dispose();
                xw.Dispose();
                VerifyResult(expected);
                return;
            }
            Assert.True(false);
        }

        //[Variation("Pass null XmlResolver, load style sheet with document function, should not resolve during transform")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51911", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        [InlineData(XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStrResolver2(XslInputType xslInputType, ReaderType readerType)
        {
            AppContext.SetSwitch("Switch.System.Xml.AllowDefaultResolver", true);

            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><result><elem>1</elem><elem>2</elem><elem>3</elem></result>";

            // "xmlResolver_document_function.xsl" contains
            // <xsl:for-each select="document('xmlResolver_document_function.xml')//elem">

            string szFullFilename = FullFilePath("fruits.xml");

            if (LoadXSL("xmlResolver_document_function.xsl", xslInputType, readerType) == 1)
            {
                xslt.Transform(szFullFilename, "out.xml");
                VerifyResult(expected);
            }
            else
            {
                _output.WriteLine("Problem loading stylesheet!");
                Assert.True(false);
            }
        }

        //[Variation("Pass XmlUrlResolver, load style sheet with document function, should resolve during transform", Param = "xmlResolver_document_function.txt")]
        [InlineData("xmlResolver_document_function.txt", XslInputType.Reader, ReaderType.XmlValidatingReader)]
        [InlineData("xmlResolver_document_function.txt", XslInputType.URI, ReaderType.XmlValidatingReader)]
        [InlineData("xmlResolver_document_function.txt", XslInputType.Navigator, ReaderType.XmlValidatingReader)]
        [Theory]
        public void TransformStrStrResolver3(object param, XslInputType xslInputType, ReaderType readerType)
        {
            AppContext.SetSwitch("Switch.System.Xml.AllowDefaultResolver", true);

            // "xmlResolver_document_function.xsl" contains
            // <xsl:for-each select="document('xmlResolver_document_function.xml')//elem">

            string szFullFilename = FullFilePath("fruits.xml");
            string Baseline = Path.Combine("baseline", (string)param);

            if (LoadXSL("xmlResolver_document_function.xsl", xslInputType, readerType) == 1)
            {
                xslt.Transform(szFullFilename, "out.xml");
                VerifyResult(Baseline, _strOutFile);
                return;
            }
            else
            {
                _output.WriteLine("Problem loading stylesheet with document function and default resolver!");
                Assert.True(false);
            }
            Assert.True(false);
        }
    }

    //[TestCase(Name = "XslCompiledTransform.Transform(IXPathNavigable, XsltArgumentList, XmlWriter, XmlResolver)", Desc = "Constructor Tests", Param = "IXPathNavigable")]
    //[TestCase(Name = "XslCompiledTransform.Transform(XmlReader, XsltArgumentList, XmlWriter, XmlResolver)", Desc = "Constructor Tests", Param = "XmlReader")]
    public class CTransformConstructorWihtFourParametersTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CTransformConstructorWihtFourParametersTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        internal class CustomXmlResolver : XmlUrlResolver
        {
            private string _baseUri;

            public CustomXmlResolver(string baseUri)
            {
                _baseUri = baseUri;
            }

            public override Uri ResolveUri(Uri baseUri, string relativeUri)
            {
                if (baseUri == null)
                    return base.ResolveUri(new Uri(Uri.UriSchemeFile + Uri.SchemeDelimiter + _baseUri), relativeUri);
                return base.ResolveUri(baseUri, relativeUri);
            }
        }

        //[Variation("Import/Include, CustomXmlResolver", Pri = 0, Params = new object[] { "XmlResolver_Main.xsl", "fruits.xml", "xmlResolver_main.txt", "CustomXmlResolver", true })]
        [InlineData("XmlResolver_Main.xsl", "fruits.xml", "xmlResolver_main.txt", "CustomXmlResolver", true, "IXPathNavigable")]
        [InlineData("XmlResolver_Main.xsl", "fruits.xml", "xmlResolver_main.txt", "CustomXmlResolver", true, "XmlReader")]
        //[Variation("Import/Include, NullResolver", Pri = 0, Params = new object[] { "XmlResolver_Main.xsl", "fruits.xml", "xmlResolver_main.txt", "NullResolver", false })]
        [InlineData("XmlResolver_Main.xsl", "fruits.xml", "xmlResolver_main.txt", "NullResolver", false, "IXPathNavigable")]
        [InlineData("XmlResolver_Main.xsl", "fruits.xml", "xmlResolver_main.txt", "NullResolver", false, "XmlReader")]
        [Theory]
        public void ValidCases_ExternalURI(object param0, object param1, object param2, object param3, object param4, object param5)
        {
            AppContext.SetSwitch("Switch.System.Xml.AllowDefaultResolver", true);

            ValidCases(param0, param1, param2, param3, param4, param5);
        }

        //[Variation("Document function 1, CustomXmlResolver", Pri = 0, Params = new object[] { "xmlResolver_document_function.xsl", "fruits.xml", "xmlResolver_document_function.txt", "CustomXmlResolver", true })]
        [InlineData("xmlResolver_document_function.xsl", "fruits.xml", "xmlResolver_document_function.txt", "CustomXmlResolver", true, "XmlReader")]
        [InlineData("xmlResolver_document_function.xsl", "fruits.xml", "xmlResolver_document_function.txt", "CustomXmlResolver", true, "IXPathNavigable")]
        //[Variation("Document function 1, XmlUrlResolver", Pri = 0, Params = new object[] { "xmlResolver_document_function.xsl", "fruits.xml", "xmlResolver_document_function.txt", "XmlUrlResolver", true })]
        [InlineData("xmlResolver_document_function.xsl", "fruits.xml", "xmlResolver_document_function.txt", "XmlUrlResolver", true, "IXPathNavigable")]
        [InlineData("xmlResolver_document_function.xsl", "fruits.xml", "xmlResolver_document_function.txt", "XmlUrlResolver", true, "XmlReader")]
        //[Variation("Document function 1, NullResolver", Pri = 0, Params = new object[] { "xmlResolver_document_function.xsl", "fruits.xml", "xmlResolver_document_function.txt", "NullResolver", false })]
       // [InlineData("xmlResolver_document_function.xsl", "fruits.xml", "xmlResolver_document_function.txt", "NullResolver", false, "IXPathNavigable")]
       // [InlineData("xmlResolver_document_function.xsl", "fruits.xml", "xmlResolver_document_function.txt", "NullResolver", false, "XmlReader")]
        //[Variation("No Import/Include, CustomXmlResolver", Pri = 0, Params = new object[] { "Bug382198.xsl", "fruits.xml", "bug382198.txt", "CustomXmlResolver", true })]
        [InlineData("Bug382198.xsl", "fruits.xml", "bug382198.txt", "CustomXmlResolver", true, "IXPathNavigable")]
        [InlineData("Bug382198.xsl", "fruits.xml", "bug382198.txt", "CustomXmlResolver", true, "XmlReader")]
        //[Variation("Import/Include, XmlUrlResolver", Pri = 0, Params = new object[] { "XmlResolver_Main.xsl", "fruits.xml", "xmlResolver_main.txt", "XmlUrlResolver", true })]
        [InlineData("XmlResolver_Main.xsl", "fruits.xml", "xmlResolver_main.txt", "XmlUrlResolver", true, "IXPathNavigable")]
        [InlineData("XmlResolver_Main.xsl", "fruits.xml", "xmlResolver_main.txt", "XmlUrlResolver", true, "XmlReader")]
        //[Variation("No Import/Include, XmlUrlResolver", Pri = 0, Params = new object[] { "Bug382198.xsl", "fruits.xml", "bug382198.txt", "XmlUrlResolver", true })]
        [InlineData("Bug382198.xsl", "fruits.xml", "bug382198.txt", "XmlUrlResolver", true, "IXPathNavigable")]
        [InlineData("Bug382198.xsl", "fruits.xml", "bug382198.txt", "XmlUrlResolver", true, "XmlReader")]
        //[Variation("No Import/Include, NullResolver", Pri = 0, Params = new object[] { "Bug382198.xsl", "fruits.xml", "bug382198.txt", "NullResolver", true })]
        [InlineData("Bug382198.xsl", "fruits.xml", "bug382198.txt", "NullResolver", true, "IXPathNavigable")]
        [InlineData("Bug382198.xsl", "fruits.xml", "bug382198.txt", "NullResolver", true, "XmlReader")]
        [Theory]
        public void ValidCases(object param0, object param1, object param2, object param3, object param4, object param5)
        {
            string xslFile = FullFilePath(param0 as string);
            string xmlFile = FullFilePath(param1 as string);
            string baseLineFile = Path.Combine("baseline", param2 as string);
            bool expectedResult = (bool)param4;
            bool actualResult = false;

            XmlReader xmlReader = XmlReader.Create(xmlFile);
            //Let's select randomly how to create navigator
            IXPathNavigable navigator = null;
            Random randGenerator = new Random(unchecked((int)DateTime.Now.Ticks));
            switch (randGenerator.Next(2))
            {
                case 0:
                    _output.WriteLine("Using XmlDocument.CreateNavigator()");
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(xmlFile);
                    navigator = xmlDoc.CreateNavigator();
                    break;

                case 1:
                    _output.WriteLine("Using XPathDocument.CreateNavigator()");
                    XPathDocument xpathDoc;
                    using (XmlReader reader = XmlReader.Create(xmlFile))
                    {
                        xpathDoc = new XPathDocument(reader);
                        navigator = xpathDoc.CreateNavigator();
                    }
                    break;

                default:
                    break;
            }

            XmlResolver resolver = null;
            switch (param3 as string)
            {
                case "NullResolver":
                    break;

                case "XmlUrlResolver":
                    resolver = new XmlUrlResolver();
                    break;

                case "CustomXmlResolver":
                    resolver = new CustomXmlResolver(Path.GetFullPath(Path.Combine(FilePathUtil.GetTestDataPath(), @"XsltApiV2")));
                    break;

                default:
                    break;
            }

            try
            {
                XslCompiledTransform localXslt = new XslCompiledTransform();
                XsltSettings settings = new XsltSettings(true, true);
                using (XmlReader xslReader = XmlReader.Create(xslFile))
                    localXslt.Load(xslReader, settings, resolver);

                using (XmlWriter writer = XmlWriter.Create("outputFile.txt"))
                {
                    if (param5 as string == "XmlReader")
                        localXslt.Transform(xmlReader, null, writer, resolver);
                    else
                        localXslt.Transform(navigator, null, writer, resolver);
                }
                VerifyResult(baseLineFile, "outputFile.txt");
                actualResult = true;
            }
            catch (Exception ex)
            {
                _output.WriteLine(ex.Message);
                actualResult = false;
            }

            if (actualResult != expectedResult)
                Assert.True(false);

            return;
        }

        //[Variation("Invalid Arguments: null, valid, valid, valid", Pri = 0, Params = new object[] { 1, false })]
        [InlineData(1, false, "IXPathNavigable")]
        [InlineData(1, false, "XmlReader")]
        //[Variation("Invalid Arguments: valid, null, valid, valid", Pri = 0, Params = new object[] { 2, true })]
        [InlineData(2, true, "XmlReader")]
        [InlineData(2, true, "IXPathNavigable")]
        //[Variation("Invalid Arguments: valid, valid, null, valid", Pri = 0, Params = new object[] { 3, false })]
        [InlineData(3, false, "IXPathNavigable")]
        [InlineData(3, false, "XmlReader")]
        //[Variation("Invalid Arguments: valid, valid, valid, null", Pri = 0, Params = new object[] { 4, true })]
        [InlineData(4, true, "IXPathNavigable")]
        [InlineData(4, true, "XmlReader")]
        [Theory]
        public void InValidCases(object param0, object param1, object param2)
        {
            int argumentNumber = (int)param0;
            bool expectedResult = (bool)param1;
            bool actualResult = false;

            XslCompiledTransform localXslt = new XslCompiledTransform();
            string stylesheet = @"<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" />";
            using (XmlReader xslReader = XmlReader.Create(new StringReader(stylesheet)))
            {
                localXslt.Load(xslReader);
            }

            string xmlString = "<root />";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);
            XmlReader xmlReader = XmlReader.Create(new StringReader(xmlString));
            XPathNavigator nav = xmlDoc.CreateNavigator();
            object[] testInput = new object[] { xmlReader, nav, new XsltArgumentList(), XmlWriter.Create(Stream.Null), new XmlUrlResolver() };
            if (argumentNumber == 1)
                testInput[0] = null;
            testInput[argumentNumber] = null;

            try
            {
                if (param2 as string == "XmlReader")
                    localXslt.Transform(testInput[0] as XmlReader, testInput[2] as XsltArgumentList, testInput[3] as XmlWriter, testInput[4] as XmlResolver);
                else
                    localXslt.Transform(testInput[1] as IXPathNavigable, testInput[2] as XsltArgumentList, testInput[3] as XmlWriter, testInput[4] as XmlResolver);
                actualResult = true;
            }
            catch (ArgumentNullException ex)
            {
                _output.WriteLine(ex.Message);
                actualResult = false;
            }

            if (actualResult != expectedResult)
                Assert.True(false);

            return;
        }
    }

    // This testcase is for bugs 109429, 111075 and 109644 fixed in Everett SP1
    //[TestCase(Name = "NDP1_1SP1 Bugs (URI,STREAM)", Desc = "URI,STREAM")]
    //[TestCase(Name = "NDP1_1SP1 Bugs (NAVIGATOR,TEXTWRITER)", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CNDP1_1SP1Test : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CNDP1_1SP1Test(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Local parameter gets overwritten with global param value", Pri = 1)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void var1(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><out>
param1 (correct answer is 'local-param1-arg'): local-param1-arg
param2 (correct answer is 'local-param2-arg'): local-param2-arg
</out>";
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("param1", string.Empty, "global-param1-arg");

            if ((LoadXSL("paramScope.xsl", xslInputType, readerType) == 1) && (Transform_ArgList("fruits.xml", outputType, navType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Local parameter gets overwritten with global variable value", Pri = 1)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void var2(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><out>
param1 (correct answer is 'local-param1-arg'): local-param1-arg
param2 (correct answer is 'local-param2-arg'): local-param2-arg
</out>";
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("param1", string.Empty, "global-param1-arg");

            if ((LoadXSL("varScope.xsl", xslInputType, readerType) == 1) && (Transform_ArgList("fruits.xml", outputType, navType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Subclassed XPathNodeIterator returned from an extension object or XsltFunction is not accepted by XPath", Pri = 1)]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void var3(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?><distinct-countries>France, Spain, Austria, Germany</distinct-countries>";

            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddExtensionObject("http://foo.com", new MyXsltExtension());

            if ((LoadXSL("Bug111075.xsl", xslInputType, readerType) == 1) && (Transform_ArgList("Bug111075.xml", outputType, navType) == 1))
            {
                VerifyResult(expected);
                return;
            }
            else
                Assert.True(false);
        }

        //[Variation("Iterator using for-each over a variable is not reset correctly while using msxsl:node-set()", Pri = 1)]
        [InlineData(XslInputType.Navigator, ReaderType.XmlValidatingReader, OutputType.TextWriter, NavType.XPathDocument)]
        [Theory]
        public void var4(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            string expected = @"<?xml version=""1.0"" encoding=""utf-8""?>
		Node Count: {3}

		
		Correct Output: (1)(2)(3)
		Incorrect Output: [1][2][3]";

            if ((LoadXSL("Bug109644.xsl", xslInputType, readerType) == 1) && (Transform((string) "foo.xml", (OutputType) outputType, navType) == 1))
            {
                Assert.Equal(expected, File.ReadAllText("out.xml"), ignoreLineEndingDifferences:true);
            }
            else
                Assert.True(false);
        }
    }

    //[TestCase(Name = "XslCompiledTransform Regression Tests for API", Desc = "XslCompiledTransform Regression Tests")]
    public class CTransformRegressionTest : XsltApiTestCaseBase2
    {
        private ITestOutputHelper _output;
        public CTransformRegressionTest(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        //[Variation("Bug398968 - Globalization is broken for document() function")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51244", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader, OutputType.Stream, NavType.XPathDocument)]
        [Theory]
        public void RegressionTest1(XslInputType xslInputType, ReaderType readerType, OutputType outputType, NavType navType)
        {
            // <SQL BU Defect Tracking 410060>
            // </SQL BU Defect Tracking 410060>

            string testFile = Path.Combine("TestFiles", FilePathUtil.GetTestDataPath(), "XsltApiV2", "Stra\u00DFe.xml");

            // Create the file.
            using (FileStream fs = File.Open(testFile, FileMode.Open))
            {
                byte[] info = new UTF8Encoding(true).GetBytes("<PASSED  />");
                fs.Write(info, 0, info.Length);
            }

            LoadXSL("398968repro.xsl", xslInputType, readerType);
            Transform((string) "data.xml", (OutputType) outputType, navType);
            return;
        }

        //[Variation("Bug412703 - Off-by-one errors for XSLT loading error column")]
        [InlineData(XslInputType.URI, ReaderType.XmlValidatingReader)]
        [Theory]
        public void RegressionTest3(XslInputType xslInputType, ReaderType readerType)
        {
            try
            {
                LoadXSL("bug370868.xsl", xslInputType, readerType);
            }
            catch (System.Xml.Xsl.XsltException e)
            {
                // Should be 3,2
                if (e.LineNumber == 3 && e.LinePosition == 2)
                    return;
                else
                    _output.WriteLine("412703: LineNumber and position were incorrect. Expected {0}, {1}. Actual {2}, {3}", 3, 2, e.LineNumber, e.LinePosition);
            }
            Assert.True(false);
        }

        //[Variation("Bug423641 - XslCompiledTransform.Load() [retail] throws a NullReferenceException when scripts are prohibited")]
        [Fact]
        public void RegressionTest4()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            // Should not throw
            xslt.Load(FullFilePath("XSLTFilewithscript.xslt"), XsltSettings.Default, new XmlUrlResolver());
            return;
        }

        //[Variation("Bug423641 - XslCompiledTransform.Load() [debug] throws a NullReferenceException when scripts are prohibited")]
        [Fact]
        public void RegressionTest5()
        {
            XslCompiledTransform xslt = new XslCompiledTransform(true);
            // Should not throw
            xslt.Load(FullFilePath("XSLTFilewithscript.xslt"), XsltSettings.Default, new XmlUrlResolver());
            return;
        }

        //[Variation("Bug469781 - Replace shouldn't relax original type 'assertion failure'")]
        [Fact]
        public void RegressionTest7()
        {
            string xslString = "<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns:user=\"urn:user\">"
                + "<xsl:template match=\"/\">"
                + "<xsl:variable name=\"foo\"/>"
                + "<xsl:for-each select=\"user:func()\">"
                + "<xsl:variable name=\"bar\"/>"
                + "<xsl:if test=\"self::node()[0]\"/>"
                + "</xsl:for-each>"
                + "</xsl:template>"
                + "</xsl:stylesheet>";

            try
            {
                XmlReader r = XmlReader.Create(new StringReader(xslString));
                XslCompiledTransform trans = new XslCompiledTransform();
                trans.Load(r);
            }
            catch (XsltException e)
            {
                _output.WriteLine(e.ToString());
                Assert.True(false);
            }
            return;
        }

        //[Variation("Bug737816 - Dynamic method will have declaring type == null")]
        [Fact]
        public void RegressionTest8()
        {
            try
            {
                DynamicMethod hello = new DynamicMethod("Hello",
                                typeof(int),
                                new Type[] { },
                                typeof(string).Module);

                ILGenerator il = hello.GetILGenerator(256);
                il.Emit(OpCodes.Ret);

                // Load into XslCompiledTransform
                var xslt = new XslCompiledTransform();
                xslt.Load(hello, new byte[] { }, new Type[] { });

                // Run the transformation
                Stream res = new MemoryStream();
                xslt.Transform(XmlReader.Create(new StringReader("<Root><Price>9.50</Price></Root>")), (XsltArgumentList)null, res);
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
            }

            Assert.True(false);
        }
    }

    internal class MyArrayIterator : XPathNodeIterator
    {
        protected ArrayList array;
        protected int index;

        public MyArrayIterator(ArrayList array)
        {
            this.array = array;
            this.index = 0;
        }

        public MyArrayIterator(MyArrayIterator it)
        {
            this.array = it.array;
            this.index = it.index;
        }

        public override XPathNodeIterator Clone()
        {
            return new MyArrayIterator(this);
        }

        public override bool MoveNext()
        {
            if (index < array.Count)
            {
                index++;
                return true;
            }
            return false;
        }

        public override XPathNavigator Current
        {
            get
            {
                return (index > 0) ? (XPathNavigator)array[index - 1] : null;
            }
        }

        public override int CurrentPosition
        {
            get
            {
                return index;
            }
        }

        public override int Count
        {
            get
            {
                return array.Count;
            }
        }

        public void Reset()
        {
            index = 0;
        }

        // BUGBUG: DCR 104760
        public override IEnumerator GetEnumerator()
        {
            MyArrayIterator it = (MyArrayIterator)this.Clone();
            it.Reset();
            return it.array.GetEnumerator();
        }
    }

    internal class MyXsltExtension
    {
        public XPathNodeIterator distinct(XPathNodeIterator nodeset)
        {
            Hashtable nodelist = new Hashtable();
            while (nodeset.MoveNext())
            {
                if (!nodelist.Contains(nodeset.Current.Value))
                {
                    nodelist.Add(nodeset.Current.Value, nodeset.Current);
                }
            }
            return new MyArrayIterator(new ArrayList(nodelist.Values));
        }
    }
}
