// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil operator having N children.
    /// </summary>
    /// <remarks>
    /// Don't construct QIL nodes directly; instead, use the <see cref="QilFactory">QilFactory</see>.
    /// </remarks>
    internal sealed class QilList : QilNode
    {
        private int _count;
        private QilNode[] _members;


        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new (empty) QilList
        /// </summary>
        public QilList(QilNodeType nodeType) : base(nodeType)
        {
            _members = new QilNode[4];
            this.xmlType = null;
        }


        //-----------------------------------------------
        // QilNode methods
        //-----------------------------------------------

        /// <summary>
        /// Lazily create the XmlQueryType.
        /// </summary>
        public override XmlQueryType XmlType
        {
            get
            {
                if (this.xmlType == null)
                {
                    XmlQueryType xt = XmlQueryTypeFactory.Empty;

                    if (_count > 0)
                    {
                        if (this.nodeType == QilNodeType.Sequence)
                        {
                            for (int i = 0; i < _count; i++)
                                xt = XmlQueryTypeFactory.Sequence(xt, _members[i].XmlType!);

                            Debug.Assert(!xt.IsDod, "Sequences do not preserve DocOrderDistinct");
                        }
                        else if (this.nodeType == QilNodeType.BranchList)
                        {
                            xt = _members[0].XmlType!;
                            for (int i = 1; i < _count; i++)
                                xt = XmlQueryTypeFactory.Choice(xt, _members[i].XmlType!);
                        }
                    }

                    this.xmlType = xt;
                }

                return this.xmlType;
            }
        }

        /// <summary>
        /// Override in order to clone the "members" array.
        /// </summary>
        public override QilNode ShallowClone(QilFactory f)
        {
            QilList n = (QilList)MemberwiseClone();
            n._members = (QilNode[])_members.Clone();
            f.TraceNode(n);
            return n;
        }


        //-----------------------------------------------
        // IList<QilNode> methods -- override
        //-----------------------------------------------

        public override int Count
        {
            get { return _count; }
        }

        public override QilNode this[int index]
        {
            get
            {
                if (index >= 0 && index < _count)
                    return _members[index];

                throw new IndexOutOfRangeException();
            }
            set
            {
                if (index >= 0 && index < _count)
                    _members[index] = value;
                else
                    throw new IndexOutOfRangeException();

                // Invalidate XmlType
                this.xmlType = null;
            }
        }

        public override void Insert(int index, QilNode node)
        {
            if (index < 0 || index > _count)
                throw new IndexOutOfRangeException();

            if (_count == _members.Length)
            {
                QilNode[] membersNew = new QilNode[_count * 2];
                Array.Copy(_members, membersNew, _count);
                _members = membersNew;
            }

            if (index < _count)
                Array.Copy(_members, index, _members, index + 1, _count - index);

            _count++;
            _members[index] = node;

            // Invalidate XmlType
            this.xmlType = null;
        }

        public override void RemoveAt(int index)
        {
            if (index < 0 || index >= _count)
                throw new IndexOutOfRangeException();

            _count--;
            if (index < _count)
                Array.Copy(_members, index + 1, _members, index, _count - index);

            _members[_count] = null!;

            // Invalidate XmlType
            this.xmlType = null;
        }
    }
}
