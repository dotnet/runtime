// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt
{
    using T = XmlQueryTypeFactory;

    #region Comments
    /*  The MatcherBuilder class implements xsl:apply-templates/imports logic, grouping patterns
     *  first by node type, then by node name of their last StepPattern. For example, suppose that
     *  we are given the following patterns, listed in order of decreasing generalized priority
     *  (3-tuple (import precedence, priority, order number in the stylesheet)):
     *
     *                          Generalized
     *      Pattern             Priority
     *      -------------------------------
     *      pattern7/foo        7
     *      pattern6/bar        6
     *      pattern5/*          5
     *      pattern4/node()     4
     *      pattern3/foo        3
     *      pattern2/bar        2
     *      pattern1/*          1
     *      pattern0/node()     0
     *      -------------------------------
     *
     *  The following code will be generated to find a first match amongst them ($it denotes a test
     *  node, and =~ denotes the match operation):
     *
     *  (: First check patterns which match only one fixed node type. :)
     *  (: Switch on the node type of the test node.                  :)
     *  let $pt :=
     *      typeswitch($it)
     *      case element() return
     *          (: First check patterns which match only one fixed node name. :)
     *          (: Switch on the node name of the test node.                  :)
     *          let $pe :=
     *              typeswitch($it)
     *              (: One case for every unique element name occurred in patterns :)
     *              case element(foo) return
     *                  if ($it =~ pattern7/foo) then 7 else
     *                  if ($it =~ pattern3/foo) then 3 else
     *                  -1                 (: -1 is used as "no match found" value :)
     *              case element(bar) return
     *                  if ($it =~ pattern6/bar) then 6 else
     *                  if ($it =~ pattern2/bar) then 2 else
     *                  -1
     *              default return
     *                  -1
     *
     *          (: Now check patterns which may match multiple node names, taking :)
     *          (: into account the priority of the previously found match        :)
     *          return
     *              if ($pe > 5)           then $pe else
     *              if ($it =~ pattern5/*) then   5 else
     *              if ($pe > 1)           then $pe else
     *              if ($it =~ pattern1/*) then   1 else
     *              if ($pe > -1)          then $pe else
     *              -1
     *
     *      (: In the general case check all other node types occurred in patterns :)
     *      (: case attribute()...              :)
     *      (: case text()...                   :)
     *      (: case document-node()...          :)
     *      (: case comment()...                :)
     *      (: case processing-instruction()... :)
     *
     *      default return
     *          -1
     *
     *  (: Now check patterns which may match multiple node types, taking :)
     *  (: into account the priority of the previously found match        :)
     *  return
     *      if ($pt > 4)         then $pt else
     *      if (pattern4/node()) then   4 else
     *      if ($pt > 0)         then $pt else
     *      if (pattern0/node()) then   0 else
     *      if ($pt > -1)        then $pt else
     *      -1
     */
    #endregion

    internal sealed class TemplateMatch
    {
        public static readonly TemplateMatchComparer Comparer = new TemplateMatchComparer();

        private readonly Template _template;
        private readonly double _priority;
        private XmlNodeKindFlags _nodeKind;
        private QilName? _qname;
        private readonly QilIterator _iterator;
        private QilNode? _condition;    // null means f.True()

        public XmlNodeKindFlags NodeKind
        {
            get { return _nodeKind; }
        }

        public QilName? QName
        {
            get { return _qname; }
        }

        public QilIterator Iterator
        {
            get { return _iterator; }
        }

        public QilNode? Condition
        {
            get { return _condition; }
        }

        public QilFunction? TemplateFunction
        {
            get { return _template.Function; }
        }

        public TemplateMatch(Template template, QilLoop filter)
        {
            _template = template;
            _priority = double.IsNaN(template.Priority) ? XPathPatternBuilder.GetPriority(filter) : template.Priority;
            _iterator = filter.Variable;
            _condition = filter.Body;

            XPathPatternBuilder.CleanAnnotation(filter);
            NipOffTypeNameCheck();

            Debug.Assert(
                _qname == null ||
                _nodeKind == XmlNodeKindFlags.Element || _nodeKind == XmlNodeKindFlags.Attribute || _nodeKind == XmlNodeKindFlags.PI,
                "qname may be not null only for element, attribute, or PI patterns"
            );
        }

        /*  NOTE: This code depends on the form of Qil expressions generated by XPathPatternBuilder.
         *  More specifically, it recognizes the following two patterns:
         *
         *  A) /, *, @*, text(), comment(), processing-instruction():
         *      (And* $x:(IsType RefTo LiteralType))
         *
         *  B) foo, @ns:foo, processing-instruction('foo'):
         *      (And* $x:(And (IsType RefTo LiteralType) (Eq (NameOf RefTo) LiteralQName)))
         *
         *  where all RefTo refer to 'it', and LiteralType has exactly one NodeKind bit set.
         *
         *  If one of patterns recognized, we nip $x off of the nested And sequence:
         *      (And* (And2 (And1 $x:* $y:*) $z:*))  =>  (And* (And2 $y:* $z:*))
         */
        private void NipOffTypeNameCheck()
        {
            QilBinary[] leftPath = new QilBinary[4];   // Circular buffer for last 4 And nodes
            int idx = -1;                 // Index of last element in leftPath
            QilNode node = _condition!;          // Walker through left path of the tree

            _nodeKind = XmlNodeKindFlags.None;
            _qname = null;

            while (node.NodeType == QilNodeType.And)
            {
                node = (leftPath[++idx & 3] = (QilBinary)node).Left;
            }

            // Recognizing (IsType RefTo LiteralType)
            if (!(node.NodeType == QilNodeType.IsType))
            {
                return;
            }

            QilBinary isType = (QilBinary)node;
            if (!(isType.Left == _iterator && isType.Right.NodeType == QilNodeType.LiteralType))
            {
                return;
            }

            XmlNodeKindFlags nodeKinds = isType.Right.XmlType!.NodeKinds;
            if (!Bits.ExactlyOne((uint)nodeKinds))
            {
                return;
            }

            // Recognized pattern A, check for B
            QilNode x = isType;
            _nodeKind = nodeKinds;
            QilBinary lastAnd = leftPath[idx & 3];

            if (lastAnd != null && lastAnd.Right.NodeType == QilNodeType.Eq)
            {
                QilBinary eq = (QilBinary)lastAnd.Right;

                // Recognizing (Eq (NameOf RefTo) LiteralQName)
                if (eq.Left.NodeType == QilNodeType.NameOf &&
                    ((QilUnary)eq.Left).Child == _iterator && eq.Right.NodeType == QilNodeType.LiteralQName
                )
                {
                    // Recognized pattern B
                    x = lastAnd;
                    _qname = (QilName?)((QilLiteral)eq.Right).Value;
                    idx--;
                }
            }

            // Nip $x off the condition
            QilBinary and1 = leftPath[idx & 3];
            QilBinary and2 = leftPath[--idx & 3];

            if (and2 != null)
            {
                and2.Left = and1.Right;
            }
            else if (and1 != null)
            {
                _condition = and1.Right;
            }
            else
            {
                _condition = null;
            }
        }

        internal sealed class TemplateMatchComparer : IComparer<TemplateMatch>
        {
            // TemplateMatch x is "greater" than TemplateMatch y iff
            // * x's priority is greater than y's priority, or
            // * x's priority is equal to y's priority, and x occurs later in the stylesheet than y.
            // Order of TemplateMatch'es from the same xsl:template/@match attribute does not matter.

            public int Compare(TemplateMatch? x, TemplateMatch? y)
            {
                Debug.Assert(!double.IsNaN(x!._priority));
                Debug.Assert(!double.IsNaN(y!._priority));
                return (
                    x._priority > y._priority ? 1 :
                    x._priority < y._priority ? -1 :
                    x._template.OrderNumber - y._template.OrderNumber
                );
            }
        }
    }

    internal readonly struct Pattern
    {
        public readonly TemplateMatch Match;

        // Generalized priority of 'match' for the xsl:apply-templates/imports currently being processed
        public readonly int Priority;

        public Pattern(TemplateMatch match, int priority)
        {
            this.Match = match;
            this.Priority = priority;
        }
    }

    internal sealed class PatternBag
    {
        public Dictionary<QilName, List<Pattern>> FixedNamePatterns = new Dictionary<QilName, List<Pattern>>();
        public List<QilName> FixedNamePatternsNames = new List<QilName>();  // Needed only to guarantee a stable order
        public List<Pattern> NonFixedNamePatterns = new List<Pattern>();

        public void Clear()
        {
            FixedNamePatterns.Clear();
            FixedNamePatternsNames.Clear();
            NonFixedNamePatterns.Clear();
        }

        public void Add(Pattern pattern)
        {
            QilName? qname = pattern.Match.QName;
            List<Pattern>? list;

            if (qname == null)
            {
                list = NonFixedNamePatterns;
            }
            else
            {
                if (!FixedNamePatterns.TryGetValue(qname, out list))
                {
                    FixedNamePatternsNames.Add(qname);
                    list = FixedNamePatterns[qname] = new List<Pattern>();
                }
            }
            list.Add(pattern);
        }
    }

    internal sealed class MatcherBuilder
    {
        private readonly XPathQilFactory _f;
        private readonly ReferenceReplacer _refReplacer;
        private readonly InvokeGenerator _invkGen;

        private const int NoMatch = -1;

        public MatcherBuilder(XPathQilFactory f, ReferenceReplacer refReplacer, InvokeGenerator invkGen)
        {
            _f = f;
            _refReplacer = refReplacer;
            _invkGen = invkGen;
        }

        private int _priority = -1;

        private readonly PatternBag _elementPatterns = new PatternBag();
        private readonly PatternBag _attributePatterns = new PatternBag();
        private readonly List<Pattern> _textPatterns = new List<Pattern>();
        private readonly List<Pattern> _documentPatterns = new List<Pattern>();
        private readonly List<Pattern> _commentPatterns = new List<Pattern>();
        private readonly PatternBag _piPatterns = new PatternBag();
        private readonly List<Pattern> _heterogenousPatterns = new List<Pattern>();

        private readonly List<List<TemplateMatch>> _allMatches = new List<List<TemplateMatch>>();

        private void Clear()
        {
            _priority = -1;

            _elementPatterns.Clear();
            _attributePatterns.Clear();
            _textPatterns.Clear();
            _documentPatterns.Clear();
            _commentPatterns.Clear();
            _piPatterns.Clear();
            _heterogenousPatterns.Clear();

            _allMatches.Clear();
        }

        private void AddPatterns(List<TemplateMatch> matches)
        {
            // Process templates in the straight order, since their order will be reverted in the result tree
            foreach (TemplateMatch match in matches)
            {
                Pattern pattern = new Pattern(match, ++_priority);

                switch (match.NodeKind)
                {
                    case XmlNodeKindFlags.Element: _elementPatterns.Add(pattern); break;
                    case XmlNodeKindFlags.Attribute: _attributePatterns.Add(pattern); break;
                    case XmlNodeKindFlags.Text: _textPatterns.Add(pattern); break;
                    case XmlNodeKindFlags.Document: _documentPatterns.Add(pattern); break;
                    case XmlNodeKindFlags.Comment: _commentPatterns.Add(pattern); break;
                    case XmlNodeKindFlags.PI: _piPatterns.Add(pattern); break;
                    default: _heterogenousPatterns.Add(pattern); break;
                }
            }
        }

        private void CollectPatternsInternal(Stylesheet sheet, QilName mode)
        {
            // Process imported stylesheets in the straight order, since their order will be reverted in the result tree
            foreach (Stylesheet import in sheet.Imports!)
            {
                CollectPatternsInternal(import, mode);
            }

            List<TemplateMatch>? matchesForMode;
            if (sheet.TemplateMatches.TryGetValue(mode, out matchesForMode))
            {
                AddPatterns(matchesForMode);
                _allMatches.Add(matchesForMode);
            }
        }

        public void CollectPatterns(StylesheetLevel sheet, QilName mode)
        {
            Clear();
            foreach (Stylesheet import in sheet.Imports!)
            {
                CollectPatternsInternal(import, mode);
            }
        }

        private QilNode MatchPattern(QilIterator it, TemplateMatch match)
        {
            QilNode? cond = match.Condition;
            if (cond == null)
            {
                return _f.True();
            }
            else
            {
                // We have to clone, because the same pattern may be used
                // in many different xsl:apply-templates/imports functions
                cond = cond.DeepClone(_f.BaseFactory);
                return _refReplacer.Replace(cond, match.Iterator, it);
            }
        }

        private QilNode MatchPatterns(QilIterator it, List<Pattern> patternList)
        {
            Debug.Assert(patternList.Count > 0);
            QilNode result = _f.Int32(NoMatch);

            foreach (Pattern pattern in patternList)
            {
                // if ($it =~ pattern.Match) then pattern.Priority else...
                result = _f.Conditional(MatchPattern(it, pattern.Match), _f.Int32(pattern.Priority), result);
            }

            return result;
        }

        private QilNode MatchPatterns(QilIterator it, XmlQueryType xt, List<Pattern> patternList, QilNode otherwise)
        {
            if (patternList.Count == 0)
            {
                return otherwise;
            }
            return _f.Conditional(_f.IsType(it, xt), MatchPatterns(it, patternList), otherwise);
        }

        private bool IsNoMatch(QilNode matcher)
        {
            if (matcher.NodeType == QilNodeType.LiteralInt32)
            {
                Debug.Assert((int)(QilLiteral)matcher == NoMatch);
                return true;
            }
            return false;
        }

        private QilNode MatchPatternsWhosePriorityGreater(QilIterator it, List<Pattern> patternList, QilNode matcher)
        {
            if (patternList.Count == 0)
            {
                return matcher;
            }
            if (IsNoMatch(matcher))
            {
                return MatchPatterns(it, patternList);
            }

            QilIterator stopPriority = _f.Let(matcher);
            QilNode result = _f.Int32(NoMatch);
            int lastPriority = NoMatch;

            foreach (Pattern pattern in patternList)
            {
                // if (stopPriority > pattern.Priority) then stopPriority     else
                // if ($it =~ pattern.Match)            then pattern.Priority else...

                // First 'if' is generated lazily since it is not needed if priorities are consecutive numbers
                Debug.Assert(pattern.Priority > lastPriority);
                if (pattern.Priority > lastPriority + 1)
                {
                    result = _f.Conditional(_f.Gt(stopPriority, _f.Int32(lastPriority)), stopPriority, result);
                }

                result = _f.Conditional(MatchPattern(it, pattern.Match), _f.Int32(pattern.Priority), result);
                lastPriority = pattern.Priority;
            }

            // If the last pattern has the highest priority, the check can be eliminated
            if (lastPriority != _priority)
            {
                result = _f.Conditional(_f.Gt(stopPriority, _f.Int32(lastPriority)), stopPriority, result);
            }

            return _f.Loop(stopPriority, result);
        }

        private QilNode MatchPatterns(QilIterator it, XmlQueryType xt, PatternBag patternBag, QilNode otherwise)
        {
            if (patternBag.FixedNamePatternsNames.Count == 0)
            {
                return MatchPatterns(it, xt, patternBag.NonFixedNamePatterns, otherwise);
            }

            QilNode matcher = _f.Int32(NoMatch);

            foreach (QilName qname in patternBag.FixedNamePatternsNames)
            {
                matcher = _f.Conditional(_f.Eq(_f.NameOf(it), qname.ShallowClone(_f.BaseFactory)),
                    MatchPatterns(it, patternBag.FixedNamePatterns[qname]),
                    matcher
                );
            }

            matcher = MatchPatternsWhosePriorityGreater(it, patternBag.NonFixedNamePatterns, matcher);
            return _f.Conditional(_f.IsType(it, xt), matcher, otherwise);
        }

#if !DISABLE_MATCH_OPTIMIZATION
        public QilNode BuildMatcher(QilIterator it, IList<XslNode> actualArgs, QilNode otherwise)
        {
            QilNode matcher = _f.Int32(NoMatch);

            matcher = MatchPatterns(it, T.PI, _piPatterns, matcher);
            matcher = MatchPatterns(it, T.Comment, _commentPatterns, matcher);
            matcher = MatchPatterns(it, T.Document, _documentPatterns, matcher);
            matcher = MatchPatterns(it, T.Text, _textPatterns, matcher);
            matcher = MatchPatterns(it, T.Attribute, _attributePatterns, matcher);
            matcher = MatchPatterns(it, T.Element, _elementPatterns, matcher);

            matcher = MatchPatternsWhosePriorityGreater(it, _heterogenousPatterns, matcher);

            if (IsNoMatch(matcher))
            {
                return otherwise;
            }

#if !DISABLE_SWITCH
            QilNode[] branches = new QilNode[_priority + 2];
            int priority = -1;

            foreach (List<TemplateMatch> list in _allMatches)
            {
                foreach (TemplateMatch match in list)
                {
                    branches[++priority] = _invkGen.GenerateInvoke(match.TemplateFunction!, actualArgs);
                }
            }

            branches[++priority] = otherwise;
            Debug.Assert(priority == branches.Length - 1);
            return _f.Choice(matcher, _f.BranchList(branches));
#else
            QilIterator p = f.Let(matcher);
            QilNode result = otherwise;
            int priority = 0;

            foreach (List<TemplateMatch> list in allMatches) {
                foreach (TemplateMatch match in list) {
                    result = f.Conditional(f.Eq(p, f.Int32(priority++)),
                        invkGen.GenerateInvoke(match.TemplateFunction, actualArgs),
                        result
                    );
                }
            }

            return f.Loop(p, result);
#endif
        }
#else
        public QilNode BuildMatcher(QilIterator it, IList<XslNode> actualArgs, QilNode otherwise) {
            QilNode result = otherwise;

            foreach (List<TemplateMatch> list in allMatches) {
                foreach (TemplateMatch match in list) {
                    XmlNodeKindFlags nodeKind = match.NodeKind;
                    QilName qname = match.QName;
                    QilNode cond = match.Condition;

                    if (cond != null) {
                        // We have to clone, because the same pattern may be used
                        // in many different xsl:apply-templates/imports functions
                        cond = cond.DeepClone(f.BaseFactory);
                        cond = refReplacer.Replace(cond, match.Iterator, it);
                    }

                    if (nodeKind != 0) {
                        XmlQueryType nodeType;
                        switch (nodeKind) {
                        case XmlNodeKindFlags.Element   : nodeType = T.Element  ;  break;
                        case XmlNodeKindFlags.Attribute : nodeType = T.Attribute;  break;
                        case XmlNodeKindFlags.Text      : nodeType = T.Text     ;  break;
                        case XmlNodeKindFlags.Document  : nodeType = T.Document ;  break;
                        case XmlNodeKindFlags.Comment   : nodeType = T.Comment  ;  break;
                        case XmlNodeKindFlags.PI        : nodeType = T.PI       ;  break;
                        default                         : nodeType = null       ;  break;
                        }

                        Debug.Assert(nodeType != null, "Unexpected nodeKind: " + nodeKind);
                        QilNode typeNameCheck = f.IsType(it, nodeType);

                        if (qname != null) {
                            typeNameCheck = f.And(typeNameCheck, f.Eq(f.NameOf(it), qname.ShallowClone(f.BaseFactory)));
                        }

                        cond = (cond == null) ? typeNameCheck : f.And(typeNameCheck, cond);
                    }

                    result = f.Conditional(cond,
                        invkGen.GenerateInvoke(match.TemplateFunction, actualArgs),
                        result
                    );
                }
            }
            return result;
        }
#endif
    }
}
