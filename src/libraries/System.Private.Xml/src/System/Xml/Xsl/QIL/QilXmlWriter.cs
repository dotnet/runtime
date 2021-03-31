// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Xml;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// If an annotation implements this interface, then QilXmlWriter will call ToString() on the annotation
    /// and serialize the result (if non-empty).
    /// </summary>
    internal interface IQilAnnotation
    {
        string Name { get; }
    };


    /// <summary>
    /// An example of QilVisitor.  Prints the QilExpression tree as XML.
    /// </summary>
    /// <remarks>
    /// <para>The QilXmlWriter Visits every node in the tree, printing out an XML representation of
    /// each node.  Several formatting options are available, including whether or not to include annotations
    /// and type information.  When full information is printed out, the graph can be reloaded from
    /// its serialized form using QilXmlReader.</para>
    /// <para>The XML format essentially uses one XML element for each node in the QIL graph.
    /// Node properties such as type information are serialized as XML attributes.
    /// Annotations are serialized as processing-instructions in front of a node.</para>
    /// <para>Feel free to subclass this visitor to customize its behavior.</para>
    /// </remarks>
    internal sealed class QilXmlWriter : QilScopedVisitor
    {
        private XmlWriter writer;
        private Options options;
        private readonly NameGenerator _ngen;

        [Flags]
        public enum Options
        {
            None = 0,               // No options selected
            Annotations = 1,        // Print annotations
            TypeInfo = 2,           // Print type information using "G" option
            RoundTripTypeInfo = 4,  // Print type information using "S" option
            LineInfo = 8,           // Print source line information
            NodeIdentity = 16,      // Print node identity (only works if QIL_TRACE_NODE_CREATION is defined)
            NodeLocation = 32,      // Print node creation location (only works if QIL_TRACE_NODE_CREATION is defined)
        };

        /// <summary>
        /// Construct a QilXmlWriter.
        /// </summary>
        public QilXmlWriter(XmlWriter writer) : this(writer, Options.Annotations | Options.TypeInfo | Options.LineInfo | Options.NodeIdentity | Options.NodeLocation)
        {
        }

        /// <summary>
        /// Construct a QilXmlWriter.
        /// </summary>
        public QilXmlWriter(XmlWriter writer, Options options)
        {
            this.writer = writer;
            _ngen = new NameGenerator();
            this.options = options;
        }

        /// <summary>
        /// Serialize a QilExpression graph as XML.
        /// </summary>
        /// <param name="node">the QilExpression graph</param>
        public void ToXml(QilNode node)
        {
            VisitAssumeReference(node);
        }

        //-----------------------------------------------
        // QilXmlWrite methods
        //-----------------------------------------------

        /// <summary>
        /// Write all annotations as comments:
        ///     1. string -- <!-- (string) ann -->
        ///     2. IQilAnnotation -- <!-- ann.Name = ann.ToString() -->
        ///     3. IList{object} -- recursively call WriteAnnotations for each object in list
        ///     4. otherwise, do not write the annotation
        /// </summary>
        private void WriteAnnotations(object? ann)
        {
            string? s = null, name = null;

            if (ann == null)
            {
                return;
            }
            else if (ann is string)
            {
                s = ann as string;
            }
            else if (ann is IQilAnnotation qilann)
            {
                name = qilann.Name;
                s = ann.ToString();
            }
            else if (ann is IList<object> list)
            {
                foreach (object annItem in list)
                    WriteAnnotations(annItem);
                return;
            }

            if (s != null && s.Length != 0)
                this.writer.WriteComment(name != null && name.Length != 0 ? name + ": " + s : s);
        }

        /// <summary>
        /// Called in order to write out source line information.
        /// </summary>
        private void WriteLineInfo(QilNode node)
        {
            this.writer.WriteAttributeString("lineInfo", string.Format(CultureInfo.InvariantCulture, "[{0},{1} -- {2},{3}]",
                node.SourceLine!.Start.Line, node.SourceLine.Start.Pos,
                node.SourceLine.End.Line, node.SourceLine.End.Pos
                ));
        }

        /// <summary>
        /// Called in order to write out the xml type of a node.
        /// </summary>
        private void WriteXmlType(QilNode node)
        {
            this.writer.WriteAttributeString("xmlType", node.XmlType!.ToString((this.options & Options.RoundTripTypeInfo) != 0 ? "S" : "G"));
        }


        //-----------------------------------------------
        // QilVisitor overrides
        //-----------------------------------------------

        /// <summary>
        /// Override certain node types in order to add additional attributes, suppress children, etc.
        /// </summary>
        protected override QilNode VisitChildren(QilNode node)
        {
            if (node is QilLiteral)
            {
                // If literal is not handled elsewhere, print its string value
                this.writer.WriteValue(Convert.ToString(((QilLiteral)node).Value, CultureInfo.InvariantCulture));
                return node;
            }
            else if (node is QilReference)
            {
                QilReference reference = (QilReference)node;

                // Write the generated identifier for this iterator
                this.writer.WriteAttributeString("id", _ngen.NameOf(node));

                // Write the debug name of this reference (if it's defined) as a "name" attribute
                if (reference.DebugName != null)
                    this.writer.WriteAttributeString("name", reference.DebugName.ToString());

                if (node.NodeType == QilNodeType.Parameter)
                {
                    // Don't visit parameter's name, or its default value if it is null
                    QilParameter param = (QilParameter)node;

                    if (param.DefaultValue != null)
                        VisitAssumeReference(param.DefaultValue);

                    return node;
                }
            }

            return base.VisitChildren(node);
        }

        /// <summary>
        /// Write references to functions or iterators like this: <RefTo id="$a"/>.
        /// </summary>
        protected override QilNode VisitReference(QilNode node)
        {
            QilReference reference = (QilReference)node;
            string name = _ngen.NameOf(node);
            if (name == null)
                name = "OUT-OF-SCOPE REFERENCE";

            this.writer.WriteStartElement("RefTo");
            this.writer.WriteAttributeString("id", name);
            if (reference.DebugName != null)
                this.writer.WriteAttributeString("name", reference.DebugName.ToString());
            this.writer.WriteEndElement();

            return node;
        }

        /// <summary>
        /// Scan through the external parameters, global variables, and function list for forward references.
        /// </summary>
        protected override QilNode VisitQilExpression(QilExpression qil)
        {
            IList<QilNode> fdecls = new ForwardRefFinder().Find(qil);
            if (fdecls != null && fdecls.Count > 0)
            {
                this.writer.WriteStartElement("ForwardDecls");
                foreach (QilNode n in fdecls)
                {
                    // i.e. <Function id="$a"/>
                    this.writer.WriteStartElement(Enum.GetName(typeof(QilNodeType), n.NodeType)!);
                    this.writer.WriteAttributeString("id", _ngen.NameOf(n));
                    WriteXmlType(n);

                    if (n.NodeType == QilNodeType.Function)
                    {
                        // Visit Arguments and SideEffects operands
                        Visit(n[0]);
                        Visit(n[2]);
                    }

                    this.writer.WriteEndElement();
                }
                this.writer.WriteEndElement();
            }

            return VisitChildren(qil);
        }

        /// <summary>
        /// Serialize literal types using either "S" or "G" formatting, depending on the option which has been set.
        /// </summary>
        protected override QilNode VisitLiteralType(QilLiteral value)
        {
            this.writer.WriteString(((XmlQueryType)value).ToString((this.options & Options.TypeInfo) != 0 ? "G" : "S"));
            return value;
        }

        /// <summary>
        /// Serialize literal QName as three separate attributes.
        /// </summary>
        protected override QilNode VisitLiteralQName(QilName value)
        {
            this.writer.WriteAttributeString("name", value.ToString());
            return value;
        }


        //-----------------------------------------------
        // QilScopedVisitor overrides
        //-----------------------------------------------

        /// <summary>
        /// Annotate this iterator or function with a generated name.
        /// </summary>
        protected override void BeginScope(QilNode node)
        {
            _ngen.NameOf(node);
        }

        /// <summary>
        /// Clear the name annotation on this iterator or function.
        /// </summary>
        protected override void EndScope(QilNode node)
        {
            _ngen.ClearName(node);
        }

        /// <summary>
        /// By default, call WriteStartElement for every node type.
        /// </summary>
        protected override void BeforeVisit(QilNode node)
        {
            base.BeforeVisit(node);

            // Write the annotations in front of the element, to avoid issues with attributes
            // and make it easier to round-trip
            if ((this.options & Options.Annotations) != 0)
                WriteAnnotations(node.Annotation);

            // Call WriteStartElement
            this.writer.WriteStartElement("", Enum.GetName(typeof(QilNodeType), node.NodeType)!, "");

            // Write common attributes
#if QIL_TRACE_NODE_CREATION
            if ((this.options & Options.NodeIdentity) != 0)
                this.writer.WriteAttributeString("nodeId", node.NodeId.ToString(CultureInfo.InvariantCulture));

            if ((this.options & Options.NodeLocation) != 0)
                this.writer.WriteAttributeString("nodeLoc", node.NodeLocation);
#endif
            if ((this.options & (Options.TypeInfo | Options.RoundTripTypeInfo)) != 0)
                WriteXmlType(node);

            if ((this.options & Options.LineInfo) != 0 && node.SourceLine != null)
                WriteLineInfo(node);
        }

        /// <summary>
        /// By default, call WriteEndElement for every node type.
        /// </summary>
        protected override void AfterVisit(QilNode node)
        {
            this.writer.WriteEndElement();

            base.AfterVisit(node);
        }


        //-----------------------------------------------
        // Helper methods
        //-----------------------------------------------

        /// <summary>
        /// Find list of all iterators and functions which are referenced before they have been declared.
        /// </summary>
        internal sealed class ForwardRefFinder : QilVisitor
        {
            private readonly List<QilNode> _fwdrefs = new List<QilNode>();
            private readonly List<QilNode> _backrefs = new List<QilNode>();

            public IList<QilNode> Find(QilExpression qil)
            {
                Visit(qil);
                return _fwdrefs;
            }

            /// <summary>
            /// Add iterators and functions to backrefs list as they are visited.
            /// </summary>
            protected override QilNode Visit(QilNode node)
            {
                if (node is QilIterator || node is QilFunction)
                    _backrefs.Add(node);

                return base.Visit(node);
            }

            /// <summary>
            /// If reference is not in scope, then it must be a forward reference.
            /// </summary>
            protected override QilNode VisitReference(QilNode node)
            {
                if (!_backrefs.Contains(node) && !_fwdrefs.Contains(node))
                    _fwdrefs.Add(node);

                return node;
            }
        }

        //=================================== Helper class: NameGenerator =========================================

        private sealed class NameGenerator
        {
            private readonly StringBuilder _name;
            private int _len;
            private readonly int _zero;
            private readonly char _start;
            private readonly char _end;

            /// <summary>
            /// Construct a new name generator with prefix "$" and alphabetical mode.
            /// </summary>
            public NameGenerator()
            {
                string prefix = "$";
                _len = _zero = prefix.Length;
                _start = 'a';
                _end = 'z';
                _name = new StringBuilder(prefix, _len + 2);
                _name.Append(_start);
            }

            /// <summary>
            /// Skolem function for names.
            /// </summary>
            /// <returns>a unique name beginning with the prefix</returns>
            public string NextName()
            {
                string result = _name.ToString();

                char c = _name[_len];
                if (c == _end)
                {
                    _name[_len] = _start;
                    int i = _len;
                    for (; i-- > _zero && _name[i] == _end;)
                        _name[i] = _start;

                    if (i < _zero)
                    {
                        _len++;
                        _name.Append(_start);
                    }
                    else
                        _name[i]++;
                }
                else
                    _name[_len] = ++c;

                return result;
            }

            /// <summary>
            /// Lookup or generate a name for a node.  Uses annotations to store the name on the node.
            /// </summary>
            /// <param name="n">the node</param>
            /// <returns>the node name (unique across nodes)</returns>
            public string NameOf(QilNode n)
            {
                string? name = null;

                object? old = n.Annotation;
                NameAnnotation? a = old as NameAnnotation;
                if (a == null)
                {
                    name = NextName();
                    n.Annotation = new NameAnnotation(name, old);
                }
                else
                {
                    name = a.Name;
                }
                return name;
            }

            /// <summary>
            /// Clear name annotation from a node.
            /// </summary>
            /// <param name="n">the node</param>
            public void ClearName(QilNode n)
            {
                if (n.Annotation is NameAnnotation)
                    n.Annotation = ((NameAnnotation)n.Annotation).PriorAnnotation;
            }

            /// <summary>
            /// Class used to hold our annotations on the graph
            /// </summary>
            private sealed class NameAnnotation : ListBase<object?>
            {
                public string Name;
                public object? PriorAnnotation;

                public NameAnnotation(string s, object? a)
                {
                    Name = s;
                    PriorAnnotation = a;
                }

                public override int Count
                {
                    get { return 1; }
                }

                public override object? this[int index]
                {
                    get
                    {
                        if (index == 0)
                            return PriorAnnotation;

                        throw new IndexOutOfRangeException();
                    }
                    set { throw new NotSupportedException(); }
                }
            }
        }
    }
}
