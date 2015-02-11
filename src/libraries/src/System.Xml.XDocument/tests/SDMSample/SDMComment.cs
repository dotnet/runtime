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
            public partial class SDM_Comment : XLinqTestCase
            {
                /// <summary>
                /// Tests the Comment constructor that takes a value.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "CreateCommentSimple")]
                public void CreateCommentSimple()
                {
                    try
                    {
                        new XComment((string)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    XComment c = new XComment("foo");
                    Validate.IsEqual(c.Value, "foo");
                    Validate.IsNull(c.Parent);
                }

                /// <summary>
                /// Tests the Comment constructor that operated from an XmlReader.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "CreateCommentFromReader")]
                public void CreateCommentFromReader()
                {
                    TextReader textReader = new StringReader("<x><!-- 12345678 --></x>");
                    XmlReader xmlReader = XmlReader.Create(textReader);
                    // Advance to the Comment and construct.
                    xmlReader.Read();
                    xmlReader.Read();
                    XComment c = (XComment)XNode.ReadFrom(xmlReader);

                    Validate.IsEqual(c.Value, " 12345678 ");
                }

                /// <summary>
                /// Validates the behavior of the Equals overload on XComment.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "CommentEquals")]
                public void CommentEquals()
                {
                    XComment c1 = new XComment("xxx");
                    XComment c2 = new XComment("xxx");
                    XComment c3 = new XComment("yyy");

                    bool b1 = c1.Equals(null);
                    bool b2 = c1.Equals("foo");
                    bool b3 = c1.Equals(c1);
                    bool b4 = c1.Equals(c2);
                    bool b5 = c1.Equals(c3);

                    Validate.IsEqual(b1, false);
                    Validate.IsEqual(b2, false);
                    Validate.IsEqual(b3, true);
                    Validate.IsEqual(b4, false);
                    Validate.IsEqual(b5, false);
                }

                /// <summary>
                /// Validates the behavior of the DeepEquals overload on XComment.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "Comment DeepEquals")]
                public void CommentDeepEquals()
                {
                    XComment c1 = new XComment("xxx");
                    XComment c2 = new XComment("xxx");
                    XComment c3 = new XComment("yyy");

                    bool b1 = XNode.DeepEquals(c1, (XComment)null);
                    bool b3 = XNode.DeepEquals(c1, c1);
                    bool b4 = XNode.DeepEquals(c1, c2);
                    bool b5 = XNode.DeepEquals(c1, c3);

                    Validate.IsEqual(b1, false);
                    Validate.IsEqual(b3, true);
                    Validate.IsEqual(b4, true);
                    Validate.IsEqual(b5, false);

                    b1 = XNode.EqualityComparer.GetHashCode(c1) == XNode.EqualityComparer.GetHashCode(c2);
                    Validate.IsEqual(b1, true);
                }


                /// <summary>
                /// Validates the behavior of the Value property on XComment.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "CommentValue")]
                public void CommentValue()
                {
                    XComment c = new XComment("xxx");
                    Validate.IsEqual(c.Value, "xxx");

                    // Null value not allowed.
                    try
                    {
                        c.Value = null;
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Try setting a value.
                    c.Value = "abcd";
                    Validate.IsEqual(c.Value, "abcd");
                }

                /// <summary>
                /// Tests the WriteTo method on XComment.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "CommentWriteTo")]
                public void CommentWriteTo()
                {
                    XComment c = new XComment("abcd ");

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
                        "<?xml version=\"1.0\" encoding=\"utf-16\"?><x><!--abcd --></x>");
                }
            }
        }
    }
}