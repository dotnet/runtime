// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Globalization;

#if NET_NATIVE
namespace System.Xml
{
    internal class XmlNodeReader : XmlReader, IXmlNamespaceResolver
    {
        public XmlNodeReader()
        {
            throw NotImplemented.ByDesign;
        }

        // Gets the type of the current node.
        public override XmlNodeType NodeType
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets the name of
        // the current node, including the namespace prefix.
        public override string Name
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets the name of the current node without the namespace prefix.
        public override string LocalName
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets the namespace URN (as defined in the W3C Namespace Specification)
        // of the current namespace scope.
        public override string NamespaceURI
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets the namespace prefix associated with the current node.
        public override string Prefix
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets a value indicating whether
        // XmlNodeReader.Value has a value to return.
        public override bool HasValue
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets the text value of the current node.
        public override string Value
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets the depth of the
        // current node in the XML element stack.
        public override int Depth
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets the base URI of the current node.
        public override String BaseURI
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public override bool CanResolveEntity
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets a value indicating whether the current
        // node is an empty element (for example, <MyElement/>.
        public override bool IsEmptyElement
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets a value indicating whether the current node is an
        // attribute that was generated from the default value defined
        // in the DTD or schema.
        public override bool IsDefault
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets the current xml:space scope.
        public override XmlSpace XmlSpace
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets the current xml:lang scope.
        public override string XmlLang
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        //
        // Attribute Accessors
        //

        // Gets the number of attributes on the current node.
        public override int AttributeCount
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Gets the value of the attribute with the specified name.
        public override string GetAttribute(string name)
        {
            throw NotImplemented.ByDesign;
        }

        // Gets the value of the attribute with the specified name and namespace.
        public override string GetAttribute(string name, string namespaceURI)
        {
            throw NotImplemented.ByDesign;
        }

        // Gets the value of the attribute with the specified index.
        public override string GetAttribute(int attributeIndex)
        {
            throw NotImplemented.ByDesign;
        }

        // Moves to the attribute with the specified name.
        public override bool MoveToAttribute(string name)
        {
            throw NotImplemented.ByDesign;
        }

        // Moves to the attribute with the specified name and namespace.
        public override bool MoveToAttribute(string name, string namespaceURI)
        {
            throw NotImplemented.ByDesign;
        }

        // Moves to the attribute with the specified index.
        public override void MoveToAttribute(int attributeIndex)
        {
            throw NotImplemented.ByDesign;
        }

        // Moves to the first attribute.
        public override bool MoveToFirstAttribute()
        {
            throw NotImplemented.ByDesign;
        }

        // Moves to the next attribute.
        public override bool MoveToNextAttribute()
        {
            throw NotImplemented.ByDesign;
        }

        // Moves to the element that contains the current attribute node.
        public override bool MoveToElement()
        {
            throw NotImplemented.ByDesign;
        }

        //
        // Moving through the Stream
        //

        // Reads the next node from the stream.
        public override bool Read()
        {
            throw NotImplemented.ByDesign;
        }

        // Gets a value indicating whether the reader is positioned at the
        // end of the stream.
        public override bool EOF
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Closes the stream, changes the XmlNodeReader.ReadState
        // to Closed, and sets all the properties back to zero.
        protected override void Dispose(bool disposing)
        {
            throw NotImplemented.ByDesign;
        }

        // Gets the read state of the stream.
        public override ReadState ReadState
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Skips to the end tag of the current element.
        public override void Skip()
        {
            throw NotImplemented.ByDesign;
        }

        //
        // Partial Content Read Methods
        //

        // Gets a value indicating whether the current node
        // has any attributes.
        public override bool HasAttributes
        {
            get
            {
                return (AttributeCount > 0);
            }
        }

        //
        // Nametable and Namespace Helpers
        //

        // Gets the XmlNameTable associated with this implementation.
        public override XmlNameTable NameTable
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Resolves a namespace prefix in the current element's scope.
        public override String LookupNamespace(string prefix)
        {
            throw NotImplemented.ByDesign;
        }

        // Resolves the entity reference for nodes of NodeType EntityReference.
        public override void ResolveEntity()
        {
            throw NotImplemented.ByDesign;
        }

        // Parses the attribute value into one or more Text and/or
        // EntityReference node types.
        public override bool ReadAttributeValue()
        {
            throw NotImplemented.ByDesign;
        }

        public override bool CanReadBinaryContent
        {
            get
            {
                return true;
            }
        }

        public override int ReadContentAsBase64(byte[] buffer, int index, int count)
        {
            throw NotImplemented.ByDesign;
        }

        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            throw NotImplemented.ByDesign;
        }

        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
        {
            throw NotImplemented.ByDesign;
        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            throw NotImplemented.ByDesign;
        }

        //
        // IXmlNamespaceResolver
        //

        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            throw NotImplemented.ByDesign;
        }

        string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            throw NotImplemented.ByDesign;
        }

        String IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            throw NotImplemented.ByDesign;
        }
    }
}
#endif
