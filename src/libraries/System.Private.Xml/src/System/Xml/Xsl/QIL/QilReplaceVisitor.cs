// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Xsl;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// Base internal class for visitors that replace the graph as they visit it.
    /// </summary>
    internal abstract class QilReplaceVisitor : QilVisitor
    {
        protected QilFactory f;

        public QilReplaceVisitor(QilFactory f)
        {
            this.f = f;
        }


        //-----------------------------------------------
        // QilVisitor overrides
        //-----------------------------------------------

        /// <summary>
        /// Visit all children of "parent", replacing each child with a copy of each child.
        /// </summary>
        protected override QilNode VisitChildren(QilNode parent)
        {
            XmlQueryType oldParentType = parent.XmlType!;
            bool recalcType = false;

            // Visit children
            for (int i = 0; i < parent.Count; i++)
            {
                QilNode oldChild = parent[i], newChild;
                XmlQueryType? oldChildType = oldChild != null ? oldChild.XmlType : null;

                // Visit child
                if (IsReference(parent, i))
                    newChild = VisitReference(oldChild!);
                else
                    newChild = Visit(oldChild!);

                // Only replace child and recalculate type if oldChild != newChild or oldChild.XmlType != newChild.XmlType
                if ((object?)oldChild != (object)newChild || (newChild != null && (object?)oldChildType != (object?)newChild.XmlType))
                {
                    recalcType = true;
                    parent[i] = newChild;
                }
            }

            if (recalcType)
                RecalculateType(parent, oldParentType);

            return parent;
        }


        //-----------------------------------------------
        // QilReplaceVisitor methods
        //-----------------------------------------------

        /// <summary>
        /// Once children have been replaced, the Xml type is recalculated.
        /// </summary>
        protected virtual void RecalculateType(QilNode node, XmlQueryType oldType)
        {
            XmlQueryType newType;

            newType = QilTypeChecker.Check(node);

            // Note the use of AtMost to account for cases when folding of Error nodes in the graph cause
            // cardinality to be recalculated.
            // For example, (Sequence (TextCtor (Error "error")) (Int32 1)) => (Sequence (Error "error") (Int32 1))
            // In this case, cardinality has gone from More to One
            Debug.Assert(newType.IsSubtypeOf(XmlQueryTypeFactory.AtMost(oldType, oldType.Cardinality)), "Replace shouldn't relax original type");

            node.XmlType = newType;
        }
    }
}
