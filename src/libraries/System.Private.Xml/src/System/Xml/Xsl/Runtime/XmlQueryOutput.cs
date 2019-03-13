// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime
{
    internal enum XmlState
    {
        WithinSequence = 0,     // Adding items to a top-level sequence
        EnumAttrs,              // Adding attributes to an element
        WithinContent,          // Adding content to an element
        WithinAttr,             // Adding text to an attribute
        WithinNmsp,             // Adding text to an namespace
        WithinComment,          // Adding text to a comment
        WithinPI,               // Adding text to a processing instruction
    };


    /// <summary>
    /// At run-time, a number of checks may need to be made in order to generate the correct sequence of calls
    /// to XmlRawWriter:
    ///   1. Well-formedness: Illegal state transitions, StartContent detection, no-content element detection
    ///   2. Cached attributes: In XSLT, attributes override previously constructed attributes with the same name,
    ///      meaning that attribute names and values cannot be prematurely sent to XmlRawWriter.
    ///   3. Cached namespaces: All namespaces are tracked in order to ensure adequate namespaces and to ensure
    ///      minimal (or something close) namespaces.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class XmlQueryOutput : XmlWriter
    {
        // Never set these fields directly--instead use corresponding properties
        private XmlRawWriter _xwrt;                  // Output to XmlRawWriter--get and set this using the Writer property

        // It is OK to set these properties directly
        private XmlQueryRuntime _runtime;            // The XmlQueryRuntime instance that keeps global state
        private XmlAttributeCache _attrCache;        // Cache used to detect duplicate attributes
        private int _depth;                          // Depth of the currently constructing tree
        private XmlState _xstate;                    // Current XML state
        private XmlSequenceWriter _seqwrt;           // Current XmlSequenceWriter
        private XmlNamespaceManager _nsmgr;          // Output namespace manager
        private int _cntNmsp;                        // Number of pending namespaces
        private Dictionary<string, string> _conflictPrefixes;         // Remembers prefixes that were auto-generated previously in case they can be reused
        private int _prefixIndex;                    // Counter used to auto-generate non-conflicting attribute prefixes
        private string _piTarget/*nmspPrefix*/;      // Cache pi target or namespace prefix
        private StringConcat _nodeText;              // Cache pi, comment, or namespace text
        private Stack<string> _stkNames;             // Keep stack of name parts computed during StartElement
        private XPathNodeType _rootType;             // NodeType of the root of the tree

        private Dictionary<string, string> _usedPrefixes = new Dictionary<string, string>(); //The prefies that used in the current scope

        /// <summary>
        /// This constructor is internal so that external users cannot construct it (and therefore we do not have to test it separately).
        /// Initialize output state to accept top-level sequences.
        /// </summary>
        internal XmlQueryOutput(XmlQueryRuntime runtime, XmlSequenceWriter seqwrt)
        {
            _runtime = runtime;
            _seqwrt = seqwrt;
            _xstate = XmlState.WithinSequence;
        }

        /// <summary>
        /// This constructor is internal so that external users cannot construct it (and therefore we do not have to test it separately).
        /// Initialize output state to accept Rtf content (top-level sequences are therefore prohibited).
        /// </summary>
        internal XmlQueryOutput(XmlQueryRuntime runtime, XmlEventCache xwrt)
        {
            _runtime = runtime;
            _xwrt = xwrt;
            _xstate = XmlState.WithinContent;
            _depth = 1;
            _rootType = XPathNodeType.Root;
        }

        /// <summary>
        /// Sequence writer to which output is directed by this class.
        /// </summary>
        internal XmlSequenceWriter SequenceWriter
        {
            get { return _seqwrt; }
        }

        /// <summary>
        /// Raw writer to which output is directed by this class.
        /// </summary>
        internal XmlRawWriter Writer
        {
            get { return _xwrt; }
            set
            {
                // If new writer might remove itself from pipeline, have it callback on this method when it's ready to go
                IRemovableWriter removable = value as IRemovableWriter;
                if (removable != null)
                    removable.OnRemoveWriterEvent = SetWrappedWriter;

                _xwrt = value;
            }
        }

        /// <summary>
        /// This method will be called if "xwrt" is a writer which no longer needs to be part of the pipeline and
        /// wishes to replace itself with a different writer.  For example, the auto-detect writer replaces itself
        /// with the Html or Xml writer once it has determined which output mode to use.
        /// </summary>
        private void SetWrappedWriter(XmlRawWriter writer)
        {
            // Reuse XmlAttributeCache so that it doesn't have to be recreated every time
            if (Writer is XmlAttributeCache)
                _attrCache = (XmlAttributeCache)Writer;

            Writer = writer;
        }


        //-----------------------------------------------
        // XmlWriter methods
        //-----------------------------------------------

        /// <summary>
        /// Should never be called.
        /// </summary>
        public override void WriteStartDocument()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Should never be called.
        /// </summary>
        public override void WriteStartDocument(bool standalone)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Should never be called.
        /// </summary>
        public override void WriteEndDocument()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Should never be called.
        /// </summary>
        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Before calling XmlRawWriter.WriteStartElement(), perform various checks to ensure well-formedness.
        /// </summary>
        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            Debug.Assert(prefix != null && localName != null && localName.Length != 0 && ns != null, "Invalid argument");
            Debug.Assert(ValidateNames.ValidateName(prefix, localName, ns, XPathNodeType.Element, ValidateNames.Flags.All), "Name validation failed");

            // Xml state transitions
            ConstructWithinContent(XPathNodeType.Element);

            // Call XmlRawWriter.WriteStartElement
            WriteStartElementUnchecked(prefix, localName, ns);

            // Ensure that element's namespace declaration is declared
            WriteNamespaceDeclarationUnchecked(prefix, ns);

            // Cache attributes in order to detect duplicates
            if (_attrCache == null)
                _attrCache = new XmlAttributeCache();

            _attrCache.Init(Writer);
            Writer = _attrCache;
            _attrCache = null;

            // Push element names onto a stack
            PushElementNames(prefix, localName, ns);
        }

        /// <summary>
        /// Before calling XmlRawWriter.WriteEndElement(), perform various checks to ensure well-formedness.
        /// </summary>
        public override void WriteEndElement()
        {
            string prefix, localName, ns;

            // Determine whether element had no content
            if (_xstate == XmlState.EnumAttrs)
            {
                // No content, so call StartElementContent now
                StartElementContentUnchecked();
            }

            // Call XmlRawWriter.WriteEndElement
            PopElementNames(out prefix, out localName, out ns);
            WriteEndElementUnchecked(prefix, localName, ns);

            // Xml state transitions
            if (_depth == 0)
                EndTree();
        }

        /// <summary>
        /// Same as calling WriteEndElement().
        /// </summary>
        public override void WriteFullEndElement()
        {
            WriteEndElement();
        }

        /// <summary>
        /// Before calling XmlRawWriter.WriteStartAttribute(), perform various checks to ensure well-formedness.
        /// </summary>
        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            Debug.Assert(prefix != null && localName != null && ns != null, "Invalid argument");

            if (prefix.Length == 5 && prefix == "xmlns")
            {
                // Handle namespace attributes that are not sent directly to WriteNamespaceDeclaration
                WriteStartNamespace(localName);
            }
            else
            {
                // All other attributes
                Debug.Assert(ValidateNames.ValidateName(prefix, localName, ns, XPathNodeType.Attribute, ValidateNames.Flags.All));

                // Xml state transitions
                ConstructInEnumAttrs(XPathNodeType.Attribute);

                // Check for prefix conflicts and possibly declare prefix
                if (ns.Length != 0 && _depth != 0)
                    prefix = CheckAttributePrefix(prefix, ns);

                // Output the attribute
                WriteStartAttributeUnchecked(prefix, localName, ns);
            }
        }

        /// <summary>
        /// Before calling XmlRawWriter.WriteEndAttribute(), perform various checks to ensure well-formedness.
        /// </summary>
        public override void WriteEndAttribute()
        {
            if (_xstate == XmlState.WithinNmsp)
            {
                WriteEndNamespace();
            }
            else
            {
                WriteEndAttributeUnchecked();

                if (_depth == 0)
                    EndTree();
            }
        }

        /// <summary>
        /// Before writing a comment, perform various checks to ensure well-formedness.
        /// </summary>
        public override void WriteComment(string text)
        {
            WriteStartComment();
            WriteCommentString(text);
            WriteEndComment();
        }

        /// <summary>
        /// Before writing a processing instruction, perform various checks to ensure well-formedness.
        /// </summary>
        public override void WriteProcessingInstruction(string target, string text)
        {
            WriteStartProcessingInstruction(target);
            WriteProcessingInstructionString(text);
            WriteEndProcessingInstruction();
        }

        /// <summary>
        /// Should never be called.
        /// </summary>
        public override void WriteEntityRef(string name)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Should never be called.
        /// </summary>
        public override void WriteCharEntity(char ch)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Should never be called.
        /// </summary>
        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Treat whitespace as regular text.
        /// </summary>
        public override void WriteWhitespace(string ws)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Before writing text, perform various checks to ensure well-formedness.
        /// </summary>
        public override void WriteString(string text)
        {
            WriteString(text, false);
        }

        /// <summary>
        /// Before writing text, perform various checks to ensure well-formedness.
        /// </summary>
        public override void WriteChars(char[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Write text, but do not escape special characters.
        /// </summary>
        public override void WriteRaw(char[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Write text, but do not escape special characters.
        /// </summary>
        public override void WriteRaw(string data)
        {
            WriteString(data, true);
        }

        /// <summary>
        /// Write CData text as regular text.
        /// </summary>
        public override void WriteCData(string text)
        {
            WriteString(text, false);
        }

        /// <summary>
        /// Should never be called.
        /// </summary>
        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Should never be called.
        /// </summary>
        public override WriteState WriteState
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// No-op.
        /// </summary>
        public override void Close()
        {
        }

        /// <summary>
        /// No-op.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Should never be called.
        /// </summary>
        public override string LookupPrefix(string ns)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Should never be called.
        /// </summary>
        public override XmlSpace XmlSpace
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Should never be called.
        /// </summary>
        public override string XmlLang
        {
            get { throw new NotSupportedException(); }
        }


        //-----------------------------------------------
        // XmlQueryOutput methods (XmlSequenceWriter)
        //-----------------------------------------------

        /// <summary>
        /// Call XmlSequenceWriter.StartTree() in order to start construction of a new tree.
        /// </summary>
        public void StartTree(XPathNodeType rootType)
        {
            Debug.Assert(_xstate == XmlState.WithinSequence, "StartTree cannot be called in the " + _xstate + " state.");
            Writer = _seqwrt.StartTree(rootType, _nsmgr, _runtime.NameTable);
            _rootType = rootType;
            _xstate = (rootType == XPathNodeType.Attribute || rootType == XPathNodeType.Namespace) ? XmlState.EnumAttrs : XmlState.WithinContent;
        }

        /// <summary>
        /// Call XmlSequenceWriter.EndTree().
        /// </summary>
        public void EndTree()
        {
            Debug.Assert(_xstate == XmlState.EnumAttrs || _xstate == XmlState.WithinContent, "EndTree cannot be called in the " + _xstate + " state.");
            _seqwrt.EndTree();
            _xstate = XmlState.WithinSequence;
            Writer = null;
        }


        //-----------------------------------------------
        // XmlQueryOutput methods (XmlRawWriter)
        //-----------------------------------------------

        /// <summary>
        /// Call XmlRawWriter.WriteStartElement() with prefix, local-name, ns, and schema type.
        /// </summary>
        public void WriteStartElementUnchecked(string prefix, string localName, string ns)
        {
            Debug.Assert(_xstate == XmlState.WithinContent, "WriteStartElement cannot be called in the " + _xstate + " state.");
            if (_nsmgr != null) _nsmgr.PushScope();
            Writer.WriteStartElement(prefix, localName, ns);
            //reset when enter element
            _usedPrefixes.Clear();
            _usedPrefixes[prefix] = ns;
            _xstate = XmlState.EnumAttrs;
            _depth++;
        }

        /// <summary>
        /// Call XmlRawWriter.WriteStartElement() with empty prefix, ns, and null schema type.
        /// </summary>
        public void WriteStartElementUnchecked(string localName)
        {
            WriteStartElementUnchecked(string.Empty, localName, string.Empty);
        }

        /// <summary>
        /// Call XmlRawWriter.StartElementContent().
        /// </summary>
        public void StartElementContentUnchecked()
        {
            Debug.Assert(_xstate == XmlState.EnumAttrs, "StartElementContent cannot be called in the " + _xstate + " state.");

            // Output any cached namespaces
            if (_cntNmsp != 0)
                WriteCachedNamespaces();

            Writer.StartElementContent();
            _xstate = XmlState.WithinContent;
        }

        /// <summary>
        /// Call XmlRawWriter.WriteEndElement() with prefix, local-name, and ns.
        /// </summary>
        public void WriteEndElementUnchecked(string prefix, string localName, string ns)
        {
            Debug.Assert(_xstate == XmlState.EnumAttrs || _xstate == XmlState.WithinContent, "WriteEndElement cannot be called in the " + _xstate + " state.");
            Writer.WriteEndElement(prefix, localName, ns);
            _xstate = XmlState.WithinContent;
            _depth--;
            if (_nsmgr != null) _nsmgr.PopScope();
        }

        /// <summary>
        /// Call XmlRawWriter.WriteEndElement() with empty prefix, ns.
        /// </summary>
        public void WriteEndElementUnchecked(string localName)
        {
            WriteEndElementUnchecked(string.Empty, localName, string.Empty);
        }

        /// <summary>
        /// XmlRawWriter.WriteStartAttribute() with prefix, local-name, ns, and schema type.
        /// </summary>
        public void WriteStartAttributeUnchecked(string prefix, string localName, string ns)
        {
            Debug.Assert(_xstate == XmlState.EnumAttrs, "WriteStartAttribute cannot be called in the " + _xstate + " state.");
            Writer.WriteStartAttribute(prefix, localName, ns);
            _xstate = XmlState.WithinAttr;
            _depth++;
        }

        /// <summary>
        /// XmlRawWriter.WriteStartAttribute() with empty prefix, ns, and null schema type.
        /// </summary>
        public void WriteStartAttributeUnchecked(string localName)
        {
            WriteStartAttributeUnchecked(string.Empty, localName, string.Empty);
        }

        /// <summary>
        /// XmlRawWriter.WriteEndAttribute().
        /// </summary>
        public void WriteEndAttributeUnchecked()
        {
            Debug.Assert(_xstate == XmlState.WithinAttr, "WriteEndAttribute cannot be called in the " + _xstate + " state.");
            Writer.WriteEndAttribute();
            _xstate = XmlState.EnumAttrs;
            _depth--;
        }

        /// <summary>
        /// Add a new namespace declaration -- xmlns:prefix="ns" -- to the set of in-scope declarations.
        /// NOTE: This method should only be called if caller can guarantee that the current state is EnumAttrs
        ///       and that there will be no namespace conflicts in the current scope (e.g. trying to map the
        ///       same prefix to different namespaces within the same element start tag).  If no such
        ///       guarantees exist, then WriteNamespaceDeclaration() should be called instead.
        /// </summary>
        public void WriteNamespaceDeclarationUnchecked(string prefix, string ns)
        {
            Debug.Assert(prefix != null && ns != null);
            Debug.Assert(_xstate == XmlState.EnumAttrs, "WriteNamespaceDeclaration cannot be called in the " + _xstate + " state.");

            // xmlns:foo="" is illegal
            Debug.Assert(prefix.Length == 0 || ns.Length != 0);

            if (_depth == 0)
            {
                // At top-level, so write namespace declaration directly to output
                Writer.WriteNamespaceDeclaration(prefix, ns);
                return;
            }

            if (_nsmgr == null)
            {
                // If namespace manager has no namespaces, then xmlns="" is in scope by default
                if (ns.Length == 0 && prefix.Length == 0)
                    return;

                _nsmgr = new XmlNamespaceManager(_runtime.NameTable);
                _nsmgr.PushScope();
            }

            if (_nsmgr.LookupNamespace(prefix) != ns)
                AddNamespace(prefix, ns);

            _usedPrefixes[prefix] = ns;
        }

        /// <summary>
        /// Write a text block to the XmlRawWriter.
        /// </summary>
        public void WriteStringUnchecked(string text)
        {
            Debug.Assert(_xstate != XmlState.WithinSequence && _xstate != XmlState.EnumAttrs, "WriteTextBlock cannot be called in the " + _xstate + " state.");
            Writer.WriteString(text);
        }

        /// <summary>
        /// Write a text block without escaping special characters.
        /// </summary>
        public void WriteRawUnchecked(string text)
        {
            Debug.Assert(_xstate != XmlState.WithinSequence && _xstate != XmlState.EnumAttrs, "WriteTextBlockNoEntities cannot be called in the " + _xstate + " state.");
            Writer.WriteRaw(text);
        }


        //-----------------------------------------------
        // XmlQueryOutput methods
        //-----------------------------------------------

        /// <summary>
        /// Before calling XmlSequenceWriter.StartTree(), perform checks to ensure well-formedness.
        /// </summary>
        public void WriteStartRoot()
        {
            Debug.Assert(_depth == 0, "Root node can only be constructed at top-level.");
            if (_xstate != XmlState.WithinSequence)
                ThrowInvalidStateError(XPathNodeType.Root);

            StartTree(XPathNodeType.Root);
            _depth++;
        }

        /// <summary>
        /// Call XmlSequenceWriter.EndTree() and reset state.
        /// </summary>
        public void WriteEndRoot()
        {
            Debug.Assert(_depth == 1, "Root node can only be constructed at top-level.");
            _depth--;
            EndTree();
        }

        /// <summary>
        /// WriteStartElement() with empty prefix, ns.
        /// </summary>
        public void WriteStartElementLocalName(string localName)
        {
            WriteStartElement(string.Empty, localName, string.Empty);
        }

        /// <summary>
        /// WriteStartAttribute() with empty prefix, ns, and null schema type.
        /// </summary>
        public void WriteStartAttributeLocalName(string localName)
        {
            WriteStartAttribute(string.Empty, localName, string.Empty);
        }

        /// <summary>
        /// Write an element with a name that is computed from a "prefix:localName" tag name and a set of prefix mappings.
        /// </summary>
        public void WriteStartElementComputed(string tagName, int prefixMappingsIndex)
        {
            WriteStartComputed(XPathNodeType.Element, tagName, prefixMappingsIndex);
        }

        /// <summary>
        /// Write an element with a name that is computed from a "prefix:localName" tag name and a namespace URI.
        /// </summary>
        public void WriteStartElementComputed(string tagName, string ns)
        {
            WriteStartComputed(XPathNodeType.Element, tagName, ns);
        }

        /// <summary>
        /// Write an element with a name that is copied from the navigator.
        /// </summary>
        public void WriteStartElementComputed(XPathNavigator navigator)
        {
            WriteStartComputed(XPathNodeType.Element, navigator);
        }

        /// <summary>
        /// Write an element with a name that is derived from the XmlQualifiedName.
        /// </summary>
        public void WriteStartElementComputed(XmlQualifiedName name)
        {
            WriteStartComputed(XPathNodeType.Element, name);
        }

        /// <summary>
        /// Write an attribute with a name that is computed from a "prefix:localName" tag name and a set of prefix mappings.
        /// </summary>
        public void WriteStartAttributeComputed(string tagName, int prefixMappingsIndex)
        {
            WriteStartComputed(XPathNodeType.Attribute, tagName, prefixMappingsIndex);
        }

        /// <summary>
        /// Write an attribute with a name that is computed from a "prefix:localName" tag name and a namespace URI.
        /// </summary>
        public void WriteStartAttributeComputed(string tagName, string ns)
        {
            WriteStartComputed(XPathNodeType.Attribute, tagName, ns);
        }

        /// <summary>
        /// Write an attribute with a name that is copied from the navigator.
        /// </summary>
        public void WriteStartAttributeComputed(XPathNavigator navigator)
        {
            WriteStartComputed(XPathNodeType.Attribute, navigator);
        }

        /// <summary>
        /// Write an attribute with a name that is derived from the XmlQualifiedName.
        /// </summary>
        public void WriteStartAttributeComputed(XmlQualifiedName name)
        {
            WriteStartComputed(XPathNodeType.Attribute, name);
        }

        /// <summary>
        /// Before calling XmlRawWriter.WriteNamespaceDeclaration(), perform various checks to ensure well-formedness.
        /// </summary>
        public void WriteNamespaceDeclaration(string prefix, string ns)
        {
            string nsExisting;
            Debug.Assert(prefix != null && ns != null);

            ConstructInEnumAttrs(XPathNodeType.Namespace);

            if (_nsmgr == null)
            {
                // If namespace manager has not yet been created, then there is no possibility of conflict
                WriteNamespaceDeclarationUnchecked(prefix, ns);
            }
            else
            {
                nsExisting = _nsmgr.LookupNamespace(prefix);
                if (ns != nsExisting)
                {
                    // prefix = "", ns = "", nsExisting --> Look for xmlns="", found xmlns="foo"
                    // prefix = "", ns, nsExisting = null --> Look for xmlns="uri", no uri found
                    // prefix = "", ns, nsExisting = "" --> Look for xmlns="uri", found xmlns=""
                    // prefix = "", ns, nsExisting --> Look for xmlns="uri", found xmlns="uri2"
                    // prefix, ns, nsExisting = null --> Look for xmlns:foo="uri", no uri found
                    // prefix, ns, nsExisting --> Look for xmlns:foo="uri", found xmlns:foo="uri2"

                    // If the prefix is mapped to a uri,
                    if (nsExisting != null)
                    {
                        // Then throw an error except if the prefix trying to redefine is already used
                        if (_usedPrefixes.ContainsKey(prefix))
                        {
                            throw new XslTransformException(SR.XmlIl_NmspConflict, new string[] { prefix.Length == 0 ? "" : ":", prefix, ns, nsExisting });
                        }
                    }

                    // Add namespace to manager and write it to output
                    AddNamespace(prefix, ns);
                }
            }

            if (_depth == 0)
                EndTree();

            _usedPrefixes[prefix] = ns;
        }

        /// <summary>
        /// Before writing a namespace, perform various checks to ensure well-formedness.
        /// </summary>
        public void WriteStartNamespace(string prefix)
        {
            Debug.Assert(prefix != null, "Invalid argument");

            // Handle namespace attributes that are not sent directly to WriteNamespaceDeclaration
            ConstructInEnumAttrs(XPathNodeType.Namespace);
            _piTarget/*nmspPrefix*/ = prefix;
            _nodeText.Clear();

            _xstate = XmlState.WithinNmsp;
            _depth++;
        }

        /// <summary>
        /// Cache the namespace's text.
        /// </summary>
        public void WriteNamespaceString(string text)
        {
            Debug.Assert(_xstate == XmlState.WithinNmsp, "WriteNamespaceString cannot be called in the " + _xstate + " state.");
            _nodeText.ConcatNoDelimiter(text);
        }

        /// <summary>
        /// Before writing a namespace, perform various checks to ensure well-formedness.
        /// </summary>
        public void WriteEndNamespace()
        {
            Debug.Assert(_xstate == XmlState.WithinNmsp, "WriteEndNamespace cannot be called in the " + _xstate + " state.");

            _xstate = XmlState.EnumAttrs;
            _depth--;

            // Write cached namespace attribute
            WriteNamespaceDeclaration(_piTarget/*nmspPrefix*/, _nodeText.GetResult());

            if (_depth == 0)
                EndTree();
        }

        /// <summary>
        /// Before writing a comment, perform various checks to ensure well-formedness.
        /// </summary>
        public void WriteStartComment()
        {
            // Xml state transitions
            ConstructWithinContent(XPathNodeType.Comment);

            _nodeText.Clear();
            _xstate = XmlState.WithinComment;
            _depth++;
        }

        /// <summary>
        /// Cache the comment's text.
        /// </summary>
        public void WriteCommentString(string text)
        {
            Debug.Assert(_xstate == XmlState.WithinComment, "WriteCommentString cannot be called in the " + _xstate + " state.");
            _nodeText.ConcatNoDelimiter(text);
        }

        /// <summary>
        /// Before writing a comment, perform various checks to ensure well-formedness.
        /// </summary>
        public void WriteEndComment()
        {
            Debug.Assert(_xstate == XmlState.WithinComment, "WriteEndComment cannot be called in the " + _xstate + " state.");

            Writer.WriteComment(_nodeText.GetResult());

            _xstate = XmlState.WithinContent;
            _depth--;

            if (_depth == 0)
                EndTree();
        }

        /// <summary>
        /// Before writing a processing instruction, perform various checks to ensure well-formedness.
        /// </summary>
        public void WriteStartProcessingInstruction(string target)
        {
            // Xml state transitions
            ConstructWithinContent(XPathNodeType.ProcessingInstruction);

            // Verify PI name
            ValidateNames.ValidateNameThrow("", target, "", XPathNodeType.ProcessingInstruction, ValidateNames.Flags.AllExceptPrefixMapping);

            _piTarget = target;
            _nodeText.Clear();

            _xstate = XmlState.WithinPI;
            _depth++;
        }

        /// <summary>
        /// Cache the processing instruction's text.
        /// </summary>
        public void WriteProcessingInstructionString(string text)
        {
            Debug.Assert(_xstate == XmlState.WithinPI, "WriteProcessingInstructionString cannot be called in the " + _xstate + " state.");
            _nodeText.ConcatNoDelimiter(text);
        }

        /// <summary>
        /// Before writing a processing instruction, perform various checks to ensure well-formedness.
        /// </summary>
        public void WriteEndProcessingInstruction()
        {
            Debug.Assert(_xstate == XmlState.WithinPI, "WriteEndProcessingInstruction cannot be called in the " + _xstate + " state.");

            Writer.WriteProcessingInstruction(_piTarget, _nodeText.GetResult());

            _xstate = XmlState.WithinContent;
            _depth--;

            // Xml state transitions
            if (_depth == 0)
                EndTree();
        }

        /// <summary>
        /// Write an item to output.  If currently constructing an Xml tree, then the item is always copied.
        /// At the top-level, the item's identity is preserved unless it's an atomic value.
        /// </summary>
        public void WriteItem(XPathItem item)
        {
            if (item.IsNode)
            {
                XPathNavigator navigator = (XPathNavigator)item;

                // If this is a top-level node, write a reference to it; else copy it by value
                if (_xstate == XmlState.WithinSequence)
                    _seqwrt.WriteItem(navigator);
                else
                    CopyNode(navigator);
            }
            else
            {
                // Call WriteItem for atomic values
                Debug.Assert(_xstate == XmlState.WithinSequence, "Values can only be written at the top-level.");
                _seqwrt.WriteItem(item);
            }
        }

        /// <summary>
        /// Copy a node by value to output according to Xslt rules:
        ///   1. Identity is never preserved
        ///   2. If the item is an Rtf, preserve serialization hints when copying.
        ///   3. If the item is a Root node, copy the children of the Root
        /// </summary>
        public void XsltCopyOf(XPathNavigator navigator)
        {
            RtfNavigator navRtf = navigator as RtfNavigator;

            if (navRtf != null)
            {
                // Copy Rtf
                navRtf.CopyToWriter(this);
            }
            else if (navigator.NodeType == XPathNodeType.Root)
            {
                // Copy children of root
                if (navigator.MoveToFirstChild())
                {
                    do
                    {
                        CopyNode(navigator);
                    }
                    while (navigator.MoveToNext());

                    navigator.MoveToParent();
                }
            }
            else
            {
                // Copy node
                CopyNode(navigator);
            }
        }

        /// <summary>
        /// Begin shallow copy of the navigator's current node to output.  Returns true if EndCopy
        /// should be called to complete the copy operation.
        /// Automatically copies all in-scope namespaces on elements.
        /// </summary>
        public bool StartCopy(XPathNavigator navigator)
        {
            // StartDocument is a no-op
            if (navigator.NodeType == XPathNodeType.Root)
                return true;

            if (StartCopy(navigator, true))
            {
                Debug.Assert(navigator.NodeType == XPathNodeType.Element, "StartCopy should return true only for Element nodes.");

                // Copy namespaces to output
                CopyNamespaces(navigator, XPathNamespaceScope.ExcludeXml);

                return true;
            }

            return false;
        }

        /// <summary>
        /// End shallow copy of the navigator's current node.  Should be called only for Element and Document nodes.
        /// </summary>
        public void EndCopy(XPathNavigator navigator)
        {
            if (navigator.NodeType == XPathNodeType.Element)
                WriteEndElement();
            else
                Debug.Assert(navigator.NodeType == XPathNodeType.Root, "EndCopy should only be called for Element and Document nodes.");
        }


        //-----------------------------------------------
        // Helper methods
        //-----------------------------------------------

        /// <summary>
        /// Add an in-scope namespace.
        /// </summary>
        private void AddNamespace(string prefix, string ns)
        {
            _nsmgr.AddNamespace(prefix, ns);
            _cntNmsp++;
            _usedPrefixes[prefix] = ns;
        }

        /// <summary>
        /// Before writing text, perform various checks to ensure well-formedness.
        /// </summary>
        private void WriteString(string text, bool disableOutputEscaping)
        {
            Debug.Assert(text != null, "Invalid argument");

            // Xml state transitions
            switch (_xstate)
            {
                case XmlState.WithinSequence:
                    // Start constructing new tree
                    StartTree(XPathNodeType.Text);
                    goto case XmlState.WithinContent;

                case XmlState.WithinContent:
                    if (disableOutputEscaping)
                        WriteRawUnchecked(text);
                    else
                        WriteStringUnchecked(text);
                    break;

                case XmlState.EnumAttrs:
                    // Enumerating attributes, so write text as element content
                    StartElementContentUnchecked();
                    goto case XmlState.WithinContent;

                case XmlState.WithinAttr:
                    WriteStringUnchecked(text);
                    break;

                case XmlState.WithinNmsp:
                    WriteNamespaceString(text);
                    break;

                case XmlState.WithinComment:
                    // Comment text
                    WriteCommentString(text);
                    break;

                case XmlState.WithinPI:
                    // PI text
                    WriteProcessingInstructionString(text);
                    break;

                default:
                    Debug.Fail("Text cannot be output in the " + _xstate + " state.");
                    break;
            }

            if (_depth == 0)
                EndTree();
        }

        /// <summary>
        /// Deep copy the subtree that is rooted at this navigator's current position to output.  If the current
        /// item is an element, copy all in-scope namespace nodes.
        /// </summary>
        private void CopyNode(XPathNavigator navigator)
        {
            XPathNodeType nodeType;
            int depthStart = _depth;
            Debug.Assert(navigator != null);

            while (true)
            {
                if (StartCopy(navigator, _depth == depthStart))
                {
                    nodeType = navigator.NodeType;
                    Debug.Assert(nodeType == XPathNodeType.Element, "StartCopy should return true only for Element nodes.");

                    // Copy attributes
                    if (navigator.MoveToFirstAttribute())
                    {
                        do
                        {
                            StartCopy(navigator, false);
                        }
                        while (navigator.MoveToNextAttribute());
                        navigator.MoveToParent();
                    }

                    // Copy namespaces in document order (navigator returns them in reverse document order)
                    CopyNamespaces(navigator, (_depth - 1 == depthStart) ? XPathNamespaceScope.ExcludeXml : XPathNamespaceScope.Local);

                    StartElementContentUnchecked();

                    // If children exist, move down to next level
                    if (navigator.MoveToFirstChild())
                        continue;

                    EndCopy(navigator, (_depth - 1) == depthStart);
                }

                // No children
                while (true)
                {
                    if (_depth == depthStart)
                    {
                        // The entire subtree has been copied
                        return;
                    }

                    if (navigator.MoveToNext())
                    {
                        // Found a sibling, so break to outer loop
                        break;
                    }

                    // No siblings, so move up to previous level
                    navigator.MoveToParent();

                    EndCopy(navigator, (_depth - 1) == depthStart);
                }
            }
        }

        /// <summary>
        /// Begin shallow copy of the navigator's current node to output.  Returns true if EndCopy
        /// should be called to complete the copy operation.
        /// </summary>
        private bool StartCopy(XPathNavigator navigator, bool callChk)
        {
            bool mayHaveChildren = false;

            switch (navigator.NodeType)
            {
                case XPathNodeType.Element:
                    // If checks need to be made, call XmlQueryOutput.WriteStartElement
                    if (callChk)
                    {
                        WriteStartElement(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI);
                    }
                    else
                    {
                        WriteStartElementUnchecked(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI);
                    }

                    mayHaveChildren = true;
                    break;

                case XPathNodeType.Attribute:
                    // If checks need to be made, call XmlQueryOutput.WriteStartAttribute
                    if (callChk)
                    {
                        WriteStartAttribute(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI);
                    }
                    else
                    {
                        WriteStartAttributeUnchecked(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI);
                    }

                    // Write attribute text
                    WriteString(navigator.Value);

                    // If checks need to be made, call XmlQueryOutput.WriteEndAttribute
                    if (callChk)
                    {
                        WriteEndAttribute();
                    }
                    else
                    {
                        WriteEndAttributeUnchecked();
                    }
                    break;

                case XPathNodeType.Namespace:
                    // If checks need to be made, call XmlQueryOutput.WriteNamespaceDeclaration
                    if (callChk)
                    {
                        // Do not allow namespaces to be copied after attributes
                        XmlAttributeCache attrCache = Writer as XmlAttributeCache;
                        if (attrCache != null && attrCache.Count != 0)
                            throw new XslTransformException(SR.XmlIl_NmspAfterAttr, string.Empty);

                        WriteNamespaceDeclaration(navigator.LocalName, navigator.Value);
                    }
                    else
                    {
                        WriteNamespaceDeclarationUnchecked(navigator.LocalName, navigator.Value);
                    }
                    break;

                case XPathNodeType.Text:
                case XPathNodeType.SignificantWhitespace:
                case XPathNodeType.Whitespace:
                    // If checks need to be made, call XmlQueryOutput.WriteString
                    if (callChk)
                    {
                        WriteString(navigator.Value, false);
                    }
                    else
                    {
                        // No flags are set, so this is simple element text (attributes, comments, pi's copy own text)
                        WriteStringUnchecked(navigator.Value);
                    }
                    break;

                case XPathNodeType.Root:
                    // Document node is invalid except at the top-level
                    Debug.Assert(_xstate != XmlState.WithinSequence, "StartCopy should not called if state is WithinSequence");
                    ThrowInvalidStateError(XPathNodeType.Root);
                    break;

                case XPathNodeType.Comment:
                    WriteStartComment();
                    WriteCommentString(navigator.Value);
                    WriteEndComment();
                    break;

                case XPathNodeType.ProcessingInstruction:
                    WriteStartProcessingInstruction(navigator.LocalName);
                    WriteProcessingInstructionString(navigator.Value);
                    WriteEndProcessingInstruction();
                    break;

                default:
                    Debug.Fail($"Unexpected node type {navigator.NodeType}");
                    break;
            }

            return mayHaveChildren;
        }

        /// <summary>
        /// End shallow copy of the navigator's current node to output.  This method should only be called if StartCopy
        /// returned true.
        /// </summary>
        private void EndCopy(XPathNavigator navigator, bool callChk)
        {
            Debug.Assert(navigator.NodeType == XPathNodeType.Element);
            Debug.Assert(_xstate == XmlState.WithinContent, "EndCopy cannot be called in the " + _xstate + " state.");

            if (callChk)
                WriteEndElement();
            else
                WriteEndElementUnchecked(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI);
        }

        /// <summary>
        /// Copy all namespaces of the specified type (in-scope, exclude-xml, local) in document order to output.
        /// </summary>
        private void CopyNamespaces(XPathNavigator navigator, XPathNamespaceScope nsScope)
        {
            Debug.Assert(navigator.NodeType == XPathNodeType.Element, "Only elements have namespaces to copy");

            // Default namespace undeclaration isn't included in navigator's namespace list, so add it now
            if (navigator.NamespaceURI.Length == 0)
            {
                Debug.Assert(navigator.LocalName.Length != 0, "xmlns:foo='' isn't allowed");
                WriteNamespaceDeclarationUnchecked(string.Empty, string.Empty);
            }

            // Since the namespace list is arranged in reverse-document order, recursively reverse it.
            if (navigator.MoveToFirstNamespace(nsScope))
            {
                CopyNamespacesHelper(navigator, nsScope);
                navigator.MoveToParent();
            }
        }

        /// <summary>
        /// Recursive helper function that reverses order of the namespaces retrieved by MoveToFirstNamespace and
        /// MoveToNextNamespace.
        /// </summary>
        private void CopyNamespacesHelper(XPathNavigator navigator, XPathNamespaceScope nsScope)
        {
            string prefix = navigator.LocalName;
            string ns = navigator.Value;

            if (navigator.MoveToNextNamespace(nsScope))
                CopyNamespacesHelper(navigator, nsScope);

            // No possibility for conflict, since we're copying namespaces from well-formed element
            WriteNamespaceDeclarationUnchecked(prefix, ns);
        }

        /// <summary>
        /// Ensure that state transitions to WithinContent.
        /// </summary>
        private void ConstructWithinContent(XPathNodeType rootType)
        {
            Debug.Assert(rootType == XPathNodeType.Element || rootType == XPathNodeType.Comment || rootType == XPathNodeType.ProcessingInstruction);

            switch (_xstate)
            {
                case XmlState.WithinSequence:
                    // If state is WithinSequence, call XmlSequenceWriter.StartTree
                    StartTree(rootType);
                    _xstate = XmlState.WithinContent;
                    break;

                case XmlState.WithinContent:
                    // Already within element content
                    break;

                case XmlState.EnumAttrs:
                    // Start element content
                    StartElementContentUnchecked();
                    break;

                default:
                    // Construction is not allowed in this state
                    ThrowInvalidStateError(rootType);
                    break;
            }
        }

        /// <summary>
        /// Ensure that state transitions to EnumAttrs.
        /// </summary>
        private void ConstructInEnumAttrs(XPathNodeType rootType)
        {
            Debug.Assert(rootType == XPathNodeType.Attribute || rootType == XPathNodeType.Namespace);

            switch (_xstate)
            {
                case XmlState.WithinSequence:
                    StartTree(rootType);
                    _xstate = XmlState.EnumAttrs;
                    break;

                case XmlState.EnumAttrs:
                    // Already in EnumAttrs state
                    break;

                default:
                    // Construction is not allowed in this state
                    ThrowInvalidStateError(rootType);
                    break;
            }
        }

        /// <summary>
        /// Namespace declarations are added to this.nsmgr.  Just before element content has begun, write out
        /// all namespaces that were declared locally on the element.
        /// </summary>
        private void WriteCachedNamespaces()
        {
            string prefix, ns;

            while (_cntNmsp != 0)
            {
                // Output each prefix->ns mapping pair
                Debug.Assert(_nsmgr != null);
                _cntNmsp--;
                _nsmgr.GetNamespaceDeclaration(_cntNmsp, out prefix, out ns);
                Writer.WriteNamespaceDeclaration(prefix, ns);
            }
        }

        /// <summary>
        /// Return the type of node that is under construction given the specified XmlState.
        /// </summary>
        private XPathNodeType XmlStateToNodeType(XmlState xstate)
        {
            switch (xstate)
            {
                case XmlState.EnumAttrs: return XPathNodeType.Element;
                case XmlState.WithinContent: return XPathNodeType.Element;
                case XmlState.WithinAttr: return XPathNodeType.Attribute;
                case XmlState.WithinComment: return XPathNodeType.Comment;
                case XmlState.WithinPI: return XPathNodeType.ProcessingInstruction;
            }

            Debug.Fail(xstate.ToString() + " is not a valid XmlState.");
            return XPathNodeType.Element;
        }

        /// <summary>
        /// If attribute's prefix conflicts with other prefixes then redeclare the prefix.  If the prefix has
        /// not yet been declared, then add it to the namespace manager.
        /// </summary>
        private string CheckAttributePrefix(string prefix, string ns)
        {
            string nsExisting;
            Debug.Assert(prefix.Length != 0 && ns.Length != 0);

            // Ensure that this attribute's prefix does not conflict with previously declared prefixes in this scope
            if (_nsmgr == null)
            {
                // If namespace manager has no namespaces, then there is no possibility of conflict
                WriteNamespaceDeclarationUnchecked(prefix, ns);
            }
            else
            {
                while (true)
                {
                    // If prefix is already mapped to a different namespace,
                    nsExisting = _nsmgr.LookupNamespace(prefix);
                    if (nsExisting != ns)
                    {
                        // Then if the prefix is already mapped,
                        if (nsExisting != null)
                        {
                            // Then there is a conflict that must be resolved by finding another prefix
                            // Always find a new prefix, even if the conflict didn't occur in the current scope
                            // This decision allows more aggressive namespace analysis at compile-time
                            prefix = RemapPrefix(prefix, ns, false);
                            continue;
                        }

                        // Add the mapping to the current scope
                        AddNamespace(prefix, ns);
                    }
                    break;
                }
            }

            return prefix;
        }

        /// <summary>
        /// Remaps an element or attribute prefix using the following rules:
        ///
        ///   1. If another in-scope prefix is already mapped to "ns", then use that
        ///   2. Otherwise, if a prefix was previously mapped to "ns" by this method, then use that
        ///   3. Otherwise, generate a new prefix of the form 'xp_??', where ?? is a stringized counter
        ///
        /// These rules tend to reduce the number of unique prefixes used throughout the tree.
        /// </summary>
        private string RemapPrefix(string prefix, string ns, bool isElemPrefix)
        {
            string genPrefix;
            Debug.Assert(prefix != null && ns != null && ns.Length != 0);

            if (_conflictPrefixes == null)
                _conflictPrefixes = new Dictionary<string, string>(16);

            if (_nsmgr == null)
            {
                _nsmgr = new XmlNamespaceManager(_runtime.NameTable);
                _nsmgr.PushScope();
            }

            // Rule #1: If another in-scope prefix is already mapped to "ns", then use that
            genPrefix = _nsmgr.LookupPrefix(ns);
            if (genPrefix != null)
            {
                // Can't use an empty prefix for an attribute
                if (isElemPrefix || genPrefix.Length != 0)
                    goto ReturnPrefix;
            }

            // Rule #2: Otherwise, if a prefix was previously mapped to "ns" by this method, then use that
            // Make sure that any previous prefix is different than "prefix"
            if (_conflictPrefixes.TryGetValue(ns, out genPrefix) && genPrefix != prefix)
            {
                // Can't use an empty prefix for an attribute
                if (isElemPrefix || genPrefix.Length != 0)
                    goto ReturnPrefix;
            }

            // Rule #3: Otherwise, generate a new prefix of the form 'xp_??', where ?? is a stringized counter
            genPrefix = "xp_" + (_prefixIndex++).ToString(CultureInfo.InvariantCulture);


        ReturnPrefix:
            // Save generated prefix so that it can be possibly be reused later
            _conflictPrefixes[ns] = genPrefix;

            return genPrefix;
        }

        /// <summary>
        /// Write an element or attribute with a name that is computed from a "prefix:localName" tag name and a set of prefix mappings.
        /// </summary>
        private void WriteStartComputed(XPathNodeType nodeType, string tagName, int prefixMappingsIndex)
        {
            string prefix, localName, ns;

            // Parse the tag name and map the prefix to a namespace
            _runtime.ParseTagName(tagName, prefixMappingsIndex, out prefix, out localName, out ns);

            // Validate the name parts
            prefix = EnsureValidName(prefix, localName, ns, nodeType);

            if (nodeType == XPathNodeType.Element)
                WriteStartElement(prefix, localName, ns);
            else
                WriteStartAttribute(prefix, localName, ns);
        }

        /// <summary>
        /// Write an element or attribute with a name that is computed from a "prefix:localName" tag name and a namespace URI.
        /// </summary>
        private void WriteStartComputed(XPathNodeType nodeType, string tagName, string ns)
        {
            string prefix, localName;

            // Parse the tagName as a prefix, localName pair
            ValidateNames.ParseQNameThrow(tagName, out prefix, out localName);

            // Validate the name parts
            prefix = EnsureValidName(prefix, localName, ns, nodeType);

            if (nodeType == XPathNodeType.Element)
                WriteStartElement(prefix, localName, ns);
            else
                WriteStartAttribute(prefix, localName, ns);
        }

        /// <summary>
        /// Write an element or attribute with a name that is copied from the navigator.
        /// </summary>
        private void WriteStartComputed(XPathNodeType nodeType, XPathNavigator navigator)
        {
            string prefix, localName, ns;

            prefix = navigator.Prefix;
            localName = navigator.LocalName;
            ns = navigator.NamespaceURI;

            if (navigator.NodeType != nodeType)
            {
                // Validate the name parts
                prefix = EnsureValidName(prefix, localName, ns, nodeType);
            }

            if (nodeType == XPathNodeType.Element)
                WriteStartElement(prefix, localName, ns);
            else
                WriteStartAttribute(prefix, localName, ns);
        }

        /// <summary>
        /// Write an element or attribute with a name that is derived from the XmlQualifiedName.
        /// </summary>
        private void WriteStartComputed(XPathNodeType nodeType, XmlQualifiedName name)
        {
            string prefix;
            Debug.Assert(ValidateNames.ParseNCName(name.Name, 0) == name.Name.Length);

            // Validate the name parts
            prefix = (name.Namespace.Length != 0) ? RemapPrefix(string.Empty, name.Namespace, nodeType == XPathNodeType.Element) : string.Empty;
            prefix = EnsureValidName(prefix, name.Name, name.Namespace, nodeType);

            if (nodeType == XPathNodeType.Element)
                WriteStartElement(prefix, name.Name, name.Namespace);
            else
                WriteStartAttribute(prefix, name.Name, name.Namespace);
        }

        /// <summary>
        /// Ensure that the specified name parts are valid according to Xml 1.0 and Namespace 1.0 rules.  Try to remap
        /// the prefix in order to attain validity.  Throw if validity is not possible.  Otherwise, return the (possibly
        /// remapped) prefix.
        /// </summary>
        private string EnsureValidName(string prefix, string localName, string ns, XPathNodeType nodeType)
        {
            if (!ValidateNames.ValidateName(prefix, localName, ns, nodeType, ValidateNames.Flags.AllExceptNCNames))
            {
                // Name parts are not valid as is.  Try to re-map the prefix.
                prefix = (ns.Length != 0) ? RemapPrefix(string.Empty, ns, nodeType == XPathNodeType.Element) : string.Empty;

                // Throw if validation does not work this time
                ValidateNames.ValidateNameThrow(prefix, localName, ns, nodeType, ValidateNames.Flags.AllExceptNCNames);
            }

            return prefix;
        }

        /// <summary>
        /// Push element name parts onto the stack.
        /// </summary>
        private void PushElementNames(string prefix, string localName, string ns)
        {
            // Push the name parts onto a stack
            if (_stkNames == null)
                _stkNames = new Stack<string>(15);

            _stkNames.Push(prefix);
            _stkNames.Push(localName);
            _stkNames.Push(ns);
        }

        /// <summary>
        /// Pop element name parts from the stack.
        /// </summary>
        private void PopElementNames(out string prefix, out string localName, out string ns)
        {
            Debug.Assert(_stkNames != null);

            ns = _stkNames.Pop();
            localName = _stkNames.Pop();
            prefix = _stkNames.Pop();
        }

        /// <summary>
        /// Throw an invalid state transition error.
        /// </summary>
        private void ThrowInvalidStateError(XPathNodeType constructorType)
        {
            switch (constructorType)
            {
                case XPathNodeType.Element:
                case XPathNodeType.Root:
                case XPathNodeType.Text:
                case XPathNodeType.Comment:
                case XPathNodeType.ProcessingInstruction:
                    throw new XslTransformException(SR.XmlIl_BadXmlState, new string[] { constructorType.ToString(), XmlStateToNodeType(_xstate).ToString() });

                case XPathNodeType.Attribute:
                case XPathNodeType.Namespace:
                    if (_depth == 1)
                        throw new XslTransformException(SR.XmlIl_BadXmlState, new string[] { constructorType.ToString(), _rootType.ToString() });

                    if (_xstate == XmlState.WithinContent)
                        throw new XslTransformException(SR.XmlIl_BadXmlStateAttr, string.Empty);

                    goto case XPathNodeType.Element;

                default:
                    throw new XslTransformException(SR.XmlIl_BadXmlState, new string[] { "Unknown", XmlStateToNodeType(_xstate).ToString() });
            }
        }
    }
}
