// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Claunia.PropertyList
{
    /// <summary>Parses XML property lists.</summary>
    /// @author Daniel Dreibrodt
    /// @author Natalia Portillo
    public static class XmlPropertyListParser
    {
        /// <summary>Parses a XML property list file.</summary>
        /// <param name="f">The XML property list file.</param>
        /// <returns>The root object of the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        public static NSObject Parse(FileInfo f)
        {
            var doc = new XmlDocument();

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore
            };

            using(Stream stream = f.OpenRead())
                using(var reader = XmlReader.Create(stream, settings))
                    doc.Load(reader);

            return ParseDocument(doc);
        }

        /// <summary>Parses a XML property list from a byte array.</summary>
        /// <param name="bytes">The byte array containing the property list's data.</param>
        /// <returns>The root object of the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        public static NSObject Parse(byte[] bytes)
        {
            var bis = new MemoryStream(bytes);

            return Parse(bis);
        }

        /// <summary>Parses a XML property list from an input stream.</summary>
        /// <param name="str">The input stream pointing to the property list's data.</param>
        /// <returns>The root object of the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        public static NSObject Parse(Stream str)
        {
            var doc = new XmlDocument();

            var settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Ignore;

            using(var reader = XmlReader.Create(str, settings))
                doc.Load(reader);

            return ParseDocument(doc);
        }

        /// <summary>Parses a XML property list from a string.</summary>
        /// <param name="value">The string pointing to the property list's data.</param>
        /// <returns>The root object of the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        public static NSObject ParseString(string value)
        {
            var doc = new XmlDocument();

            var settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Ignore;

            doc.LoadXml(value);

            return ParseDocument(doc);
        }

        /// <summary>Parses the XML document by generating the appropriate NSObjects for each XML node.</summary>
        /// <returns>The root NSObject of the property list contained in the XML document.</returns>
        /// <param name="doc">The XML document.</param>
        private static NSObject ParseDocument(XmlDocument doc)
        {
            XmlNode docType = doc.ChildNodes.OfType<XmlNode>().
                                  SingleOrDefault(n => n.NodeType == XmlNodeType.DocumentType);

            if(docType == null)
            {
                if(doc.DocumentElement != null &&
                   !doc.DocumentElement.Name.Equals("plist"))
                    throw new XmlException("The given XML document is not a property list.");
            }
            else if(!docType.Name.Equals("plist"))
                throw new XmlException("The given XML document is not a property list.");

            XmlNode rootNode;

            if(doc.DocumentElement is { Name: "plist" })
            {
                //Root element wrapped in plist tag
                List<XmlNode> rootNodes = FilterElementNodes(doc.DocumentElement.ChildNodes);

                rootNode = rootNodes.Count switch
                {
                    0 => throw new PropertyListFormatException("The given XML property list has no root element!"),
                    1 => rootNodes[0],
                    _ => throw new
                             PropertyListFormatException("The given XML property list has more than one root element!")
                };
            }
            else

                //Root NSObject not wrapped in plist-tag
                rootNode = doc.DocumentElement;

            return ParseObject(rootNode);
        }

        /// <summary>Parses a node in the XML structure and returns the corresponding NSObject</summary>
        /// <returns>The corresponding NSObject.</returns>
        /// <param name="n">The XML node.</param>
        private static NSObject ParseObject(XmlNode n)
        {
            switch(n.Name)
            {
                // Special case for UID values
                case "dict" when n.ChildNodes.Count        == 2        && n.ChildNodes[0].Name == "key"     &&
                                 n.ChildNodes[0].InnerText == "CF$UID" && n.ChildNodes[1].Name == "integer" &&
                                 uint.TryParse(n.ChildNodes[1].InnerText, out uint uidValue): return new UID(uidValue);
                case "dict":
                {
                    var           dict     = new NSDictionary();
                    List<XmlNode> children = FilterElementNodes(n.ChildNodes);

                    for(int i = 0; i < children.Count; i += 2)
                    {
                        XmlNode key = children[i];
                        XmlNode val = children[i + 1];

                        string keyString = GetNodeTextContents(key);

                        dict.Add(keyString, ParseObject(val));
                    }

                    return dict;
                }
                case "array":
                {
                    List<XmlNode> children = FilterElementNodes(n.ChildNodes);
                    var           array    = new NSArray(children.Count);

                    for(int i = 0; i < children.Count; i++)
                        array.Add(ParseObject(children[i]));

                    return array;
                }
                case "true":    return new NSNumber(true);
                case "false":   return new NSNumber(false);
                case "integer": return new NSNumber(GetNodeTextContents(n), NSNumber.INTEGER);
                case "real":    return new NSNumber(GetNodeTextContents(n), NSNumber.REAL);
                case "string":  return new NSString(GetNodeTextContents(n));
                case "data":    return new NSData(GetNodeTextContents(n));
                default:        return n.Name.Equals("date") ? new NSDate(GetNodeTextContents(n)) : null;
            }
        }

        /// <summary>Returns all element nodes that are contained in a list of nodes.</summary>
        /// <returns>The sublist containing only nodes representing actual elements.</returns>
        /// <param name="list">The list of nodes to search.</param>
        private static List<XmlNode> FilterElementNodes(XmlNodeList list)
        {
            List<XmlNode> result = new();

            foreach(XmlNode child in list)
                if(child.NodeType == XmlNodeType.Element)
                    result.Add(child);

            return result;
        }

        /// <summary>
        ///     Returns a node's text content. This method will return the text value represented by the node's direct
        ///     children. If the given node is a TEXT or CDATA node, then its value is returned.
        /// </summary>
        /// <returns>The node's text content.</returns>
        /// <param name="n">The node.</param>
        private static string GetNodeTextContents(XmlNode n)
        {
            if(n.NodeType is XmlNodeType.Text or XmlNodeType.CDATA)
            {
                string content = n.Value; //This concatenates any adjacent text/cdata/entity nodes

                return content ?? "";
            }

            if(!n.HasChildNodes)
                return "";

            XmlNodeList children = n.ChildNodes;

            foreach(XmlNode child in children)

                //Skip any non-text nodes, like comments or entities
                if(child.NodeType is XmlNodeType.Text or XmlNodeType.CDATA)
                {
                    string content = child.Value; //This concatenates any adjacent text/cdata/entity nodes

                    return content ?? "";
                }

            return "";
        }
    }
}
