// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>An internal class that validates QilExpression graphs.</summary>
    /// <remarks>
    /// QilValidationVisitor traverses the QilExpression graph once to enforce the following constraints:
    /// <list type="bullet">
    ///     <item>No circular references</item>
    ///     <item>No duplicate nodes (except for references)</item>
    ///     <item>No out-of-scope references</item>
    ///     <item>Type constraints on operands</item>
    ///     <item>Type constraints on operators</item>
    ///     <item>No null objects (except where allowed)</item>
    ///     <item>No Unknown node types</item>
    /// </list>
    /// <p>When an error occurs, it marks the offending node with an annotation and continues checking,
    /// allowing the detection of multiple errors at once and printing the structure after validation.
    /// (In the case of circular references, it breaks the loop at the circular reference to allow the graph
    /// to print correctly.)</p>
    /// </remarks>
    ///
    internal sealed class QilValidationVisitor : QilScopedVisitor
    {
        //-----------------------------------------------
        // Entry
        //-----------------------------------------------

        [Conditional("DEBUG")]
        public static void Validate(QilNode node)
        {
            Debug.Assert(node != null);
            new QilValidationVisitor().VisitAssumeReference(node);
        }

        private QilValidationVisitor() { }

#if DEBUG
        private Hashtable allNodes = new ObjectHashtable();
        private Hashtable parents = new ObjectHashtable();
        private Hashtable scope = new ObjectHashtable();


        //-----------------------------------------------
        // QilVisitor overrides
        //-----------------------------------------------

        protected override QilNode VisitChildren(QilNode parent)
        {
            if (this.parents.Contains(parent))
            {
                // We have already visited the node that starts the infinite loop, but don't visit its children
                SetError(parent, "Infinite loop");
            }
            else if (AddNode(parent))
            {
                if (parent.XmlType == null)
                {
                    SetError(parent, "Type information missing");
                }
                else
                {
                    XmlQueryType type = QilTypeChecker.Check(parent);

                    // BUGBUG: Hack to account for Xslt compiler type fixups
                    if (!type.IsSubtypeOf(parent.XmlType))
                        SetError(parent, "Type information was not correctly inferred");
                }

                this.parents.Add(parent, parent);

                for (int i = 0; i < parent.Count; i++)
                {
                    if (parent[i] == null)
                    {
                        // Allow parameter name and default value to be null
                        if (parent.NodeType == QilNodeType.Parameter)
                            continue;
                        // Do not allow null anywhere else in the graph
                        else
                            SetError(parent, "Child " + i + " must not be null");
                    }

                    if (parent.NodeType == QilNodeType.GlobalVariableList ||
                        parent.NodeType == QilNodeType.GlobalParameterList ||
                        parent.NodeType == QilNodeType.FunctionList)
                    {
                        if (((QilReference)parent[i]).DebugName == null)
                            SetError(parent[i], "DebugName must not be null");
                    }

                    // If child is a reference, then call VisitReference instead of Visit in order to avoid circular visits.
                    if (IsReference(parent, i))
                        VisitReference(parent[i]);
                    else
                        Visit(parent[i]);
                }

                this.parents.Remove(parent);
            }

            return parent;
        }

        /// <summary>
        /// Ensure that the function or iterator reference is already in scope.
        /// </summary>
        protected override QilNode VisitReference(QilNode node)
        {
            if (!this.scope.Contains(node))
                SetError(node, "Out-of-scope reference");

            return node;
        }


        //-----------------------------------------------
        // QilScopedVisitor overrides
        //-----------------------------------------------

        /// <summary>
        /// Add an iterator or function to scope if it hasn't been added already.
        /// </summary>
        protected override void BeginScope(QilNode node)
        {
            if (this.scope.Contains(node))
                SetError(node, "Reference already in scope");
            else
                this.scope.Add(node, node);
        }

        /// <summary>
        /// Pop scope.
        /// </summary>
        protected override void EndScope(QilNode node)
        {
            this.scope.Remove(node);
        }


        //-----------------------------------------------
        // Helper methods
        //-----------------------------------------------

        private sealed class ObjectHashtable : Hashtable
        {
            protected override bool KeyEquals(object? item, object key)
            {
                return item == key;
            }
        }

        private bool AddNode(QilNode n)
        {
            if (!this.allNodes.Contains(n))
            {
                this.allNodes.Add(n, n);
                return true;
            }
            else
            {
                SetError(n, "Duplicate " + n.NodeType + " node");
                return false;
            }
        }
#endif // DEBUG

        [Conditional("DEBUG")]
        internal static void SetError(QilNode n, string message)
        {
            message = SR.Format(SR.Qil_Validation, message);

#if QIL_TRACE_NODE_CREATION
            message += " ["+ n.NodeId + " (" + n.NodeType.ToString("G") + ")]";
#endif
            if (n.Annotation is string s)
            {
                message = $"{s}{Environment.NewLine}{message}";
            }
            n.Annotation = message;
            Debug.Fail(message);
        }
    }
}
