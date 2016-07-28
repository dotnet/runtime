// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;
using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

namespace System.Xml.Tests
{
    // TODO: BinCompat - this is testing API which is currently not available - re-enable if it is back
    // //[TestCase(Name = "TemporaryFiles", Desc = "This testcase tests the Temporary Files property on XslCompiledTransform")]
    // public class TempFiles : XsltApiTestCaseBase2
    // {
    //     private XslCompiledTransform _xsl = null;
    //     private string _xmlFile = string.Empty;
    //     private string _xslFile = string.Empty;

    //     private ITestOutputHelper _output;
    //     public TempFiles(ITestOutputHelper output) : base(output)
    //     {
    //         _output = output;
    //     }

    //     private void Init(string xmlFile, string xslFile, bool enableDebug)
    //     {
    //         if (enableDebug)
    //             _xsl = new XslCompiledTransform(true);
    //         else
    //             _xsl = new XslCompiledTransform(false);

    //         _xmlFile = FullFilePath(xmlFile);
    //         _xslFile = FullFilePath(xslFile);

    //         return;
    //     }

    //     private StringWriter Transform()
    //     {
    //         StringWriter sw = new StringWriter();
    //         _xsl.Transform(_xmlFile, null, sw);
    //         return sw;
    //     }

    //     private void VerifyResult(object actual, object expected, string message)
    //     {
    //         _output.WriteLine("Expected : {0}", expected);
    //         _output.WriteLine("Actual : {0}", actual);

    //         Assert.Equal(actual, expected);
    //     }

    //     private void VerifyResult(bool expression, string message)
    //     {
    //         Assert.True(expression);
    //     }

    //     //[Variation(id = 1, Desc = "Default value of TemporaryFiles before load, expected null", Pri = 0)]
    //     [InlineData()]
    //     [Theory]
    //     public void TempFiles1()
    //     {
    //         XslCompiledTransform xslt = new XslCompiledTransform();
    //         VerifyResult(xslt.TemporaryFiles, null, "Default value of TemporaryFiles must be null");
    //     }

    //     //[Variation(id = 2, Desc = "TemporaryFiles after load in Retail Mode with no script block, expected count = 0", Pri = 0, Params = new object[] { "books.xml", "NoScripts.xsl", false })]
    //     [InlineData("books.xml", "NoScripts.xsl", false, 2)]
    //     //[Variation(id = 3, Desc = "TemporaryFiles after load in Debug Mode with no script block, expected count = 0", Pri = 0, Params = new object[] { "books.xml", "NoScripts.xsl", true })]
    //     [InlineData("books.xml", "NoScripts.xsl", true, 3)]
    //     [Theory]
    //     public void TempFiles2(object param0, object param1, object param2, object id)
    //     {
    //         if (_isInProc && (int)id == 3)
    //             return; //TEST_SKIPPED;

    //         Init(param0.ToString(), param1.ToString(), (bool)param2);
    //         _xsl.Load(_xslFile);

    //         VerifyResult(_xsl.TemporaryFiles.Count, 0, "There should be no temp files generated, when there are no script block");
    //     }

    //     //[Variation(id = 4, Desc = "TemporaryFiles after load in Retail Mode with script block and EnableScript", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", false })]
    //     [ActiveIssue(9873)]
    //     [InlineData("books.xml", "TempFiles.xsl", false)]
    //     [Theory]
    //     public void TempFiles3(object param0, object param1, object param2)
    //     {
    //         Init(param0.ToString(), param1.ToString(), (bool)param2);

    //         if (_isInProc)
    //             return; //TEST_SKIPPED;

    //         _xsl.Load(_xslFile, new XsltSettings(false, true), new XmlUrlResolver());

    //         //In retail mode temporary files are not generated if a script block exist
    //         VerifyResult(_xsl.TemporaryFiles.Count, 0, "TemporaryFiles generated in retail mode");
    //     }

    //     //[Variation(id = 5, Desc = "TemporaryFiles after load in Debug Mode with script block and EnableScript", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
    //     [ActiveIssue(9873)]
    //     [InlineData("books.xml", "TempFiles.xsl", true)]
    //     [Theory]
    //     public void TempFiles3AndHalf(object param0, object param1, object param2)
    //     {
    //         Init(param0.ToString(), param1.ToString(), (bool)param2);

    //         if (_isInProc)
    //             return; //TEST_SKIPPED;

    //         _xsl.Load(_xslFile, new XsltSettings(false, true), new XmlUrlResolver());

    //         //In debug mode temporary files are generated if a script block exist
    //         //An extra .pdb info is generated if debugging is enabled
    //         VerifyResult(_xsl.TemporaryFiles.Count > 0, "TemporaryFiles must be generated when there is a script block");
    //     }

    //     //[Variation(id = 6, Desc = "TemporaryFiles after load in retail mode with script block and default settings", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", false })]
    //     [InlineData("books.xml", "TempFiles.xsl", false, 6)]
    //     //[Variation(id = 7, Desc = "TemporaryFiles after load in debug mode with script block and default settings", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
    //     [InlineData("books.xml", "TempFiles.xsl", true, 7)]
    //     [Theory]
    //     public void TempFiles4(object param0, object param1, object param2, object id)
    //     {
    //         if (_isInProc && (int)id == 7)
    //             return; //TEST_SKIPPED;

    //         Init(param0.ToString(), param1.ToString(), (bool)param2);
    //         _xsl.Load(_xslFile, XsltSettings.Default, new XmlUrlResolver());

    //         VerifyResult(_xsl.TemporaryFiles.Count, 0, "TemporaryFiles must not be generated, when XsltSettings is None");
    //     }

    //     //[Variation(id = 8, Desc = "TemporaryFiles after load in retail mode with script block and EnableDocumentFunction", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", false })]
    //     [InlineData("books.xml", "TempFiles.xsl", false, 8)]
    //     //[Variation(id = 9, Desc = "TemporaryFiles after load in debug mode with script block and EnableDocumentFunction", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
    //     [InlineData("books.xml", "TempFiles.xsl", true, 9)]
    //     [Theory]
    //     public void TempFiles5(object param0, object param1, object param2, object id)
    //     {
    //         if (_isInProc && (int)id == 9)
    //             return; //TEST_SKIPPED;

    //         Init(param0.ToString(), param1.ToString(), (bool)param2);
    //         _xsl.Load(_xslFile, new XsltSettings(true, false), new XmlUrlResolver());

    //         VerifyResult(_xsl.TemporaryFiles.Count, 0, "TemporaryFiles must not be generated, when XsltSettings is EnableDocumentFunction alone");
    //     }

    //     //[Variation(id = 10, Desc = "Verify the existence of TemporaryFiles after load in debug mode with script block", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
    //     [ActiveIssue(9873)]
    //     [InlineData("books.xml", "TempFiles.xsl", true)]
    //     [Theory]
    //     public void TempFiles6(object param0, object param1, object param2)
    //     {
    //         Init(param0.ToString(), param1.ToString(), (bool)param2);

    //         if (_isInProc)
    //             return; //TEST_SKIPPED;

    //         _xsl.Load(_xslFile, new XsltSettings(false, true), new XmlUrlResolver());

    //         foreach (string filename in _xsl.TemporaryFiles)
    //         {
    //             _output.WriteLine(filename);
    //             Assert.True(File.Exists(filename)); //Temporary file
    //         }
    //         return;
    //     }

    //     //[Variation(id = 11, Desc = "Verify if the user can delete the TemporaryFiles after load in debug mode with script block", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
    //     [ActiveIssue(9873)]
    //     [InlineData("books.xml", "TempFiles.xsl", true)]
    //     [Theory]
    //     public void TempFiles7(object param0, object param1, object param2)
    //     {
    //         Init(param0.ToString(), param1.ToString(), (bool)param2);

    //         if (_isInProc)
    //             return; //Test_SKIPPED;

    //         _xsl.Load(_xslFile, new XsltSettings(false, true), new XmlUrlResolver());

    //         TempFileCollection tempFiles = _xsl.TemporaryFiles;

    //         int fileCount = tempFiles.Count;
    //         tempFiles.Delete();
    //         fileCount = tempFiles.Count;

    //         VerifyResult(fileCount, 0, "Temp files could not be deleted");
    //     }

    //     //[Variation(id = 12, Desc = "Verify if the user can rename the TemporaryFiles after load in debug mode with script block", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
    //     [ActiveIssue(9873)]
    //     [InlineData("books.xml", "TempFiles.xsl", true)]
    //     [Theory]
    //     public void TempFiles8(object param0, object param1, object param2)
    //     {
    //         Init(param0.ToString(), param1.ToString(), (bool)param2);

    //         if (_isInProc)
    //             return; //Test_SKIPPED;

    //         _xsl.Load(_xslFile, new XsltSettings(false, true), new XmlUrlResolver());

    //         TempFileCollection tempFiles = _xsl.TemporaryFiles;

    //         string newfilename = string.Empty;
    //         foreach (string filename in tempFiles)
    //         {
    //             string tempdir = filename.Substring(0, filename.LastIndexOf("\\") + 1);
    //             newfilename = tempdir + "new" + filename.Substring(filename.LastIndexOf("\\") + 1);
    //             File.Move(filename, newfilename);
    //         }

    //         return;
    //     }

    //     //[Variation(id = 13, Desc = "Verify if the necessary files are generated after load in debug mode with script block", Pri = 1, Params = new object[] { "books.xml", "TempFiles.xsl", true })]
    //     [ActiveIssue(9873)]
    //     [InlineData("books.xml", "TempFiles.xsl", true)]
    //     [Theory]
    //     public void TempFiles9(object param0, object param1, object param2)
    //     {
    //         Init(param0.ToString(), param1.ToString(), (bool)param2);

    //         if (_isInProc)
    //             return; //Test_SKIPPED;

    //         _xsl.Load(_xslFile, new XsltSettings(false, true), new XmlUrlResolver());

    //         TempFileCollection tempFiles = _xsl.TemporaryFiles;
    //         string filelist = string.Empty;

    //         foreach (string filename in tempFiles)
    //         {
    //             filelist += filename.Substring(filename.LastIndexOf("\\") + 1) + ";";
    //         }

    //         _output.WriteLine("Verifying the existence of .dll, .pdb, .err, .out, .cmdline, .cs and .tmp");
    //         Assert.True(filelist.IndexOf(".dll") > 0); //Script DLL tempfile is not generated
    //         Assert.True(filelist.IndexOf(".pdb") > 0); //Debug info tempfile is not generated
    //         Assert.True(filelist.IndexOf(".err") > 0); //Error tempfile is not generated
    //         Assert.True(filelist.IndexOf(".out") > 0); //Output tempfile is not generated
    //         Assert.True(filelist.IndexOf(".cmdline") > 0); //Command Line tempfile is not generated
    //         Assert.True(filelist.IndexOf(".cs") > 0); //CSharp tempfile is not generated
    //         Assert.True(filelist.IndexOf(".tmp") > 0); //Tempfile is not generated

    //         return;
    //     }

    //     //[Variation(id = 14, Desc = "TemporaryFiles after unsuccessful load of an invalid stylesheet in debug mode with script block", Pri = 2, Params = new object[] { "books.xml", "Invalid.xsl", true })]
    //     [InlineData("books.xml", "Invalid.xsl", true)]
    //     [Theory]
    //     public void TempFiles10(object param0, object param1, object param2)
    //     {
    //         TempFileCollection tempFiles = null;
    //         Init(param0.ToString(), param1.ToString(), (bool)param2);

    //         try
    //         {
    //             _xsl.Load(_xslFile, new XsltSettings(false, true), new XmlUrlResolver());
    //         }
    //         catch (XsltException e)
    //         {
    //             _output.WriteLine(e.ToString());
    //             tempFiles = _xsl.TemporaryFiles;
    //         }

    //         VerifyResult(tempFiles, null, "Temporary files must not be generated");
    //     }

    //     //[Variation(id = 15, Desc = "TemporaryFiles after unsuccessful load of a valid stylesheet in debug mode with a missing function in the script block", Pri = 2, Params = new object[] { "books.xml", "InvalidFn.xsl", true })]
    //     [ActiveIssue(9873)]
    //     [InlineData("books.xml", "InvalidFn.xsl", true)]
    //     [Theory]
    //     public void TempFiles11(object param0, object param1, object param2)
    //     {
    //         TempFileCollection tempFiles = null;
    //         Init(param0.ToString(), param1.ToString(), (bool)param2);

    //         if (_isInProc)
    //             return; //Test_SKIPPED;

    //         try
    //         {
    //             _xsl.Load(_xslFile, new XsltSettings(false, true), new XmlUrlResolver());
    //         }
    //         catch (XsltException e)
    //         {
    //             _output.WriteLine(e.ToString());
    //             tempFiles = _xsl.TemporaryFiles;
    //         }

    //         VerifyResult(tempFiles != null, "Temporary files should have been generated even when Load() is unsuccessful");
    //     }

    //     //[Variation(Desc = "Load File from a drive c:", Pri = 2)]
    //     [InlineData()]
    //     [Theory]
    //     public void TempFiles12()
    //     {
    //         if (_isInProc)
    //             return; //Test_SKIPPED;

    //         string childFile = Path.Combine(Directory.GetCurrentDirectory(), "child.xsl");

    //         string parentString = "<?xml version=\"1.0\"?>"
    //             + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
    //             + "<xsl:import href=\"" + childFile + "\"/>"
    //             + "<xsl:output method=\"xml\" omit-xml-declaration=\"yes\" indent=\"yes\"/>"
    //             + "<xsl:template match=\"book[@style='autobiography']\">"
    //             + "<SPAN style=\"color=blue\">From B<xsl:value-of select=\"name()\"/> : <xsl:value-of select=\"title\"/>"
    //             + "</SPAN><br/>"
    //             + "<xsl:apply-templates />"
    //             + "</xsl:template>"
    //             + "<xsl:template match=\"text()\" >"
    //             + "</xsl:template>"
    //             + "</xsl:stylesheet>";

    //         string childString = "<?xml version=\"1.0\"?>"
    //             + "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\">"
    //             + "<xsl:output method=\"xml\" omit-xml-declaration=\"yes\" indent=\"yes\"/>"
    //             + "<xsl:template match=\"book[@style='autobiography']\">"
    //             + "<SPAN style=\"color=blue\">From B<xsl:value-of select=\"name()\"/> : <xsl:value-of select=\"title\"/>"
    //             + "</SPAN><br/>"
    //             + "<xsl:apply-templates />"
    //             + "</xsl:template>"
    //             + "<xsl:template match=\"text()\" >"
    //             + "</xsl:template>"
    //             + "</xsl:stylesheet>";

    //         try
    //         {
    //             // create a xsl file in current directory on some drive, this is included in XSL above
    //             StreamWriter file = new StreamWriter(new FileStream(childFile, FileMode.Create, FileAccess.Write));
    //             file.WriteLine(childString);
    //             file.Dispose();
    //             StreamWriter parentFile = new StreamWriter(new FileStream("parent.xsl", FileMode.Create, FileAccess.Write));
    //             parentFile.WriteLine(parentString);
    //             parentFile.Dispose();
    //         }
    //         catch (Exception e)
    //         {
    //             _output.WriteLine(e.ToString());
    //             Assert.True(false);
    //         }

    //         try
    //         {
    //             // initialize XslCompiledTransform instance
    //             _xsl = new XslCompiledTransform();
    //
    //             // Now let's load the parent xsl file
    //             _xsl.Load("parent.xsl", new XsltSettings(false, true), new XmlUrlResolver());
    //         }
    //         catch (XsltException e)
    //         {
    //             _output.WriteLine(e.ToString());
    //             Assert.True(false);
    //         }
    //         return;
    //     }

    //     //[Variation(Desc = "Bug 469775 - XSLT V2 : Exception thrown if xsl:preserve-space/xsl:strip-space is used and input document contains entities", Pri = 2)]
    //     [InlineData()]
    //     [Theory]
    //     public void TempFiles13()
    //     {
    //         try
    //         {
    //             XslCompiledTransform xslt = new XslCompiledTransform();
    //             xslt.Load(FullFilePath("bug469775.xsl"));
    //             Stream res = new MemoryStream();
    //             xslt.Transform(new XmlTextReader(FullFilePath("bug469775.xml")), (XsltArgumentList)null, res);
    //             _output.WriteLine(res.ToString());
    //         }
    //         catch (System.Xml.XmlException)
    //         {
    //             Assert.True(false);
    //         }
    //         return;
    //     }

    //     //[Variation(Desc = "Bug 469770 - XslCompiledTransform failed to load embedded stylesheets when prefixes are defined outside of xsl:stylesheet element", Pri = 2)]
    //     [InlineData()]
    //     [Theory]
    //     public void TempFiles14()
    //     {
    //         try
    //         {
    //             string xsl = "<root xmlns:ns=\"testing\">"
    //                 + "<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\">"
    //                 + "<xsl:template match=\"/\">"
    //                 + "<xsl:value-of select=\"ns:test\" />"
    //                 + "</xsl:template>"
    //                 + "</xsl:stylesheet>"
    //                 + "</root>";

    //             XmlReader r = XmlReader.Create(new StringReader(xsl));
    //             while (r.NodeType != XmlNodeType.Element || r.LocalName != "stylesheet")
    //             {
    //                 if (!r.Read())
    //                 {
    //                     _output.WriteLine("There is no 'stylesheet' element in the file");
    //                     Assert.True(false);
    //                 }
    //             }

    //             XslCompiledTransform t = new XslCompiledTransform();
    //             t.Load(r);
    //         }
    //         catch (XsltException exception)
    //         {
    //             _output.WriteLine("The following exception should not have been thrown");
    //             _output.WriteLine(exception.ToString());
    //             Assert.True(false);
    //         }

    //         return;
    //     }

    //     //[Variation(Desc = "Bug 482971 - XslCompiledTransform cannot output numeric character reference after long output", Pri = 2)]
    //     [InlineData()]
    //     [Theory]
    //     public void TempFiles15()
    //     {
    //         try
    //         {
    //             XslCompiledTransform xslt = new XslCompiledTransform();
    //             xslt.Load(FullFilePath("bug482971.xsl"));
    //             xslt.Transform(FullFilePath("bug482971.xml"), "out.txt");
    //         }
    //         catch (Exception exception)
    //         {
    //             _output.WriteLine("No exception should not have been thrown");
    //             _output.WriteLine(exception.ToString());
    //             Assert.True(false);
    //         }
    //         return;
    //     }
    // }
}