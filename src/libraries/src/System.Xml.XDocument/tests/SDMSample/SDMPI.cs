// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class SDMSamplesTests : XLinqTestCase
        {
            public partial class SDM__PI : XLinqTestCase
            {
                /// <summary>
                /// Tests the ProcessingInstruction constructor that takes a value.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "CreateProcessingInstructionSimple")]
                public void CreateProcessingInstructionSimple()
                {
                    try
                    {
                        new XProcessingInstruction(null, "abcd");
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    try
                    {
                        new XProcessingInstruction("abcd", null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    XProcessingInstruction c = new XProcessingInstruction("foo", "bar");
                    Validate.IsEqual(c.Target, "foo");
                    Validate.IsEqual(c.Data, "bar");
                    Validate.IsNull(c.Parent);
                }

                /// <summary>
                /// Tests the ProcessingInstruction constructor that operated from an XmlReader.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "CreateProcessingInstructionFromReader")]
                public void CreateProcessingInstructionFromReader()
                {
                    TextReader textReader = new StringReader("<x><?target data?></x>");
                    XmlReader xmlReader = XmlReader.Create(textReader);
                    // Advance to the processing instruction and construct.
                    xmlReader.Read();
                    xmlReader.Read();
                    XProcessingInstruction c = (XProcessingInstruction)XNode.ReadFrom(xmlReader);

                    Validate.IsEqual(c.Target, "target");
                    Validate.IsEqual(c.Data, "data");
                }

                /// <summary>
                /// Validates the behavior of the Equals overload on XProcessingInstruction.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ProcessingInstructionEquals")]
                public void ProcessingInstructionEquals()
                {
                    XProcessingInstruction c1 = new XProcessingInstruction("targetx", "datax");
                    XProcessingInstruction c2 = new XProcessingInstruction("targetx", "datay");
                    XProcessingInstruction c3 = new XProcessingInstruction("targety", "datax");
                    XProcessingInstruction c4 = new XProcessingInstruction("targety", "datay");
                    XProcessingInstruction c5 = new XProcessingInstruction("targetx", "datax");

                    bool b1 = XNode.DeepEquals(c1, (XProcessingInstruction)null);
                    bool b3 = XNode.DeepEquals(c1, c1);
                    bool b4 = XNode.DeepEquals(c1, c2);
                    bool b5 = XNode.DeepEquals(c1, c3);
                    bool b6 = XNode.DeepEquals(c1, c4);
                    bool b7 = XNode.DeepEquals(c1, c5);

                    Validate.IsEqual(b1, false);
                    Validate.IsEqual(b3, true);
                    Validate.IsEqual(b4, false);
                    Validate.IsEqual(b5, false);
                    Validate.IsEqual(b6, false);
                    Validate.IsEqual(b7, true);

                    b1 = XNode.EqualityComparer.GetHashCode(c1) == XNode.EqualityComparer.GetHashCode(c5);
                    Validate.IsEqual(b1, true);
                }

                /// <summary>
                /// Validates the behavior of the Target and Data properties on XProcessingInstruction.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ProcessingInstructionValues")]
                public void ProcessingInstructionValues()
                {
                    XProcessingInstruction c = new XProcessingInstruction("xxx", "yyy");
                    Validate.IsEqual(c.Target, "xxx");
                    Validate.IsEqual(c.Data, "yyy");

                    // Null values not allowed.
                    try
                    {
                        c.Target = null;
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    try
                    {
                        c.Data = null;
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Try setting values.
                    c.Target = "abcd";
                    Validate.IsEqual(c.Target, "abcd");

                    c.Data = "efgh";
                    Validate.IsEqual(c.Data, "efgh");
                    Validate.IsEqual(c.Target, "abcd");
                }

                /// <summary>
                /// Tests the WriteTo method on XComment.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ProcessingInstructionWriteTo")]
                public void ProcessingInstructionWriteTo()
                {
                    XProcessingInstruction c = new XProcessingInstruction("target", "data");

                    // Null writer not allowed.
                    try
                    {
                        c.WriteTo(null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test.
                    StringBuilder stringBuilder = new StringBuilder();
                    XmlWriter xmlWriter = XmlWriter.Create(stringBuilder);

                    xmlWriter.WriteStartElement("x");
                    c.WriteTo(xmlWriter);
                    xmlWriter.WriteEndElement();

                    xmlWriter.Flush();

                    Validate.IsEqual(
                        stringBuilder.ToString(),
                        "<?xml version=\"1.0\" encoding=\"utf-16\"?><x><?target data?></x>");
                }
            }
        }
    }
}