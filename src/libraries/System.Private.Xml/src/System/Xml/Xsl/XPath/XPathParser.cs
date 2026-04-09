// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using XPathNodeType = System.Xml.XPath.XPathNodeType;

namespace System.Xml.Xsl.XPath
{
    internal sealed class XPathParser<Node>
    {
        private XPathScanner? _scanner;
        private IXPathBuilder<Node>? _builder;
        private readonly Stack<int> _posInfo = new Stack<int>();

        // Six possible causes of exceptions in the builder:
        // 1. Undefined prefix in a node test.
        // 2. Undefined prefix in a variable reference, or unknown variable.
        // 3. Undefined prefix in a function call, or unknown function, or wrong number/types of arguments.
        // 4. Argument of Union operator is not a node-set.
        // 5. First argument of Predicate is not a node-set.
        // 6. Argument of Axis is not a node-set.

        public Node Parse(XPathScanner scanner, IXPathBuilder<Node> builder, LexKind endLex)
        {
            Debug.Assert(_scanner == null && _builder == null);
            Debug.Assert(scanner != null && builder != null);

            Node? result = default(Node);
            _scanner = scanner;
            _builder = builder;
            _posInfo.Clear();

            try
            {
                builder.StartBuild();
                result = ParseExpr();
                scanner.CheckToken(endLex);
            }
            catch (XPathCompileException e)
            {
                if (e.queryString == null)
                {
                    e.queryString = scanner.Source;
                    PopPosInfo(out e.startChar, out e.endChar);
                }
                throw;
            }
            finally
            {
                result = builder.EndBuild(result);
#if DEBUG
                _builder = null;
                _scanner = null;
#endif
            }
            Debug.Assert(_posInfo.Count == 0, "PushPosInfo() and PopPosInfo() calls have been unbalanced");
            return result!;
        }

        #region Location paths and node tests
        /**************************************************************************************************/
        /*  Location paths and node tests                                                                 */
        /**************************************************************************************************/

        internal static bool IsStep(LexKind lexKind)
        {
            return (
                lexKind == LexKind.Dot ||
                lexKind == LexKind.DotDot ||
                lexKind == LexKind.At ||
                lexKind == LexKind.Axis ||
                lexKind == LexKind.Star ||
                lexKind == LexKind.Name   // NodeTest is also Name
            );
        }

        /*
        *   LocationPath ::= RelativeLocationPath | '/' RelativeLocationPath? | '//' RelativeLocationPath
        */
        private Node ParseLocationPath()
        {
            if (_scanner!.Kind == LexKind.Slash)
            {
                _scanner.NextLex();
                Node opnd = _builder!.Axis(XPathAxis.Root, XPathNodeType.All, null, null);

                if (IsStep(_scanner.Kind))
                {
                    opnd = _builder.JoinStep(opnd, ParseRelativeLocationPath());
                }
                return opnd;
            }
            else if (_scanner.Kind == LexKind.SlashSlash)
            {
                _scanner.NextLex();
                return _builder!.JoinStep(
                    _builder.Axis(XPathAxis.Root, XPathNodeType.All, null, null),
                    _builder.JoinStep(
                        _builder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null),
                        ParseRelativeLocationPath()
                    )
                );
            }
            else
            {
                return ParseRelativeLocationPath();
            }
        }

        /*
        *   RelativeLocationPath ::= Step (('/' | '//') Step)*
        */
        //Max depth to avoid StackOverflow
        private const int MaxParseRelativePathDepth = 1024;
        private int _parseRelativePath;
        private Node ParseRelativeLocationPath()
        {
            if (++_parseRelativePath > MaxParseRelativePathDepth)
            {
                if (LocalAppContextSwitches.LimitXPathComplexity)
                {
                    throw _scanner!.CreateException(SR.Xslt_InputTooComplex);
                }
            }
            Node opnd = ParseStep();
            if (_scanner!.Kind == LexKind.Slash)
            {
                _scanner.NextLex();
                opnd = _builder!.JoinStep(opnd, ParseRelativeLocationPath());
            }
            else if (_scanner.Kind == LexKind.SlashSlash)
            {
                _scanner.NextLex();
                opnd = _builder!.JoinStep(opnd,
                    _builder.JoinStep(
                        _builder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null),
                        ParseRelativeLocationPath()
                    )
                );
            }
            --_parseRelativePath;
            return opnd;
        }

        /*
        *   Step ::= '.' | '..' | (AxisName '::' | '@')? NodeTest Predicate*
        */
        private Node ParseStep()
        {
            Node opnd;
            if (LexKind.Dot == _scanner!.Kind)
            {                  // '.'
                _scanner.NextLex();
                opnd = _builder!.Axis(XPathAxis.Self, XPathNodeType.All, null, null);
                if (LexKind.LBracket == _scanner.Kind)
                {
                    throw _scanner.CreateException(SR.XPath_PredicateAfterDot);
                }
            }
            else if (LexKind.DotDot == _scanner.Kind)
            {        // '..'
                _scanner.NextLex();
                opnd = _builder!.Axis(XPathAxis.Parent, XPathNodeType.All, null, null);
                if (LexKind.LBracket == _scanner.Kind)
                {
                    throw _scanner.CreateException(SR.XPath_PredicateAfterDotDot);
                }
            }
            else
            {                                            // (AxisName '::' | '@')? NodeTest Predicate*
                XPathAxis axis;
                switch (_scanner.Kind)
                {
                    case LexKind.Axis:                              // AxisName '::'
                        axis = _scanner.Axis;
                        _scanner.NextLex();
                        _scanner.NextLex();
                        break;
                    case LexKind.At:                                // '@'
                        axis = XPathAxis.Attribute;
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

                opnd = ParseNodeTest(axis);

                while (LexKind.LBracket == _scanner.Kind)
                {
                    opnd = _builder!.Predicate(opnd, ParsePredicate(), IsReverseAxis(axis));
                }
            }
            return opnd;
        }

        private static bool IsReverseAxis(XPathAxis axis)
        {
            return (
                axis == XPathAxis.Ancestor || axis == XPathAxis.Preceding ||
                axis == XPathAxis.AncestorOrSelf || axis == XPathAxis.PrecedingSibling
            );
        }

        /*
        *   NodeTest ::= NameTest | ('comment' | 'text' | 'node') '(' ')' | 'processing-instruction' '('  Literal? ')'
        *   NameTest ::= '*' | NCName ':' '*' | QName
        */
        private Node ParseNodeTest(XPathAxis axis)
        {
            XPathNodeType nodeType;
            string? nodePrefix, nodeName;

            int startChar = _scanner!.LexStart;
            InternalParseNodeTest(_scanner, axis, out nodeType, out nodePrefix, out nodeName);
            PushPosInfo(startChar, _scanner.PrevLexEnd);
            Node result = _builder!.Axis(axis, nodeType, nodePrefix, nodeName);
            PopPosInfo();
            return result;
        }

        private static bool IsNodeType(XPathScanner scanner)
        {
            return scanner.Prefix.Length == 0 && (
                scanner.Name == "node" ||
                scanner.Name == "text" ||
                scanner.Name == "processing-instruction" ||
                scanner.Name == "comment"
            );
        }

        private static XPathNodeType PrincipalNodeType(XPathAxis axis)
        {
            return (
                axis == XPathAxis.Attribute ? XPathNodeType.Attribute :
                axis == XPathAxis.Namespace ? XPathNodeType.Namespace :
                /*else*/                      XPathNodeType.Element
            );
        }

        internal static void InternalParseNodeTest(XPathScanner scanner, XPathAxis axis, out XPathNodeType nodeType, out string? nodePrefix, out string? nodeName)
        {
            switch (scanner.Kind)
            {
                case LexKind.Name:
                    if (scanner.CanBeFunction && IsNodeType(scanner))
                    {
                        nodePrefix = null;
                        nodeName = null;
                        switch (scanner.Name)
                        {
                            case "comment": nodeType = XPathNodeType.Comment; break;
                            case "text": nodeType = XPathNodeType.Text; break;
                            case "node": nodeType = XPathNodeType.All; break;
                            default:
                                Debug.Assert(scanner.Name == "processing-instruction");
                                nodeType = XPathNodeType.ProcessingInstruction;
                                break;
                        }

                        scanner.NextLex();
                        scanner.PassToken(LexKind.LParens);

                        if (nodeType == XPathNodeType.ProcessingInstruction)
                        {
                            if (scanner.Kind != LexKind.RParens)
                            {  // 'processing-instruction' '(' Literal ')'
                                scanner.CheckToken(LexKind.String);
                                // It is not needed to set nodePrefix here, but for our current implementation
                                // comparing whole QNames is faster than comparing just local names
                                nodePrefix = string.Empty;
                                nodeName = scanner.StringValue;
                                scanner.NextLex();
                            }
                        }

                        scanner.PassToken(LexKind.RParens);
                    }
                    else
                    {
                        nodePrefix = scanner.Prefix;
                        nodeName = scanner.Name;
                        nodeType = PrincipalNodeType(axis);
                        scanner.NextLex();
                        if (nodeName == "*")
                        {
                            nodeName = null;
                        }
                    }
                    break;
                case LexKind.Star:
                    nodePrefix = null;
                    nodeName = null;
                    nodeType = PrincipalNodeType(axis);
                    scanner.NextLex();
                    break;
                default:
                    throw scanner.CreateException(SR.XPath_NodeTestExpected, scanner.RawValue);
            }
        }

        /*
        *   Predicate ::= '[' Expr ']'
        */
        private Node ParsePredicate()
        {
            _scanner!.PassToken(LexKind.LBracket);
            Node opnd = ParseExpr();
            _scanner.PassToken(LexKind.RBracket);
            return opnd;
        }
        #endregion

        #region Expressions
        /**************************************************************************************************/
        /*  Expressions                                                                                   */
        /**************************************************************************************************/

        /*
        *   Expr   ::= OrExpr
        *   OrExpr ::= AndExpr ('or' AndExpr)*
        *   AndExpr ::= EqualityExpr ('and' EqualityExpr)*
        *   EqualityExpr ::= RelationalExpr (('=' | '!=') RelationalExpr)*
        *   RelationalExpr ::= AdditiveExpr (('<' | '>' | '<=' | '>=') AdditiveExpr)*
        *   AdditiveExpr ::= MultiplicativeExpr (('+' | '-') MultiplicativeExpr)*
        *   MultiplicativeExpr ::= UnaryExpr (('*' | 'div' | 'mod') UnaryExpr)*
        *   UnaryExpr ::= ('-')* UnionExpr
        */
        private Node ParseExpr()
        {
            return ParseSubExpr(/*callerPrec:*/0);
        }

        //Max depth to avoid StackOverflow
        //limit the recursive call from ParseSubExpr -> ParseSubExpr
        //and also ParseSubExpr->ParseUnionExpr->ParsePathExpr->...->ParseExpr->ParseSubExpr
        private const int MaxParseSubExprDepth = 1024;
        private int _parseSubExprDepth;
        private Node ParseSubExpr(int callerPrec)
        {
            if (++_parseSubExprDepth > MaxParseSubExprDepth)
            {
                if (LocalAppContextSwitches.LimitXPathComplexity)
                {
                    throw _scanner!.CreateException(SR.Xslt_InputTooComplex);
                }
            }

            XPathOperator op;
            Node opnd;

            ReadOnlySpan<byte> xpathOperatorPrecedence =
            [
                /*Unknown    */ 0,
                /*Or         */ 1,
                /*And        */ 2,
                /*Eq         */ 3,
                /*Ne         */ 3,
                /*Lt         */ 4,
                /*Le         */ 4,
                /*Gt         */ 4,
                /*Ge         */ 4,
                /*Plus       */ 5,
                /*Minus      */ 5,
                /*Multiply   */ 6,
                /*Divide     */ 6,
                /*Modulo     */ 6,
                /*UnaryMinus */ 7,
                /*Union      */ 8,  // Not used
            ];

            // Check for unary operators
            if (_scanner!.Kind == LexKind.Minus)
            {
                op = XPathOperator.UnaryMinus;
                byte opPrec = xpathOperatorPrecedence[(int)op];
                _scanner.NextLex();
                opnd = _builder!.Operator(op, ParseSubExpr(opPrec), default(Node));
            }
            else
            {
                opnd = ParseUnionExpr();
            }

            // Process binary operators
            while (true)
            {
                op = (_scanner.Kind <= LexKind.LastOperator) ? (XPathOperator)_scanner.Kind : XPathOperator.Unknown;
                byte opPrec = xpathOperatorPrecedence[(int)op];
                if (opPrec <= callerPrec)
                {
                    break;
                }

                // Operator's precedence is greater than the one of our caller, so process it here
                _scanner.NextLex();
                opnd = _builder!.Operator(op, opnd, ParseSubExpr(/*callerPrec:*/opPrec));
            }
            --_parseSubExprDepth;
            return opnd;
        }

        /*
        *   UnionExpr ::= PathExpr ('|' PathExpr)*
        */
        private Node ParseUnionExpr()
        {
            int startChar = _scanner!.LexStart;
            Node opnd1 = ParsePathExpr();

            if (_scanner.Kind == LexKind.Union)
            {
                PushPosInfo(startChar, _scanner.PrevLexEnd);
                opnd1 = _builder!.Operator(XPathOperator.Union, default(Node), opnd1);
                PopPosInfo();

                while (_scanner.Kind == LexKind.Union)
                {
                    _scanner.NextLex();
                    startChar = _scanner.LexStart;
                    Node opnd2 = ParsePathExpr();
                    PushPosInfo(startChar, _scanner.PrevLexEnd);
                    opnd1 = _builder.Operator(XPathOperator.Union, opnd1, opnd2);
                    PopPosInfo();
                }
            }
            return opnd1;
        }

        /*
        *   PathExpr ::= LocationPath | FilterExpr (('/' | '//') RelativeLocationPath )?
        */
        private Node ParsePathExpr()
        {
            // Here we distinguish FilterExpr from LocationPath - the former starts with PrimaryExpr
            if (IsPrimaryExpr())
            {
                int startChar = _scanner!.LexStart;
                Node opnd = ParseFilterExpr();
                int endChar = _scanner.PrevLexEnd;

                if (_scanner.Kind == LexKind.Slash)
                {
                    _scanner.NextLex();
                    PushPosInfo(startChar, endChar);
                    opnd = _builder!.JoinStep(opnd, ParseRelativeLocationPath());
                    PopPosInfo();
                }
                else if (_scanner.Kind == LexKind.SlashSlash)
                {
                    _scanner.NextLex();
                    PushPosInfo(startChar, endChar);
                    opnd = _builder!.JoinStep(opnd,
                        _builder.JoinStep(
                            _builder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null),
                            ParseRelativeLocationPath()
                        )
                    );
                    PopPosInfo();
                }
                return opnd;
            }
            else
            {
                return ParseLocationPath();
            }
        }

        /*
        *   FilterExpr ::= PrimaryExpr Predicate*
        */
        private Node ParseFilterExpr()
        {
            int startChar = _scanner!.LexStart;
            Node opnd = ParsePrimaryExpr();
            int endChar = _scanner.PrevLexEnd;

            while (_scanner.Kind == LexKind.LBracket)
            {
                PushPosInfo(startChar, endChar);
                opnd = _builder!.Predicate(opnd, ParsePredicate(), /*reverseStep:*/false);
                PopPosInfo();
            }
            return opnd;
        }

        private bool IsPrimaryExpr()
        {
            return (
                _scanner!.Kind == LexKind.String ||
                _scanner.Kind == LexKind.Number ||
                _scanner.Kind == LexKind.Dollar ||
                _scanner.Kind == LexKind.LParens ||
                _scanner.Kind == LexKind.Name && _scanner.CanBeFunction && !IsNodeType(_scanner)
            );
        }

        /*
        *   PrimaryExpr ::= Literal | Number | VariableReference | '(' Expr ')' | FunctionCall
        */
        private Node ParsePrimaryExpr()
        {
            Debug.Assert(IsPrimaryExpr());
            Node opnd;
            switch (_scanner!.Kind)
            {
                case LexKind.String:
                    opnd = _builder!.String(_scanner.StringValue);
                    _scanner.NextLex();
                    break;
                case LexKind.Number:
                    opnd = _builder!.Number(XPathConvert.StringToDouble(_scanner.RawValue));
                    _scanner.NextLex();
                    break;
                case LexKind.Dollar:
                    int startChar = _scanner.LexStart;
                    _scanner.NextLex();
                    _scanner.CheckToken(LexKind.Name);
                    PushPosInfo(startChar, _scanner.LexStart + _scanner.LexSize);
                    opnd = _builder!.Variable(_scanner.Prefix, _scanner.Name);
                    PopPosInfo();
                    _scanner.NextLex();
                    break;
                case LexKind.LParens:
                    _scanner.NextLex();
                    opnd = ParseExpr();
                    _scanner.PassToken(LexKind.RParens);
                    break;
                default:
                    Debug.Assert(
                        _scanner.Kind == LexKind.Name && _scanner.CanBeFunction && !IsNodeType(_scanner),
                        "IsPrimaryExpr() returned true, but the lexeme is not recognized"
                    );
                    opnd = ParseFunctionCall();
                    break;
            }
            return opnd;
        }

        /*
        *   FunctionCall ::= FunctionName '(' (Expr (',' Expr)* )? ')'
        */
        private Node ParseFunctionCall()
        {
            List<Node> argList = new List<Node>();
            string name = _scanner!.Name;
            string prefix = _scanner.Prefix;
            int startChar = _scanner.LexStart;

            _scanner.PassToken(LexKind.Name);
            _scanner.PassToken(LexKind.LParens);

            if (_scanner.Kind != LexKind.RParens)
            {
                while (true)
                {
                    argList.Add(ParseExpr());
                    if (_scanner.Kind != LexKind.Comma)
                    {
                        _scanner.CheckToken(LexKind.RParens);
                        break;
                    }
                    _scanner.NextLex();  // move off the ','
                }
            }

            _scanner.NextLex();          // move off the ')'
            PushPosInfo(startChar, _scanner.PrevLexEnd);
            Node result = _builder!.Function(prefix, name, argList);
            PopPosInfo();
            return result;
        }
        #endregion

        /**************************************************************************************************/
        /*  Helper methods                                                                                */
        /**************************************************************************************************/

        private void PushPosInfo(int startChar, int endChar)
        {
            _posInfo.Push(startChar);
            _posInfo.Push(endChar);
        }

        private void PopPosInfo()
        {
            _posInfo.Pop();
            _posInfo.Pop();
        }

        private void PopPosInfo(out int startChar, out int endChar)
        {
            endChar = _posInfo.Pop();
            startChar = _posInfo.Pop();
        }
    }
}
