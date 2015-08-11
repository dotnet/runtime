// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class SDMSamplesTests : XLinqTestCase
        {
            public partial class SDM_LoadSave : XLinqTestCase
            {
                /// <summary>
                /// Test loading a document from an XmlReader.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "DocumentLoadFromXmlReader")]
                public void DocumentLoadFromXmlReader()
                {
                    // Null reader not allowed.
                    try
                    {
                        XDocument.Load((XmlReader)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Extra content at end of reader.
                    StringReader stringReader = new StringReader("<x/><y/>");
                    using (XmlReader xmlReader = XmlReader.Create(stringReader))
                    {
                        try
                        {
                            XDocument.Load(xmlReader);
                            Validate.ExpectedThrow(typeof(XmlException));
                        }
                        catch (Exception ex)
                        {
                            Validate.Catch(ex, typeof(XmlException));
                        }
                    }

                    // Empty content.
                    stringReader = new StringReader("");
                    using (XmlReader xmlReader = XmlReader.Create(stringReader))
                    {
                        try
                        {
                            XDocument.Load(xmlReader);
                            Validate.ExpectedThrow(typeof(XmlException));
                        }
                        catch (Exception ex)
                        {
                            Validate.Catch(ex, typeof(XmlException));
                        }
                    }

                    // No root element.
                    stringReader = new StringReader("<!-- comment -->");
                    using (XmlReader xmlReader = XmlReader.Create(stringReader))
                    {
                        try
                        {
                            XDocument.Load(xmlReader);
                            Validate.ExpectedThrow(typeof(XmlException));
                        }
                        catch (Exception ex)
                        {
                            Validate.Catch(ex, typeof(XmlException));
                        }
                    }

                    // Reader mispositioned, so not at eof when done
                    stringReader = new StringReader("<x></x>");
                    using (XmlReader xmlReader = XmlReader.Create(stringReader))
                    {
                        // Position the reader on the end element.
                        xmlReader.Read();
                        xmlReader.Read();

                        try
                        {
                            XDocument.Load(xmlReader);
                            Validate.ExpectedThrow(typeof(InvalidOperationException));
                        }
                        catch (Exception ex)
                        {
                            Validate.Catch(ex, typeof(InvalidOperationException));
                        }
                    }

                    // Reader mispositioned, so empty root when done
                    stringReader = new StringReader("<x></x>");
                    using (XmlReader xmlReader = XmlReader.Create(stringReader))
                    {
                        // Position the reader at eof.
                        xmlReader.Read();
                        xmlReader.Read();
                        xmlReader.Read();

                        try
                        {
                            XDocument.Load(xmlReader);
                            Validate.ExpectedThrow(typeof(InvalidOperationException));
                        }
                        catch (Exception ex)
                        {
                            Validate.Catch(ex, typeof(InvalidOperationException));
                        }
                    }
                }

                /// <summary>
                /// Tests the Save overloads on document, that write to an XmlWriter.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "DocumentSaveToXmlWriter")]
                public void DocumentSaveToXmlWriter()
                {
                    XDocument ee = new XDocument();
                    try
                    {
                        ee.Save((XmlWriter)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }
                }

                /// <summary>
                /// Tests WriteTo on document.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "DocumentWriteTo")]
                public void DocumentWriteTo()
                {
                    XDocument ee = new XDocument();
                    try
                    {
                        ee.WriteTo(null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }
                }

                /// <summary>
                /// Test loading an element from an XmlReader.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementLoadFromXmlReader")]
                public void ElementLoadFromXmlReader()
                {
                    // Null reader not allowed.
                    try
                    {
                        XElement.Load((XmlReader)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Extra stuff in xml after the element is not allowed
                    StringReader reader = new StringReader("<abc><def/></abc>");
                    XmlReader xmlreader = XmlReader.Create(reader);
                    xmlreader.Read();
                    xmlreader.Read();   // position on <def>

                    try
                    {
                        XElement.Load(xmlreader);
                        Validate.ExpectedThrow(typeof(InvalidOperationException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(InvalidOperationException));
                    }

                    reader.Dispose();
                    xmlreader.Dispose();
                }


                /// <summary>
                /// Tests the Save overloads on element, that write to an XmlWriter.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementSaveToXmlWriter")]
                public void ElementSaveToXmlWriter()
                {
                    XElement ee = new XElement("x");
                    try
                    {
                        ee.Save((XmlWriter)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }
                }

                /// <summary>
                /// Gets the full path of the RuntimeTestXml directory, allowing for the
                /// possibility of being run from the VS build (bin\debug, etc).
                /// </summary>
                /// <returns></returns>
                internal string GetTestXmlDirectory()
                {
                    const string TestXmlDirectoryName = "RuntimeTestXml";
                    string baseDir = Path.Combine(XLinqTestCase.RootPath, "TestData", "XLinq");
                    string dir = Path.Combine(baseDir, TestXmlDirectoryName);
                    return dir;
                }

                /// <summary>
                /// Gets the filenames of all files in the RuntimeTestXml directory.
                /// We use all files we find in a directory with a known name and location.
                /// </summary>
                /// <returns></returns>
                internal string[] GetTestXmlFilenames()
                {
                    string[] filenames = new string[] {
                        GetTestXmlFilename("LoadSave0.xml"),
                        GetTestXmlFilename("LoadSave1.xml"),
                        GetTestXmlFilename("LoadSave2.xml"),
                        GetTestXmlFilename("LoadSave3.xml"),
                        GetTestXmlFilename("LoadSave4.xml")};

                    if (filenames.Length == 0)
                    {
                        throw new TestFailedException("No files in xml directory for tests");
                    }

                    return filenames;
                }

                /// <summary>
                /// Gets the filenames of all files in the RuntimeTestXml directory.
                /// We use all files we find in a directory with a known name and location.
                /// </summary>
                /// <returns></returns>
                internal string GetTestXmlFilename(string filename)
                {
                    string directory = GetTestXmlDirectory();
                    string fullName = Path.Combine(directory, filename);

                    return fullName;
                }
            }
        }
    }
}