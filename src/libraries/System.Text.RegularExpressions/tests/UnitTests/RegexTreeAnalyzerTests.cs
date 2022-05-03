// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;
using Xunit.Sdk;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexTreeAnalyzerTests
    {
        [Fact]
        public void SimpleString()
        {
            (RegexTree tree, AnalysisResults analysis) = Analyze("abc");

            RegexNode rootCapture = AssertNode(analysis, tree.Root, RegexNodeKind.Capture, atomicByAncestor: true, backtracks: false, captures: true, inLoop: false);
            RegexNode abc = AssertNode(analysis, rootCapture.Child(0), RegexNodeKind.Multi, atomicByAncestor: true, backtracks: false, captures: false, inLoop: false);
        }

        [Fact]
        public void AlternationWithCaptures()
        {
            (RegexTree tree, AnalysisResults analysis) = Analyze("abc|d(e)f|(ghi)");

            RegexNode rootCapture = AssertNode(analysis, tree.Root, RegexNodeKind.Capture, atomicByAncestor: true, backtracks: false, captures: true, inLoop: false);
            RegexNode implicitAtomic = AssertNode(analysis, rootCapture.Child(0), RegexNodeKind.Atomic, atomicByAncestor: true, backtracks: false, captures: true, inLoop: false);
            RegexNode alternation = AssertNode(analysis, implicitAtomic.Child(0), RegexNodeKind.Alternate, atomicByAncestor: true, backtracks: false, captures: true, inLoop: false);

            RegexNode abc = AssertNode(analysis, alternation.Child(0), RegexNodeKind.Multi, atomicByAncestor: true, backtracks: false, captures: false, inLoop: false);
            RegexNode def = AssertNode(analysis, alternation.Child(1), RegexNodeKind.Concatenate, atomicByAncestor: true, backtracks: false, captures: true, inLoop: false);
            RegexNode ghiCapture = AssertNode(analysis, alternation.Child(2), RegexNodeKind.Capture, atomicByAncestor: true, backtracks: false, captures: true, inLoop: false);

            RegexNode d = AssertNode(analysis, def.Child(0), RegexNodeKind.One, atomicByAncestor: false, backtracks: false, captures: false, inLoop: false);
            RegexNode eCapture = AssertNode(analysis, def.Child(1), RegexNodeKind.Capture, atomicByAncestor: false, backtracks: false, captures: true, inLoop: false);
            RegexNode f = AssertNode(analysis, def.Child(2), RegexNodeKind.One, atomicByAncestor: true, backtracks: false, captures: false, inLoop: false);

            RegexNode e = AssertNode(analysis, eCapture.Child(0), RegexNodeKind.One, atomicByAncestor: false, backtracks: false, captures: false, inLoop: false);

            RegexNode ghi = AssertNode(analysis, ghiCapture.Child(0), RegexNodeKind.Multi, atomicByAncestor: true, backtracks: false, captures: false, inLoop: false);
        }

        [Fact]
        public void LoopsReducedWithAutoAtomic()
        {
            (RegexTree tree, AnalysisResults analysis) = Analyze("a*(b*)c*");

            RegexNode rootCapture = AssertNode(analysis, tree.Root, RegexNodeKind.Capture, atomicByAncestor: true, backtracks: false, captures: true, inLoop: false);
            RegexNode concat = AssertNode(analysis, rootCapture.Child(0), RegexNodeKind.Concatenate, atomicByAncestor: true, backtracks: false, captures: true, inLoop: false);

            RegexNode aStar = AssertNode(analysis, concat.Child(0), RegexNodeKind.Oneloopatomic, atomicByAncestor: false, backtracks: false, captures: false, inLoop: false);
            RegexNode implicitBumpalong = AssertNode(analysis, concat.Child(1), RegexNodeKind.UpdateBumpalong, atomicByAncestor: false, backtracks: false, captures: false, inLoop: false);
            RegexNode bStarCapture = AssertNode(analysis, concat.Child(2), RegexNodeKind.Capture, atomicByAncestor: false, backtracks: false, captures: true, inLoop: false);
            RegexNode cStar = AssertNode(analysis, concat.Child(3), RegexNodeKind.Oneloopatomic, atomicByAncestor: true, backtracks: false, captures: false, inLoop: false);

            RegexNode bStar = AssertNode(analysis, bStarCapture.Child(0), RegexNodeKind.Oneloopatomic, atomicByAncestor: false, backtracks: false, captures: false, inLoop: false);
        }

        [Fact]
        public void AtomicGroupAroundBacktracking()
        {
            (RegexTree tree, AnalysisResults analysis) = Analyze("[ab]*(?>[bc]*[cd])[ef]");

            RegexNode rootCapture = AssertNode(analysis, tree.Root, RegexNodeKind.Capture, atomicByAncestor: true, backtracks: true, captures: true, inLoop: false);
            RegexNode rootConcat = AssertNode(analysis, rootCapture.Child(0), RegexNodeKind.Concatenate, atomicByAncestor: true, backtracks: true, captures: false, inLoop: false);

            RegexNode abStar = AssertNode(analysis, rootConcat.Child(0), RegexNodeKind.Setloop, atomicByAncestor: false, backtracks: true, captures: false, inLoop: false);
            RegexNode implicitBumpalong = AssertNode(analysis, rootConcat.Child(1), RegexNodeKind.UpdateBumpalong, atomicByAncestor: false, backtracks: false, captures: false, inLoop: false);
            RegexNode atomic = AssertNode(analysis, rootConcat.Child(2), RegexNodeKind.Atomic, atomicByAncestor: false, backtracks: false, captures: false, inLoop: false);
            RegexNode ef = AssertNode(analysis, rootConcat.Child(3), RegexNodeKind.Set, atomicByAncestor: true, backtracks: false, captures: false, inLoop: false);

            RegexNode atomicConcat = AssertNode(analysis, atomic.Child(0), RegexNodeKind.Concatenate, atomicByAncestor: true, backtracks: true, captures: false, inLoop: false);

            RegexNode bcStar = AssertNode(analysis, atomicConcat.Child(0), RegexNodeKind.Setloop, atomicByAncestor: false, backtracks: true, captures: false, inLoop: false);
            RegexNode cd = AssertNode(analysis, atomicConcat.Child(1), RegexNodeKind.Set, atomicByAncestor: true, backtracks: false, captures: false, inLoop: false);
        }

        [Fact]
        public void LoopsAroundVariousConstructs()
        {
            (RegexTree tree, AnalysisResults analysis) = Analyze("(abc|def)*(?:[ab]*[cd])+?d");

            RegexNode rootCapture = AssertNode(analysis, tree.Root, RegexNodeKind.Capture, atomicByAncestor: true, backtracks: true, captures: true, inLoop: false);
            RegexNode rootConcat = AssertNode(analysis, rootCapture.Child(0), RegexNodeKind.Concatenate, atomicByAncestor: true, backtracks: true, captures: true, inLoop: false);

            RegexNode loop = AssertNode(analysis, rootConcat.Child(0), RegexNodeKind.Loop, atomicByAncestor: false, backtracks: true, captures: true, inLoop: false);
            RegexNode loopCapture = AssertNode(analysis, loop.Child(0), RegexNodeKind.Capture, atomicByAncestor: false, backtracks: true, captures: true, inLoop: true);
            RegexNode alternation = AssertNode(analysis, loopCapture.Child(0), RegexNodeKind.Alternate, atomicByAncestor: false, backtracks: true, captures: false, inLoop: true);
            RegexNode abc = AssertNode(analysis, alternation.Child(0), RegexNodeKind.Multi, atomicByAncestor: false, backtracks: false, captures: false, inLoop: true);
            RegexNode def = AssertNode(analysis, alternation.Child(1), RegexNodeKind.Multi, atomicByAncestor: false, backtracks: false, captures: false, inLoop: true);

            RegexNode lazyLoop = AssertNode(analysis, rootConcat.Child(1), RegexNodeKind.Lazyloop, atomicByAncestor: false, backtracks: true, captures: false, inLoop: false);
            RegexNode lazyLoopConcat = AssertNode(analysis, lazyLoop.Child(0), RegexNodeKind.Concatenate, atomicByAncestor: false, backtracks: false, captures: false, inLoop: true);
            RegexNode abStar = AssertNode(analysis, lazyLoopConcat.Child(0), RegexNodeKind.Setloopatomic, atomicByAncestor: false, backtracks: false, captures: false, inLoop: true);
            RegexNode cd = AssertNode(analysis, lazyLoopConcat.Child(1), RegexNodeKind.Set, atomicByAncestor: false, backtracks: false, captures: false, inLoop: true);

            RegexNode d = AssertNode(analysis, rootConcat.Child(2), RegexNodeKind.One, atomicByAncestor: true, backtracks: false, captures: false, inLoop: false);
        }

        private static (RegexTree Tree, AnalysisResults Analysis) Analyze(string pattern)
        {
            RegexTree tree = RegexParser.Parse(pattern, RegexOptions.None, CultureInfo.InvariantCulture);
            return (tree, RegexTreeAnalyzer.Analyze(tree));
        }

        private static RegexNode AssertNode(AnalysisResults analysis, RegexNode node, RegexNodeKind kind, bool atomicByAncestor, bool backtracks, bool captures, bool inLoop)
        {
            Assert.Equal(kind, node.Kind);

            if (atomicByAncestor != analysis.IsAtomicByAncestor(node))
            {
                throw new XunitException($"Expected atomicByParent == {atomicByAncestor} for {node.Kind}, got {!atomicByAncestor}");
            }

            if (backtracks != analysis.MayBacktrack(node))
            {
                throw new XunitException($"Expected backtracks == {backtracks} for {node.Kind}, got {!backtracks}");
            }

            if (captures != analysis.MayContainCapture(node))
            {
                throw new XunitException($"Expected captures == {captures} for {node.Kind}, got {!captures}");
            }

            if (inLoop != analysis.IsInLoop(node))
            {
                throw new XunitException($"Expected inLoop == {inLoop} for {node.Kind}, got {!inLoop}");
            }

            return node;
        }
    }
}
