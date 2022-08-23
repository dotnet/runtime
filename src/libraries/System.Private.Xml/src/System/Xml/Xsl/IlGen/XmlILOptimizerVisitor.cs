// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.IlGen
{
    internal sealed class XmlILOptimizerVisitor : QilPatternVisitor
    {
        private static readonly QilPatterns s_patternsNoOpt = CreatePatternsNoOpt();

        // Enable all normalizations and annotations for Release code
        // Enable all patterns for Release code
        private static readonly QilPatterns s_patternsOpt = new QilPatterns((int)XmlILOptimization.Last_, allSet: true);

        private readonly QilExpression _qil;
        private readonly XmlILElementAnalyzer _elemAnalyzer;
        private readonly XmlILStateAnalyzer _contentAnalyzer;
        private readonly XmlILNamespaceAnalyzer _nmspAnalyzer;
        private readonly NodeCounter _nodeCounter = new NodeCounter();
        private readonly SubstitutionList _subs = new SubstitutionList();

        private static QilPatterns CreatePatternsNoOpt()
        {
            // Only enable Required and OptimizedConstruction pattern groups
            // Only enable Required patterns
            var patterns = new QilPatterns((int)XmlILOptimization.Last_, allSet: false);

            patterns.Add((int)XmlILOptimization.FoldNone);
            patterns.Add((int)XmlILOptimization.EliminatePositionOf);
            patterns.Add((int)XmlILOptimization.EliminateTypeAssert);
            patterns.Add((int)XmlILOptimization.EliminateIsType);
            patterns.Add((int)XmlILOptimization.EliminateIsEmpty);
            patterns.Add((int)XmlILOptimization.EliminateAverage);
            patterns.Add((int)XmlILOptimization.EliminateSum);
            patterns.Add((int)XmlILOptimization.EliminateMinimum);
            patterns.Add((int)XmlILOptimization.EliminateMaximum);
            patterns.Add((int)XmlILOptimization.EliminateSort);
            patterns.Add((int)XmlILOptimization.EliminateStrConcatSingle);

            patterns.Add((int)XmlILOptimization.NormalizeUnion);
            patterns.Add((int)XmlILOptimization.NormalizeIntersect);
            patterns.Add((int)XmlILOptimization.NormalizeDifference);

            patterns.Add((int)XmlILOptimization.AnnotatePositionalIterator);
            patterns.Add((int)XmlILOptimization.AnnotateTrackCallers);
            patterns.Add((int)XmlILOptimization.AnnotateDod);
            patterns.Add((int)XmlILOptimization.AnnotateConstruction);

            // Enable indexes in debug mode
            patterns.Add((int)XmlILOptimization.AnnotateIndex1);
            patterns.Add((int)XmlILOptimization.AnnotateIndex2);
            patterns.Add((int)XmlILOptimization.AnnotateBarrier);
            patterns.Add((int)XmlILOptimization.AnnotateFilter);

            return patterns;
        }

        public XmlILOptimizerVisitor(QilExpression qil, bool optimize) : base(optimize ? s_patternsOpt : s_patternsNoOpt, qil.Factory)
        {
            _qil = qil;
            _elemAnalyzer = new XmlILElementAnalyzer(qil.Factory);
            _contentAnalyzer = new XmlILStateAnalyzer(qil.Factory);
            _nmspAnalyzer = new XmlILNamespaceAnalyzer();
        }

        /// <summary>
        /// Perform normalization and annotation.
        /// </summary>
        public QilExpression Optimize()
        {
            QilExpression qil = (QilExpression)Visit(_qil);

            // Perform tail-call analysis on all functions within the Qil expression
            if (this[XmlILOptimization.TailCall])
                TailCallAnalyzer.Analyze(qil);

            return qil;
        }

        /// <summary>
        /// Override the Visit method in order to scan for redundant namespaces and compute side-effect bit.
        /// </summary>
        protected override QilNode Visit(QilNode nd)
        {
            if (nd != null)
            {
                if (this[XmlILOptimization.EliminateNamespaceDecl])
                {
                    // Eliminate redundant namespaces in the tree.  Don't perform the scan on an ElementCtor which
                    // has already been marked as having a redundant namespace.
                    switch (nd.NodeType)
                    {
                        case QilNodeType.QilExpression:
                            // Perform namespace analysis on root expression (xmlns="" is in-scope for this expression)
                            _nmspAnalyzer.Analyze(((QilExpression)nd).Root, true);
                            break;

                        case QilNodeType.ElementCtor:
                            if (!XmlILConstructInfo.Read(nd).IsNamespaceInScope)
                                _nmspAnalyzer.Analyze(nd, false);
                            break;

                        case QilNodeType.DocumentCtor:
                            _nmspAnalyzer.Analyze(nd, true);
                            break;
                    }
                }
            }

            // Continue visitation
            return base.Visit(nd!);
        }

        /// <summary>
        /// Override the VisitReference method in order to possibly substitute.
        /// </summary>
        protected override QilNode VisitReference(QilNode oldNode)
        {
            QilNode? newNode = _subs.FindReplacement(oldNode) ?? oldNode;

            // Fold reference to constant value
            // This is done here because "p" currently cannot match references
            if (this[XmlILOptimization.EliminateLiteralVariables] && newNode != null)
            {
                if (newNode.NodeType == QilNodeType.Let || newNode.NodeType == QilNodeType.For)
                {
                    QilNode binding = ((QilIterator)oldNode).Binding!;

                    if (IsLiteral(binding))
                        return Replace(XmlILOptimization.EliminateLiteralVariables, newNode, binding.ShallowClone(f));
                }
            }
            if (this[XmlILOptimization.EliminateUnusedGlobals])
            {
                if (IsGlobalValue(newNode!))
                    OptimizerPatterns.Write(newNode!).AddPattern(OptimizerPatternName.IsReferenced);
            }

            return base.VisitReference(newNode!);
        }

        /// <summary>
        /// Strongly-typed AllowReplace.
        /// </summary>
        private bool AllowReplace(XmlILOptimization pattern, QilNode original)
        {
            return base.AllowReplace((int)pattern, original);
        }

        /// <summary>
        /// Strongly-typed Replace.
        /// </summary>
        private QilNode Replace(XmlILOptimization pattern, QilNode original, QilNode replacement)
        {
            return base.Replace((int)pattern, original, replacement);
        }

        /// <summary>
        /// Called when all replacements have already been made and all annotations are complete.
        /// </summary>
        [return: NotNullIfNotNull(nameof(node))]
        protected override QilNode? NoReplace(QilNode? node)
        {
            // Calculate MaybeSideEffects pattern.  This is done here rather than using P because every node needs
            // to compute it and P has no good way of matching every node type.
            if (node != null)
            {
                switch (node.NodeType)
                {
                    case QilNodeType.Error:
                    case QilNodeType.Warning:
                    case QilNodeType.XsltInvokeLateBound:
                        // Error, Warning, and XsltInvokeLateBound are always assumed to have side-effects
                        OptimizerPatterns.Write(node).AddPattern(OptimizerPatternName.MaybeSideEffects);
                        break;

                    case QilNodeType.XsltInvokeEarlyBound:
                        // XsltInvokeEarlyBound is assumed to have side-effects if it is not a built-in function
                        if (((QilInvokeEarlyBound)node).Name.NamespaceUri.Length != 0)
                            goto case QilNodeType.XsltInvokeLateBound;
                        goto default;

                    case QilNodeType.Invoke:
                        // Invoke is assumed to have side-effects if it invokes a function with its SideEffects flag set
                        if (((QilInvoke)node).Function.MaybeSideEffects)
                            goto case QilNodeType.XsltInvokeLateBound;

                        // Otherwise, check children
                        goto default;

                    default:
                        // If any of the visited node's children have side effects, then mark the node as also having side effects
                        for (int i = 0; i < node.Count; i++)
                        {
                            if (node[i] != null)
                            {
                                if (OptimizerPatterns.Read(node[i]).MatchesPattern(OptimizerPatternName.MaybeSideEffects))
                                    goto case QilNodeType.XsltInvokeLateBound;
                            }
                        }
                        break;
                }
            }

            return node;
        }

        /// <summary>
        /// Override the RecalculateType method so that global variable type is not recalculated.
        /// </summary>
        protected override void RecalculateType(QilNode node, XmlQueryType oldType)
        {
            if (node.NodeType != QilNodeType.Let || !_qil.GlobalVariableList.Contains(node))
                base.RecalculateType(node, oldType);
        }

        // Do not edit this region
        // It is auto-generated
        #region AUTOGENERATED

        #region meta
        protected override QilNode VisitQilExpression(QilExpression local0)
        {
            if (this[XmlILOptimization.EliminateUnusedGlobals])
            {
                // PATTERN: [EliminateUnusedGlobals] $qil:(QilExpression *) => { ... }
                if (AllowReplace(XmlILOptimization.EliminateUnusedGlobals, local0))
                {
                    EliminateUnusedGlobals(local0.GlobalVariableList);
                    EliminateUnusedGlobals(local0.GlobalParameterList);
                    EliminateUnusedGlobals(local0.FunctionList);
                }
            }
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $qil:(QilExpression *) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    foreach (QilFunction ndFunc in local0.FunctionList)
                    {
                        // Functions that construct Xml trees should stream output to writer; otherwise, results should
                        // be cached and returned.
                        if (IsConstructedExpression(ndFunc.Definition))
                        {
                            // Perform state analysis on function's content
                            ndFunc.Definition = _contentAnalyzer.Analyze(ndFunc, ndFunc.Definition)!;
                        }
                    }

                    // Perform state analysis on the root expression
                    local0.Root = _contentAnalyzer.Analyze(null, local0.Root)!;

                    // Make sure that root expression is pushed to writer
                    XmlILConstructInfo.Write(local0.Root).PushToWriterLast = true;
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitOptimizeBarrier(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.AnnotateBarrier])
            {
                // PATTERN: [AnnotateBarrier] $outer:(OptimizeBarrier $expr:*) => (InheritPattern $outer $expr {IsDocOrderDistinct}) ^ (InheritPattern $outer $expr {SameDepth}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateBarrier, local0))
                {
                    OptimizerPatterns.Inherit((QilNode)(local1), (QilNode)(local0), OptimizerPatternName.IsDocOrderDistinct); OptimizerPatterns.Inherit((QilNode)(local1), (QilNode)(local0), OptimizerPatternName.SameDepth);
                }
            }
            return NoReplace(local0);
        }

        #endregion // meta

        #region specials
        protected override QilNode VisitDataSource(QilDataSource local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (DataSource $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (DataSource * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitNop(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.EliminateNop])
            {
                // PATTERN: [EliminateNop] (Nop $x:*) => $x
                if (AllowReplace(XmlILOptimization.EliminateNop, local0))
                {
                    return Replace(XmlILOptimization.EliminateNop, local0, local1);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitError(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Error $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitWarning(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Warning $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // specials

        #region variables
        protected override QilNode VisitLet(QilIterator local0)
        {
            QilNode local1 = local0[0];
            if (((((local0).XmlType)!.IsSingleton) && (!(IsGlobalVariable(local0)))) && (this[XmlILOptimization.NormalizeSingletonLet]))
            {
                // PATTERN: [NormalizeSingletonLet] $iter:(Let $bind:*) ^ (Single? (TypeOf $iter)) ^ ~((GlobalVariable? $iter)) => { ... }
                if (AllowReplace(XmlILOptimization.NormalizeSingletonLet, local0))
                {
                    local0.NodeType = QilNodeType.For;
                    VisitFor(local0);
                }
            }
            if (this[XmlILOptimization.AnnotateLet])
            {
                // PATTERN: [AnnotateLet] $outer:(Let $bind:*) => (InheritPattern $outer $bind {Step}) ^ (InheritPattern $outer $bind {IsDocOrderDistinct}) ^ (InheritPattern $outer $bind {SameDepth}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateLet, local0))
                {
                    OptimizerPatterns.Inherit((QilNode)(local1), (QilNode)(local0), OptimizerPatternName.Step); OptimizerPatterns.Inherit((QilNode)(local1), (QilNode)(local0), OptimizerPatternName.IsDocOrderDistinct); OptimizerPatterns.Inherit((QilNode)(local1), (QilNode)(local0), OptimizerPatternName.SameDepth);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitPositionOf(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.EliminatePositionOf])
            {
                if (!((local1).NodeType == QilNodeType.For))
                {
                    // PATTERN: [EliminatePositionOf] (PositionOf $x:* ^ ~((NodeType? $x {For}))) => (LiteralInt32 1)
                    if (AllowReplace(XmlILOptimization.EliminatePositionOf, local0))
                    {
                        return Replace(XmlILOptimization.EliminatePositionOf, local0, VisitLiteralInt32(f.LiteralInt32(1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminatePositionOf])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local2 = local1[0];
                    if (((local2).XmlType)!.IsSingleton)
                    {
                        // PATTERN: [EliminatePositionOf] (PositionOf (For $x:* ^ (Single? (TypeOf $x)))) => (LiteralInt32 1)
                        if (AllowReplace(XmlILOptimization.EliminatePositionOf, local0))
                        {
                            return Replace(XmlILOptimization.EliminatePositionOf, local0, VisitLiteralInt32(f.LiteralInt32(1)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotatePositionalIterator])
            {
                // PATTERN: [AnnotatePositionalIterator] (PositionOf $iter:*) => (AddPattern $iter {IsPositional}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotatePositionalIterator, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local1)).AddPattern(OptimizerPatternName.IsPositional);
                }
            }
            return NoReplace(local0);
        }

        #endregion // variables

        #region literals
        #endregion // literals

        #region boolean operators
        protected override QilNode VisitAnd(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (And $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (And * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateAnd])
            {
                if (local1.NodeType == QilNodeType.True)
                {
                    // PATTERN: [EliminateAnd] (And (True) $x:*) => $x
                    if (AllowReplace(XmlILOptimization.EliminateAnd, local0))
                    {
                        return Replace(XmlILOptimization.EliminateAnd, local0, local2);
                    }
                }
            }
            if (this[XmlILOptimization.EliminateAnd])
            {
                if (local1.NodeType == QilNodeType.False)
                {
                    // PATTERN: [EliminateAnd] (And $x:(False) *) => $x
                    if (AllowReplace(XmlILOptimization.EliminateAnd, local0))
                    {
                        return Replace(XmlILOptimization.EliminateAnd, local0, local1);
                    }
                }
            }
            if (this[XmlILOptimization.EliminateAnd])
            {
                if (local2.NodeType == QilNodeType.True)
                {
                    // PATTERN: [EliminateAnd] (And $x:* (True)) => $x
                    if (AllowReplace(XmlILOptimization.EliminateAnd, local0))
                    {
                        return Replace(XmlILOptimization.EliminateAnd, local0, local1);
                    }
                }
            }
            if (this[XmlILOptimization.EliminateAnd])
            {
                if (local2.NodeType == QilNodeType.False)
                {
                    // PATTERN: [EliminateAnd] (And * $x:(False)) => $x
                    if (AllowReplace(XmlILOptimization.EliminateAnd, local0))
                    {
                        return Replace(XmlILOptimization.EliminateAnd, local0, local2);
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitOr(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Or $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Or * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateOr])
            {
                if (local1.NodeType == QilNodeType.True)
                {
                    // PATTERN: [EliminateOr] (Or $x:(True) *) => $x
                    if (AllowReplace(XmlILOptimization.EliminateOr, local0))
                    {
                        return Replace(XmlILOptimization.EliminateOr, local0, local1);
                    }
                }
            }
            if (this[XmlILOptimization.EliminateOr])
            {
                if (local1.NodeType == QilNodeType.False)
                {
                    // PATTERN: [EliminateOr] (Or (False) $x:*) => $x
                    if (AllowReplace(XmlILOptimization.EliminateOr, local0))
                    {
                        return Replace(XmlILOptimization.EliminateOr, local0, local2);
                    }
                }
            }
            if (this[XmlILOptimization.EliminateOr])
            {
                if (local2.NodeType == QilNodeType.True)
                {
                    // PATTERN: [EliminateOr] (Or * $x:(True)) => $x
                    if (AllowReplace(XmlILOptimization.EliminateOr, local0))
                    {
                        return Replace(XmlILOptimization.EliminateOr, local0, local2);
                    }
                }
            }
            if (this[XmlILOptimization.EliminateOr])
            {
                if (local2.NodeType == QilNodeType.False)
                {
                    // PATTERN: [EliminateOr] (Or $x:* (False)) => $x
                    if (AllowReplace(XmlILOptimization.EliminateOr, local0))
                    {
                        return Replace(XmlILOptimization.EliminateOr, local0, local1);
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitNot(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Not $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateNot])
            {
                if (local1.NodeType == QilNodeType.True)
                {
                    // PATTERN: [EliminateNot] (Not (True)) => (False)
                    if (AllowReplace(XmlILOptimization.EliminateNot, local0))
                    {
                        return Replace(XmlILOptimization.EliminateNot, local0, VisitFalse(f.False()));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateNot])
            {
                if (local1.NodeType == QilNodeType.False)
                {
                    // PATTERN: [EliminateNot] (Not (False)) => (True)
                    if (AllowReplace(XmlILOptimization.EliminateNot, local0))
                    {
                        return Replace(XmlILOptimization.EliminateNot, local0, VisitTrue(f.True()));
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // boolean operators

        #region choice
        protected override QilNode VisitConditional(QilTernary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            QilNode local3 = local0[2];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Conditional $x:* ^ (None? (TypeOf $x)) * *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateConditional])
            {
                if (local1.NodeType == QilNodeType.True)
                {
                    // PATTERN: [EliminateConditional] (Conditional (True) $x:* *) => $x
                    if (AllowReplace(XmlILOptimization.EliminateConditional, local0))
                    {
                        return Replace(XmlILOptimization.EliminateConditional, local0, local2);
                    }
                }
            }
            if (this[XmlILOptimization.EliminateConditional])
            {
                if (local1.NodeType == QilNodeType.False)
                {
                    // PATTERN: [EliminateConditional] (Conditional (False) * $x:*) => $x
                    if (AllowReplace(XmlILOptimization.EliminateConditional, local0))
                    {
                        return Replace(XmlILOptimization.EliminateConditional, local0, local3);
                    }
                }
            }
            if (this[XmlILOptimization.EliminateConditional])
            {
                if (local2.NodeType == QilNodeType.True)
                {
                    if (local3.NodeType == QilNodeType.False)
                    {
                        // PATTERN: [EliminateConditional] (Conditional $x:* (True) (False)) => $x
                        if (AllowReplace(XmlILOptimization.EliminateConditional, local0))
                        {
                            return Replace(XmlILOptimization.EliminateConditional, local0, local1);
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateConditional])
            {
                if (local2.NodeType == QilNodeType.False)
                {
                    if (local3.NodeType == QilNodeType.True)
                    {
                        // PATTERN: [EliminateConditional] (Conditional $x:* (False) (True)) => (Not $x)
                        if (AllowReplace(XmlILOptimization.EliminateConditional, local0))
                        {
                            return Replace(XmlILOptimization.EliminateConditional, local0, VisitNot(f.Not(local1)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.FoldConditionalNot])
            {
                if (local1.NodeType == QilNodeType.Not)
                {
                    QilNode local4 = local1[0];
                    // PATTERN: [FoldConditionalNot] (Conditional (Not $x:*) $t:* $f:*) => (Conditional $x $f $t)
                    if (AllowReplace(XmlILOptimization.FoldConditionalNot, local0))
                    {
                        return Replace(XmlILOptimization.FoldConditionalNot, local0, VisitConditional(f.Conditional(local4, local3, local2)));
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeConditionalText])
            {
                if (local2.NodeType == QilNodeType.TextCtor)
                {
                    QilNode local4 = local2[0];
                    if (local3.NodeType == QilNodeType.TextCtor)
                    {
                        QilNode local5 = local3[0];
                        // PATTERN: [NormalizeConditionalText] (Conditional $cond:* $left:(TextCtor $leftText:*) $right:(TextCtor $rightText:*)) => (TextCtor (Conditional $cond $leftText $rightText))
                        if (AllowReplace(XmlILOptimization.NormalizeConditionalText, local0))
                        {
                            return Replace(XmlILOptimization.NormalizeConditionalText, local0, VisitTextCtor(f.TextCtor(VisitConditional(f.Conditional(local1, local4, local5)))));
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitChoice(QilChoice local0)
        {
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $ctor:(Choice * *) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    _contentAnalyzer.Analyze(local0, null);
                }
            }
            return NoReplace(local0);
        }

        #endregion // choice

        #region collection operators
        protected override QilNode VisitLength(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Length $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateLength])
            {
                if (local1.NodeType == QilNodeType.Sequence)
                {
                    if ((local1).Count == (0))
                    {
                        // PATTERN: [EliminateLength] (Length $x:(Sequence) ^ (Count? $x 0)) => (LiteralInt32 0)
                        if (AllowReplace(XmlILOptimization.EliminateLength, local0))
                        {
                            return Replace(XmlILOptimization.EliminateLength, local0, VisitLiteralInt32(f.LiteralInt32(0)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateLength])
            {
                if ((((local1).XmlType)!.IsSingleton) && (!OptimizerPatterns.Read(local1).MatchesPattern(OptimizerPatternName.MaybeSideEffects)))
                {
                    // PATTERN: [EliminateLength] (Length $x:* ^ (Single? (TypeOf $x)) ^ (NoSideEffects? $x)) => (LiteralInt32 1)
                    if (AllowReplace(XmlILOptimization.EliminateLength, local0))
                    {
                        return Replace(XmlILOptimization.EliminateLength, local0, VisitLiteralInt32(f.LiteralInt32(1)));
                    }
                }
            }
            if (this[XmlILOptimization.IntroducePrecedingDod])
            {
                if ((!(IsDocOrderDistinct(local1))) && ((IsStepPattern(local1, QilNodeType.XPathPreceding)) || (IsStepPattern(local1, QilNodeType.PrecedingSibling))))
                {
                    // PATTERN: [IntroducePrecedingDod] (Length $expr:* ^ ~((DocOrderDistinct? $expr)) ^ (StepPattern? $expr {XPathPreceding}) | (StepPattern? $expr {PrecedingSibling})) => (Length (DocOrderDistinct $expr))
                    if (AllowReplace(XmlILOptimization.IntroducePrecedingDod, local0))
                    {
                        return Replace(XmlILOptimization.IntroducePrecedingDod, local0, VisitLength(f.Length(VisitDocOrderDistinct(f.DocOrderDistinct(local1)))));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitSequence(QilList local0)
        {
            if (((local0).Count == (1)) && (this[XmlILOptimization.EliminateSequence]))
            {
                // PATTERN: [EliminateSequence] $x:(Sequence) ^ (Count? $x 1) => (First $x)
                if (AllowReplace(XmlILOptimization.EliminateSequence, local0))
                {
                    return Replace(XmlILOptimization.EliminateSequence, local0, (QilNode)(local0)[0]);
                }
            }
            if ((HasNestedSequence(local0)) && (this[XmlILOptimization.NormalizeNestedSequences]))
            {
                // PATTERN: [NormalizeNestedSequences] $seq:(Sequence) ^ (NestedSequences? $seq) => $result:(Sequence) ^ { ... } ^ $result
                if (AllowReplace(XmlILOptimization.NormalizeNestedSequences, local0))
                {
                    QilNode local1 = VisitSequence(f.Sequence());
                    foreach (QilNode nd in local0)
                    {
                        if (nd.NodeType == QilNodeType.Sequence)
                            local1.Add((IList<QilNode>)nd);
                        else
                            local1.Add(nd);
                    }

                    // Match patterns on new sequence
                    local1 = VisitSequence((QilList)local1);
                    return Replace(XmlILOptimization.NormalizeNestedSequences, local0, local1);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitUnion(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Union $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Union * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateUnion])
            {
                if (local2 == local1)
                {
                    // PATTERN: [EliminateUnion] (Union $x:* $x) => (DocOrderDistinct $x)
                    if (AllowReplace(XmlILOptimization.EliminateUnion, local0))
                    {
                        return Replace(XmlILOptimization.EliminateUnion, local0, VisitDocOrderDistinct(f.DocOrderDistinct(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateUnion])
            {
                if (local1.NodeType == QilNodeType.Sequence)
                {
                    if ((local1).Count == (0))
                    {
                        // PATTERN: [EliminateUnion] (Union $x:(Sequence) ^ (Count? $x 0) $y:*) => (DocOrderDistinct $y)
                        if (AllowReplace(XmlILOptimization.EliminateUnion, local0))
                        {
                            return Replace(XmlILOptimization.EliminateUnion, local0, VisitDocOrderDistinct(f.DocOrderDistinct(local2)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateUnion])
            {
                if (local2.NodeType == QilNodeType.Sequence)
                {
                    if ((local2).Count == (0))
                    {
                        // PATTERN: [EliminateUnion] (Union $x:* $y:(Sequence) ^ (Count? $y 0)) => (DocOrderDistinct $x)
                        if (AllowReplace(XmlILOptimization.EliminateUnion, local0))
                        {
                            return Replace(XmlILOptimization.EliminateUnion, local0, VisitDocOrderDistinct(f.DocOrderDistinct(local1)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateUnion])
            {
                if (local1.NodeType == QilNodeType.XmlContext)
                {
                    if (local2.NodeType == QilNodeType.XmlContext)
                    {
                        // PATTERN: [EliminateUnion] (Union $x:XmlContext XmlContext) => $x
                        if (AllowReplace(XmlILOptimization.EliminateUnion, local0))
                        {
                            return Replace(XmlILOptimization.EliminateUnion, local0, local1);
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeUnion])
            {
                if ((!(IsDocOrderDistinct(local1))) || (!(IsDocOrderDistinct(local2))))
                {
                    // PATTERN: [NormalizeUnion] (Union $left:* $right:* ^ ~((DocOrderDistinct? $left)) | ~((DocOrderDistinct? $right))) => (Union (DocOrderDistinct $left) (DocOrderDistinct $right))
                    if (AllowReplace(XmlILOptimization.NormalizeUnion, local0))
                    {
                        return Replace(XmlILOptimization.NormalizeUnion, local0, VisitUnion(f.Union(VisitDocOrderDistinct(f.DocOrderDistinct(local1)), VisitDocOrderDistinct(f.DocOrderDistinct(local2)))));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateUnion])
            {
                // PATTERN: [AnnotateUnion] $outer:(Union * *) => (AddPattern $outer {IsDocOrderDistinct}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateUnion, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
                }
            }
            if (this[XmlILOptimization.AnnotateUnionContent])
            {
                if ((IsStepPattern(local1, QilNodeType.Content)) || (IsStepPattern(local1, QilNodeType.Union)))
                {
                    if (((IsStepPattern(local2, QilNodeType.Content)) || (IsStepPattern(local2, QilNodeType.Union))) && ((OptimizerPatterns.Read((QilNode)(local1)).GetArgument(OptimizerPatternArgument.StepInput)) == (OptimizerPatterns.Read((QilNode)(local2)).GetArgument(OptimizerPatternArgument.StepInput))))
                    {
                        // PATTERN: [AnnotateUnionContent] $outer:(Union $left:* ^ (StepPattern? $left {Content}) | (StepPattern? $left {Union}) $right:* ^ (StepPattern? $right {Content}) | (StepPattern? $right {Union}) ^ (Equal? (Argument $left {StepInput}) (Argument $right {StepInput}))) => (AddStepPattern $outer (Argument $left {StepInput})) ^ (AddPattern $outer {SameDepth}) ^ { }
                        if (AllowReplace(XmlILOptimization.AnnotateUnionContent, local0))
                        {
                            AddStepPattern((QilNode)(local0), (QilNode)(OptimizerPatterns.Read((QilNode)(local1)).GetArgument(OptimizerPatternArgument.StepInput))); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.SameDepth);
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitIntersection(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Intersection $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Intersection * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIntersection])
            {
                if (local2 == local1)
                {
                    // PATTERN: [EliminateIntersection] (Intersection $x:* $x) => (DocOrderDistinct $x)
                    if (AllowReplace(XmlILOptimization.EliminateIntersection, local0))
                    {
                        return Replace(XmlILOptimization.EliminateIntersection, local0, VisitDocOrderDistinct(f.DocOrderDistinct(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIntersection])
            {
                if (local1.NodeType == QilNodeType.Sequence)
                {
                    if ((local1).Count == (0))
                    {
                        // PATTERN: [EliminateIntersection] (Intersection $x:(Sequence) ^ (Count? $x 0) *) => $x
                        if (AllowReplace(XmlILOptimization.EliminateIntersection, local0))
                        {
                            return Replace(XmlILOptimization.EliminateIntersection, local0, local1);
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIntersection])
            {
                if (local2.NodeType == QilNodeType.Sequence)
                {
                    if ((local2).Count == (0))
                    {
                        // PATTERN: [EliminateIntersection] (Intersection * $y:(Sequence) ^ (Count? $y 0)) => $y
                        if (AllowReplace(XmlILOptimization.EliminateIntersection, local0))
                        {
                            return Replace(XmlILOptimization.EliminateIntersection, local0, local2);
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIntersection])
            {
                if (local1.NodeType == QilNodeType.XmlContext)
                {
                    if (local2.NodeType == QilNodeType.XmlContext)
                    {
                        // PATTERN: [EliminateIntersection] (Intersection $x:XmlContext XmlContext) => $x
                        if (AllowReplace(XmlILOptimization.EliminateIntersection, local0))
                        {
                            return Replace(XmlILOptimization.EliminateIntersection, local0, local1);
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeIntersect])
            {
                if ((!(IsDocOrderDistinct(local1))) || (!(IsDocOrderDistinct(local2))))
                {
                    // PATTERN: [NormalizeIntersect] (Intersection $left:* $right:* ^ ~((DocOrderDistinct? $left)) | ~((DocOrderDistinct? $right))) => (Intersection (DocOrderDistinct $left) (DocOrderDistinct $right))
                    if (AllowReplace(XmlILOptimization.NormalizeIntersect, local0))
                    {
                        return Replace(XmlILOptimization.NormalizeIntersect, local0, VisitIntersection(f.Intersection(VisitDocOrderDistinct(f.DocOrderDistinct(local1)), VisitDocOrderDistinct(f.DocOrderDistinct(local2)))));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateIntersect])
            {
                // PATTERN: [AnnotateIntersect] $outer:(Intersection * *) => (AddPattern $outer {IsDocOrderDistinct}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateIntersect, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitDifference(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Difference $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Difference * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateDifference])
            {
                if (local1.NodeType == QilNodeType.Sequence)
                {
                    if ((local1).Count == (0))
                    {
                        // PATTERN: [EliminateDifference] (Difference $x:(Sequence) ^ (Count? $x 0) $y:*) => $x
                        if (AllowReplace(XmlILOptimization.EliminateDifference, local0))
                        {
                            return Replace(XmlILOptimization.EliminateDifference, local0, local1);
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateDifference])
            {
                if (local2.NodeType == QilNodeType.Sequence)
                {
                    if ((local2).Count == (0))
                    {
                        // PATTERN: [EliminateDifference] (Difference $x:* $y:(Sequence) ^ (Count? $y 0)) => (DocOrderDistinct $x)
                        if (AllowReplace(XmlILOptimization.EliminateDifference, local0))
                        {
                            return Replace(XmlILOptimization.EliminateDifference, local0, VisitDocOrderDistinct(f.DocOrderDistinct(local1)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateDifference])
            {
                if (local2 == local1)
                {
                    // PATTERN: [EliminateDifference] (Difference $x:* $x) => (Sequence)
                    if (AllowReplace(XmlILOptimization.EliminateDifference, local0))
                    {
                        return Replace(XmlILOptimization.EliminateDifference, local0, VisitSequence(f.Sequence()));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateDifference])
            {
                if (local1.NodeType == QilNodeType.XmlContext)
                {
                    if (local2.NodeType == QilNodeType.XmlContext)
                    {
                        // PATTERN: [EliminateDifference] (Difference XmlContext XmlContext) => (Sequence)
                        if (AllowReplace(XmlILOptimization.EliminateDifference, local0))
                        {
                            return Replace(XmlILOptimization.EliminateDifference, local0, VisitSequence(f.Sequence()));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeDifference])
            {
                if ((!(IsDocOrderDistinct(local1))) || (!(IsDocOrderDistinct(local2))))
                {
                    // PATTERN: [NormalizeDifference] (Difference $left:* $right:* ^ ~((DocOrderDistinct? $left)) | ~((DocOrderDistinct? $right))) => (Difference (DocOrderDistinct $left) (DocOrderDistinct $right))
                    if (AllowReplace(XmlILOptimization.NormalizeDifference, local0))
                    {
                        return Replace(XmlILOptimization.NormalizeDifference, local0, VisitDifference(f.Difference(VisitDocOrderDistinct(f.DocOrderDistinct(local1)), VisitDocOrderDistinct(f.DocOrderDistinct(local2)))));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateDifference])
            {
                // PATTERN: [AnnotateDifference] $outer:(Difference * *) => (AddPattern $outer {IsDocOrderDistinct}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateDifference, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitAverage(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Average $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateAverage])
            {
                if (((local1).XmlType)!.Cardinality == XmlQueryCardinality.Zero)
                {
                    // PATTERN: [EliminateAverage] (Average $x:* ^ (Empty? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.EliminateAverage, local0))
                    {
                        return Replace(XmlILOptimization.EliminateAverage, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitSum(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Sum $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateSum])
            {
                if (((local1).XmlType)!.Cardinality == XmlQueryCardinality.Zero)
                {
                    // PATTERN: [EliminateSum] (Sum $x:* ^ (Empty? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.EliminateSum, local0))
                    {
                        return Replace(XmlILOptimization.EliminateSum, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitMinimum(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Minimum $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateMinimum])
            {
                if (((local1).XmlType)!.Cardinality == XmlQueryCardinality.Zero)
                {
                    // PATTERN: [EliminateMinimum] (Minimum $x:* ^ (Empty? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.EliminateMinimum, local0))
                    {
                        return Replace(XmlILOptimization.EliminateMinimum, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitMaximum(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Maximum $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateMaximum])
            {
                if (((local1).XmlType)!.Cardinality == XmlQueryCardinality.Zero)
                {
                    // PATTERN: [EliminateMaximum] (Maximum $x:* ^ (Empty? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.EliminateMaximum, local0))
                    {
                        return Replace(XmlILOptimization.EliminateMaximum, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // collection operators

        #region arithmetic operators
        protected override QilNode VisitNegate(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Negate $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateNegate])
            {
                if (local1.NodeType == QilNodeType.LiteralDecimal)
                {
                    decimal local2 = (decimal)((QilLiteral)local1).Value!;
                    // PATTERN: [EliminateNegate] (Negate (LiteralDecimal $x:*)) => (LiteralDecimal { -{$x} })
                    if (AllowReplace(XmlILOptimization.EliminateNegate, local0))
                    {
                        return Replace(XmlILOptimization.EliminateNegate, local0, VisitLiteralDecimal(f.LiteralDecimal(-local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateNegate])
            {
                if (local1.NodeType == QilNodeType.LiteralDouble)
                {
                    double local2 = (double)((QilLiteral)local1).Value!;
                    // PATTERN: [EliminateNegate] (Negate (LiteralDouble $x:*)) => (LiteralDouble { -{$x} })
                    if (AllowReplace(XmlILOptimization.EliminateNegate, local0))
                    {
                        return Replace(XmlILOptimization.EliminateNegate, local0, VisitLiteralDouble(f.LiteralDouble(-local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateNegate])
            {
                if (local1.NodeType == QilNodeType.LiteralInt32)
                {
                    int local2 = (int)((QilLiteral)local1).Value!;
                    // PATTERN: [EliminateNegate] (Negate (LiteralInt32 $x:*)) => (LiteralInt32 { -{$x} })
                    if (AllowReplace(XmlILOptimization.EliminateNegate, local0))
                    {
                        return Replace(XmlILOptimization.EliminateNegate, local0, VisitLiteralInt32(f.LiteralInt32(-local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateNegate])
            {
                if (local1.NodeType == QilNodeType.LiteralInt64)
                {
                    long local2 = (long)((QilLiteral)local1).Value!;
                    // PATTERN: [EliminateNegate] (Negate (LiteralInt64 $x:*)) => (LiteralInt64 { -{$x} })
                    if (AllowReplace(XmlILOptimization.EliminateNegate, local0))
                    {
                        return Replace(XmlILOptimization.EliminateNegate, local0, VisitLiteralInt64(f.LiteralInt64(-local2)));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitAdd(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Add $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Add * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateAdd])
            {
                if (IsLiteral((local1)))
                {
                    if ((IsLiteral((local2))) && (CanFoldArithmetic(QilNodeType.Add, (QilLiteral)local1, (QilLiteral)local2)))
                    {
                        // PATTERN: [EliminateAdd] (Add $x:* ^ (Literal? $x) $y:* ^ (Literal? $y) ^ (CanFoldAdd? $x $y)) => (Add! $x $y)
                        if (AllowReplace(XmlILOptimization.EliminateAdd, local0))
                        {
                            return Replace(XmlILOptimization.EliminateAdd, local0, FoldArithmetic(QilNodeType.Add, (QilLiteral)local1, (QilLiteral)local2));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeAddLiteral])
            {
                if (IsLiteral((local1)))
                {
                    if (!(IsLiteral((local2))))
                    {
                        // PATTERN: [NormalizeAddLiteral] (Add $left:* ^ (Literal? $left) $right:* ^ ~((Literal? $right))) => (Add $right $left)
                        if (AllowReplace(XmlILOptimization.NormalizeAddLiteral, local0))
                        {
                            return Replace(XmlILOptimization.NormalizeAddLiteral, local0, VisitAdd(f.Add(local2, local1)));
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitSubtract(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Subtract $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Subtract * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateSubtract])
            {
                if (IsLiteral((local1)))
                {
                    if ((IsLiteral((local2))) && (CanFoldArithmetic(QilNodeType.Subtract, (QilLiteral)local1, (QilLiteral)local2)))
                    {
                        // PATTERN: [EliminateSubtract] (Subtract $x:* ^ (Literal? $x) $y:* ^ (Literal? $y) ^ (CanFoldSub? $x $y)) => (Sub! $x $y)
                        if (AllowReplace(XmlILOptimization.EliminateSubtract, local0))
                        {
                            return Replace(XmlILOptimization.EliminateSubtract, local0, FoldArithmetic(QilNodeType.Subtract, (QilLiteral)local1, (QilLiteral)local2));
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitMultiply(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Multiply $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Multiply * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateMultiply])
            {
                if (IsLiteral((local1)))
                {
                    if ((IsLiteral((local2))) && (CanFoldArithmetic(QilNodeType.Multiply, (QilLiteral)local1, (QilLiteral)local2)))
                    {
                        // PATTERN: [EliminateMultiply] (Multiply $x:* ^ (Literal? $x) $y:* ^ (Literal? $y) ^ (CanFoldMul? $x $y)) => (Mul! $x $y)
                        if (AllowReplace(XmlILOptimization.EliminateMultiply, local0))
                        {
                            return Replace(XmlILOptimization.EliminateMultiply, local0, FoldArithmetic(QilNodeType.Multiply, (QilLiteral)local1, (QilLiteral)local2));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeMultiplyLiteral])
            {
                if (IsLiteral((local1)))
                {
                    if (!(IsLiteral((local2))))
                    {
                        // PATTERN: [NormalizeMultiplyLiteral] (Multiply $left:* ^ (Literal? $left) $right:* ^ ~((Literal? $right))) => (Multiply $right $left)
                        if (AllowReplace(XmlILOptimization.NormalizeMultiplyLiteral, local0))
                        {
                            return Replace(XmlILOptimization.NormalizeMultiplyLiteral, local0, VisitMultiply(f.Multiply(local2, local1)));
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitDivide(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Divide $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Divide * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateDivide])
            {
                if (IsLiteral((local1)))
                {
                    if ((IsLiteral((local2))) && (CanFoldArithmetic(QilNodeType.Divide, (QilLiteral)local1, (QilLiteral)local2)))
                    {
                        // PATTERN: [EliminateDivide] (Divide $x:* ^ (Literal? $x) $y:* ^ (Literal? $y) ^ (CanFoldDiv? $x $y)) => (Div! $x $y)
                        if (AllowReplace(XmlILOptimization.EliminateDivide, local0))
                        {
                            return Replace(XmlILOptimization.EliminateDivide, local0, FoldArithmetic(QilNodeType.Divide, (QilLiteral)local1, (QilLiteral)local2));
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitModulo(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Modulo $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Modulo * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateModulo])
            {
                if (IsLiteral((local1)))
                {
                    if ((IsLiteral((local2))) && (CanFoldArithmetic(QilNodeType.Modulo, (QilLiteral)local1, (QilLiteral)local2)))
                    {
                        // PATTERN: [EliminateModulo] (Modulo $x:* ^ (Literal? $x) $y:* ^ (Literal? $y) ^ (CanFoldMod? $x $y)) => (Mod! $x $y)
                        if (AllowReplace(XmlILOptimization.EliminateModulo, local0))
                        {
                            return Replace(XmlILOptimization.EliminateModulo, local0, FoldArithmetic(QilNodeType.Modulo, (QilLiteral)local1, (QilLiteral)local2));
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // arithmetic operators

        #region string operators
        protected override QilNode VisitStrLength(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (StrLength $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateStrLength])
            {
                if (local1.NodeType == QilNodeType.LiteralString)
                {
                    string local2 = (string)((QilLiteral)local1).Value!;
                    // PATTERN: [EliminateStrLength] (StrLength (LiteralString $x:*)) => (LiteralInt32 { {$x}.Length })
                    if (AllowReplace(XmlILOptimization.EliminateStrLength, local0))
                    {
                        return Replace(XmlILOptimization.EliminateStrLength, local0, VisitLiteralInt32(f.LiteralInt32(local2.Length)));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitStrConcat(QilStrConcat local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (StrConcat $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (StrConcat * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if ((((local2).XmlType)!.IsSingleton) && (this[XmlILOptimization.EliminateStrConcatSingle]))
            {
                // PATTERN: [EliminateStrConcatSingle] (StrConcat * $x:*) ^ (Single? (TypeOf $x)) => (Nop $x)
                if (AllowReplace(XmlILOptimization.EliminateStrConcatSingle, local0))
                {
                    return Replace(XmlILOptimization.EliminateStrConcatSingle, local0, VisitNop(f.Nop(local2)));
                }
            }
            if (this[XmlILOptimization.EliminateStrConcat])
            {
                if (local1.NodeType == QilNodeType.LiteralString)
                {
                    string local3 = (string)((QilLiteral)local1).Value!;
                    if (local2.NodeType == QilNodeType.Sequence)
                    {
                        if (AreLiteralArgs(local2))
                        {
                            // PATTERN: [EliminateStrConcat] (StrConcat (LiteralString $delim:*) $values:(Sequence) ^ (LiteralArgs? $values)) => { ... } ^ (LiteralString { concat.GetResult() })
                            if (AllowReplace(XmlILOptimization.EliminateStrConcat, local0))
                            {
                                // Concatenate all constant arguments
                                StringConcat concat = default;
                                concat.Delimiter = local3;

                                foreach (QilLiteral lit in local2)
                                    concat.Concat((string)lit);
                                return Replace(XmlILOptimization.EliminateStrConcat, local0, VisitLiteralString(f.LiteralString(concat.GetResult())));
                            }
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitStrParseQName(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (StrParseQName $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (StrParseQName * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // string operators

        #region value comparison operators
        protected override QilNode VisitNe(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Ne $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Ne * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateNe])
            {
                if (IsLiteral((local1)))
                {
                    if (IsLiteral((local2)))
                    {
                        // PATTERN: [EliminateNe] (Ne $x:* ^ (Literal? $x) $y:* ^ (Literal? $y)) => (Ne! $x $y)
                        if (AllowReplace(XmlILOptimization.EliminateNe, local0))
                        {
                            return Replace(XmlILOptimization.EliminateNe, local0, FoldComparison(QilNodeType.Ne, local1, local2));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeNeLiteral])
            {
                if (IsLiteral((local1)))
                {
                    if (!(IsLiteral((local2))))
                    {
                        // PATTERN: [NormalizeNeLiteral] (Ne $left:* ^ (Literal? $left) $right:* ^ ~((Literal? $right))) => (Ne $right $left)
                        if (AllowReplace(XmlILOptimization.NormalizeNeLiteral, local0))
                        {
                            return Replace(XmlILOptimization.NormalizeNeLiteral, local0, VisitNe(f.Ne(local2, local1)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeXsltConvertNe])
            {
                if (local1.NodeType == QilNodeType.XsltConvert)
                {
                    QilNode local3 = local1[0];
                    QilNode local4 = local1[1];
                    if (local4.NodeType == QilNodeType.LiteralType)
                    {
                        XmlQueryType local5 = (XmlQueryType)((QilLiteral)local4).Value!;
                        if ((IsPrimitiveNumeric((local3).XmlType)) && (IsPrimitiveNumeric(local5)))
                        {
                            if ((IsLiteral((local2))) && (CanFoldXsltConvertNonLossy(local2, (local3).XmlType!)))
                            {
                                // PATTERN: [NormalizeXsltConvertNe] (Ne (XsltConvert $expr:* (LiteralType $typ:*)) ^ (PrimitiveNumeric? (TypeOf $expr)) ^ (PrimitiveNumeric? $typ) $lit:* ^ (Literal? $lit) ^ (CanFoldXsltConvertNonLossy? $lit (TypeOf $expr))) => (Ne $expr (FoldXsltConvert $lit (TypeOf $expr)))
                                if (AllowReplace(XmlILOptimization.NormalizeXsltConvertNe, local0))
                                {
                                    return Replace(XmlILOptimization.NormalizeXsltConvertNe, local0, VisitNe(f.Ne(local3, FoldXsltConvert(local2, (local3).XmlType!))));
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeIdNe])
            {
                if (local1.NodeType == QilNodeType.XsltGenerateId)
                {
                    QilNode local3 = local1[0];
                    if (((local3).XmlType)!.IsSingleton)
                    {
                        if (local2.NodeType == QilNodeType.XsltGenerateId)
                        {
                            QilNode local4 = local2[0];
                            if (((local4).XmlType)!.IsSingleton)
                            {
                                // PATTERN: [NormalizeIdNe] (Ne (XsltGenerateId $arg1:*) ^ (Single? (TypeOf $arg1)) (XsltGenerateId $arg2:*) ^ (Single? (TypeOf $arg2))) => (Not (Is $arg1 $arg2))
                                if (AllowReplace(XmlILOptimization.NormalizeIdNe, local0))
                                {
                                    return Replace(XmlILOptimization.NormalizeIdNe, local0, VisitNot(f.Not(VisitIs(f.Is(local3, local4)))));
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeLengthNe])
            {
                if (local1.NodeType == QilNodeType.Length)
                {
                    QilNode local3 = local1[0];
                    if (local2.NodeType == QilNodeType.LiteralInt32)
                    {
                        int local4 = (int)((QilLiteral)local2).Value!;
                        if (local4 == 0)
                        {
                            // PATTERN: [NormalizeLengthNe] (Ne (Length $expr:*) (LiteralInt32 0)) => (Not (IsEmpty $expr))
                            if (AllowReplace(XmlILOptimization.NormalizeLengthNe, local0))
                            {
                                return Replace(XmlILOptimization.NormalizeLengthNe, local0, VisitNot(f.Not(VisitIsEmpty(f.IsEmpty(local3)))));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateMaxLengthNe])
            {
                if (local1.NodeType == QilNodeType.Length)
                {
                    if (local2.NodeType == QilNodeType.LiteralInt32)
                    {
                        int local4 = (int)((QilLiteral)local2).Value!;
                        // PATTERN: [AnnotateMaxLengthNe] (Ne $len:(Length *) (LiteralInt32 $num:*)) => (AddPattern $len {MaxPosition}) ^ (AddArgument $len {MaxPosition} $num) ^ { }
                        if (AllowReplace(XmlILOptimization.AnnotateMaxLengthNe, local0))
                        {
                            OptimizerPatterns.Write((QilNode)(local1)).AddPattern(OptimizerPatternName.MaxPosition); OptimizerPatterns.Write((QilNode)(local1)).AddArgument(OptimizerPatternArgument.MaxPosition, local4);
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitEq(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Eq $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Eq * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateEq])
            {
                if (IsLiteral((local1)))
                {
                    if (IsLiteral((local2)))
                    {
                        // PATTERN: [EliminateEq] (Eq $x:* ^ (Literal? $x) $y:* ^ (Literal? $y)) => (Eq! $x $y)
                        if (AllowReplace(XmlILOptimization.EliminateEq, local0))
                        {
                            return Replace(XmlILOptimization.EliminateEq, local0, FoldComparison(QilNodeType.Eq, local1, local2));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeEqLiteral])
            {
                if (IsLiteral((local1)))
                {
                    if (!(IsLiteral((local2))))
                    {
                        // PATTERN: [NormalizeEqLiteral] (Eq $left:* ^ (Literal? $left) $right:* ^ ~((Literal? $right))) => (Eq $right $left)
                        if (AllowReplace(XmlILOptimization.NormalizeEqLiteral, local0))
                        {
                            return Replace(XmlILOptimization.NormalizeEqLiteral, local0, VisitEq(f.Eq(local2, local1)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeXsltConvertEq])
            {
                if (local1.NodeType == QilNodeType.XsltConvert)
                {
                    QilNode local3 = local1[0];
                    QilNode local4 = local1[1];
                    if (local4.NodeType == QilNodeType.LiteralType)
                    {
                        XmlQueryType local5 = (XmlQueryType)((QilLiteral)local4).Value!;
                        if ((IsPrimitiveNumeric((local3).XmlType)) && (IsPrimitiveNumeric(local5)))
                        {
                            if ((IsLiteral((local2))) && (CanFoldXsltConvertNonLossy(local2, (local3).XmlType!)))
                            {
                                // PATTERN: [NormalizeXsltConvertEq] (Eq (XsltConvert $expr:* (LiteralType $typ:*)) ^ (PrimitiveNumeric? (TypeOf $expr)) ^ (PrimitiveNumeric? $typ) $lit:* ^ (Literal? $lit) ^ (CanFoldXsltConvertNonLossy? $lit (TypeOf $expr))) => (Eq $expr (FoldXsltConvert $lit (TypeOf $expr)))
                                if (AllowReplace(XmlILOptimization.NormalizeXsltConvertEq, local0))
                                {
                                    return Replace(XmlILOptimization.NormalizeXsltConvertEq, local0, VisitEq(f.Eq(local3, FoldXsltConvert(local2, (local3).XmlType!))));
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeAddEq])
            {
                if (local1.NodeType == QilNodeType.Add)
                {
                    QilNode local3 = local1[0];
                    QilNode local4 = local1[1];
                    if (IsLiteral((local4)))
                    {
                        if ((IsLiteral((local2))) && (CanFoldArithmetic(QilNodeType.Subtract, (QilLiteral)local2, (QilLiteral)local4)))
                        {
                            // PATTERN: [NormalizeAddEq] (Eq (Add $exprAdd:* $litAdd:*) ^ (Literal? $litAdd) $litEq:* ^ (Literal? $litEq) ^ (CanFoldSub? $litEq $litAdd)) => (Eq $exprAdd (Sub! $litEq $litAdd))
                            if (AllowReplace(XmlILOptimization.NormalizeAddEq, local0))
                            {
                                return Replace(XmlILOptimization.NormalizeAddEq, local0, VisitEq(f.Eq(local3, FoldArithmetic(QilNodeType.Subtract, (QilLiteral)local2, (QilLiteral)local4))));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeIdEq])
            {
                if (local1.NodeType == QilNodeType.XsltGenerateId)
                {
                    QilNode local3 = local1[0];
                    if (((local3).XmlType)!.IsSingleton)
                    {
                        if (local2.NodeType == QilNodeType.XsltGenerateId)
                        {
                            QilNode local4 = local2[0];
                            if (((local4).XmlType)!.IsSingleton)
                            {
                                // PATTERN: [NormalizeIdEq] (Eq (XsltGenerateId $arg1:*) ^ (Single? (TypeOf $arg1)) (XsltGenerateId $arg2:*) ^ (Single? (TypeOf $arg2))) => (Is $arg1 $arg2)
                                if (AllowReplace(XmlILOptimization.NormalizeIdEq, local0))
                                {
                                    return Replace(XmlILOptimization.NormalizeIdEq, local0, VisitIs(f.Is(local3, local4)));
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeIdEq])
            {
                if (local1.NodeType == QilNodeType.XsltGenerateId)
                {
                    QilNode local3 = local1[0];
                    if (((local3).XmlType)!.IsSingleton)
                    {
                        if (local2.NodeType == QilNodeType.StrConcat)
                        {
                            QilNode local5 = local2[1];
                            if (local5.NodeType == QilNodeType.Loop)
                            {
                                QilNode local6 = local5[0];
                                QilNode local8 = local5[1];
                                if (local6.NodeType == QilNodeType.For)
                                {
                                    QilNode local7 = local6[0];
                                    if (!((local7).XmlType)!.MaybeMany)
                                    {
                                        if (local8.NodeType == QilNodeType.XsltGenerateId)
                                        {
                                            QilNode local9 = local8[0];
                                            if (local9 == local6)
                                            {
                                                // PATTERN: [NormalizeIdEq] (Eq (XsltGenerateId $arg:*) ^ (Single? (TypeOf $arg)) (StrConcat * (Loop $iter:(For $bind:* ^ (AtMostOne? (TypeOf $bind))) (XsltGenerateId $iter)))) => (Not (IsEmpty (Filter $iterNew:(For $bind) (Is $arg $iterNew))))
                                                if (AllowReplace(XmlILOptimization.NormalizeIdEq, local0))
                                                {
                                                    QilNode local10 = VisitFor(f.For(local7));
                                                    return Replace(XmlILOptimization.NormalizeIdEq, local0, VisitNot(f.Not(VisitIsEmpty(f.IsEmpty(VisitFilter(f.Filter(local10, VisitIs(f.Is(local3, local10)))))))));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeIdEq])
            {
                if (local1.NodeType == QilNodeType.StrConcat)
                {
                    QilNode local4 = local1[1];
                    if (local4.NodeType == QilNodeType.Loop)
                    {
                        QilNode local5 = local4[0];
                        QilNode local7 = local4[1];
                        if (local5.NodeType == QilNodeType.For)
                        {
                            QilNode local6 = local5[0];
                            if (!((local6).XmlType)!.MaybeMany)
                            {
                                if (local7.NodeType == QilNodeType.XsltGenerateId)
                                {
                                    QilNode local8 = local7[0];
                                    if (local8 == local5)
                                    {
                                        if (local2.NodeType == QilNodeType.XsltGenerateId)
                                        {
                                            QilNode local9 = local2[0];
                                            if (((local9).XmlType)!.IsSingleton)
                                            {
                                                // PATTERN: [NormalizeIdEq] (Eq (StrConcat * (Loop $iter:(For $bind:* ^ (AtMostOne? (TypeOf $bind))) (XsltGenerateId $iter))) (XsltGenerateId $arg:*) ^ (Single? (TypeOf $arg))) => (Not (IsEmpty (Filter $iterNew:(For $bind) (Is $arg $iterNew))))
                                                if (AllowReplace(XmlILOptimization.NormalizeIdEq, local0))
                                                {
                                                    QilNode local10 = VisitFor(f.For(local6));
                                                    return Replace(XmlILOptimization.NormalizeIdEq, local0, VisitNot(f.Not(VisitIsEmpty(f.IsEmpty(VisitFilter(f.Filter(local10, VisitIs(f.Is(local9, local10)))))))));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeMuenchian])
            {
                if (local1.NodeType == QilNodeType.Length)
                {
                    QilNode local3 = local1[0];
                    if (local3.NodeType == QilNodeType.Union)
                    {
                        QilNode local4 = local3[0];
                        QilNode local5 = local3[1];
                        if ((((local4).XmlType)!.IsSingleton) && (!((local5).XmlType)!.MaybeMany))
                        {
                            if (local2.NodeType == QilNodeType.LiteralInt32)
                            {
                                int local6 = (int)((QilLiteral)local2).Value!;
                                if (local6 == 1)
                                {
                                    // PATTERN: [NormalizeMuenchian] (Eq (Length (Union $arg1:* $arg2:*) ^ (Single? (TypeOf $arg1)) ^ (AtMostOne? (TypeOf $arg2))) (LiteralInt32 1)) => (IsEmpty (Filter $iterNew:(For $arg2) (Not (Is $arg1 $iterNew))))
                                    if (AllowReplace(XmlILOptimization.NormalizeMuenchian, local0))
                                    {
                                        QilNode local7 = VisitFor(f.For(local5));
                                        return Replace(XmlILOptimization.NormalizeMuenchian, local0, VisitIsEmpty(f.IsEmpty(VisitFilter(f.Filter(local7, VisitNot(f.Not(VisitIs(f.Is(local4, local7)))))))));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeMuenchian])
            {
                if (local1.NodeType == QilNodeType.Length)
                {
                    QilNode local3 = local1[0];
                    if (local3.NodeType == QilNodeType.Union)
                    {
                        QilNode local4 = local3[0];
                        QilNode local5 = local3[1];
                        if ((!((local4).XmlType)!.MaybeMany) && (((local5).XmlType)!.IsSingleton))
                        {
                            if (local2.NodeType == QilNodeType.LiteralInt32)
                            {
                                int local6 = (int)((QilLiteral)local2).Value!;
                                if (local6 == 1)
                                {
                                    // PATTERN: [NormalizeMuenchian] (Eq (Length (Union $arg1:* $arg2:*) ^ (AtMostOne? (TypeOf $arg1)) ^ (Single? (TypeOf $arg2))) (LiteralInt32 1)) => (IsEmpty (Filter $iterNew:(For $arg1) (Not (Is $iterNew $arg2))))
                                    if (AllowReplace(XmlILOptimization.NormalizeMuenchian, local0))
                                    {
                                        QilNode local7 = VisitFor(f.For(local4));
                                        return Replace(XmlILOptimization.NormalizeMuenchian, local0, VisitIsEmpty(f.IsEmpty(VisitFilter(f.Filter(local7, VisitNot(f.Not(VisitIs(f.Is(local7, local5)))))))));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateMaxLengthEq])
            {
                if (local1.NodeType == QilNodeType.Length)
                {
                    if (local2.NodeType == QilNodeType.LiteralInt32)
                    {
                        int local4 = (int)((QilLiteral)local2).Value!;
                        // PATTERN: [AnnotateMaxLengthEq] (Eq $len:(Length *) (LiteralInt32 $num:*)) => (AddPattern $len {MaxPosition}) ^ (AddArgument $len {MaxPosition} $num) ^ { }
                        if (AllowReplace(XmlILOptimization.AnnotateMaxLengthEq, local0))
                        {
                            OptimizerPatterns.Write((QilNode)(local1)).AddPattern(OptimizerPatternName.MaxPosition); OptimizerPatterns.Write((QilNode)(local1)).AddArgument(OptimizerPatternArgument.MaxPosition, local4);
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitGt(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Gt $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Gt * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateGt])
            {
                if (IsLiteral((local1)))
                {
                    if (IsLiteral((local2)))
                    {
                        // PATTERN: [EliminateGt] (Gt $x:* ^ (Literal? $x) $y:* ^ (Literal? $y)) => (Gt! $x $y)
                        if (AllowReplace(XmlILOptimization.EliminateGt, local0))
                        {
                            return Replace(XmlILOptimization.EliminateGt, local0, FoldComparison(QilNodeType.Gt, local1, local2));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeGtLiteral])
            {
                if (IsLiteral((local1)))
                {
                    if (!(IsLiteral((local2))))
                    {
                        // PATTERN: [NormalizeGtLiteral] (Gt $left:* ^ (Literal? $left) $right:* ^ ~((Literal? $right))) => (Lt $right $left)
                        if (AllowReplace(XmlILOptimization.NormalizeGtLiteral, local0))
                        {
                            return Replace(XmlILOptimization.NormalizeGtLiteral, local0, VisitLt(f.Lt(local2, local1)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeXsltConvertGt])
            {
                if (local1.NodeType == QilNodeType.XsltConvert)
                {
                    QilNode local3 = local1[0];
                    QilNode local4 = local1[1];
                    if (local4.NodeType == QilNodeType.LiteralType)
                    {
                        XmlQueryType local5 = (XmlQueryType)((QilLiteral)local4).Value!;
                        if ((IsPrimitiveNumeric((local3).XmlType)) && (IsPrimitiveNumeric(local5)))
                        {
                            if ((IsLiteral((local2))) && (CanFoldXsltConvertNonLossy(local2, (local3).XmlType!)))
                            {
                                // PATTERN: [NormalizeXsltConvertGt] (Gt (XsltConvert $expr:* (LiteralType $typ:*)) ^ (PrimitiveNumeric? (TypeOf $expr)) ^ (PrimitiveNumeric? $typ) $lit:* ^ (Literal? $lit) ^ (CanFoldXsltConvertNonLossy? $lit (TypeOf $expr))) => (Gt $expr (FoldXsltConvert $lit (TypeOf $expr)))
                                if (AllowReplace(XmlILOptimization.NormalizeXsltConvertGt, local0))
                                {
                                    return Replace(XmlILOptimization.NormalizeXsltConvertGt, local0, VisitGt(f.Gt(local3, FoldXsltConvert(local2, (local3).XmlType!))));
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeLengthGt])
            {
                if (local1.NodeType == QilNodeType.Length)
                {
                    QilNode local3 = local1[0];
                    if (local2.NodeType == QilNodeType.LiteralInt32)
                    {
                        int local4 = (int)((QilLiteral)local2).Value!;
                        if (local4 == 0)
                        {
                            // PATTERN: [NormalizeLengthGt] (Gt (Length $expr:*) (LiteralInt32 0)) => (Not (IsEmpty $expr))
                            if (AllowReplace(XmlILOptimization.NormalizeLengthGt, local0))
                            {
                                return Replace(XmlILOptimization.NormalizeLengthGt, local0, VisitNot(f.Not(VisitIsEmpty(f.IsEmpty(local3)))));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateMaxLengthGt])
            {
                if (local1.NodeType == QilNodeType.Length)
                {
                    if (local2.NodeType == QilNodeType.LiteralInt32)
                    {
                        int local4 = (int)((QilLiteral)local2).Value!;
                        // PATTERN: [AnnotateMaxLengthGt] (Gt $len:(Length *) (LiteralInt32 $num:*)) => (AddPattern $len {MaxPosition}) ^ (AddArgument $len {MaxPosition} $num) ^ { }
                        if (AllowReplace(XmlILOptimization.AnnotateMaxLengthGt, local0))
                        {
                            OptimizerPatterns.Write((QilNode)(local1)).AddPattern(OptimizerPatternName.MaxPosition); OptimizerPatterns.Write((QilNode)(local1)).AddArgument(OptimizerPatternArgument.MaxPosition, local4);
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitGe(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Ge $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Ge * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateGe])
            {
                if (IsLiteral((local1)))
                {
                    if (IsLiteral((local2)))
                    {
                        // PATTERN: [EliminateGe] (Ge $x:* ^ (Literal? $x) $y:* ^ (Literal? $y)) => (Ge! $x $y)
                        if (AllowReplace(XmlILOptimization.EliminateGe, local0))
                        {
                            return Replace(XmlILOptimization.EliminateGe, local0, FoldComparison(QilNodeType.Ge, local1, local2));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeGeLiteral])
            {
                if (IsLiteral((local1)))
                {
                    if (!(IsLiteral((local2))))
                    {
                        // PATTERN: [NormalizeGeLiteral] (Ge $left:* ^ (Literal? $left) $right:* ^ ~((Literal? $right))) => (Le $right $left)
                        if (AllowReplace(XmlILOptimization.NormalizeGeLiteral, local0))
                        {
                            return Replace(XmlILOptimization.NormalizeGeLiteral, local0, VisitLe(f.Le(local2, local1)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeXsltConvertGe])
            {
                if (local1.NodeType == QilNodeType.XsltConvert)
                {
                    QilNode local3 = local1[0];
                    QilNode local4 = local1[1];
                    if (local4.NodeType == QilNodeType.LiteralType)
                    {
                        XmlQueryType local5 = (XmlQueryType)((QilLiteral)local4).Value!;
                        if ((IsPrimitiveNumeric((local3).XmlType)) && (IsPrimitiveNumeric(local5)))
                        {
                            if ((IsLiteral((local2))) && (CanFoldXsltConvertNonLossy(local2, (local3).XmlType!)))
                            {
                                // PATTERN: [NormalizeXsltConvertGe] (Ge (XsltConvert $expr:* (LiteralType $typ:*)) ^ (PrimitiveNumeric? (TypeOf $expr)) ^ (PrimitiveNumeric? $typ) $lit:* ^ (Literal? $lit) ^ (CanFoldXsltConvertNonLossy? $lit (TypeOf $expr))) => (Ge $expr (FoldXsltConvert $lit (TypeOf $expr)))
                                if (AllowReplace(XmlILOptimization.NormalizeXsltConvertGe, local0))
                                {
                                    return Replace(XmlILOptimization.NormalizeXsltConvertGe, local0, VisitGe(f.Ge(local3, FoldXsltConvert(local2, (local3).XmlType!))));
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateMaxLengthGe])
            {
                if (local1.NodeType == QilNodeType.Length)
                {
                    if (local2.NodeType == QilNodeType.LiteralInt32)
                    {
                        int local4 = (int)((QilLiteral)local2).Value!;
                        // PATTERN: [AnnotateMaxLengthGe] (Ge $len:(Length *) (LiteralInt32 $num:*)) => (AddPattern $len {MaxPosition}) ^ (AddArgument $len {MaxPosition} $num) ^ { }
                        if (AllowReplace(XmlILOptimization.AnnotateMaxLengthGe, local0))
                        {
                            OptimizerPatterns.Write((QilNode)(local1)).AddPattern(OptimizerPatternName.MaxPosition); OptimizerPatterns.Write((QilNode)(local1)).AddArgument(OptimizerPatternArgument.MaxPosition, local4);
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitLt(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Lt $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Lt * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateLt])
            {
                if (IsLiteral((local1)))
                {
                    if (IsLiteral((local2)))
                    {
                        // PATTERN: [EliminateLt] (Lt $x:* ^ (Literal? $x) $y:* ^ (Literal? $y)) => (Lt! $x $y)
                        if (AllowReplace(XmlILOptimization.EliminateLt, local0))
                        {
                            return Replace(XmlILOptimization.EliminateLt, local0, FoldComparison(QilNodeType.Lt, local1, local2));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeLtLiteral])
            {
                if (IsLiteral((local1)))
                {
                    if (!(IsLiteral((local2))))
                    {
                        // PATTERN: [NormalizeLtLiteral] (Lt $left:* ^ (Literal? $left) $right:* ^ ~((Literal? $right))) => (Gt $right $left)
                        if (AllowReplace(XmlILOptimization.NormalizeLtLiteral, local0))
                        {
                            return Replace(XmlILOptimization.NormalizeLtLiteral, local0, VisitGt(f.Gt(local2, local1)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeXsltConvertLt])
            {
                if (local1.NodeType == QilNodeType.XsltConvert)
                {
                    QilNode local3 = local1[0];
                    QilNode local4 = local1[1];
                    if (local4.NodeType == QilNodeType.LiteralType)
                    {
                        XmlQueryType local5 = (XmlQueryType)((QilLiteral)local4).Value!;
                        if ((IsPrimitiveNumeric((local3).XmlType)) && (IsPrimitiveNumeric(local5)))
                        {
                            if ((IsLiteral((local2))) && (CanFoldXsltConvertNonLossy(local2, (local3).XmlType!)))
                            {
                                // PATTERN: [NormalizeXsltConvertLt] (Lt (XsltConvert $expr:* (LiteralType $typ:*)) ^ (PrimitiveNumeric? (TypeOf $expr)) ^ (PrimitiveNumeric? $typ) $lit:* ^ (Literal? $lit) ^ (CanFoldXsltConvertNonLossy? $lit (TypeOf $expr))) => (Lt $expr (FoldXsltConvert $lit (TypeOf $expr)))
                                if (AllowReplace(XmlILOptimization.NormalizeXsltConvertLt, local0))
                                {
                                    return Replace(XmlILOptimization.NormalizeXsltConvertLt, local0, VisitLt(f.Lt(local3, FoldXsltConvert(local2, (local3).XmlType!))));
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateMaxLengthLt])
            {
                if (local1.NodeType == QilNodeType.Length)
                {
                    if (local2.NodeType == QilNodeType.LiteralInt32)
                    {
                        int local4 = (int)((QilLiteral)local2).Value!;
                        // PATTERN: [AnnotateMaxLengthLt] (Lt $len:(Length *) (LiteralInt32 $num:*)) => (AddPattern $len {MaxPosition}) ^ (AddArgument $len {MaxPosition} $num) ^ { }
                        if (AllowReplace(XmlILOptimization.AnnotateMaxLengthLt, local0))
                        {
                            OptimizerPatterns.Write((QilNode)(local1)).AddPattern(OptimizerPatternName.MaxPosition); OptimizerPatterns.Write((QilNode)(local1)).AddArgument(OptimizerPatternArgument.MaxPosition, local4);
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitLe(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Le $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Le * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateLe])
            {
                if (IsLiteral((local1)))
                {
                    if (IsLiteral((local2)))
                    {
                        // PATTERN: [EliminateLe] (Le $x:* ^ (Literal? $x) $y:* ^ (Literal? $y)) => (Le! $x $y)
                        if (AllowReplace(XmlILOptimization.EliminateLe, local0))
                        {
                            return Replace(XmlILOptimization.EliminateLe, local0, FoldComparison(QilNodeType.Le, local1, local2));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeLeLiteral])
            {
                if (IsLiteral((local1)))
                {
                    if (!(IsLiteral((local2))))
                    {
                        // PATTERN: [NormalizeLeLiteral] (Le $left:* ^ (Literal? $left) $right:* ^ ~((Literal? $right))) => (Ge $right $left)
                        if (AllowReplace(XmlILOptimization.NormalizeLeLiteral, local0))
                        {
                            return Replace(XmlILOptimization.NormalizeLeLiteral, local0, VisitGe(f.Ge(local2, local1)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeXsltConvertLe])
            {
                if (local1.NodeType == QilNodeType.XsltConvert)
                {
                    QilNode local3 = local1[0];
                    QilNode local4 = local1[1];
                    if (local4.NodeType == QilNodeType.LiteralType)
                    {
                        XmlQueryType local5 = (XmlQueryType)((QilLiteral)local4).Value!;
                        if ((IsPrimitiveNumeric((local3).XmlType)) && (IsPrimitiveNumeric(local5)))
                        {
                            if ((IsLiteral((local2))) && (CanFoldXsltConvertNonLossy(local2, (local3).XmlType!)))
                            {
                                // PATTERN: [NormalizeXsltConvertLe] (Le (XsltConvert $expr:* (LiteralType $typ:*)) ^ (PrimitiveNumeric? (TypeOf $expr)) ^ (PrimitiveNumeric? $typ) $lit:* ^ (Literal? $lit) ^ (CanFoldXsltConvertNonLossy? $lit (TypeOf $expr))) => (Le $expr (FoldXsltConvert $lit (TypeOf $expr)))
                                if (AllowReplace(XmlILOptimization.NormalizeXsltConvertLe, local0))
                                {
                                    return Replace(XmlILOptimization.NormalizeXsltConvertLe, local0, VisitLe(f.Le(local3, FoldXsltConvert(local2, (local3).XmlType!))));
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateMaxLengthLe])
            {
                if (local1.NodeType == QilNodeType.Length)
                {
                    if (local2.NodeType == QilNodeType.LiteralInt32)
                    {
                        int local4 = (int)((QilLiteral)local2).Value!;
                        // PATTERN: [AnnotateMaxLengthLe] (Le $len:(Length *) (LiteralInt32 $num:*)) => (AddPattern $len {MaxPosition}) ^ (AddArgument $len {MaxPosition} $num) ^ { }
                        if (AllowReplace(XmlILOptimization.AnnotateMaxLengthLe, local0))
                        {
                            OptimizerPatterns.Write((QilNode)(local1)).AddPattern(OptimizerPatternName.MaxPosition); OptimizerPatterns.Write((QilNode)(local1)).AddArgument(OptimizerPatternArgument.MaxPosition, local4);
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // value comparison operators

        #region node comparison operators
        protected override QilNode VisitIs(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Is $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Is * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIs])
            {
                if (local2 == local1)
                {
                    // PATTERN: [EliminateIs] (Is $x:* $x) => (True)
                    if (AllowReplace(XmlILOptimization.EliminateIs, local0))
                    {
                        return Replace(XmlILOptimization.EliminateIs, local0, VisitTrue(f.True()));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitAfter(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (After $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (After * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateAfter])
            {
                if (local2 == local1)
                {
                    // PATTERN: [EliminateAfter] (After $x:* $x) => (False)
                    if (AllowReplace(XmlILOptimization.EliminateAfter, local0))
                    {
                        return Replace(XmlILOptimization.EliminateAfter, local0, VisitFalse(f.False()));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitBefore(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Before $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Before * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateBefore])
            {
                if (local2 == local1)
                {
                    // PATTERN: [EliminateBefore] (Before $x:* $x) => (False)
                    if (AllowReplace(XmlILOptimization.EliminateBefore, local0))
                    {
                        return Replace(XmlILOptimization.EliminateBefore, local0, VisitFalse(f.False()));
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // node comparison operators

        #region loops
        protected override QilNode VisitLoop(QilLoop local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Loop $i:* ^ (None? (TypeOf $i)) *) => (Nop (First $i))
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop((QilNode)(local1)[0])));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIterator])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if (local3.NodeType == QilNodeType.For)
                    {
                        if (!OptimizerPatterns.Read(local1).MatchesPattern(OptimizerPatternName.IsPositional))
                        {
                            // PATTERN: [EliminateIterator] $outer:(Loop $iter:(For $iterRef:(For *)) ^ (NonPositionalIterator? $iter) $ret:*) => (Subs $ret $iter $iterRef)
                            if (AllowReplace(XmlILOptimization.EliminateIterator, local0))
                            {
                                return Replace(XmlILOptimization.EliminateIterator, local0, Subs(local2, local1, local3));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateLoop])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if (local3.NodeType == QilNodeType.Sequence)
                    {
                        if ((local3).Count == (0))
                        {
                            // PATTERN: [EliminateLoop] (Loop (For $x:(Sequence) ^ (Count? $x 0)) *) => (Sequence)
                            if (AllowReplace(XmlILOptimization.EliminateLoop, local0))
                            {
                                return Replace(XmlILOptimization.EliminateLoop, local0, VisitSequence(f.Sequence()));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateLoop])
            {
                if (!OptimizerPatterns.Read(local1).MatchesPattern(OptimizerPatternName.MaybeSideEffects))
                {
                    if (local2.NodeType == QilNodeType.Sequence)
                    {
                        if ((local2).Count == (0))
                        {
                            // PATTERN: [EliminateLoop] (Loop $i:* ^ (NoSideEffects? $i) $x:(Sequence) ^ (Count? $x 0)) => (Sequence)
                            if (AllowReplace(XmlILOptimization.EliminateLoop, local0))
                            {
                                return Replace(XmlILOptimization.EliminateLoop, local0, VisitSequence(f.Sequence()));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateLoop])
            {
                if (local2 == local1)
                {
                    // PATTERN: [EliminateLoop] (Loop $iter:* $iter) => (First $iter)
                    if (AllowReplace(XmlILOptimization.EliminateLoop, local0))
                    {
                        return Replace(XmlILOptimization.EliminateLoop, local0, (QilNode)(local1)[0]);
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeLoopText])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if (((local3).XmlType!).IsSingleton)
                    {
                        if (local2.NodeType == QilNodeType.TextCtor)
                        {
                            QilNode local4 = local2[0];
                            // PATTERN: [NormalizeLoopText] (Loop $iter:(For $bind:* ^ (Single? (TypeOf $bind))) $ctor:(TextCtor $text:*)) => (TextCtor (Loop $iter $text))
                            if (AllowReplace(XmlILOptimization.NormalizeLoopText, local0))
                            {
                                return Replace(XmlILOptimization.NormalizeLoopText, local0, VisitTextCtor(f.TextCtor(VisitLoop(f.Loop(local1, local4)))));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIteratorUsedAtMostOnce])
            {
                if ((((local1).NodeType == QilNodeType.Let) || ((((QilNode)(local1)[0]).XmlType!).IsSingleton)) && (!OptimizerPatterns.Read(local1).MatchesPattern(OptimizerPatternName.MaybeSideEffects)))
                {
                    if (_nodeCounter.Count(local2, local1) <= 1)
                    {
                        // PATTERN: [EliminateIteratorUsedAtMostOnce] (Loop $iter:* ^ (NodeType? $iter {Let}) | (Single? (TypeOf (First $iter))) ^ (NoSideEffects? $iter) $ret:* ^ (RefCountZeroOrOne? $ret $iter)) => (Subs $ret $iter (First $iter))
                        if (AllowReplace(XmlILOptimization.EliminateIteratorUsedAtMostOnce, local0))
                        {
                            return Replace(XmlILOptimization.EliminateIteratorUsedAtMostOnce, local0, Subs(local2, local1, (QilNode)(local1)[0]));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeLoopConditional])
            {
                if (local2.NodeType == QilNodeType.Conditional)
                {
                    QilNode local3 = local2[0];
                    QilNode local4 = local2[1];
                    QilNode local5 = local2[2];
                    if (local4.NodeType == QilNodeType.Sequence)
                    {
                        if ((local4).Count == (0))
                        {
                            if (local5 == local1)
                            {
                                // PATTERN: [NormalizeLoopConditional] (Loop $iter:* (Conditional $cond:* $left:(Sequence) ^ (Count? $left 0) $iter)) => (Filter $iter (Not $cond))
                                if (AllowReplace(XmlILOptimization.NormalizeLoopConditional, local0))
                                {
                                    return Replace(XmlILOptimization.NormalizeLoopConditional, local0, VisitFilter(f.Filter(local1, VisitNot(f.Not(local3)))));
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeLoopConditional])
            {
                if (local2.NodeType == QilNodeType.Conditional)
                {
                    QilNode local3 = local2[0];
                    QilNode local4 = local2[1];
                    QilNode local5 = local2[2];
                    if (local4 == local1)
                    {
                        if (local5.NodeType == QilNodeType.Sequence)
                        {
                            if ((local5).Count == (0))
                            {
                                // PATTERN: [NormalizeLoopConditional] (Loop $iter:* (Conditional $cond:* $iter $right:(Sequence) ^ (Count? $right 0))) => (Filter $iter $cond)
                                if (AllowReplace(XmlILOptimization.NormalizeLoopConditional, local0))
                                {
                                    return Replace(XmlILOptimization.NormalizeLoopConditional, local0, VisitFilter(f.Filter(local1, local3)));
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeLoopConditional])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    if (local2.NodeType == QilNodeType.Conditional)
                    {
                        QilNode local4 = local2[0];
                        QilNode local5 = local2[1];
                        QilNode local6 = local2[2];
                        if (local5.NodeType == QilNodeType.Sequence)
                        {
                            if ((local5).Count == (0))
                            {
                                if (NonPositional(local6, local1))
                                {
                                    // PATTERN: [NormalizeLoopConditional] (Loop $iter:(For *) (Conditional $cond:* $left:(Sequence) ^ (Count? $left 0) $right:* ^ (NonPositional? $right $iter))) => (Loop $iter2:(For (Filter $iter (Not $cond))) (Subs $right $iter $iter2))
                                    if (AllowReplace(XmlILOptimization.NormalizeLoopConditional, local0))
                                    {
                                        QilNode local7 = VisitFor(f.For(VisitFilter(f.Filter(local1, VisitNot(f.Not(local4))))));
                                        return Replace(XmlILOptimization.NormalizeLoopConditional, local0, VisitLoop(f.Loop(local7, Subs(local6, local1, local7))));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeLoopConditional])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    if (local2.NodeType == QilNodeType.Conditional)
                    {
                        QilNode local4 = local2[0];
                        QilNode local5 = local2[1];
                        QilNode local6 = local2[2];
                        if (NonPositional(local5, local1))
                        {
                            if (local6.NodeType == QilNodeType.Sequence)
                            {
                                if ((local6).Count == (0))
                                {
                                    // PATTERN: [NormalizeLoopConditional] (Loop $iter:(For *) (Conditional $cond:* $left:* ^ (NonPositional? $left $iter) $right:(Sequence) ^ (Count? $right 0))) => (Loop $iter2:(For (Filter $iter $cond)) (Subs $left $iter $iter2))
                                    if (AllowReplace(XmlILOptimization.NormalizeLoopConditional, local0))
                                    {
                                        QilNode local7 = VisitFor(f.For(VisitFilter(f.Filter(local1, local4))));
                                        return Replace(XmlILOptimization.NormalizeLoopConditional, local0, VisitLoop(f.Loop(local7, Subs(local5, local1, local7))));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeLoopLoop])
            {
                if (local2.NodeType == QilNodeType.Loop)
                {
                    QilNode local3 = local2[0];
                    QilNode local5 = local2[1];
                    if (local3.NodeType == QilNodeType.For)
                    {
                        QilNode local4 = local3[0];
                        if ((!(DependsOn(local5, local1))) && (NonPositional(local5, local3)))
                        {
                            // PATTERN: [NormalizeLoopLoop] (Loop $iter:* $ret:(Loop $iter2:(For $bind2:*) $ret2:* ^ ~($ret2 >> $iter) ^ (NonPositional? $ret2 $iter2))) => (Loop $iter3:(For (Loop $iter $bind2)) (Subs $ret2 $iter2 $iter3))
                            if (AllowReplace(XmlILOptimization.NormalizeLoopLoop, local0))
                            {
                                QilNode local6 = VisitFor(f.For(VisitLoop(f.Loop(local1, local4))));
                                return Replace(XmlILOptimization.NormalizeLoopLoop, local0, VisitLoop(f.Loop(local6, Subs(local5, local3, local6))));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateSingletonLoop])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if (!((local3).XmlType!).MaybeMany)
                    {
                        // PATTERN: [AnnotateSingletonLoop] $outer:(Loop (For $bind:* ^ (AtMostOne? (TypeOf $bind))) $ret:*) => (InheritPattern $outer $ret {IsDocOrderDistinct}) ^ (InheritPattern $outer $ret {SameDepth}) ^ { }
                        if (AllowReplace(XmlILOptimization.AnnotateSingletonLoop, local0))
                        {
                            OptimizerPatterns.Inherit((QilNode)(local2), (QilNode)(local0), OptimizerPatternName.IsDocOrderDistinct); OptimizerPatterns.Inherit((QilNode)(local2), (QilNode)(local0), OptimizerPatternName.SameDepth);
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateRootLoop])
            {
                if (IsStepPattern(local2, QilNodeType.Root))
                {
                    // PATTERN: [AnnotateRootLoop] $outer:(Loop * $ret:* ^ (StepPattern? $ret {Root})) => (AddPattern $outer {SameDepth}) ^ { }
                    if (AllowReplace(XmlILOptimization.AnnotateRootLoop, local0))
                    {
                        OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.SameDepth);
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateContentLoop])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if (OptimizerPatterns.Read((QilNode)(local3)).MatchesPattern(OptimizerPatternName.SameDepth))
                    {
                        if (((IsStepPattern(local2, QilNodeType.Content)) || (IsStepPattern(local2, QilNodeType.Union))) && ((local1) == (OptimizerPatterns.Read((QilNode)(local2)).GetArgument(OptimizerPatternArgument.StepInput))))
                        {
                            // PATTERN: [AnnotateContentLoop] $outer:(Loop $iter:(For $bind:* ^ (Pattern? $bind {SameDepth})) $ret:* ^ (StepPattern? $ret {Content}) | (StepPattern? $ret {Union}) ^ (Equal? $iter (Argument $ret {StepInput}))) => (AddPattern $outer {SameDepth}) ^ (InheritPattern $outer $bind {IsDocOrderDistinct}) ^ { }
                            if (AllowReplace(XmlILOptimization.AnnotateContentLoop, local0))
                            {
                                OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.SameDepth); OptimizerPatterns.Inherit((QilNode)(local3), (QilNode)(local0), OptimizerPatternName.IsDocOrderDistinct);
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateAttrNmspLoop])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if ((((IsStepPattern(local2, QilNodeType.Attribute)) || (IsStepPattern(local2, QilNodeType.XPathNamespace))) || (OptimizerPatterns.Read((QilNode)(local2)).MatchesPattern(OptimizerPatternName.FilterAttributeKind))) && ((local1) == (OptimizerPatterns.Read((QilNode)(local2)).GetArgument(OptimizerPatternArgument.StepInput))))
                    {
                        // PATTERN: [AnnotateAttrNmspLoop] $outer:(Loop $iter:(For $bind:*) $ret:* ^ (StepPattern? $ret {Attribute}) | (StepPattern? $ret {XPathNamespace}) | (Pattern? $ret {FilterAttributeKind}) ^ (Equal? $iter (Argument $ret {StepInput}))) => (InheritPattern $outer $bind {SameDepth}) ^ (InheritPattern $outer $bind {IsDocOrderDistinct}) ^ { }
                        if (AllowReplace(XmlILOptimization.AnnotateAttrNmspLoop, local0))
                        {
                            OptimizerPatterns.Inherit((QilNode)(local3), (QilNode)(local0), OptimizerPatternName.SameDepth); OptimizerPatterns.Inherit((QilNode)(local3), (QilNode)(local0), OptimizerPatternName.IsDocOrderDistinct);
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateDescendantLoop])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if (OptimizerPatterns.Read((QilNode)(local3)).MatchesPattern(OptimizerPatternName.SameDepth))
                    {
                        if (((IsStepPattern(local2, QilNodeType.Descendant)) || (IsStepPattern(local2, QilNodeType.DescendantOrSelf))) && ((local1) == (OptimizerPatterns.Read((QilNode)(local2)).GetArgument(OptimizerPatternArgument.StepInput))))
                        {
                            // PATTERN: [AnnotateDescendantLoop] $outer:(Loop $iter:(For $bind:* ^ (Pattern? $bind {SameDepth})) $ret:* ^ (StepPattern? $ret {Descendant}) | (StepPattern? $ret {DescendantOrSelf}) ^ (Equal? $iter (Argument $ret {StepInput}))) => (InheritPattern $outer $bind {IsDocOrderDistinct}) ^ { }
                            if (AllowReplace(XmlILOptimization.AnnotateDescendantLoop, local0))
                            {
                                OptimizerPatterns.Inherit((QilNode)(local3), (QilNode)(local0), OptimizerPatternName.IsDocOrderDistinct);
                            }
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitFilter(QilLoop local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Filter $i:* ^ (None? (TypeOf $i)) *) => (Nop (First $i))
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop((QilNode)(local1)[0])));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Filter $i:* $w:* ^ (None? (TypeOf $w))) => (Loop $i $w)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitLoop(f.Loop(local1, local2)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateFilter])
            {
                if (!OptimizerPatterns.Read(local1).MatchesPattern(OptimizerPatternName.MaybeSideEffects))
                {
                    if (local2.NodeType == QilNodeType.False)
                    {
                        // PATTERN: [EliminateFilter] (Filter $i:* ^ (NoSideEffects? $i) (False)) => (Sequence)
                        if (AllowReplace(XmlILOptimization.EliminateFilter, local0))
                        {
                            return Replace(XmlILOptimization.EliminateFilter, local0, VisitSequence(f.Sequence()));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateFilter])
            {
                if (local2.NodeType == QilNodeType.True)
                {
                    // PATTERN: [EliminateFilter] (Filter $i:* (True)) => (First $i)
                    if (AllowReplace(XmlILOptimization.EliminateFilter, local0))
                    {
                        return Replace(XmlILOptimization.EliminateFilter, local0, (QilNode)(local1)[0]);
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeAttribute])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if (local3.NodeType == QilNodeType.Content)
                    {
                        QilNode local4 = local3[0];
                        if (local2.NodeType == QilNodeType.And)
                        {
                            QilNode local5 = local2[0];
                            QilNode local9 = local2[1];
                            if (local5.NodeType == QilNodeType.IsType)
                            {
                                QilNode local6 = local5[0];
                                QilNode local7 = local5[1];
                                if (local6 == local1)
                                {
                                    if (local7.NodeType == QilNodeType.LiteralType)
                                    {
                                        XmlQueryType local8 = (XmlQueryType)((QilLiteral)local7).Value!;
                                        if ((local8) == (XmlQueryTypeFactory.Attribute))
                                        {
                                            if (local9.NodeType == QilNodeType.Eq)
                                            {
                                                QilNode local10 = local9[0];
                                                QilNode local12 = local9[1];
                                                if (local10.NodeType == QilNodeType.NameOf)
                                                {
                                                    QilNode local11 = local10[0];
                                                    if (local11 == local1)
                                                    {
                                                        if (local12.NodeType == QilNodeType.LiteralQName)
                                                        {
                                                            // PATTERN: [NormalizeAttribute] (Filter $iter:(For (Content $input:*)) (And (IsType $iter (LiteralType $typ:* ^ (Equal? $typ (ConstructType {Attribute})))) (Eq (NameOf $iter) $qname:(LiteralQName * * *)))) => (Attribute $input $qname)
                                                            if (AllowReplace(XmlILOptimization.NormalizeAttribute, local0))
                                                            {
                                                                return Replace(XmlILOptimization.NormalizeAttribute, local0, VisitAttribute(f.Attribute(local4, local12)));
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.CommuteFilterLoop])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if (local3.NodeType == QilNodeType.Loop)
                    {
                        QilNode local4 = local3[0];
                        QilNode local5 = local3[1];
                        if ((NonPositional(local2, local1)) && (!(IsDocOrderDistinct(local3))))
                        {
                            // PATTERN: [CommuteFilterLoop] (Filter $iter:(For $loop:(Loop $iter2:* $ret2:*)) $cond:* ^ (NonPositional? $cond $iter) ^ ~((DocOrderDistinct? $loop))) => (Loop $iter2 (Filter $iter3:(For $ret2) (Subs $cond $iter $iter3)))
                            if (AllowReplace(XmlILOptimization.CommuteFilterLoop, local0))
                            {
                                QilNode local6 = VisitFor(f.For(local5));
                                return Replace(XmlILOptimization.CommuteFilterLoop, local0, VisitLoop(f.Loop(local4, VisitFilter(f.Filter(local6, Subs(local2, local1, local6))))));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.NormalizeLoopInvariant])
            {
                if ((!OptimizerPatterns.Read(local1).MatchesPattern(OptimizerPatternName.MaybeSideEffects)) && (!(((QilNode)(local1)[0]).NodeType == QilNodeType.OptimizeBarrier)))
                {
                    if ((!(DependsOn(local2, local1))) && (!OptimizerPatterns.Read(local2).MatchesPattern(OptimizerPatternName.MaybeSideEffects)))
                    {
                        // PATTERN: [NormalizeLoopInvariant] (Filter $iter:* ^ (NoSideEffects? $iter) ^ ~((NodeType? (First $iter) {OptimizeBarrier})) $cond:* ^ ~($cond >> $iter) ^ (NoSideEffects? $cond)) => (Conditional $cond (First $iter) (Sequence))
                        if (AllowReplace(XmlILOptimization.NormalizeLoopInvariant, local0))
                        {
                            return Replace(XmlILOptimization.NormalizeLoopInvariant, local0, VisitConditional(f.Conditional(local2, (QilNode)(local1)[0], VisitSequence(f.Sequence()))));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateMaxPositionEq])
            {
                if (local2.NodeType == QilNodeType.Eq)
                {
                    QilNode local3 = local2[0];
                    QilNode local5 = local2[1];
                    if (local3.NodeType == QilNodeType.PositionOf)
                    {
                        QilNode local4 = local3[0];
                        if (local4 == local1)
                        {
                            if (local5.NodeType == QilNodeType.LiteralInt32)
                            {
                                int local6 = (int)((QilLiteral)local5).Value!;
                                // PATTERN: [AnnotateMaxPositionEq] $outer:(Filter $iter:* (Eq (PositionOf $iter) (LiteralInt32 $num:*))) => (AddPattern $iter {MaxPosition}) ^ (AddArgument $iter {MaxPosition} $num) ^ { }
                                if (AllowReplace(XmlILOptimization.AnnotateMaxPositionEq, local0))
                                {
                                    OptimizerPatterns.Write((QilNode)(local1)).AddPattern(OptimizerPatternName.MaxPosition); OptimizerPatterns.Write((QilNode)(local1)).AddArgument(OptimizerPatternArgument.MaxPosition, local6);
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateMaxPositionLe])
            {
                if (local2.NodeType == QilNodeType.Le)
                {
                    QilNode local3 = local2[0];
                    QilNode local5 = local2[1];
                    if (local3.NodeType == QilNodeType.PositionOf)
                    {
                        QilNode local4 = local3[0];
                        if (local4 == local1)
                        {
                            if (local5.NodeType == QilNodeType.LiteralInt32)
                            {
                                int local6 = (int)((QilLiteral)local5).Value!;
                                // PATTERN: [AnnotateMaxPositionLe] $outer:(Filter $iter:* (Le (PositionOf $iter) (LiteralInt32 $num:*))) => (AddPattern $iter {MaxPosition}) ^ (AddArgument $iter {MaxPosition} $num) ^ { }
                                if (AllowReplace(XmlILOptimization.AnnotateMaxPositionLe, local0))
                                {
                                    OptimizerPatterns.Write((QilNode)(local1)).AddPattern(OptimizerPatternName.MaxPosition); OptimizerPatterns.Write((QilNode)(local1)).AddArgument(OptimizerPatternArgument.MaxPosition, local6);
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateMaxPositionLt])
            {
                if (local2.NodeType == QilNodeType.Lt)
                {
                    QilNode local3 = local2[0];
                    QilNode local5 = local2[1];
                    if (local3.NodeType == QilNodeType.PositionOf)
                    {
                        QilNode local4 = local3[0];
                        if (local4 == local1)
                        {
                            if (local5.NodeType == QilNodeType.LiteralInt32)
                            {
                                int local6 = (int)((QilLiteral)local5).Value!;
                                // PATTERN: [AnnotateMaxPositionLt] $outer:(Filter $iter:* (Lt (PositionOf $iter) (LiteralInt32 $num:*))) => (AddPattern $iter {MaxPosition}) ^ (AddArgument $iter {MaxPosition} { {$num} - 1 }) ^ { }
                                if (AllowReplace(XmlILOptimization.AnnotateMaxPositionLt, local0))
                                {
                                    OptimizerPatterns.Write((QilNode)(local1)).AddPattern(OptimizerPatternName.MaxPosition); OptimizerPatterns.Write((QilNode)(local1)).AddArgument(OptimizerPatternArgument.MaxPosition, local6 - 1);
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateFilter])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    // PATTERN: [AnnotateFilter] $outer:(Filter $iter:(For $bind:*) *) => (InheritPattern $outer $bind {Step}) ^ (InheritPattern $outer $bind {IsDocOrderDistinct}) ^ (InheritPattern $outer $bind {SameDepth}) ^ { }
                    if (AllowReplace(XmlILOptimization.AnnotateFilter, local0))
                    {
                        OptimizerPatterns.Inherit((QilNode)(local3), (QilNode)(local0), OptimizerPatternName.Step); OptimizerPatterns.Inherit((QilNode)(local3), (QilNode)(local0), OptimizerPatternName.IsDocOrderDistinct); OptimizerPatterns.Inherit((QilNode)(local3), (QilNode)(local0), OptimizerPatternName.SameDepth);
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateFilterElements])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if (OptimizerPatterns.Read((QilNode)(local3)).MatchesPattern(OptimizerPatternName.Axis))
                    {
                        if (local2.NodeType == QilNodeType.And)
                        {
                            QilNode local4 = local2[0];
                            QilNode local8 = local2[1];
                            if (local4.NodeType == QilNodeType.IsType)
                            {
                                QilNode local5 = local4[0];
                                QilNode local6 = local4[1];
                                if (local5 == local1)
                                {
                                    if (local6.NodeType == QilNodeType.LiteralType)
                                    {
                                        XmlQueryType local7 = (XmlQueryType)((QilLiteral)local6).Value!;
                                        if ((local7) == (XmlQueryTypeFactory.Element))
                                        {
                                            if (local8.NodeType == QilNodeType.Eq)
                                            {
                                                QilNode local9 = local8[0];
                                                QilNode local11 = local8[1];
                                                if (local9.NodeType == QilNodeType.NameOf)
                                                {
                                                    QilNode local10 = local9[0];
                                                    if (local10 == local1)
                                                    {
                                                        if (local11.NodeType == QilNodeType.LiteralQName)
                                                        {
                                                            // PATTERN: [AnnotateFilterElements] $outer:(Filter $iter:(For $bind:* ^ (Pattern? $bind {Axis})) (And (IsType $iter (LiteralType $typ:* ^ (Equal? $typ (ConstructType {Element})))) (Eq (NameOf $iter) $qname:(LiteralQName * * *)))) => (AddPattern $outer {FilterElements}) ^ (AddArgument $outer {ElementQName} $qname) ^ { }
                                                            if (AllowReplace(XmlILOptimization.AnnotateFilterElements, local0))
                                                            {
                                                                OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.FilterElements); OptimizerPatterns.Write((QilNode)(local0)).AddArgument(OptimizerPatternArgument.ElementQName, local11);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateFilterContentKind])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if (OptimizerPatterns.Read((QilNode)(local3)).MatchesPattern(OptimizerPatternName.Axis))
                    {
                        if (local2.NodeType == QilNodeType.IsType)
                        {
                            QilNode local4 = local2[0];
                            QilNode local5 = local2[1];
                            if (local4 == local1)
                            {
                                if (local5.NodeType == QilNodeType.LiteralType)
                                {
                                    XmlQueryType local6 = (XmlQueryType)((QilLiteral)local5).Value!;
                                    if (MatchesContentTest(local6))
                                    {
                                        // PATTERN: [AnnotateFilterContentKind] $outer:(Filter $iter:(For $bind:* ^ (Pattern? $bind {Axis})) (IsType $iter (LiteralType $kind:* ^ (ContentTest? $kind)))) => (AddPattern $outer {FilterContentKind}) ^ (AddArgument $outer {KindTestType} $kind) ^ { }
                                        if (AllowReplace(XmlILOptimization.AnnotateFilterContentKind, local0))
                                        {
                                            OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.FilterContentKind); OptimizerPatterns.Write((QilNode)(local0)).AddArgument(OptimizerPatternArgument.KindTestType, local6);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateFilterAttributeKind])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if (local3.NodeType == QilNodeType.Content)
                    {
                        if (local2.NodeType == QilNodeType.IsType)
                        {
                            QilNode local5 = local2[0];
                            QilNode local6 = local2[1];
                            if (local5 == local1)
                            {
                                if (local6.NodeType == QilNodeType.LiteralType)
                                {
                                    XmlQueryType local7 = (XmlQueryType)((QilLiteral)local6).Value!;
                                    if ((local7) == (XmlQueryTypeFactory.Attribute))
                                    {
                                        // PATTERN: [AnnotateFilterAttributeKind] $outer:(Filter $iter:(For (Content *)) (IsType $iter (LiteralType $kind:*) ^ (Equal? $kind (ConstructType {Attribute})))) => (AddPattern $outer {FilterAttributeKind}) ^ { }
                                        if (AllowReplace(XmlILOptimization.AnnotateFilterAttributeKind, local0))
                                        {
                                            OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.FilterAttributeKind);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // loops

        #region sorting
        protected override QilNode VisitSort(QilLoop local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Sort $i:* ^ (None? (TypeOf $i)) *) => (Nop (First $i))
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop((QilNode)(local1)[0])));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateSort])
            {
                if (local1.NodeType == QilNodeType.For)
                {
                    QilNode local3 = local1[0];
                    if (((local3).XmlType)!.IsSingleton)
                    {
                        // PATTERN: [EliminateSort] (Sort (For $bind:* ^ (Single? (TypeOf $bind))) *) => (Nop $bind)
                        if (AllowReplace(XmlILOptimization.EliminateSort, local0))
                        {
                            return Replace(XmlILOptimization.EliminateSort, local0, VisitNop(f.Nop(local3)));
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitSortKey(QilSortKey local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.NormalizeSortXsltConvert])
            {
                if (local1.NodeType == QilNodeType.XsltConvert)
                {
                    QilNode local3 = local1[0];
                    QilNode local4 = local1[1];
                    if (local4.NodeType == QilNodeType.LiteralType)
                    {
                        XmlQueryType local5 = (XmlQueryType)((QilLiteral)local4).Value!;
                        if ((((local3).XmlType) == (XmlQueryTypeFactory.IntX)) && ((local5) == (XmlQueryTypeFactory.DoubleX)))
                        {
                            // PATTERN: [NormalizeSortXsltConvert] (SortKey (XsltConvert $expr:* (LiteralType $typ:*)) ^ (Equal? (TypeOf $expr) (ConstructType {IntX})) ^ (Equal? $typ (ConstructType {DoubleX})) $coll:*) => (SortKey $expr $coll)
                            if (AllowReplace(XmlILOptimization.NormalizeSortXsltConvert, local0))
                            {
                                return Replace(XmlILOptimization.NormalizeSortXsltConvert, local0, VisitSortKey(f.SortKey(local3, local2)));
                            }
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitDocOrderDistinct(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (DocOrderDistinct $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateDod])
            {
                if (IsDocOrderDistinct(local1))
                {
                    // PATTERN: [EliminateDod] (DocOrderDistinct $arg:* ^ (DocOrderDistinct? $arg)) => $arg
                    if (AllowReplace(XmlILOptimization.EliminateDod, local0))
                    {
                        return Replace(XmlILOptimization.EliminateDod, local0, local1);
                    }
                }
            }
            if (this[XmlILOptimization.FoldNamedDescendants])
            {
                if (local1.NodeType == QilNodeType.Loop)
                {
                    QilNode local2 = local1[0];
                    QilNode local7 = local1[1];
                    if (local2.NodeType == QilNodeType.For)
                    {
                        QilNode local3 = local2[0];
                        if (local3.NodeType == QilNodeType.Loop)
                        {
                            QilNode local4 = local3[0];
                            QilNode local5 = local3[1];
                            if (local5.NodeType == QilNodeType.DescendantOrSelf)
                            {
                                QilNode local6 = local5[0];
                                if (local7.NodeType == QilNodeType.Filter)
                                {
                                    QilNode local8 = local7[0];
                                    QilNode local9 = local7[1];
                                    if (((OptimizerPatterns.Read((QilNode)(local7)).MatchesPattern(OptimizerPatternName.FilterElements)) || (OptimizerPatterns.Read((QilNode)(local7)).MatchesPattern(OptimizerPatternName.FilterContentKind))) && (IsStepPattern(local7, QilNodeType.Content)))
                                    {
                                        // PATTERN: [FoldNamedDescendants] (DocOrderDistinct $path:(Loop $iter1:(For (Loop $iter2:* $ret2:(DescendantOrSelf $input:*))) $ret1:(Filter $iter3:* $cond3:*) ^ (Pattern? $ret1 {FilterElements}) | (Pattern? $ret1 {FilterContentKind}) ^ (StepPattern? $ret1 {Content}))) => (DocOrderDistinct (Loop $iter2 (Filter $iterNew:(For (Descendant $input)) (Subs $cond3 $iter3 $iterNew))))
                                        if (AllowReplace(XmlILOptimization.FoldNamedDescendants, local0))
                                        {
                                            QilNode local10 = VisitFor(f.For(VisitDescendant(f.Descendant(local6))));
                                            return Replace(XmlILOptimization.FoldNamedDescendants, local0, VisitDocOrderDistinct(f.DocOrderDistinct(VisitLoop(f.Loop(local4, VisitFilter(f.Filter(local10, Subs(local9, local8, local10))))))));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.FoldNamedDescendants])
            {
                if (local1.NodeType == QilNodeType.Loop)
                {
                    QilNode local2 = local1[0];
                    QilNode local5 = local1[1];
                    if (local2.NodeType == QilNodeType.For)
                    {
                        QilNode local3 = local2[0];
                        if (local3.NodeType == QilNodeType.DescendantOrSelf)
                        {
                            QilNode local4 = local3[0];
                            if (local5.NodeType == QilNodeType.Filter)
                            {
                                QilNode local6 = local5[0];
                                QilNode local7 = local5[1];
                                if (((OptimizerPatterns.Read((QilNode)(local5)).MatchesPattern(OptimizerPatternName.FilterElements)) || (OptimizerPatterns.Read((QilNode)(local5)).MatchesPattern(OptimizerPatternName.FilterContentKind))) && (IsStepPattern(local5, QilNodeType.Content)))
                                {
                                    // PATTERN: [FoldNamedDescendants] (DocOrderDistinct $path:(Loop $iter1:(For (DescendantOrSelf $input:*)) $ret1:(Filter $iter2:* $cond2:*) ^ (Pattern? $ret1 {FilterElements}) | (Pattern? $ret1 {FilterContentKind}) ^ (StepPattern? $ret1 {Content}))) => (Filter $iterNew:(For (Descendant $input)) (Subs $cond2 $iter2 $iterNew))
                                    if (AllowReplace(XmlILOptimization.FoldNamedDescendants, local0))
                                    {
                                        QilNode local8 = VisitFor(f.For(VisitDescendant(f.Descendant(local4))));
                                        return Replace(XmlILOptimization.FoldNamedDescendants, local0, VisitFilter(f.Filter(local8, Subs(local7, local6, local8))));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.CommuteDodFilter])
            {
                if (local1.NodeType == QilNodeType.Filter)
                {
                    QilNode local2 = local1[0];
                    QilNode local4 = local1[1];
                    if (local2.NodeType == QilNodeType.For)
                    {
                        QilNode local3 = local2[0];
                        if (!OptimizerPatterns.Read(local2).MatchesPattern(OptimizerPatternName.IsPositional))
                        {
                            if (((!(OptimizerPatterns.Read((QilNode)(local1)).MatchesPattern(OptimizerPatternName.FilterElements))) && (!(OptimizerPatterns.Read((QilNode)(local1)).MatchesPattern(OptimizerPatternName.FilterContentKind)))) && (!(OptimizerPatterns.Read((QilNode)(local1)).MatchesPattern(OptimizerPatternName.FilterAttributeKind))))
                            {
                                // PATTERN: [CommuteDodFilter] (DocOrderDistinct $filter:(Filter $iter:(For $bind:*) ^ (NonPositionalIterator? $iter) $cond:*) ^ ~((Pattern? $filter {FilterElements})) ^ ~((Pattern? $filter {FilterContentKind})) ^ ~((Pattern? $filter {FilterAttributeKind}))) => (Filter $iterNew:(For (DocOrderDistinct $bind)) (Subs $cond $iter $iterNew))
                                if (AllowReplace(XmlILOptimization.CommuteDodFilter, local0))
                                {
                                    QilNode local5 = VisitFor(f.For(VisitDocOrderDistinct(f.DocOrderDistinct(local3))));
                                    return Replace(XmlILOptimization.CommuteDodFilter, local0, VisitFilter(f.Filter(local5, Subs(local4, local2, local5))));
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.CommuteDodFilter])
            {
                if (local1.NodeType == QilNodeType.Loop)
                {
                    QilNode local2 = local1[0];
                    QilNode local3 = local1[1];
                    if (local3.NodeType == QilNodeType.Filter)
                    {
                        QilNode local4 = local3[0];
                        QilNode local6 = local3[1];
                        if (local4.NodeType == QilNodeType.For)
                        {
                            QilNode local5 = local4[0];
                            if (!OptimizerPatterns.Read(local4).MatchesPattern(OptimizerPatternName.IsPositional))
                            {
                                if (!(DependsOn(local6, local2)))
                                {
                                    if (((!(OptimizerPatterns.Read((QilNode)(local3)).MatchesPattern(OptimizerPatternName.FilterElements))) && (!(OptimizerPatterns.Read((QilNode)(local3)).MatchesPattern(OptimizerPatternName.FilterContentKind)))) && (!(OptimizerPatterns.Read((QilNode)(local3)).MatchesPattern(OptimizerPatternName.FilterAttributeKind))))
                                    {
                                        // PATTERN: [CommuteDodFilter] (DocOrderDistinct (Loop $iter1:* $ret1:(Filter $iter2:(For $bind2:*) ^ (NonPositionalIterator? $iter2) $cond2:* ^ ~($cond2 >> $iter1)) ^ ~((Pattern? $ret1 {FilterElements})) ^ ~((Pattern? $ret1 {FilterContentKind})) ^ ~((Pattern? $ret1 {FilterAttributeKind})))) => (Filter $iterNew:(For (DocOrderDistinct (Loop $iter1 $bind2))) (Subs $cond2 $iter2 $iterNew))
                                        if (AllowReplace(XmlILOptimization.CommuteDodFilter, local0))
                                        {
                                            QilNode local7 = VisitFor(f.For(VisitDocOrderDistinct(f.DocOrderDistinct(VisitLoop(f.Loop(local2, local5))))));
                                            return Replace(XmlILOptimization.CommuteDodFilter, local0, VisitFilter(f.Filter(local7, Subs(local6, local4, local7))));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.IntroduceDod])
            {
                if (local1.NodeType == QilNodeType.Loop)
                {
                    QilNode local2 = local1[0];
                    QilNode local4 = local1[1];
                    if (local2.NodeType == QilNodeType.For)
                    {
                        QilNode local3 = local2[0];
                        if (!(IsDocOrderDistinct(local3)))
                        {
                            if ((!OptimizerPatterns.Read(local2).MatchesPattern(OptimizerPatternName.IsPositional)) && ((local3).XmlType!.IsSubtypeOf(XmlQueryTypeFactory.NodeNotRtfS)))
                            {
                                if (((!(OptimizerPatterns.Read((QilNode)(local1)).MatchesPattern(OptimizerPatternName.FilterElements))) && (!(OptimizerPatterns.Read((QilNode)(local1)).MatchesPattern(OptimizerPatternName.FilterContentKind)))) && (!(OptimizerPatterns.Read((QilNode)(local1)).MatchesPattern(OptimizerPatternName.FilterAttributeKind))))
                                {
                                    // PATTERN: [IntroduceDod] (DocOrderDistinct $loop:(Loop $iter:(For $bind:* ^ ~((DocOrderDistinct? $bind))) ^ (NonPositionalIterator? $iter) ^ (SubtypeOf? (TypeOf $bind) (ConstructType {NodeNotRtfS})) $ret:*) ^ ~((Pattern? $loop {FilterElements})) ^ ~((Pattern? $loop {FilterContentKind})) ^ ~((Pattern? $loop {FilterAttributeKind}))) => (DocOrderDistinct (Loop $iterNew:(For (DocOrderDistinct $bind)) (Subs $ret $iter $iterNew)))
                                    if (AllowReplace(XmlILOptimization.IntroduceDod, local0))
                                    {
                                        QilNode local5 = VisitFor(f.For(VisitDocOrderDistinct(f.DocOrderDistinct(local3))));
                                        return Replace(XmlILOptimization.IntroduceDod, local0, VisitDocOrderDistinct(f.DocOrderDistinct(VisitLoop(f.Loop(local5, Subs(local4, local2, local5))))));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.IntroducePrecedingDod])
            {
                if (local1.NodeType == QilNodeType.Loop)
                {
                    QilNode local2 = local1[0];
                    QilNode local3 = local1[1];
                    if ((!(IsDocOrderDistinct(local3))) && (IsStepPattern(local3, QilNodeType.PrecedingSibling)))
                    {
                        // PATTERN: [IntroducePrecedingDod] (DocOrderDistinct (Loop $iter:* $ret:* ^ ~((DocOrderDistinct? $ret)) ^ (StepPattern? $ret {PrecedingSibling}))) => (DocOrderDistinct (Loop $iter (DocOrderDistinct $ret)))
                        if (AllowReplace(XmlILOptimization.IntroducePrecedingDod, local0))
                        {
                            return Replace(XmlILOptimization.IntroducePrecedingDod, local0, VisitDocOrderDistinct(f.DocOrderDistinct(VisitLoop(f.Loop(local2, VisitDocOrderDistinct(f.DocOrderDistinct(local3)))))));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateReturnDod])
            {
                if (local1.NodeType == QilNodeType.Loop)
                {
                    QilNode local2 = local1[0];
                    QilNode local3 = local1[1];
                    if (local3.NodeType == QilNodeType.DocOrderDistinct)
                    {
                        QilNode local4 = local3[0];
                        if (!(IsStepPattern(local4, QilNodeType.PrecedingSibling)))
                        {
                            // PATTERN: [EliminateReturnDod] (DocOrderDistinct (Loop $iter:* $ret:(DocOrderDistinct $opnd:*) ^ ~((StepPattern? $opnd {PrecedingSibling})))) => (DocOrderDistinct (Loop $iter $opnd))
                            if (AllowReplace(XmlILOptimization.EliminateReturnDod, local0))
                            {
                                return Replace(XmlILOptimization.EliminateReturnDod, local0, VisitDocOrderDistinct(f.DocOrderDistinct(VisitLoop(f.Loop(local2, local4)))));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateDod])
            {
                // PATTERN: [AnnotateDod] $outer:(DocOrderDistinct $inner:*) => (AddPattern $outer {IsDocOrderDistinct}) ^ (InheritPattern $outer $inner {SameDepth}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateDod, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct); OptimizerPatterns.Inherit((QilNode)(local1), (QilNode)(local0), OptimizerPatternName.SameDepth);
                }
            }
            if (this[XmlILOptimization.AnnotateDodReverse])
            {
                if (AllowDodReverse(local1))
                {
                    // PATTERN: [AnnotateDodReverse] $outer:(DocOrderDistinct $inner:* ^ (DodReverse? $inner)) => (AddPattern $outer {DodReverse}) ^ (AddArgument $outer {DodStep} $inner) ^ { }
                    if (AllowReplace(XmlILOptimization.AnnotateDodReverse, local0))
                    {
                        OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.DodReverse); OptimizerPatterns.Write((QilNode)(local0)).AddArgument(OptimizerPatternArgument.DodStep, local1);
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateJoinAndDod])
            {
                if (local1.NodeType == QilNodeType.Loop)
                {
                    QilNode local2 = local1[0];
                    QilNode local4 = local1[1];
                    if (local2.NodeType == QilNodeType.For)
                    {
                        QilNode local3 = local2[0];
                        if (IsDocOrderDistinct(local3))
                        {
                            if ((AllowJoinAndDod(local4)) && ((local2) == (OptimizerPatterns.Read((QilNode)(local4)).GetArgument(OptimizerPatternArgument.StepInput))))
                            {
                                // PATTERN: [AnnotateJoinAndDod] $outer:(DocOrderDistinct $join:(Loop $iter:(For $bind:*) ^ (DocOrderDistinct? $bind) $ret:* ^ (JoinAndDod? $ret) ^ (Equal? $iter (Argument $ret {StepInput})))) => (AddPattern $outer {JoinAndDod}) ^ (AddArgument $outer {DodStep} $ret) ^ { }
                                if (AllowReplace(XmlILOptimization.AnnotateJoinAndDod, local0))
                                {
                                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.JoinAndDod); OptimizerPatterns.Write((QilNode)(local0)).AddArgument(OptimizerPatternArgument.DodStep, local4);
                                }
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateDodMerge])
            {
                if (local1.NodeType == QilNodeType.Loop)
                {
                    QilNode local3 = local1[1];
                    if (local3.NodeType == QilNodeType.Invoke)
                    {
                        if (IsDocOrderDistinct(local3))
                        {
                            // PATTERN: [AnnotateDodMerge] $outer:(DocOrderDistinct (Loop * $ret:(Invoke * *) ^ (DocOrderDistinct? $ret))) => (AddPattern $outer {DodMerge}) ^ { }
                            if (AllowReplace(XmlILOptimization.AnnotateDodMerge, local0))
                            {
                                OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.DodMerge);
                            }
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // sorting

        #region function definition and invocation
        protected override QilNode VisitFunction(QilFunction local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (((local0).XmlType!.IsSubtypeOf(XmlQueryTypeFactory.NodeS)) && (this[XmlILOptimization.AnnotateIndex1]))
            {
                if (((local1.Count == 2) && (((QilNode)(local1)[0]).XmlType!.IsSubtypeOf(XmlQueryTypeFactory.Node))) && ((((QilNode)(local1)[1]).XmlType) == (XmlQueryTypeFactory.StringX)))
                {
                    if (local2.NodeType == QilNodeType.Filter)
                    {
                        QilNode local5 = local2[0];
                        QilNode local7 = local2[1];
                        if (local5.NodeType == QilNodeType.For)
                        {
                            QilNode local6 = local5[0];
                            if (local7.NodeType == QilNodeType.Not)
                            {
                                QilNode local8 = local7[0];
                                if (local8.NodeType == QilNodeType.IsEmpty)
                                {
                                    QilNode local9 = local8[0];
                                    if (local9.NodeType == QilNodeType.Filter)
                                    {
                                        QilNode local10 = local9[0];
                                        QilNode local12 = local9[1];
                                        if (local10.NodeType == QilNodeType.For)
                                        {
                                            QilNode local11 = local10[0];
                                            if (local12.NodeType == QilNodeType.Eq)
                                            {
                                                QilNode local13 = local12[0];
                                                QilNode local14 = local12[1];
                                                if (local13 == local10)
                                                {
                                                    if (local14.NodeType == QilNodeType.Parameter)
                                                    {
                                                        if ((local14) == ((QilNode)(local1)[1]))
                                                        {
                                                            if (IsDocOrderDistinct(local2))
                                                            {
                                                                // PATTERN: [AnnotateIndex1] $outer:(Function $args:* ^ { {$args}.Count == 2 } ^ (SubtypeOf? (TypeOf (Nth $args 0)) (ConstructType {Node})) ^ (Equal? (TypeOf (Nth $args 1)) (ConstructType {StringX})) $def:(Filter $iterNodes:(For $bindingNodes:*) (Not (IsEmpty (Filter $iterKeys:(For $bindingKeys:*) (Eq $iterKeys $keyParam:(Parameter * * *) ^ (Equal? $keyParam (Nth $args 1))))))) ^ (DocOrderDistinct? $def) * *) ^ (SubtypeOf? (TypeOf $outer) (ConstructType {NodeS})) => { ... }
                                                                if (AllowReplace(XmlILOptimization.AnnotateIndex1, local0))
                                                                {
                                                                    // The following conditions must be true for this pattern to match:
                                                                    //   1. The function must have exactly two arguments
                                                                    //   2. The type of the first argument must be a subtype of Node
                                                                    //   3. The type of the second argument must be String
                                                                    //   4. The return type must be a subtype of Node*
                                                                    //   5. The function must return nodes in document order
                                                                    //   6. Every reference to $args[0] (context document) must be wrapped in an (Root *) function
                                                                    //   7. $keyParam cannot be used with the $bindingNodes and $bindingKeys expressions

                                                                    EqualityIndexVisitor visitor = new EqualityIndexVisitor();
                                                                    if (visitor.Scan(local6, local1[0], local14) && visitor.Scan(local11, local1[0], local14))
                                                                    {
                                                                        // All conditions were true, so annotate Filter with the EqualityIndex pattern
                                                                        OptimizerPatterns patt = OptimizerPatterns.Write(local2);
                                                                        patt.AddPattern(OptimizerPatternName.EqualityIndex);
                                                                        patt.AddArgument(OptimizerPatternArgument.IndexedNodes, local5);
                                                                        patt.AddArgument(OptimizerPatternArgument.KeyExpression, local11);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (((local0).XmlType.IsSubtypeOf(XmlQueryTypeFactory.NodeS)) && (this[XmlILOptimization.AnnotateIndex2]))
            {
                if (((local1.Count == 2) && ((((QilNode)(local1)[0]).XmlType) == (XmlQueryTypeFactory.Node))) && ((((QilNode)(local1)[1]).XmlType) == (XmlQueryTypeFactory.StringX)))
                {
                    if (local2.NodeType == QilNodeType.Filter)
                    {
                        QilNode local5 = local2[0];
                        QilNode local7 = local2[1];
                        if (local5.NodeType == QilNodeType.For)
                        {
                            QilNode local6 = local5[0];
                            if (local7.NodeType == QilNodeType.Eq)
                            {
                                QilNode local8 = local7[0];
                                QilNode local9 = local7[1];
                                if (local9.NodeType == QilNodeType.Parameter)
                                {
                                    if ((local9) == ((QilNode)(local1)[1]))
                                    {
                                        if (IsDocOrderDistinct(local2))
                                        {
                                            // PATTERN: [AnnotateIndex2] $outer:(Function $args:* ^ { {$args}.Count == 2 } ^ (Equal? (TypeOf (Nth $args 0)) (ConstructType {Node})) ^ (Equal? (TypeOf (Nth $args 1)) (ConstructType {StringX})) $def:(Filter $iterNodes:(For $bindingNodes:*) (Eq $keyExpr:* $keyParam:(Parameter * * *) ^ (Equal? $keyParam (Nth $args 1)))) ^ (DocOrderDistinct? $def) * *) ^ (SubtypeOf? (TypeOf $outer) (ConstructType {NodeS})) => { ... }
                                            if (AllowReplace(XmlILOptimization.AnnotateIndex2, local0))
                                            {
                                                // Same as EqualityIndex1, except that each nodes has at most one key value

                                                EqualityIndexVisitor visitor = new EqualityIndexVisitor();
                                                if (visitor.Scan(local6, local1[0], local9) && visitor.Scan(local8, local1[0], local9))
                                                {
                                                    // All conditions were true, so annotate Filter with the EqualityIndex pattern
                                                    OptimizerPatterns patt = OptimizerPatterns.Write(local2);
                                                    patt.AddPattern(OptimizerPatternName.EqualityIndex);
                                                    patt.AddArgument(OptimizerPatternArgument.IndexedNodes, local5);
                                                    patt.AddArgument(OptimizerPatternArgument.KeyExpression, local8);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitInvoke(QilInvoke local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.NormalizeInvokeEmpty])
            {
                if (local1.NodeType == QilNodeType.Function)
                {
                    QilNode local4 = local1[1];
                    if (local4.NodeType == QilNodeType.Sequence)
                    {
                        if ((local4).Count == (0))
                        {
                            // PATTERN: [NormalizeInvokeEmpty] (Invoke (Function * $seq:(Sequence) ^ (Count? $seq 0) * *) *) => (Sequence)
                            if (AllowReplace(XmlILOptimization.NormalizeInvokeEmpty, local0))
                            {
                                return Replace(XmlILOptimization.NormalizeInvokeEmpty, local0, VisitSequence(f.Sequence()));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateTrackCallers])
            {
                // PATTERN: [AnnotateTrackCallers] $caller:(Invoke $func:* *) => (AddCaller $func $caller) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateTrackCallers, local0))
                {
                    XmlILConstructInfo.Write(local1).CallersInfo.Add(XmlILConstructInfo.Write(local0));
                }
            }
            if (this[XmlILOptimization.AnnotateInvoke])
            {
                if (local1.NodeType == QilNodeType.Function)
                {
                    QilNode local4 = local1[1];
                    // PATTERN: [AnnotateInvoke] $outer:(Invoke (Function * $defn:* * *) *) => (InheritPattern $outer $defn {IsDocOrderDistinct}) ^ (InheritPattern $outer $defn {SameDepth}) ^ { }
                    if (AllowReplace(XmlILOptimization.AnnotateInvoke, local0))
                    {
                        OptimizerPatterns.Inherit((QilNode)(local4), (QilNode)(local0), OptimizerPatternName.IsDocOrderDistinct); OptimizerPatterns.Inherit((QilNode)(local4), (QilNode)(local0), OptimizerPatternName.SameDepth);
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // function definition and invocation

        #region XML navigation
        protected override QilNode VisitContent(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Content $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateContent])
            {
                // PATTERN: [AnnotateContent] $outer:(Content $input:*) => (AddStepPattern $outer $input) ^ (AddPattern $outer {Axis}) ^ (AddPattern $outer {IsDocOrderDistinct}) ^ (AddPattern $outer {SameDepth}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateContent, local0))
                {
                    AddStepPattern((QilNode)(local0), (QilNode)(local1)); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.SameDepth);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitAttribute(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Attribute $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Attribute * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateAttribute])
            {
                // PATTERN: [AnnotateAttribute] $outer:(Attribute $input:* *) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ (AddPattern $outer {IsDocOrderDistinct}) ^ (AddPattern $outer {SameDepth}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateAttribute, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1)); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.SameDepth);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitParent(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Parent $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateParent])
            {
                // PATTERN: [AnnotateParent] $outer:(Parent $input:*) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ (AddPattern $outer {IsDocOrderDistinct}) ^ (AddPattern $outer {SameDepth}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateParent, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1)); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.SameDepth);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitRoot(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Root $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateRoot])
            {
                // PATTERN: [AnnotateRoot] $outer:(Root $input:*) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ (AddPattern $outer {IsDocOrderDistinct}) ^ (AddPattern $outer {SameDepth}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateRoot, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1)); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.SameDepth);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitDescendant(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Descendant $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateDescendant])
            {
                // PATTERN: [AnnotateDescendant] $outer:(Descendant $input:*) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ (AddPattern $outer {IsDocOrderDistinct}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateDescendant, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1)); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitDescendantOrSelf(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (DescendantOrSelf $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateDescendantSelf])
            {
                // PATTERN: [AnnotateDescendantSelf] $outer:(DescendantOrSelf $input:*) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ (AddPattern $outer {IsDocOrderDistinct}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateDescendantSelf, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1)); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitAncestor(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Ancestor $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateAncestor])
            {
                // PATTERN: [AnnotateAncestor] $outer:(Ancestor $input:*) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateAncestor, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1));
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitAncestorOrSelf(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (AncestorOrSelf $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateAncestorSelf])
            {
                // PATTERN: [AnnotateAncestorSelf] $outer:(AncestorOrSelf $input:*) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateAncestorSelf, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1));
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitPreceding(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Preceding $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotatePreceding])
            {
                // PATTERN: [AnnotatePreceding] $outer:(Preceding $input:*) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotatePreceding, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1));
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitFollowingSibling(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (FollowingSibling $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateFollowingSibling])
            {
                // PATTERN: [AnnotateFollowingSibling] $outer:(FollowingSibling $input:*) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ (AddPattern $outer {IsDocOrderDistinct}) ^ (AddPattern $outer {SameDepth}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateFollowingSibling, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1)); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.SameDepth);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitPrecedingSibling(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (PrecedingSibling $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotatePrecedingSibling])
            {
                // PATTERN: [AnnotatePrecedingSibling] $outer:(PrecedingSibling $input:*) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ (AddPattern $outer {SameDepth}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotatePrecedingSibling, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1)); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.SameDepth);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitNodeRange(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (NodeRange $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (NodeRange * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateNodeRange])
            {
                // PATTERN: [AnnotateNodeRange] $outer:(NodeRange $start:* *) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $start) ^ (AddPattern $outer {IsDocOrderDistinct}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateNodeRange, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1)); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitDeref(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Deref $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (Deref * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // XML navigation

        #region XML construction
        protected override QilNode VisitElementCtor(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (ElementCtor $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (ElementCtor * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $ctor:(ElementCtor * $content:*) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    // The analysis occasionally makes small changes to the content of constructors, which is
                    // why the result of Analyze is assigned to $ctor.Right.
                    local0.Right = _elemAnalyzer.Analyze(local0, local2)!;
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitAttributeCtor(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (AttributeCtor $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (AttributeCtor * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $ctor:(AttributeCtor * $content:*) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    local0.Right = _contentAnalyzer.Analyze(local0, local2)!;
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitCommentCtor(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (CommentCtor $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $ctor:(CommentCtor $content:*) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    local0.Child = _contentAnalyzer.Analyze(local0, local1)!;
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitPICtor(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (PICtor $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (PICtor * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $ctor:(PICtor * $content:*) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    local0.Right = _contentAnalyzer.Analyze(local0, local2)!;
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitTextCtor(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (TextCtor $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $ctor:(TextCtor *) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    _contentAnalyzer.Analyze(local0, null);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitRawTextCtor(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (RawTextCtor $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $ctor:(RawTextCtor *) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    _contentAnalyzer.Analyze(local0, null);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitDocumentCtor(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (DocumentCtor $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $ctor:(DocumentCtor $content:*) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    local0.Child = _contentAnalyzer.Analyze(local0, local1)!;
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitNamespaceDecl(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (NamespaceDecl $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (NamespaceDecl * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if ((XmlILConstructInfo.Read(local0).IsNamespaceInScope) && (this[XmlILOptimization.EliminateNamespaceDecl]))
            {
                // PATTERN: [EliminateNamespaceDecl] $nmsp:(NamespaceDecl * *) ^ (NamespaceInScope? $nmsp) => (Sequence)
                if (AllowReplace(XmlILOptimization.EliminateNamespaceDecl, local0))
                {
                    return Replace(XmlILOptimization.EliminateNamespaceDecl, local0, VisitSequence(f.Sequence()));
                }
            }
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $ctor:(NamespaceDecl * *) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    _contentAnalyzer.Analyze(local0, null);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitRtfCtor(QilBinary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (RtfCtor $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $ctor:(RtfCtor $content:* *) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    local0.Left = _contentAnalyzer.Analyze(local0, local1)!;
                }
            }
            if (this[XmlILOptimization.AnnotateSingleTextRtf])
            {
                if (local1.NodeType == QilNodeType.TextCtor)
                {
                    QilNode local3 = local1[0];
                    // PATTERN: [AnnotateSingleTextRtf] $outer:(RtfCtor $ctor:(TextCtor $text:*) *) => (AddPattern $outer {SingleTextRtf}) ^ (AddArgument $outer {RtfText} $text) ^ { ... }
                    if (AllowReplace(XmlILOptimization.AnnotateSingleTextRtf, local0))
                    {
                        OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.SingleTextRtf); OptimizerPatterns.Write((QilNode)(local0)).AddArgument(OptimizerPatternArgument.RtfText, local3);
                        // In this case, Rtf will be pushed onto the stack rather than pushed to the writer
                        XmlILConstructInfo.Write(local0).PullFromIteratorFirst = true;
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // XML construction

        #region Node properties
        protected override QilNode VisitNameOf(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (NameOf $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitLocalNameOf(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (LocalNameOf $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitNamespaceUriOf(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (NamespaceUriOf $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitPrefixOf(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (PrefixOf $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // Node properties

        #region Type operators
        protected override QilNode VisitTypeAssert(QilTargetType local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (TypeAssert $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateTypeAssert])
            {
                if (local2.NodeType == QilNodeType.LiteralType)
                {
                    XmlQueryType local3 = (XmlQueryType)((QilLiteral)local2).Value!;
                    if ((local1).XmlType!.NeverSubtypeOf(local3))
                    {
                        // PATTERN: [EliminateTypeAssert] (TypeAssert $opnd:* (LiteralType $typ:*) ^ (NeverSubtypeOf? (TypeOf $opnd) $typ)) => (Error (LiteralString ""))
                        if (AllowReplace(XmlILOptimization.EliminateTypeAssert, local0))
                        {
                            return Replace(XmlILOptimization.EliminateTypeAssert, local0, VisitError(f.Error(VisitLiteralString(f.LiteralString(string.Empty)))));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateTypeAssert])
            {
                if (local2.NodeType == QilNodeType.LiteralType)
                {
                    XmlQueryType local3 = (XmlQueryType)((QilLiteral)local2).Value!;
                    if ((local1).XmlType!.Prime.NeverSubtypeOf(local3.Prime))
                    {
                        // PATTERN: [EliminateTypeAssert] (TypeAssert $opnd:* (LiteralType $typ:*) ^ (NeverSubtypeOf? (Prime (TypeOf $opnd)) (Prime $typ))) => (Conditional (IsEmpty $opnd) (Sequence) (Error (LiteralString "")))
                        if (AllowReplace(XmlILOptimization.EliminateTypeAssert, local0))
                        {
                            return Replace(XmlILOptimization.EliminateTypeAssert, local0, VisitConditional(f.Conditional(VisitIsEmpty(f.IsEmpty(local1)), VisitSequence(f.Sequence()), VisitError(f.Error(VisitLiteralString(f.LiteralString(string.Empty)))))));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateTypeAssertOptional])
            {
                if (local2.NodeType == QilNodeType.LiteralType)
                {
                    XmlQueryType local3 = (XmlQueryType)((QilLiteral)local2).Value!;
                    if ((local1).XmlType!.IsSubtypeOf(local3))
                    {
                        // PATTERN: [EliminateTypeAssertOptional] (TypeAssert $opnd:* (LiteralType $base:*) ^ (SubtypeOf? (TypeOf $opnd) $base)) => $opnd
                        if (AllowReplace(XmlILOptimization.EliminateTypeAssertOptional, local0))
                        {
                            return Replace(XmlILOptimization.EliminateTypeAssertOptional, local0, local1);
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitIsType(QilTargetType local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (IsType $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIsType])
            {
                if (!OptimizerPatterns.Read(local1).MatchesPattern(OptimizerPatternName.MaybeSideEffects))
                {
                    if (local2.NodeType == QilNodeType.LiteralType)
                    {
                        XmlQueryType local3 = (XmlQueryType)((QilLiteral)local2).Value!;
                        if ((local1).XmlType!.IsSubtypeOf(local3))
                        {
                            // PATTERN: [EliminateIsType] (IsType $opnd:* ^ (NoSideEffects? $opnd) (LiteralType $base:*) ^ (SubtypeOf? (TypeOf $opnd) $base)) => (True)
                            if (AllowReplace(XmlILOptimization.EliminateIsType, local0))
                            {
                                return Replace(XmlILOptimization.EliminateIsType, local0, VisitTrue(f.True()));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIsType])
            {
                if (!OptimizerPatterns.Read(local1).MatchesPattern(OptimizerPatternName.MaybeSideEffects))
                {
                    if (local2.NodeType == QilNodeType.LiteralType)
                    {
                        XmlQueryType local3 = (XmlQueryType)((QilLiteral)local2).Value!;
                        if ((local1).XmlType!.NeverSubtypeOf(local3))
                        {
                            // PATTERN: [EliminateIsType] (IsType $opnd:* ^ (NoSideEffects? $opnd) (LiteralType $typ:*) ^ (NeverSubtypeOf? (TypeOf $opnd) $typ)) => (False)
                            if (AllowReplace(XmlILOptimization.EliminateIsType, local0))
                            {
                                return Replace(XmlILOptimization.EliminateIsType, local0, VisitFalse(f.False()));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIsType])
            {
                if (local2.NodeType == QilNodeType.LiteralType)
                {
                    XmlQueryType local3 = (XmlQueryType)((QilLiteral)local2).Value!;
                    if ((local1).XmlType!.Prime.NeverSubtypeOf(local3.Prime))
                    {
                        // PATTERN: [EliminateIsType] (IsType $opnd:* (LiteralType $typ:*) ^ (NeverSubtypeOf? (Prime (TypeOf $opnd)) (Prime $typ))) => (IsEmpty $opnd)
                        if (AllowReplace(XmlILOptimization.EliminateIsType, local0))
                        {
                            return Replace(XmlILOptimization.EliminateIsType, local0, VisitIsEmpty(f.IsEmpty(local1)));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIsType])
            {
                if (!(!OptimizerPatterns.Read(local1).MatchesPattern(OptimizerPatternName.MaybeSideEffects)))
                {
                    if (local2.NodeType == QilNodeType.LiteralType)
                    {
                        XmlQueryType local3 = (XmlQueryType)((QilLiteral)local2).Value!;
                        if ((local1).XmlType!.IsSubtypeOf(local3))
                        {
                            // PATTERN: [EliminateIsType] (IsType $opnd:* ^ ~((NoSideEffects? $opnd)) (LiteralType $base:*) ^ (SubtypeOf? (TypeOf $opnd) $base)) => (Loop (Let $opnd) (True))
                            if (AllowReplace(XmlILOptimization.EliminateIsType, local0))
                            {
                                return Replace(XmlILOptimization.EliminateIsType, local0, VisitLoop(f.Loop(VisitLet(f.Let(local1)), VisitTrue(f.True()))));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIsType])
            {
                if (!(!OptimizerPatterns.Read(local1).MatchesPattern(OptimizerPatternName.MaybeSideEffects)))
                {
                    if (local2.NodeType == QilNodeType.LiteralType)
                    {
                        XmlQueryType local3 = (XmlQueryType)((QilLiteral)local2).Value!;
                        if ((local1).XmlType!.NeverSubtypeOf(local3))
                        {
                            // PATTERN: [EliminateIsType] (IsType $opnd:* ^ ~((NoSideEffects? $opnd)) (LiteralType $typ:*) ^ (NeverSubtypeOf? (TypeOf $opnd) $typ)) => (Loop (Let $opnd) (False))
                            if (AllowReplace(XmlILOptimization.EliminateIsType, local0))
                            {
                                return Replace(XmlILOptimization.EliminateIsType, local0, VisitLoop(f.Loop(VisitLet(f.Let(local1)), VisitFalse(f.False()))));
                            }
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitIsEmpty(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (IsEmpty $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIsEmpty])
            {
                if (local1.NodeType == QilNodeType.Sequence)
                {
                    if ((local1).Count == (0))
                    {
                        // PATTERN: [EliminateIsEmpty] (IsEmpty $expr:(Sequence) ^ (Count? $expr 0)) => (True)
                        if (AllowReplace(XmlILOptimization.EliminateIsEmpty, local0))
                        {
                            return Replace(XmlILOptimization.EliminateIsEmpty, local0, VisitTrue(f.True()));
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIsEmpty])
            {
                if ((!((local1).XmlType)!.MaybeEmpty) && (!OptimizerPatterns.Read(local1).MatchesPattern(OptimizerPatternName.MaybeSideEffects)))
                {
                    // PATTERN: [EliminateIsEmpty] (IsEmpty $expr:* ^ (NonEmpty? (TypeOf $expr)) ^ (NoSideEffects? $expr)) => (False)
                    if (AllowReplace(XmlILOptimization.EliminateIsEmpty, local0))
                    {
                        return Replace(XmlILOptimization.EliminateIsEmpty, local0, VisitFalse(f.False()));
                    }
                }
            }
            if (this[XmlILOptimization.EliminateIsEmpty])
            {
                if (!((local1).XmlType)!.MaybeEmpty)
                {
                    // PATTERN: [EliminateIsEmpty] (IsEmpty $expr:* ^ (NonEmpty? (TypeOf $expr))) => (Loop (Let $expr) (False))
                    if (AllowReplace(XmlILOptimization.EliminateIsEmpty, local0))
                    {
                        return Replace(XmlILOptimization.EliminateIsEmpty, local0, VisitLoop(f.Loop(VisitLet(f.Let(local1)), VisitFalse(f.False()))));
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // Type operators

        #region XPath operators
        protected override QilNode VisitXPathNodeValue(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (XPathNodeValue $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitXPathFollowing(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (XPathFollowing $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateXPathFollowing])
            {
                // PATTERN: [AnnotateXPathFollowing] $outer:(XPathFollowing $input:*) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ (AddPattern $outer {IsDocOrderDistinct}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateXPathFollowing, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1)); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitXPathPreceding(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (XPathPreceding $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateXPathPreceding])
            {
                // PATTERN: [AnnotateXPathPreceding] $outer:(XPathPreceding $input:*) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateXPathPreceding, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1));
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitXPathNamespace(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (XPathNamespace $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateNamespace])
            {
                // PATTERN: [AnnotateNamespace] $outer:(XPathNamespace $input:*) => (AddPattern $outer {Axis}) ^ (AddStepPattern $outer $input) ^ (AddPattern $outer {IsDocOrderDistinct}) ^ (AddPattern $outer {SameDepth}) ^ { }
                if (AllowReplace(XmlILOptimization.AnnotateNamespace, local0))
                {
                    OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.Axis); AddStepPattern((QilNode)(local0), (QilNode)(local1)); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.IsDocOrderDistinct); OptimizerPatterns.Write((QilNode)(local0)).AddPattern(OptimizerPatternName.SameDepth);
                }
            }
            return NoReplace(local0);
        }

        #endregion // XPath operators

        #region XSLT
        protected override QilNode VisitXsltGenerateId(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (XsltGenerateId $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitXsltCopy(QilBinary local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (XsltCopy $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local2).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (XsltCopy * $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local2)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $ctor:(XsltCopy * $content:*) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    local0.Right = _contentAnalyzer.Analyze(local0, local2)!;
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitXsltCopyOf(QilUnary local0)
        {
            QilNode local1 = local0[0];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (XsltCopyOf $x:* ^ (None? (TypeOf $x))) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.AnnotateConstruction])
            {
                // PATTERN: [AnnotateConstruction] $ctor:(XsltCopyOf *) => { ... }
                if (AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
                {
                    _contentAnalyzer.Analyze(local0, null);
                }
            }
            return NoReplace(local0);
        }

        protected override QilNode VisitXsltConvert(QilTargetType local0)
        {
            QilNode local1 = local0[0];
            QilNode local2 = local0[1];
            if (this[XmlILOptimization.FoldNone])
            {
                if ((object?)((local1).XmlType) == (object)XmlQueryTypeFactory.None)
                {
                    // PATTERN: [FoldNone] (XsltConvert $x:* ^ (None? (TypeOf $x)) *) => (Nop $x)
                    if (AllowReplace(XmlILOptimization.FoldNone, local0))
                    {
                        return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(local1)));
                    }
                }
            }
            if (this[XmlILOptimization.FoldXsltConvertLiteral])
            {
                if (IsLiteral((local1)))
                {
                    if (local2.NodeType == QilNodeType.LiteralType)
                    {
                        XmlQueryType local3 = (XmlQueryType)((QilLiteral)local2).Value!;
                        if (CanFoldXsltConvert(local1, local3))
                        {
                            // PATTERN: [FoldXsltConvertLiteral] (XsltConvert $lit:* ^ (Literal? $lit) (LiteralType $typ:*) ^ (CanFoldXsltConvert? $lit $typ)) => (FoldXsltConvert $lit $typ)
                            if (AllowReplace(XmlILOptimization.FoldXsltConvertLiteral, local0))
                            {
                                return Replace(XmlILOptimization.FoldXsltConvertLiteral, local0, FoldXsltConvert(local1, local3));
                            }
                        }
                    }
                }
            }
            if (this[XmlILOptimization.EliminateXsltConvert])
            {
                if (local2.NodeType == QilNodeType.LiteralType)
                {
                    XmlQueryType local3 = (XmlQueryType)((QilLiteral)local2).Value!;
                    if (((local1).XmlType) == (local3))
                    {
                        // PATTERN: [EliminateXsltConvert] (XsltConvert $expr:* (LiteralType $typ:*) ^ (Equal? (TypeOf $expr) $typ)) => $expr
                        if (AllowReplace(XmlILOptimization.EliminateXsltConvert, local0))
                        {
                            return Replace(XmlILOptimization.EliminateXsltConvert, local0, local1);
                        }
                    }
                }
            }
            return NoReplace(local0);
        }

        #endregion // XSLT

        #endregion // AUTOGENERATED

        /// <summary>
        /// Selectively enable/disable optimizations
        /// </summary>
        private bool this[XmlILOptimization ann]
        {
            get { return Patterns.IsSet((int)ann); }
        }

        private sealed class NodeCounter : QilVisitor
        {
            private QilNode? target;
            private int cnt;

            /// <summary>
            /// Returns number of occurrences of "target" node within the subtree of "expr".
            /// </summary>
            public int Count(QilNode expr, QilNode target)
            {
                this.cnt = 0;
                this.target = target;
                Visit(expr);
                return this.cnt;
            }

            protected override QilNode Visit(QilNode n)
            {
                if (n == null)
                    return null!;

                if (n == this.target)
                    this.cnt++;

                return VisitChildren(n);
            }

            protected override QilNode VisitReference(QilNode n)
            {
                if (n == this.target)
                    this.cnt++;

                return n;
            }
        }

        private class NodeFinder : QilVisitor
        {
            protected bool result;
            protected QilNode? target, parent;

            /// <summary>
            /// Returns true if "target" node exists within the subtree of "expr".
            /// </summary>
            public bool Find(QilNode expr, QilNode target)
            {
                this.result = false;
                this.target = target;
                this.parent = null;
                VisitAssumeReference(expr);
                return this.result;
            }

            /// <summary>
            /// Recursively visit, searching for target.  If/when found, call OnFound() method.
            /// </summary>
            protected override QilNode Visit(QilNode expr)
            {
                if (!this.result)
                {
                    if (expr == this.target)
                        this.result = OnFound(expr);

                    if (!this.result)
                    {
                        QilNode? parentOld = this.parent;
                        this.parent = expr;
                        VisitChildren(expr);
                        this.parent = parentOld;
                    }
                }

                return expr;
            }

            /// <summary>
            /// Determine whether target is a reference.
            /// </summary>
            protected override QilNode VisitReference(QilNode expr)
            {
                if (expr == this.target)
                    this.result = OnFound(expr);

                return expr;
            }

            /// <summary>
            /// By default, just return true.
            /// </summary>
            protected virtual bool OnFound(QilNode expr)
            {
                return true;
            }
        }

        private sealed class PositionOfFinder : NodeFinder
        {
            /// <summary>
            /// Return true only if parent node type is PositionOf.
            /// </summary>
            protected override bool OnFound(QilNode expr)
            {
                return this.parent != null && this.parent.NodeType == QilNodeType.PositionOf;
            }
        }

        private sealed class EqualityIndexVisitor : QilVisitor
        {
            private bool result;
            private QilNode? ctxt, key;

            /// <summary>
            /// Returns true if the subtree of "expr" meets the following requirements:
            ///   1. Does not contain a reference to "key"
            ///   2. Every reference to "ctxt" is wrapped by a QilNodeType.Root node
            /// </summary>
            public bool Scan(QilNode expr, QilNode ctxt, QilNode key)
            {
                this.result = true;
                this.ctxt = ctxt;
                this.key = key;
                Visit(expr);
                return this.result;
            }

            /// <summary>
            /// Recursively visit, looking for references to "key" and "ctxt".
            /// </summary>
            protected override QilNode VisitReference(QilNode expr)
            {
                if (this.result)
                {
                    if (expr == this.key || expr == this.ctxt)
                    {
                        this.result = false;
                        return expr;
                    }
                }
                return expr;
            }

            /// <summary>
            /// If Root wraps a reference to "ctxt", then don't visit "ctxt" and continue scan.
            /// </summary>
            protected override QilNode VisitRoot(QilUnary root)
            {
                if (root.Child == this.ctxt)
                    return root;

                return VisitChildren(root);
            }
        }

        /// <summary>
        /// Returns true if any operator within the "expr" subtree references "target".
        /// </summary>
        private static bool DependsOn(QilNode expr, QilNode target)
        {
            return new NodeFinder().Find(expr, target);
        }

        /// <summary>
        /// Returns true if there is no PositionOf operator within the "expr" subtree that references iterator "iter".
        /// </summary>
        private static bool NonPositional(QilNode expr, QilNode iter)
        {
            return !(new PositionOfFinder().Find(expr, iter));
        }

        /// <summary>
        /// Scans "expr" subtree, looking for "refOld" references and replacing them with "refNew" references.
        /// </summary>
        private QilNode Subs(QilNode expr, QilNode refOld, QilNode refNew)
        {
            QilNode result;

            _subs.AddSubstitutionPair(refOld, refNew);
            if (expr is QilReference)
                result = VisitReference(expr);
            else
                result = Visit(expr);
            _subs.RemoveLastSubstitutionPair();

            return result;
        }

        /// <summary>
        /// True if the specified iterator is a global variable Let iterator.
        /// </summary>
        private bool IsGlobalVariable(QilIterator iter)
        {
            return _qil.GlobalVariableList.Contains(iter);
        }

        /// <summary>
        /// True if the specified node is a global variable or parameter.
        /// </summary>
        private bool IsGlobalValue(QilNode nd)
        {
            if (nd.NodeType == QilNodeType.Let)
                return _qil.GlobalVariableList.Contains(nd);

            if (nd.NodeType == QilNodeType.Parameter)
                return _qil.GlobalParameterList.Contains(nd);

            return false;
        }

        /// <summary>
        /// Return true if "typ" is xs:decimal=, xs:integer=, xs:int=, xs:double=, or xs:float=.
        /// </summary>
        private static bool IsPrimitiveNumeric(XmlQueryType? typ)
        {
            if (typ == XmlQueryTypeFactory.IntX) return true;
            if (typ == XmlQueryTypeFactory.IntegerX) return true;
            if (typ == XmlQueryTypeFactory.DecimalX) return true;
            if (typ == XmlQueryTypeFactory.FloatX) return true;
            if (typ == XmlQueryTypeFactory.DoubleX) return true;

            return false;
        }

        /// <summary>
        /// Returns true if "typ" matches one of the XPath content node tests: *, text(), comment(), pi(), or node().
        /// </summary>
        private static bool MatchesContentTest(XmlQueryType typ)
        {
            if (typ == XmlQueryTypeFactory.Element) return true;
            if (typ == XmlQueryTypeFactory.Text) return true;
            if (typ == XmlQueryTypeFactory.Comment) return true;
            if (typ == XmlQueryTypeFactory.PI) return true;
            if (typ == XmlQueryTypeFactory.Content) return true;

            return false;
        }

        /// <summary>
        /// True if the specified expression constructs one or more nodes using QilExpression constructor operators.
        /// This information is used to determine whether the results of a function should be streamed to a writer
        /// rather than cached.
        /// </summary>
        private bool IsConstructedExpression(QilNode nd)
        {
            QilTernary ndCond;

            // In debug mode, all functions should return void (streamed to writer), so that call stack
            // consistently shows caller's line number
            if (_qil.IsDebug)
                return true;

            if (nd.XmlType!.IsNode)
            {
                switch (nd.NodeType)
                {
                    case QilNodeType.ElementCtor:
                    case QilNodeType.AttributeCtor:
                    case QilNodeType.CommentCtor:
                    case QilNodeType.PICtor:
                    case QilNodeType.TextCtor:
                    case QilNodeType.RawTextCtor:
                    case QilNodeType.DocumentCtor:
                    case QilNodeType.NamespaceDecl:
                    case QilNodeType.XsltCopy:
                    case QilNodeType.XsltCopyOf:
                    case QilNodeType.Choice:
                        return true;

                    case QilNodeType.Loop:
                        // Return true if the return expression is constructed
                        return IsConstructedExpression(((QilLoop)nd).Body);

                    case QilNodeType.Sequence:
                        // Return true if the list is empty or at least one expression in the list is constructed
                        if (nd.Count == 0)
                            return true;

                        foreach (QilNode ndItem in nd)
                        {
                            if (IsConstructedExpression(ndItem))
                                return true;
                        }
                        break;

                    case QilNodeType.Conditional:
                        // Return true if either left and right branches of the conditional are constructed
                        ndCond = (QilTernary)nd;
                        return IsConstructedExpression(ndCond.Center) || IsConstructedExpression(ndCond.Right);

                    case QilNodeType.Invoke:
                        // Return true if the function might return nodes
                        return !((QilInvoke)nd).Function.XmlType!.IsAtomicValue;
                }
            }

            return false;
        }

        /// <summary>
        /// True if the specified expression is a literal value.
        /// </summary>
        private static bool IsLiteral(QilNode nd)
        {
            switch (nd.NodeType)
            {
                case QilNodeType.True:
                case QilNodeType.False:
                case QilNodeType.LiteralString:
                case QilNodeType.LiteralInt32:
                case QilNodeType.LiteralInt64:
                case QilNodeType.LiteralDouble:
                case QilNodeType.LiteralDecimal:
                case QilNodeType.LiteralQName:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Return true if all children of "nd" are constant.
        /// </summary>
        private static bool AreLiteralArgs(QilNode nd)
        {
            foreach (QilNode child in nd)
                if (!IsLiteral(child))
                    return false;

            return true;
        }

        /// <summary>
        /// Extract the value of a literal.
        /// </summary>
        private static object ExtractLiteralValue(QilNode nd)
        {
            if (nd.NodeType == QilNodeType.True)
                return true;
            else if (nd.NodeType == QilNodeType.False)
                return false;
            else if (nd.NodeType == QilNodeType.LiteralQName)
                return nd;

            Debug.Assert(nd is QilLiteral, "All literals except True, False, and QName must use QilLiteral");
            return ((QilLiteral)nd).Value!;
        }

        /// <summary>
        /// Return true if "nd" has a child of type Sequence.
        /// </summary>
        private static bool HasNestedSequence(QilNode nd)
        {
            foreach (QilNode child in nd)
            {
                if (child.NodeType == QilNodeType.Sequence)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// True if the JoinAndDod pattern is allowed to match the specified node.
        /// </summary>
        private static bool AllowJoinAndDod(QilNode nd)
        {
            OptimizerPatterns patt = OptimizerPatterns.Read(nd);

            // AllowJoinAndDod if this pattern is the descendant, descendant-or-self, content, preceding, following, or
            // following-sibling axis, filtered by either an element name or a node kind test.
            if (patt.MatchesPattern(OptimizerPatternName.FilterElements) || patt.MatchesPattern(OptimizerPatternName.FilterContentKind))
            {
                if (IsStepPattern(patt, QilNodeType.DescendantOrSelf) || IsStepPattern(patt, QilNodeType.Descendant) ||
                    IsStepPattern(patt, QilNodeType.Content) || IsStepPattern(patt, QilNodeType.XPathPreceding) ||
                    IsStepPattern(patt, QilNodeType.XPathFollowing) || IsStepPattern(patt, QilNodeType.FollowingSibling))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// True if the DodReverse pattern is allowed to match the specified node.
        /// </summary>
        private static bool AllowDodReverse(QilNode nd)
        {
            OptimizerPatterns patt = OptimizerPatterns.Read(nd);

            // AllowDodReverse if this pattern is the ancestor, ancestor-or-self, preceding, or preceding-sibling axis,
            // filtered by either an element name or a node kind test.
            if (patt.MatchesPattern(OptimizerPatternName.Axis) ||
                patt.MatchesPattern(OptimizerPatternName.FilterElements) ||
                patt.MatchesPattern(OptimizerPatternName.FilterContentKind))
            {
                if (IsStepPattern(patt, QilNodeType.Ancestor) || IsStepPattern(patt, QilNodeType.AncestorOrSelf) ||
                    IsStepPattern(patt, QilNodeType.XPathPreceding) || IsStepPattern(patt, QilNodeType.PrecedingSibling))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Return true if XsltConvert applied to a Literal can be folded (i.e. the XsltConvert eliminated).
        /// </summary>
        private bool CanFoldXsltConvert(QilNode ndLiteral, XmlQueryType typTarget)
        {
            // Attempt to fold--on failure, an unfolded XsltConvert node will be returned
            return FoldXsltConvert(ndLiteral, typTarget).NodeType != QilNodeType.XsltConvert;
        }

        /// <summary>
        /// Return true if XsltConvert applied to a Literal can be folded (i.e. the XsltConvert eliminated), without
        /// any loss of information.
        /// </summary>
        private bool CanFoldXsltConvertNonLossy(QilNode ndLiteral, XmlQueryType typTarget)
        {
            QilNode ndDest;

            // Fold conversion to target type; if conversion cannot be folded, a XsltConvert node is returned
            ndDest = FoldXsltConvert(ndLiteral, typTarget);
            if (ndDest.NodeType == QilNodeType.XsltConvert)
                return false;

            // Convert back to source type; if conversion cannot be folded, a XsltConvert node is returned
            ndDest = FoldXsltConvert(ndDest, ndLiteral.XmlType!);
            if (ndDest.NodeType == QilNodeType.XsltConvert)
                return false;

            // If original value is the same as the round-tripped value, then conversion is non-lossy
            return ExtractLiteralValue(ndLiteral).Equals(ExtractLiteralValue(ndDest));
        }

        /// <summary>
        /// Fold a XsltConvert applied to a Literal into another Literal.  If the fold results in some kind of
        /// conversion error, or if the QilExpression cannot represent the result as a Literal, return an unfolded
        /// XsltConvert expression.
        /// </summary>
        private QilNode FoldXsltConvert(QilNode ndLiteral, XmlQueryType typTarget)
        {
            try
            {
                if (typTarget.IsAtomicValue)
                {
                    // Convert the literal to an XmlAtomicValue
                    XmlAtomicValue value = new XmlAtomicValue(ndLiteral.XmlType!.SchemaType, ExtractLiteralValue(ndLiteral));
                    value = XsltConvert.ConvertToType(value, typTarget);

                    if (typTarget == XmlQueryTypeFactory.StringX)
                        return this.f.LiteralString(value.Value);
                    else if (typTarget == XmlQueryTypeFactory.IntX)
                        return this.f.LiteralInt32(value.ValueAsInt);
                    else if (typTarget == XmlQueryTypeFactory.IntegerX)
                        return this.f.LiteralInt64(value.ValueAsLong);
                    else if (typTarget == XmlQueryTypeFactory.DecimalX)
                        return this.f.LiteralDecimal((decimal)value.ValueAs(XsltConvert.DecimalType));
                    else if (typTarget == XmlQueryTypeFactory.DoubleX)
                        return this.f.LiteralDouble(value.ValueAsDouble);
                    else if (typTarget == XmlQueryTypeFactory.BooleanX)
                        return value.ValueAsBoolean ? this.f.True() : this.f.False();
                }
            }
            catch (OverflowException) { }
            catch (FormatException) { }

            // Conversion error or QilExpression cannot represent resulting literal
            return this.f.XsltConvert(ndLiteral, typTarget);
        }

        /// <summary>
        /// Compute the arithmetic operation "opType" over two literal operands and return the result as a QilLiteral.
        /// In the case of an overflow or divide by zero exception, return the unfolded result.
        /// </summary>
        private QilNode FoldComparison(QilNodeType opType, QilNode left, QilNode right)
        {
            object litLeft, litRight;
            int cmp;
            Debug.Assert(left.XmlType == right.XmlType, $"Comparison is not defined between {left.XmlType} and {right.XmlType}");

            // Extract objects that represent each literal value
            litLeft = ExtractLiteralValue(left);
            litRight = ExtractLiteralValue(right);

            if (left.NodeType == QilNodeType.LiteralDouble)
            {
                // Equals and CompareTo do not handle NaN correctly
                if (double.IsNaN((double)litLeft) || double.IsNaN((double)litRight))
                    return (opType == QilNodeType.Ne) ? f.True() : f.False();
            }

            if (opType == QilNodeType.Eq)
                return litLeft.Equals(litRight) ? f.True() : f.False();

            if (opType == QilNodeType.Ne)
                return litLeft.Equals(litRight) ? f.False() : f.True();

            if (left.NodeType == QilNodeType.LiteralString)
            {
                // CompareTo does not use Ordinal comparison
                cmp = string.CompareOrdinal((string)litLeft, (string)litRight);
            }
            else
            {
                cmp = ((IComparable)litLeft).CompareTo(litRight);
            }

            switch (opType)
            {
                case QilNodeType.Gt: return cmp > 0 ? f.True() : f.False();
                case QilNodeType.Ge: return cmp >= 0 ? f.True() : f.False();
                case QilNodeType.Lt: return cmp < 0 ? f.True() : f.False();
                case QilNodeType.Le: return cmp <= 0 ? f.True() : f.False();
            }

            Debug.Fail($"Cannot fold this comparison operation: {opType}");
            return null;
        }

        /// <summary>
        /// Return true if arithmetic operation "opType" can be computed over two literal operands without causing
        /// an overflow or divide by zero exception.
        /// </summary>
        private bool CanFoldArithmetic(QilNodeType opType, QilLiteral left, QilLiteral right)
        {
            return (FoldArithmetic(opType, left, right) is QilLiteral);
        }

        /// <summary>
        /// Compute the arithmetic operation "opType" over two literal operands and return the result as a QilLiteral.
        /// Arithmetic operations are always checked; in the case of an overflow or divide by zero exception, return
        /// the unfolded result.
        /// </summary>
        private QilNode FoldArithmetic(QilNodeType opType, QilLiteral left, QilLiteral right)
        {
            Debug.Assert(left.NodeType == right.NodeType);

            // Catch any overflow or divide by zero exceptions
            try
            {
                checked
                {
                    switch (left.NodeType)
                    {
                        case QilNodeType.LiteralInt32:
                            {
                                int intLeft = left;
                                int intRight = right;

                                switch (opType)
                                {
                                    case QilNodeType.Add: return f.LiteralInt32(intLeft + intRight);
                                    case QilNodeType.Subtract: return f.LiteralInt32(intLeft - intRight);
                                    case QilNodeType.Multiply: return f.LiteralInt32(intLeft * intRight);
                                    case QilNodeType.Divide: return f.LiteralInt32(intLeft / intRight);
                                    case QilNodeType.Modulo: return f.LiteralInt32(intLeft % intRight);
                                }
                                break;
                            }

                        case QilNodeType.LiteralInt64:
                            {
                                long lngLeft = left;
                                long lngRight = right;

                                switch (opType)
                                {
                                    case QilNodeType.Add: return f.LiteralInt64(lngLeft + lngRight);
                                    case QilNodeType.Subtract: return f.LiteralInt64(lngLeft - lngRight);
                                    case QilNodeType.Multiply: return f.LiteralInt64(lngLeft * lngRight);
                                    case QilNodeType.Divide: return f.LiteralInt64(lngLeft / lngRight);
                                    case QilNodeType.Modulo: return f.LiteralInt64(lngLeft % lngRight);
                                }
                                break;
                            }

                        case QilNodeType.LiteralDecimal:
                            {
                                decimal lngLeft = left;
                                decimal lngRight = right;

                                switch (opType)
                                {
                                    case QilNodeType.Add: return f.LiteralDecimal(lngLeft + lngRight);
                                    case QilNodeType.Subtract: return f.LiteralDecimal(lngLeft - lngRight);
                                    case QilNodeType.Multiply: return f.LiteralDecimal(lngLeft * lngRight);
                                    case QilNodeType.Divide: return f.LiteralDecimal(lngLeft / lngRight);
                                    case QilNodeType.Modulo: return f.LiteralDecimal(lngLeft % lngRight);
                                }
                                break;
                            }

                        case QilNodeType.LiteralDouble:
                            {
                                double lngLeft = left;
                                double lngRight = right;

                                switch (opType)
                                {
                                    case QilNodeType.Add: return f.LiteralDouble(lngLeft + lngRight);
                                    case QilNodeType.Subtract: return f.LiteralDouble(lngLeft - lngRight);
                                    case QilNodeType.Multiply: return f.LiteralDouble(lngLeft * lngRight);
                                    case QilNodeType.Divide: return f.LiteralDouble(lngLeft / lngRight);
                                    case QilNodeType.Modulo: return f.LiteralDouble(lngLeft % lngRight);
                                }
                                break;
                            }
                    }
                }
            }
            catch (OverflowException)
            {
            }
            catch (DivideByZeroException)
            {
            }

            // An error occurred, so don't fold operationo
            switch (opType)
            {
                case QilNodeType.Add: return f.Add(left, right);
                case QilNodeType.Subtract: return f.Subtract(left, right);
                case QilNodeType.Multiply: return f.Multiply(left, right);
                case QilNodeType.Divide: return f.Divide(left, right);
                case QilNodeType.Modulo: return f.Modulo(left, right);
            }

            Debug.Fail($"Cannot fold this arithmetic operation: {opType}");
            return null;
        }

        /// <summary>
        /// Mark the specified node as matching the Step pattern and set the step node and step input arguments.
        /// </summary>
        private static void AddStepPattern(QilNode nd, QilNode input)
        {
            OptimizerPatterns patt = OptimizerPatterns.Write(nd);
            patt.AddPattern(OptimizerPatternName.Step);
            patt.AddArgument(OptimizerPatternArgument.StepNode, nd);
            patt.AddArgument(OptimizerPatternArgument.StepInput, input);
        }

        /// <summary>
        /// Return true if "nd" matches the Step pattern and the StepType argument is equal to "stepType".
        /// </summary>
        private static bool IsDocOrderDistinct(QilNode nd)
        {
            return OptimizerPatterns.Read(nd).MatchesPattern(OptimizerPatternName.IsDocOrderDistinct);
        }

        /// <summary>
        /// Return true if "nd" matches the Step pattern and the StepType argument is equal to "stepType".
        /// </summary>
        private static bool IsStepPattern(QilNode nd, QilNodeType stepType)
        {
            return IsStepPattern(OptimizerPatterns.Read(nd), stepType);
        }

        /// <summary>
        /// Return true if "patt" matches the Step pattern and the StepType argument is equal to "stepType".
        /// </summary>
        private static bool IsStepPattern(OptimizerPatterns patt, QilNodeType stepType)
        {
            return patt.MatchesPattern(OptimizerPatternName.Step) && ((QilNode)patt.GetArgument(OptimizerPatternArgument.StepNode)).NodeType == stepType;
        }

        /// <summary>
        /// Remove unused global functions, variables, or parameters from the list.
        /// </summary>
        private static void EliminateUnusedGlobals(IList<QilNode> globals)
        {
            int newIdx = 0;

            for (int oldIdx = 0; oldIdx < globals.Count; oldIdx++)
            {
                QilNode nd = globals[oldIdx];
                bool isUsed;

                if (nd.NodeType == QilNodeType.Function)
                {
                    // Keep a function if it has at least one caller
                    isUsed = XmlILConstructInfo.Read(nd).CallersInfo.Count != 0;
                }
                else
                {
                    Debug.Assert(nd.NodeType == QilNodeType.Let || nd.NodeType == QilNodeType.Parameter, "Unexpected type of a global");

                    // Keep a variable or parameter if it was referenced at least once or may have side effects
                    OptimizerPatterns optPatt = OptimizerPatterns.Read(nd);
                    isUsed = optPatt.MatchesPattern(OptimizerPatternName.IsReferenced) || optPatt.MatchesPattern(OptimizerPatternName.MaybeSideEffects);
                }

                if (isUsed)
                {
                    if (newIdx < oldIdx)
                        globals[newIdx] = globals[oldIdx];

                    newIdx++;
                }
            }

            // Removing elements from the end makes the algorithm linear
            for (int oldIdx = globals.Count - 1; oldIdx >= newIdx; oldIdx--)
            {
                globals.RemoveAt(oldIdx);
            }
        }
    }
}
