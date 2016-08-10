// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    [TestCase(Name = "Create Overloads", Desc = "Create Overloads")]
    public partial class TCCreateOverloads : TCXMLReaderBaseGeneral
    {
        private string _sampleFileName = @"sample.xml";
        private string _sampleXml = "<root><a/></root>";
        private void CreateFile()
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            sw.WriteLine(_sampleXml);
            sw.Flush();
            FilePathUtil.addStream(_sampleFileName, ms);
        }

        public override int Init(object o)
        {
            CreateFile();
            return base.Init(o);
        }

        public Stream GetStream()
        {
            return FilePathUtil.getStream(_sampleFileName);
        }

        public String GetUrl()
        {
            return _sampleFileName;
        }

        public TextReader GetTextReader()
        {
            return new StringReader(_sampleXml);
        }

        public string GetBaseUri()
        {
            return _sampleFileName;
        }

        public XmlReaderSettings GetSettings()
        {
            return new XmlReaderSettings();
        }

        public XmlReader GetXmlReader()
        {
            return ReaderHelper.Create(GetTextReader());
        }

        public XmlParserContext GetParserContext()
        {
            NameTable nt = new NameTable();
            XmlParserContext pc = new XmlParserContext(nt, new XmlNamespaceManager(nt), null, XmlSpace.Default);
            return pc;
        }

        public class ReaderDelegate
        {
            public static bool Create(String url)
            {
                XmlReader reader = null;
                try
                {
                    reader = ReaderHelper.Create(url);
                    while (reader.Read()) ;

                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    CError.WriteLineIgnore(ane.ToString());
                    return false;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }
            }


            public static bool Create(Stream input)
            {
                XmlReader reader = null;
                try
                {
                    reader = ReaderHelper.Create(input);
                    while (reader.Read()) ;
                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    CError.WriteLineIgnore(ane.ToString());
                    return false;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }
            }
            public static bool Create(TextReader input)
            {
                XmlReader reader = null;
                try
                {
                    reader = ReaderHelper.Create(input);
                    while (reader.Read()) ;
                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    CError.WriteLineIgnore(ane.ToString());
                    return false;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }
            }

            public static bool Create(String url, XmlReaderSettings settings)
            {
                XmlReader reader = null;
                try
                {
                    reader = ReaderHelper.Create(url, settings);
                    while (reader.Read()) ;
                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    CError.WriteLineIgnore(ane.ToString());
                    return false;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }
            }

            public static bool Create(XmlReader input, XmlReaderSettings settings)
            {
                XmlReader reader = null;
                try
                {
                    reader = ReaderHelper.Create(input, settings);
                    while (reader.Read()) ;
                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    CError.WriteLineIgnore(ane.ToString());
                    return false;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }
            }

            public static bool Create(Stream input, XmlReaderSettings settings)
            {
                XmlReader reader = null;
                try
                {
                    reader = ReaderHelper.Create(input, settings);
                    while (reader.Read()) ;
                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    CError.WriteLineIgnore(ane.ToString());
                    return false;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }
            }

            public static bool Create(TextReader input, XmlReaderSettings settings)
            {
                XmlReader reader = null;
                try
                {
                    reader = ReaderHelper.Create(input, settings);
                    while (reader.Read()) ;
                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    CError.WriteLineIgnore(ane.ToString());
                    return false;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }
            }

            public static bool Create(Stream input, XmlReaderSettings settings, String baseUri)
            {
                XmlReader reader = null;
                try
                {
                    reader = ReaderHelper.Create(input, settings, baseUri);
                    while (reader.Read()) ;
                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    CError.WriteLineIgnore(ane.ToString());
                    return false;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }
            }

            public static bool Create(Stream input, XmlReaderSettings settings, XmlParserContext parserContext)
            {
                XmlReader reader = null;
                try
                {
                    reader = ReaderHelper.Create(input, settings, parserContext);
                    while (reader.Read()) ;
                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    CError.WriteLineIgnore(ane.ToString());
                    return false;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }
            }

            public static bool Create(TextReader input, XmlReaderSettings settings, String baseUri)
            {
                XmlReader reader = null;
                try
                {
                    reader = ReaderHelper.Create(input, settings, baseUri);
                    while (reader.Read()) ;
                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    CError.WriteLineIgnore(ane.ToString());
                    return false;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }
            }

            public static bool Create(TextReader input, XmlReaderSettings settings, XmlParserContext parserContext)
            {
                XmlReader reader = null;
                try
                {
                    reader = ReaderHelper.Create(input, settings, parserContext);
                    while (reader.Read()) ;
                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    CError.WriteLineIgnore(ane.ToString());
                    return false;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }
            }

            public static bool Create(String url, XmlReaderSettings settings, XmlParserContext context)
            {
                XmlReader reader = null;
                try
                {
                    reader = ReaderHelper.Create(url, settings, context);
                    while (reader.Read()) ;
                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    CError.WriteLineIgnore(ane.ToString());
                    return false;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }
            }
        }

        [Variation("Null Input")]
        public int v1()
        {
            CError.Equals(ReaderDelegate.Create((Stream)null), false, "Null Stream doesn't throw error1");
            CError.Equals(ReaderDelegate.Create((Stream)null, GetSettings(), GetBaseUri()), false, "Null Stream doesn't throw error2");
            CError.Equals(ReaderDelegate.Create((Stream)null, GetSettings()), false, "Null Stream doesn't throw error3");

            CError.Equals(ReaderDelegate.Create((string)null), false, "Null URL doesn't throw error1");
            CError.Equals(ReaderDelegate.Create((string)null, GetSettings()), false, "Null URL doesn't throw error2");
            CError.Equals(ReaderDelegate.Create((string)null, GetSettings(), GetParserContext()), false, "Null URL doesn't throw error3");

            CError.Equals(ReaderDelegate.Create((TextReader)null), false, "Null TextReader doesn't throw error1");
            CError.Equals(ReaderDelegate.Create((TextReader)null, GetSettings(), GetBaseUri()), false, "Null TextReader doesn't throw error2");
            CError.Equals(ReaderDelegate.Create((TextReader)null, GetSettings()), false, "Null TextReader doesn't throw error2");

            return TEST_PASS;
        }

        [Variation("Valid Input")]
        public int v2()
        {
            XmlReader r = null;

            Stream s = GetStream();
            r = ReaderHelper.Create(s);
            while (r.Read()) ;
            r.Dispose();

            r = ReaderHelper.Create(GetUrl());
            while (r.Read()) ;
            r.Dispose();

            r = ReaderHelper.Create(GetTextReader());
            while (r.Read()) ;
            r.Dispose();

            return TEST_PASS;
        }


        [Variation("Null Settings")]
        public int v3()
        {
            CError.Equals(ReaderDelegate.Create(GetStream(), null, GetParserContext()), true, "StreamOverload2");
            CError.Equals(ReaderDelegate.Create(GetStream(), null), true, "StreamOverload3");

            CError.Equals(ReaderDelegate.Create(GetUrl(), null), true, "URL Overload 1");
            CError.Equals(ReaderDelegate.Create(GetUrl(), null, GetParserContext()), true, "URL Overload 2");

            CError.Equals(ReaderDelegate.Create(GetTextReader(), null, GetParserContext()), true, "TextReader Overload2");
            CError.Equals(ReaderDelegate.Create(GetTextReader(), null), true, "TextReader Overload3");

            CError.Equals(ReaderDelegate.Create(GetXmlReader(), null), true, "XmlReader Overload1");
            return TEST_PASS;
        }

        [Variation("Null ParserContext")]
        public int v5()
        {
            CError.Equals(ReaderDelegate.Create(GetStream(), GetSettings(), (string)null), true, "StreamOverload3");
            CError.Equals(ReaderDelegate.Create(GetStream(), GetSettings(), (XmlParserContext)null), true, "StreamOverload4");

            CError.Equals(ReaderDelegate.Create(GetTextReader(), GetSettings(), (string)null), true, "TextOverload3");
            CError.Equals(ReaderDelegate.Create(GetTextReader(), GetSettings(), GetParserContext()), true, "TextOverload4");

            return TEST_PASS;
        }

        [Variation("Valid Settings")]
        public int v6()
        {
            CError.Equals(ReaderDelegate.Create(GetStream(), GetSettings(), (string)null), true, "StreamOverload2");
            CError.Equals(ReaderDelegate.Create(GetStream(), GetSettings(), (XmlParserContext)null), true, "StreamOverload2");

            CError.Equals(ReaderDelegate.Create(GetUrl(), GetSettings()), true, "URL Overload 1");
            CError.Equals(ReaderDelegate.Create(GetUrl(), GetSettings(), GetParserContext()), true, "URL Overload 2");

            CError.Equals(ReaderDelegate.Create(GetTextReader(), GetSettings(), (string)null), true, "TextReader Overload2");
            CError.Equals(ReaderDelegate.Create(GetTextReader(), GetSettings(), (XmlParserContext)null), true, "TextReader Overload2");

            CError.Equals(ReaderDelegate.Create(GetXmlReader(), GetSettings()), true, "XmlReader Overload1");
            return TEST_PASS;
        }

        [Variation("Valid ParserContext")]
        public int v7()
        {
            CError.Equals(ReaderDelegate.Create(GetStream(), GetSettings(), GetParserContext()), true, "StreamOverload3");

            CError.Equals(ReaderDelegate.Create(GetTextReader(), GetSettings(), GetParserContext()), true, "TextOverload3");

            return TEST_PASS;
        }
    }
}
