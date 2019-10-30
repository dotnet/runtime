// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// A node in the QIL tree.
    /// </summary>
    /// <remarks>
    /// Don't construct QIL nodes directly; instead, use the <see cref="QilFactory">QilFactory</see>.
    /// This base internal class is not abstract and may be instantiated in some cases (for example, true/false boolean literals).
    /// </remarks>
    internal class QilNode : IList<QilNode>
    {
        protected QilNodeType nodeType;
        protected XmlQueryType xmlType;
        protected ISourceLineInfo sourceLine;
        protected object annotation;

        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilNode(QilNodeType nodeType)
        {
            this.nodeType = nodeType;
        }

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilNode(QilNodeType nodeType, XmlQueryType xmlType)
        {
            this.nodeType = nodeType;
            this.xmlType = xmlType;
        }


        //-----------------------------------------------
        // QilNode methods
        //-----------------------------------------------

        /// <summary>
        /// Access the QIL node type.
        /// </summary>
        public QilNodeType NodeType
        {
            get { return this.nodeType; }
            set { this.nodeType = value; }
        }

        /// <summary>
        /// Access the QIL type.
        /// </summary>
        public virtual XmlQueryType XmlType
        {
            get { return this.xmlType; }
            set { this.xmlType = value; }
        }

        /// <summary>
        /// Line info information for tools support.
        /// </summary>
        public ISourceLineInfo SourceLine
        {
            get { return this.sourceLine; }
            set { this.sourceLine = value; }
        }

        /// <summary>
        /// Access an annotation which may have been attached to this node.
        /// </summary>
        public object Annotation
        {
            get { return this.annotation; }
            set { this.annotation = value; }
        }

        /// <summary>
        /// Create a new deep copy of this node.
        /// </summary>
        public virtual QilNode DeepClone(QilFactory f)
        {
            return new QilCloneVisitor(f).Clone(this);
        }

        /// <summary>
        /// Create a shallow copy of this node, copying all the fields.
        /// </summary>
        public virtual QilNode ShallowClone(QilFactory f)
        {
            QilNode n = (QilNode)MemberwiseClone();
            f.TraceNode(n);
            return n;
        }


        //-----------------------------------------------
        // IList<QilNode> methods -- override
        //-----------------------------------------------

        public virtual int Count
        {
            get { return 0; }
        }

        public virtual QilNode this[int index]
        {
            get { throw new IndexOutOfRangeException(); }
            set { throw new IndexOutOfRangeException(); }
        }

        public virtual void Insert(int index, QilNode node)
        {
            throw new NotSupportedException();
        }

        public virtual void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }


        //-----------------------------------------------
        // IList<QilNode> methods -- no need to override
        //-----------------------------------------------

        public IEnumerator<QilNode> GetEnumerator()
        {
            return new IListEnumerator<QilNode>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new IListEnumerator<QilNode>(this);
        }

        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        public virtual void Add(QilNode node)
        {
            Insert(Count, node);
        }

        public virtual void Add(IList<QilNode> list)
        {
            for (int i = 0; i < list.Count; i++)
                Insert(Count, list[i]);
        }

        public virtual void Clear()
        {
            for (int index = Count - 1; index >= 0; index--)
                RemoveAt(index);
        }

        public virtual bool Contains(QilNode node)
        {
            return IndexOf(node) != -1;
        }

        public virtual void CopyTo(QilNode[] array, int index)
        {
            for (int i = 0; i < Count; i++)
                array[index + i] = this[i];
        }

        public virtual bool Remove(QilNode node)
        {
            int index = IndexOf(node);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public virtual int IndexOf(QilNode node)
        {
            for (int i = 0; i < Count; i++)
                if (node.Equals(this[i]))
                    return i;

            return -1;
        }

        //-----------------------------------------------
        // Debug
        //-----------------------------------------------

#if QIL_TRACE_NODE_CREATION
        private int nodeId;
        private string nodeLoc;

        public int NodeId {
            get { return nodeId; }
            set { nodeId = value; }
        }

        public string NodeLocation {
            get { return nodeLoc; }
            set { nodeLoc = value; }
        }
#endif
    }
}
