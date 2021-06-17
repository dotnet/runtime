// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt
{
    using XPathParser = XPathParser<QilNode>;
    using XPathNodeType = System.Xml.XPath.XPathNodeType;

    internal sealed class XPathPatternParser
    {
        public interface IPatternBuilder : IXPathBuilder<QilNode>
        {
            IXPathBuilder<QilNode> GetPredicateBuilder(QilNode context);
        }

        private XPathScanner? _scanner;
        private IPatternBuilder? _ptrnBuilder;
        private readonly XPathParser _predicateParser = new XPathParser();

        public QilNode Parse(XPathScanner scanner, IPatternBuilder ptrnBuilder)
        {
            Debug.Assert(_scanner == null && _ptrnBuilder == null);
            Debug.Assert(scanner != null && ptrnBuilder != null);
            QilNode? result = null;
            ptrnBuilder.StartBuild();
            try
            {
                _scanner = scanner;
                _ptrnBuilder = ptrnBuilder;
                result = this.ParsePattern();
                _scanner.CheckToken(LexKind.Eof);
            }
            finally
            {
                result = ptrnBuilder.EndBuild(result);
#if DEBUG
                _ptrnBuilder = null;
                _scanner = null;
#endif
            }
            return result!;
        }

        /*
        *   Pattern ::= LocationPathPattern ('|' LocationPathPattern)*
        */
        private QilNode ParsePattern()
        {
            QilNode opnd = ParseLocationPathPattern();

            while (_scanner!.Kind == LexKind.Union)
            {
                _scanner.NextLex();
                opnd = _ptrnBuilder!.Operator(XPathOperator.Union, opnd, ParseLocationPathPattern());
            }
            return opnd;
        }

        /*
        *   LocationPathPattern ::= '/' RelativePathPattern? | '//'? RelativePathPattern | IdKeyPattern (('/' | '//') RelativePathPattern)?
        */
        private QilNode ParseLocationPathPattern()
        {
            QilNode opnd;

            switch (_scanner!.Kind)
            {
                case LexKind.Slash:
                    _scanner.NextLex();
                    opnd = _ptrnBuilder!.Axis(XPathAxis.Root, XPathNodeType.All, null, null);

                    if (XPathParser.IsStep(_scanner.Kind))
                    {
                        opnd = _ptrnBuilder.JoinStep(opnd, ParseRelativePathPattern());
                    }
                    return opnd;
                case LexKind.SlashSlash:
                    _scanner.NextLex();
                    return _ptrnBuilder!.JoinStep(
                        _ptrnBuilder.Axis(XPathAxis.Root, XPathNodeType.All, null, null),
                        _ptrnBuilder.JoinStep(
                            _ptrnBuilder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null),
                            ParseRelativePathPattern()
                        )
                    );
                case LexKind.Name:
                    if (_scanner.CanBeFunction && _scanner.Prefix.Length == 0 && (_scanner.Name == "id" || _scanner.Name == "key"))
                    {
                        opnd = ParseIdKeyPattern();
                        switch (_scanner.Kind)
                        {
                            case LexKind.Slash:
                                _scanner.NextLex();
                                opnd = _ptrnBuilder!.JoinStep(opnd, ParseRelativePathPattern());
                                break;
                            case LexKind.SlashSlash:
                                _scanner.NextLex();
                                opnd = _ptrnBuilder!.JoinStep(opnd,
                                    _ptrnBuilder.JoinStep(
                                        _ptrnBuilder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null),
                                        ParseRelativePathPattern()
                                    )
                                );
                                break;
                        }
                        return opnd;
                    }
                    break;
            }
            opnd = ParseRelativePathPattern();
            return opnd;
        }

        /*
        *   IdKeyPattern ::= 'id' '(' Literal ')' | 'key' '(' Literal ',' Literal ')'
        */
        private QilNode ParseIdKeyPattern()
        {
            Debug.Assert(_scanner!.CanBeFunction);
            Debug.Assert(_scanner.Prefix.Length == 0);
            Debug.Assert(_scanner.Name == "id" || _scanner.Name == "key");
            List<QilNode> args = new List<QilNode>(2);

            if (_scanner.Name == "id")
            {
                _scanner.NextLex();
                _scanner.PassToken(LexKind.LParens);
                _scanner.CheckToken(LexKind.String);
                args.Add(_ptrnBuilder!.String(_scanner.StringValue));
                _scanner.NextLex();
                _scanner.PassToken(LexKind.RParens);
                return _ptrnBuilder.Function("", "id", args);
            }
            else
            {
                _scanner.NextLex();
                _scanner.PassToken(LexKind.LParens);
                _scanner.CheckToken(LexKind.String);
                args.Add(_ptrnBuilder!.String(_scanner.StringValue));
                _scanner.NextLex();
                _scanner.PassToken(LexKind.Comma);
                _scanner.CheckToken(LexKind.String);
                args.Add(_ptrnBuilder.String(_scanner.StringValue));
                _scanner.NextLex();
                _scanner.PassToken(LexKind.RParens);
                return _ptrnBuilder.Function("", "key", args);
            }
        }

        /*
        *   RelativePathPattern ::= StepPattern (('/' | '//') StepPattern)*
        */
        //Max depth to avoid StackOverflow
        private const int MaxParseRelativePathDepth = 1024;
        private int _parseRelativePath;
        private QilNode ParseRelativePathPattern()
        {
            if (++_parseRelativePath > MaxParseRelativePathDepth)
            {
                if (LocalAppContextSwitches.LimitXPathComplexity)
                {
                    throw _scanner!.CreateException(SR.Xslt_InputTooComplex);
                }
            }
            QilNode opnd = ParseStepPattern();
            if (_scanner!.Kind == LexKind.Slash)
            {
                _scanner.NextLex();
                opnd = _ptrnBuilder!.JoinStep(opnd, ParseRelativePathPattern());
            }
            else if (_scanner.Kind == LexKind.SlashSlash)
            {
                _scanner.NextLex();
                opnd = _ptrnBuilder!.JoinStep(opnd,
                    _ptrnBuilder.JoinStep(
                        _ptrnBuilder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null),
                        ParseRelativePathPattern()
                    )
                );
            }
            --_parseRelativePath;
            return opnd;
        }

        /*
        *   StepPattern ::= ChildOrAttributeAxisSpecifier NodeTest Predicate*
        *   ChildOrAttributeAxisSpecifier ::= @ ? | ('child' | 'attribute') '::'
        */
        private QilNode ParseStepPattern()
        {
            QilNode opnd;
            XPathAxis axis;

            switch (_scanner!.Kind)
            {
                case LexKind.Dot:
                case LexKind.DotDot:
                    throw _scanner.CreateException(SR.XPath_InvalidAxisInPattern);
                case LexKind.At:
                    axis = XPathAxis.Attribute;
                    _scanner.NextLex();
                    break;
                case LexKind.Axis:
                    axis = _scanner.Axis;
                    if (axis != XPathAxis.Child && axis != XPathAxis.Attribute)
                    {
                        throw _scanner.CreateException(SR.XPath_InvalidAxisInPattern);
                    }
                    _scanner.NextLex();  // Skip '::'
                    _scanner.NextLex();
                    break;
                case LexKind.Name:
                case LexKind.Star:
                    // NodeTest must start with Name or '*'
                    axis = XPathAxis.Child;
                    break;
                default:
                    throw _scanner.CreateException(SR.XPath_UnexpectedToken, _scanner.RawValue);
            }

            XPathNodeType nodeType;
            string? nodePrefix, nodeName;
            XPathParser.InternalParseNodeTest(_scanner, axis, out nodeType, out nodePrefix, out nodeName);
            opnd = _ptrnBuilder!.Axis(axis, nodeType, nodePrefix, nodeName);

            XPathPatternBuilder? xpathPatternBuilder = _ptrnBuilder as XPathPatternBuilder;
            if (xpathPatternBuilder != null)
            {
                //for XPathPatternBuilder, get all predicates and then build them
                List<QilNode> predicates = new List<QilNode>();
                while (_scanner.Kind == LexKind.LBracket)
                {
                    predicates.Add(ParsePredicate(opnd));
                }
                if (predicates.Count > 0)
                    opnd = xpathPatternBuilder.BuildPredicates(opnd, predicates);
            }
            else
            {
                while (_scanner.Kind == LexKind.LBracket)
                {
                    opnd = _ptrnBuilder.Predicate(opnd, ParsePredicate(opnd), /*reverseStep:*/false);
                }
            }
            return opnd;
        }

        /*
        *   Predicate ::= '[' Expr ']'
        */
        private QilNode ParsePredicate(QilNode context)
        {
            Debug.Assert(_scanner!.Kind == LexKind.LBracket);
            _scanner.NextLex();
            QilNode result = _predicateParser.Parse(_scanner, _ptrnBuilder!.GetPredicateBuilder(context), LexKind.RBracket);
            Debug.Assert(_scanner.Kind == LexKind.RBracket);
            _scanner.NextLex();
            return result;
        }
    }
}
