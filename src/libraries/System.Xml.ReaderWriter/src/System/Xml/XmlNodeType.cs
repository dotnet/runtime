// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    /// <devdoc>
    ///    Specifies the type of node.
    /// </devdoc>
    public enum XmlNodeType
    {
        /// <devdoc>
        ///    For XPathNavigator, cursor is not positioned
        ///    on a node.
        /// </devdoc>
        None,
        /// <devdoc>
        ///    <para>
        ///       An Element.
        ///    </para>
        ///    <para>
        ///       Example XML: &lt;Name&gt;
        ///    </para>
        ///    An Element node can have
        ///    the following child node types: Element, Text, Comment, ProcessingInstruction,
        ///    CDATA, and EntityReference. The Element node can be the child of the Document,
        ///    DocumentFragment, EntityReference, and Element nodes.
        /// </devdoc>
        Element,
        /// <devdoc>
        ///    <para>
        ///       An
        ///       Attribute.
        ///    </para>
        ///    <para>
        ///       Example XML: id='123'
        ///    </para>
        ///    <para>
        ///       An Attribute node can have the following child node types: Text and
        ///       EntityReference. The Attribute node does not appear as the child node of any
        ///       other node type; note that it is not considered a child node of an Element.
        ///    </para>
        /// </devdoc>
        Attribute,
        /// <devdoc>
        ///    <para>
        ///       The
        ///       text content of an element.
        ///    </para>
        ///    <para>
        ///       A Text node cannot have any child nodes. The Text node can appear as the
        ///       child node of the Attribute, DocumentFragment, Element, and EntityReference
        ///       nodes.
        ///    </para>
        /// </devdoc>
        Text,
        /// <devdoc>
        ///    A CDATA section.
        ///    Example XML: &lt;![CDATA[my escaped text]]&gt;
        ///    CDATA sections are used to escape blocks of text that would otherwise be
        ///    recognized as markup. A CDATASection node cannot have any child nodes. The
        ///    CDATASection node can appear as the child of the DocumentFragment,
        ///    EntityReference, and Element nodes.
        /// </devdoc>
        CDATA,
        /// <devdoc>
        ///    <para>A reference to an entity.</para>
        ///    <para>Example XML: &amp;foo;</para>
        ///    <para>This applies to all entities, including character entity references. An
        ///       EntityReference node can have the following child node types: Element,
        ///       ProcessingInstruction, Comment, Text, CDATASection, and EntityReference. The
        ///       EntityReference node can appear as the child of the Attribute, DocumentFragment,
        ///       Element, and EntityReference nodes.</para>
        /// </devdoc>
        EntityReference,
        /// <devdoc>
        ///    <para>An entity declaration.</para>
        ///    <para>Example XML: &lt;!ENTITY ...&gt;</para>
        ///    <para>An Entity node can have child nodes that represent the expanded entity (for
        ///       example, Text and EntityReference nodes). The Entity node can appear as the
        ///       child of the DocumentType node.</para>
        /// </devdoc>
        Entity,
        /// <devdoc>
        ///    <para>
        ///       A processing instruction (PI).
        ///    </para>
        ///    <para>
        ///       Example XML: &lt;?pi test?&gt;
        ///    </para>
        ///    <para>
        ///       A PI node cannot have any child nodes. The PI node can
        ///       appear as the child of the Document, DocumentFragment, Element, and
        ///       EntityReference nodes.
        ///    </para>
        /// </devdoc>
        ProcessingInstruction,
        /// <devdoc>
        ///    <para>
        ///       A Comment.
        ///    </para>
        ///    <para>
        ///       Example XML: &lt;!-- my comment --&gt;
        ///    </para>
        ///    <para>
        ///       A Comment node cannot have any child nodes. The Comment node can appear as
        ///       the child of the Document, DocumentFragment, Element, and EntityReference
        ///       nodes.
        ///    </para>
        /// </devdoc>
        Comment,
        /// <devdoc>
        ///    <para>
        ///       A document object, which, as the root of the document tree, provides access
        ///       to the entire XML document.
        ///    </para>
        ///    <para>
        ///       A Document node can have the following child node types: Element (maximum of
        ///       one), ProcessingInstruction, Comment, and DocumentType. The Document node cannot
        ///       appear as the child of any node types.
        ///    </para>
        /// </devdoc>
        Document,
        /// <devdoc>
        ///    <para>
        ///       The document type declaration, indicated by the &lt;!DOCTYPE&gt; tag.
        ///    </para>
        ///    <para>
        ///       Example XML: &lt;!DOCTYPE ...&gt;
        ///    </para>
        ///    <para>
        ///       A DocumentType node can have the following child node types: Notation and
        ///       Entity. The DocumentType node can appear as the child of the Document node.
        ///    </para>
        /// </devdoc>
        DocumentType,
        /// <devdoc>
        ///    <para>
        ///       A document fragment.
        ///    </para>
        ///    <para>
        ///       The DocumentFragment node associates a node or subtree with a document
        ///       without actually being contained within the document. A DocumentFragment node
        ///       can have the following child node types: Element, ProcessingInstruction,
        ///       Comment, Text, CDATASection, and EntityReference. The DocumentFragment node
        ///       cannot appear as the child of any node types.
        ///    </para>
        /// </devdoc>
        DocumentFragment,
        /// <devdoc>
        ///    <para>
        ///       A notation in the document type declaration.
        ///    </para>
        ///    <para>
        ///       Example XML: &lt;!NOTATION ...&gt;
        ///    </para>
        ///    <para>
        ///       A Notation node cannot have any child nodes. The Notation node can appear as
        ///       the child of the DocumentType node.
        ///    </para>
        /// </devdoc>
        Notation,
        /// <devdoc>
        ///    <para>
        ///       Whitespace between markup.
        ///    </para>
        /// </devdoc>
        Whitespace,
        /// <devdoc>
        ///    <para>
        ///       Whitespace between markup in a mixed content model.
        ///    </para>
        /// </devdoc>
        SignificantWhitespace,
        /// <devdoc>
        ///    <para>Returned when XmlReader gets to the end of an element.</para>
        ///    <para>Example XML: &lt;/foo&gt;</para>
        /// </devdoc>
        EndElement,
        /// <devdoc>
        ///    <para>Returned when XmlReader gets to the end of the entity 
        ///       replacement as a result of a call to <see cref='System.Xml.XmlReader.ResolveEntity'/>
        ///       .</para>
        /// </devdoc>
        EndEntity,
        /// <devdoc>
        ///    <para>
        ///       The XML declaration node..
        ///    </para>
        ///    <para>
        ///       Example XML: &lt;?xml version='1.0'?&gt;;
        ///    </para>
        ///    <para>
        ///        This has to be the first node in the document. It can have no children. 
        ///        It is a child of the root node. It can have attributes that provide version 
        ///        and encoding information.
        ///    </para>
        /// </devdoc>
        XmlDeclaration
    }
}
