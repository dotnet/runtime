// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.Xsl;

namespace System.Xml.Xsl.Qil
{
    // Create an exact replica of a QIL graph
    internal class QilCloneVisitor : QilScopedVisitor
    {
        private QilFactory _fac;
        private SubstitutionList _subs;


        //-----------------------------------------------
        // Constructors
        //-----------------------------------------------

        public QilCloneVisitor(QilFactory fac) : this(fac, new SubstitutionList())
        {
        }

        public QilCloneVisitor(QilFactory fac, SubstitutionList subs)
        {
            _fac = fac;
            _subs = subs;
        }


        //-----------------------------------------------
        // Entry
        //-----------------------------------------------

        public QilNode Clone(QilNode node)
        {
            QilDepthChecker.Check(node);
            // Assume that iterator nodes at the top-level are references rather than definitions
            return VisitAssumeReference(node);
        }


        //-----------------------------------------------
        // QilVisitor overrides
        //-----------------------------------------------

        /// <summary>
        /// Visit all children of "parent", replacing each child with a copy of each child.
        /// </summary>
        protected override QilNode Visit(QilNode oldNode)
        {
            QilNode newNode = null;

            if (oldNode == null)
                return null;

            // ShallowClone any nodes which have not yet been cloned
            if (oldNode is QilReference)
            {
                // Reference nodes may have been cloned previously and put into scope
                newNode = FindClonedReference(oldNode);
            }

            if (newNode == null)
                newNode = oldNode.ShallowClone(_fac);

            return base.Visit(newNode);
        }

        /// <summary>
        /// Visit all children of "parent", replacing each child with a copy of each child.
        /// </summary>
        protected override QilNode VisitChildren(QilNode parent)
        {
            // Visit children
            for (int i = 0; i < parent.Count; i++)
            {
                QilNode child = parent[i];

                // If child is a reference,
                if (IsReference(parent, i))
                {
                    // Visit the reference and substitute its copy
                    parent[i] = VisitReference(child);

                    // If no substutition found, then use original child
                    if (parent[i] == null)
                        parent[i] = child;
                }
                else
                {
                    // Otherwise, visit the node and substitute its copy
                    parent[i] = Visit(child);
                }
            }

            return parent;
        }

        /// <summary>
        /// If a cloned reference is in scope, replace "oldNode".  Otherwise, return "oldNode".
        /// </summary>
        protected override QilNode VisitReference(QilNode oldNode)
        {
            QilNode newNode = FindClonedReference(oldNode);
            return base.VisitReference(newNode == null ? oldNode : newNode);
        }


        //-----------------------------------------------
        // QilScopedVisitor methods
        //-----------------------------------------------

        /// <summary>
        /// Push node and its shallow clone onto the substitution list.
        /// </summary>
        protected override void BeginScope(QilNode node)
        {
            _subs.AddSubstitutionPair(node, node.ShallowClone(_fac));
        }

        /// <summary>
        /// Pop entry from substitution list.
        /// </summary>
        protected override void EndScope(QilNode node)
        {
            _subs.RemoveLastSubstitutionPair();
        }


        //-----------------------------------------------
        // QilCloneVisitor methods
        //-----------------------------------------------

        /// <summary>
        /// Find the clone of an in-scope reference.
        /// </summary>
        protected QilNode FindClonedReference(QilNode node)
        {
            return _subs.FindReplacement(node);
        }
    }
}
