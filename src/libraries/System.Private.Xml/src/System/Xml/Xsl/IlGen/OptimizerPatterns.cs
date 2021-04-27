// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.IlGen
{
    internal enum OptimizerPatternName
    {
        None,
        DodReverse,                         // (Dod $reverse-axis:*)
        EqualityIndex,                      // ILGen will build an equality index when this pattern is recognized
        FilterAttributeKind,                // (Filter $iter:(Content *) (IsType $iter Attribute))
        FilterContentKind,                  // (Filter $iter:* (IsType $iter $kind:*))
        FilterElements,                     // (Filter $iter:* (And (IsType $iter Element) (NameOf $iter (LiteralQName * * *))))
        IsDocOrderDistinct,                 // True if the annotated expression always returns nodes in document order, with no duplicates
        IsPositional,                       // True if the annotated iterator should track current position during iteration
        JoinAndDod,                         // (Dod (Loop $path1:* $path2:*)), where $path2.ContextNode = $path1
        MaxPosition,                        // True if the position range of the annoted iterator or length expression has a maximum
        SameDepth,                          // True if the annotated expression always returns nodes at the same depth in the tree
        Step,                               // True if the annotated expression returns nodes from one of the simple axis operators, or from a union of Content operators
        SingleTextRtf,                      // (RtfCtor (TextCtor *) *)
        Axis,                               // (AnyAxis *)
        MaybeSideEffects,                   // True if annotated expression might have side effects
        TailCall,                           // (Invoke * *) True if invocation can be compiled as using .tailcall
        DodMerge,                           // (Dod (Loop * (Invoke * *))), where invoked function returns nodes in document order
        IsReferenced,                       // True if the annotated global iterator is referenced at least once
    }

    internal enum OptimizerPatternArgument
    {
        StepNode = 0,                       // Step, QilNode: The QilNode of the inner step expression (Content, DescendantOrSelf, XPathFollowing, Union, etc.)
        StepInput = 1,                      // Step, QilNode: The expression from which navigation begins

        ElementQName = 2,                   // FilterElements, QilLiteral: All but elements of this QName are filtered by FilterElements expression

        KindTestType = 2,                   // FilterContentKind, XmlType: All but nodes of this XmlType are filtered by FilterContentKind expression

        IndexedNodes = 0,                   // EqualityIndex, QilNode: Expression that returns the nodes to be indexed
        KeyExpression = 1,                  // EqualityIndex, QilNode: Expression that returns the keys for the index

        DodStep = 2,                        // JoinAndDod | DodReverse, QilNode: Last step in a JoinAndDod expression, or only step in DodReverse expression

        MaxPosition = 2,                    // MaxPosition, int: Maximum position of the annotated iterator or length expression

        RtfText = 2,                        // SingleTextRtf, QilNode: Expression that constructs the text of the simple text Rtf
    }

    /// <summary>
    /// As the Qil graph is traversed, patterns are identified.  Subtrees that match these patterns are
    /// annotated with this class, which identifies the matching patterns and their arguments.
    /// </summary>
    internal sealed class OptimizerPatterns : IQilAnnotation
    {
        private static readonly int s_patternCount = Enum.GetValues(typeof(OptimizerPatternName)).Length;

        private int _patterns;               // Set of patterns that the annotated Qil node and its subtree matches
        private bool _isReadOnly;            // True if setters are disabled in the case of singleton OptimizerPatterns
        private object? _arg0, _arg1, _arg2;    // Arguments to the matching patterns

        private static volatile OptimizerPatterns? s_zeroOrOneDefault;
        private static volatile OptimizerPatterns? s_maybeManyDefault;
        private static volatile OptimizerPatterns? s_dodDefault;

        /// <summary>
        /// Get OptimizerPatterns annotation for the specified node.  Lazily create if necessary.
        /// </summary>
        public static OptimizerPatterns Read(QilNode nd)
        {
            XmlILAnnotation? ann = nd.Annotation as XmlILAnnotation;
            OptimizerPatterns? optPatt = (ann != null) ? ann.Patterns : null;

            if (optPatt == null)
            {
                if (!nd.XmlType!.MaybeMany)
                {
                    // Expressions with ZeroOrOne cardinality should always report IsDocOrderDistinct and NoContainedNodes
                    if (s_zeroOrOneDefault == null)
                    {
                        optPatt = new OptimizerPatterns();
                        optPatt.AddPattern(OptimizerPatternName.IsDocOrderDistinct);
                        optPatt.AddPattern(OptimizerPatternName.SameDepth);
                        optPatt._isReadOnly = true;

                        s_zeroOrOneDefault = optPatt;
                    }
                    else
                    {
                        optPatt = s_zeroOrOneDefault;
                    }
                }
                else if (nd.XmlType.IsDod)
                {
                    if (s_dodDefault == null)
                    {
                        optPatt = new OptimizerPatterns();
                        optPatt.AddPattern(OptimizerPatternName.IsDocOrderDistinct);
                        optPatt._isReadOnly = true;

                        s_dodDefault = optPatt;
                    }
                    else
                    {
                        optPatt = s_dodDefault;
                    }
                }
                else
                {
                    if (s_maybeManyDefault == null)
                    {
                        optPatt = new OptimizerPatterns();
                        optPatt._isReadOnly = true;

                        s_maybeManyDefault = optPatt;
                    }
                    else
                    {
                        optPatt = s_maybeManyDefault;
                    }
                }
            }

            return optPatt;
        }

        /// <summary>
        /// Create and initialize OptimizerPatterns annotation for the specified node.
        /// </summary>
        public static OptimizerPatterns Write(QilNode nd)
        {
            XmlILAnnotation ann = XmlILAnnotation.Write(nd);
            OptimizerPatterns? optPatt = ann.Patterns;

            if (optPatt == null || optPatt._isReadOnly)
            {
                optPatt = new OptimizerPatterns();
                ann.Patterns = optPatt;

                if (!nd.XmlType!.MaybeMany)
                {
                    optPatt.AddPattern(OptimizerPatternName.IsDocOrderDistinct);
                    optPatt.AddPattern(OptimizerPatternName.SameDepth);
                }
                else if (nd.XmlType.IsDod)
                {
                    optPatt.AddPattern(OptimizerPatternName.IsDocOrderDistinct);
                }
            }

            return optPatt;
        }

        /// <summary>
        /// Create and initialize OptimizerPatterns annotation for the specified node.
        /// </summary>
        public static void Inherit(QilNode ndSrc, QilNode ndDst, OptimizerPatternName pattern)
        {
            OptimizerPatterns annSrc = OptimizerPatterns.Read(ndSrc);

            if (annSrc.MatchesPattern(pattern))
            {
                OptimizerPatterns annDst = OptimizerPatterns.Write(ndDst);
                annDst.AddPattern(pattern);

                // Inherit pattern arguments
                switch (pattern)
                {
                    case OptimizerPatternName.Step:
                        annDst.AddArgument(OptimizerPatternArgument.StepNode, annSrc.GetArgument(OptimizerPatternArgument.StepNode));
                        annDst.AddArgument(OptimizerPatternArgument.StepInput, annSrc.GetArgument(OptimizerPatternArgument.StepInput));
                        break;

                    case OptimizerPatternName.FilterElements:
                        annDst.AddArgument(OptimizerPatternArgument.ElementQName, annSrc.GetArgument(OptimizerPatternArgument.ElementQName));
                        break;

                    case OptimizerPatternName.FilterContentKind:
                        annDst.AddArgument(OptimizerPatternArgument.KindTestType, annSrc.GetArgument(OptimizerPatternArgument.KindTestType));
                        break;

                    case OptimizerPatternName.EqualityIndex:
                        annDst.AddArgument(OptimizerPatternArgument.IndexedNodes, annSrc.GetArgument(OptimizerPatternArgument.IndexedNodes));
                        annDst.AddArgument(OptimizerPatternArgument.KeyExpression, annSrc.GetArgument(OptimizerPatternArgument.KeyExpression));
                        break;

                    case OptimizerPatternName.DodReverse:
                    case OptimizerPatternName.JoinAndDod:
                        annDst.AddArgument(OptimizerPatternArgument.DodStep, annSrc.GetArgument(OptimizerPatternArgument.DodStep));
                        break;

                    case OptimizerPatternName.MaxPosition:
                        annDst.AddArgument(OptimizerPatternArgument.MaxPosition, annSrc.GetArgument(OptimizerPatternArgument.MaxPosition));
                        break;

                    case OptimizerPatternName.SingleTextRtf:
                        annDst.AddArgument(OptimizerPatternArgument.RtfText, annSrc.GetArgument(OptimizerPatternArgument.RtfText));
                        break;
                }
            }
        }

        /// <summary>
        /// Add an argument to one of the matching patterns.
        /// </summary>
        public void AddArgument(OptimizerPatternArgument argId, object arg)
        {
            Debug.Assert(!_isReadOnly, "This OptimizerPatterns instance is read-only.");

            switch ((int)argId)
            {
                case 0: _arg0 = arg; break;
                case 1: _arg1 = arg; break;
                case 2: _arg2 = arg; break;
                default:
                    Debug.Fail("Cannot handle more than 2 arguments.");
                    break;
            }
        }

        /// <summary>
        /// Get an argument of one of the matching patterns.
        /// </summary>
        public object GetArgument(OptimizerPatternArgument argNum)
        {
            object? arg = null;

            switch ((int)argNum)
            {
                case 0: arg = _arg0; break;
                case 1: arg = _arg1; break;
                case 2: arg = _arg2; break;
            }

            Debug.Assert(arg != null, "There is no '" + argNum + "' argument.");
            return arg;
        }

        /// <summary>
        /// Add a pattern to the list of patterns that the annotated node matches.
        /// </summary>
        public void AddPattern(OptimizerPatternName pattern)
        {
            Debug.Assert(Enum.IsDefined(typeof(OptimizerPatternName), pattern));
            Debug.Assert((int)pattern < 32);
            Debug.Assert(!_isReadOnly, "This OptimizerPatterns instance is read-only.");
            _patterns |= (1 << (int)pattern);
        }

        /// <summary>
        /// Return true if the annotated node matches the specified pattern.
        /// </summary>
        public bool MatchesPattern(OptimizerPatternName pattern)
        {
            Debug.Assert(Enum.IsDefined(typeof(OptimizerPatternName), pattern));
            return (_patterns & (1 << (int)pattern)) != 0;
        }

        /// <summary>
        /// Return name of this annotation.
        /// </summary>
        public string Name
        {
            get { return "Patterns"; }
        }

        /// <summary>
        /// Return string representation of this annotation.
        /// </summary>
        public override string ToString()
        {
            string s = "";

            for (int pattNum = 0; pattNum < s_patternCount; pattNum++)
            {
                if (MatchesPattern((OptimizerPatternName)pattNum))
                {
                    if (s.Length != 0)
                        s += ", ";

                    s += ((OptimizerPatternName)pattNum).ToString();
                }
            }

            return s;
        }
    }
}
