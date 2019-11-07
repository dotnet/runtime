// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public enum InsertType { Prepend, Append, InsertBefore, InsertAfter }

    public static class TestHelper
    {
        // A delegate to XmlNode.PrependChild or XmlNode.AppendChild
        public delegate XmlNode InsertFrontOrEnd(XmlNode parent, XmlNode newChild);

        // A delegate to XmlNode.InsertBefore or XmlNode.InsertAfter
        public delegate XmlNode InsertBeforeOrAfter(XmlNode parent, XmlNode newChild, XmlNode refChild);

        public static XmlNode CreateNode(XmlDocument doc, XmlNodeType nodeType)
        {
            Assert.NotNull(doc);

            switch (nodeType)
            {
                case XmlNodeType.CDATA:
                    return doc.CreateCDataSection(@"&lt; &amp; <tag> < ! > & </tag> 	 ");
                case XmlNodeType.Comment:
                    return doc.CreateComment(@"comment");
                case XmlNodeType.Element:
                    return doc.CreateElement("E");
                case XmlNodeType.Text:
                    return doc.CreateTextNode("text");
                case XmlNodeType.Whitespace:
                    return doc.CreateWhitespace(@"	  ");
                case XmlNodeType.SignificantWhitespace:
                    return doc.CreateSignificantWhitespace("	");
                default:
                    throw new ArgumentException("Wrong XmlNodeType: '" + nodeType + "'");
            }
        }

        public static XmlNode AppendChild(XmlNode parent, XmlNode newChild)
        {
            Assert.NotNull(parent);
            Assert.NotNull(newChild);
            return parent.AppendChild(newChild);
        }

        public static XmlNode PrependChild(XmlNode parent, XmlNode newChild)
        {
            Assert.NotNull(parent);
            Assert.NotNull(newChild);
            return parent.PrependChild(newChild);
        }

        public static XmlNode InsertBefore(XmlNode parent, XmlNode newChild, XmlNode refChild)
        {
            Assert.NotNull(parent);
            Assert.NotNull(newChild);
            return parent.InsertBefore(newChild, refChild);
        }

        public static XmlNode InsertAfter(XmlNode parent, XmlNode newChild, XmlNode refChild)
        {
            Assert.NotNull(parent);
            Assert.NotNull(newChild);
            return parent.InsertAfter(newChild, refChild);
        }

        public static InsertFrontOrEnd CreateInsertFrontOrEnd(InsertType insertType)
        {
            switch (insertType)
            {
                case InsertType.Prepend:
                    return new InsertFrontOrEnd(PrependChild);
                case InsertType.Append:
                    return new InsertFrontOrEnd(AppendChild);
                default:
                    throw new ArgumentException("Not supported InsertType='" + insertType + "'");
            }
        }

        public static InsertBeforeOrAfter CreateInsertBeforeOrAfter(InsertType insertType)
        {
            switch (insertType)
            {
                case InsertType.Prepend:
                case InsertType.InsertBefore:
                    return new InsertBeforeOrAfter(InsertBefore);
                case InsertType.Append:
                case InsertType.InsertAfter:
                    return new InsertBeforeOrAfter(InsertAfter);
                default:
                    throw new ArgumentException("Not supported InsertType '" + insertType + "'");
            }
        }

        /// <summary>
        /// Verify that child and newChild are children of parent
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        /// <param name="newChild"></param>
        public static void Verify(XmlNode parent, XmlNode child, XmlNode newChild)
        {
            Assert.NotNull(parent);
            Assert.NotNull(child);
            Assert.NotNull(newChild);

            Assert.Equal(child.ParentNode, parent);
            Assert.Equal(newChild.ParentNode, parent);
        }

        /// <summary>
        /// Verify that child and newChild are siblings according to insertType
        /// </summary>
        /// <param name="child"></param>
        /// <param name="newChild"></param>
        /// <param name="insertType"></param>
        public static void Verify(XmlNode child, XmlNode newChild, InsertType insertType)
        {
            Assert.NotNull(child);
            Assert.NotNull(newChild);

            switch (insertType)
            {
                case InsertType.Prepend:
                    Assert.Equal(child, newChild.NextSibling);
                    Assert.Equal(newChild, child.PreviousSibling);
                    Assert.Null(newChild.PreviousSibling);
                    break;
                case InsertType.Append:
                    Assert.Equal(newChild, child.NextSibling);
                    Assert.Equal(child, newChild.PreviousSibling);
                    Assert.Null(newChild.NextSibling);
                    break;
                default:
                    throw new ArgumentException("Wrong insert type: '" + insertType + "'");
            }
        }

        public static void VerifySiblings(XmlNode refChild, XmlNode newChild, InsertType insertType)
        {
            Assert.NotNull(refChild);
            Assert.NotNull(newChild);

            XmlNode prev = null;
            XmlNode next = null;

            switch (insertType)
            {
                case InsertType.Prepend:
                case InsertType.InsertBefore:
                    prev = newChild;
                    next = refChild;
                    break;
                case InsertType.Append:
                case InsertType.InsertAfter:
                    prev = refChild;
                    next = newChild;
                    break;
                default:
                    throw new ArgumentException("Wrong InsertType: '" + insertType + "'");
            }

            Assert.NotNull(prev);
            Assert.NotNull(next);

            Assert.Equal(next, prev.NextSibling);
            Assert.Equal(prev, next.PreviousSibling);
        }
    }
}
