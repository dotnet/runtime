// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.XPath;
using MS.Internal.Xml;

namespace System.Xml.Xsl.Xslt
{
    internal sealed class KeyMatchBuilder : XPathBuilder, XPathPatternParser.IPatternBuilder
    {
        private int _depth;
        private readonly PathConvertor _convertor;

        public KeyMatchBuilder(IXPathEnvironment env) : base(env)
        {
            _convertor = new PathConvertor(env.Factory);
        }

        public override void StartBuild()
        {
            Debug.Assert(0 <= _depth && _depth <= 1, "this shouldn't happen");
            if (_depth == 0)
            {
                base.StartBuild();
            }
            _depth++;
        }

        [return: NotNullIfNotNull(nameof(result))]
        public override QilNode? EndBuild(QilNode? result)
        {
            _depth--;
            Debug.Assert(0 <= _depth && _depth <= 1, "this shouldn't happen");
            if (result == null)
            { // special door to clean builder state in exception handlers
                return base.EndBuild(result);
            }
            if (_depth == 0)
            {
                Debug.Assert(base.numFixupLast == 0);
                Debug.Assert(base.numFixupPosition == 0);
                result = _convertor.ConvertReletive2Absolute(result, base.fixupCurrent);
                result = base.EndBuild(result);
            }
            return result;
        }

        // -------------------------------------- GetPredicateBuilder() ---------------------------------------

        public IXPathBuilder<QilNode> GetPredicateBuilder(QilNode ctx)
        {
            return this;
        }

        // This code depends on particula shapes that XPathBuilder generates.
        // It works only on pathes.
        internal sealed class PathConvertor : QilReplaceVisitor
        {
            private new readonly XPathQilFactory f;
            private QilNode? _fixup;
            public PathConvertor(XPathQilFactory f) : base(f.BaseFactory)
            {
                this.f = f;
            }

            public QilNode ConvertReletive2Absolute(QilNode node, QilNode fixup)
            {
                QilDepthChecker.Check(node);
                Debug.Assert(node != null);
                Debug.Assert(fixup != null);
                _fixup = fixup;
                return this.Visit(node);
            }

            // transparantly passing through Union and DocOrder
            protected override QilNode Visit(QilNode n)
            {
                if (
                    n.NodeType == QilNodeType.Union ||
                    n.NodeType == QilNodeType.DocOrderDistinct ||
                    n.NodeType == QilNodeType.Filter ||
                    n.NodeType == QilNodeType.Loop
                )
                {
                    return base.Visit(n);
                }
                return n;
            }
            // Filers that travers Content being converted to global travers:
            // Filter($j= ... Filter($i = Content(fixup), ...))  -> Filter($j= ... Filter($i = Loop($j = DesendentOrSelf(Root(fixup)), Content($j), ...)))
            protected override QilNode VisitLoop(QilLoop n)
            {
                if (n.Variable.Binding!.NodeType == QilNodeType.Root || n.Variable.Binding.NodeType == QilNodeType.Deref)
                {
                    // This is absolute path already. We shouldn't touch it
                    return n;
                }
                if (n.Variable.Binding.NodeType == QilNodeType.Content)
                {
                    // This is "begin" of reletive path. Let's rewrite it as absolute:
                    QilUnary content = (QilUnary)n.Variable.Binding;
                    Debug.Assert(content.Child == _fixup, "Unexpected content node");
                    QilIterator it = f.For(f.DescendantOrSelf(f.Root(_fixup)));
                    content.Child = it;
                    n.Variable.Binding = f.Loop(it, content);
                    return n;
                }
                n.Variable.Binding = Visit(n.Variable.Binding);
                return n;
            }

            protected override QilNode VisitFilter(QilLoop n)
            {
                return VisitLoop(n);
            }
        }
    }
}
