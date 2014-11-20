// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class XPathParser
    {
        XPathScanner scanner;

        private XPathParser(XPathScanner scanner)
        {
            this.scanner = scanner;
        }

        public static AstNode ParseXPathExpresion(string xpathExpresion)
        {
            XPathScanner scanner = new XPathScanner(xpathExpresion);
            XPathParser parser = new XPathParser(scanner);
            AstNode result = parser.ParseExpresion(null);
            if (scanner.Kind != XPathScanner.LexKind.Eof)
            {
                throw XPathException.Create(SR.Xp_InvalidToken, scanner.SourceText);
            }
            return result;
        }

        public static AstNode ParseXPathPattern(string xpathPattern)
        {
            XPathScanner scanner = new XPathScanner(xpathPattern);
            XPathParser parser = new XPathParser(scanner);
            AstNode result = parser.ParsePattern();
            if (scanner.Kind != XPathScanner.LexKind.Eof)
            {
                throw XPathException.Create(SR.Xp_InvalidToken, scanner.SourceText);
            }
            return result;
        }

        // --------------- Expresion Parsing ----------------------


        //The recursive is like 
        //ParseOrExpr->ParseAndExpr->ParseEqualityExpr->ParseRelationalExpr...->ParseFilterExpr->ParsePredicate->ParseExpresion
        //So put 200 limitation here will max cause about 2000~3000 depth stack.
        private int parseDepth = 0;
        private const int MaxParseDepth = 200;

        private AstNode ParseExpresion(AstNode qyInput)
        {
            if (++parseDepth > MaxParseDepth)
            {
                throw XPathException.Create(SR.Xp_QueryTooComplex);
            }
            AstNode result = ParseOrExpr(qyInput);
            --parseDepth;
            return result;
        }

        //>> OrExpr ::= ( OrExpr 'or' )? AndExpr 
        private AstNode ParseOrExpr(AstNode qyInput)
        {
            AstNode opnd = ParseAndExpr(qyInput);

            do
            {
                if (!TestOp("or"))
                {
                    return opnd;
                }
                NextLex();
                opnd = new Operator(Operator.Op.OR, opnd, ParseAndExpr(qyInput));
            } while (true);
        }

        //>> AndExpr ::= ( AndExpr 'and' )? EqualityExpr 
        private AstNode ParseAndExpr(AstNode qyInput)
        {
            AstNode opnd = ParseEqualityExpr(qyInput);

            do
            {
                if (!TestOp("and"))
                {
                    return opnd;
                }
                NextLex();
                opnd = new Operator(Operator.Op.AND, opnd, ParseEqualityExpr(qyInput));
            } while (true);
        }

        //>> EqualityOp ::= '=' | '!='
        //>> EqualityExpr    ::= ( EqualityExpr EqualityOp )? RelationalExpr
        private AstNode ParseEqualityExpr(AstNode qyInput)
        {
            AstNode opnd = ParseRelationalExpr(qyInput);

            do
            {
                Operator.Op op = (
                    this.scanner.Kind == XPathScanner.LexKind.Eq ? Operator.Op.EQ :
                    this.scanner.Kind == XPathScanner.LexKind.Ne ? Operator.Op.NE :
                    /*default :*/                                  Operator.Op.INVALID
                );
                if (op == Operator.Op.INVALID)
                {
                    return opnd;
                }
                NextLex();
                opnd = new Operator(op, opnd, ParseRelationalExpr(qyInput));
            } while (true);
        }

        //>> RelationalOp ::= '<' | '>' | '<=' | '>='
        //>> RelationalExpr    ::= ( RelationalExpr RelationalOp )? AdditiveExpr  
        private AstNode ParseRelationalExpr(AstNode qyInput)
        {
            AstNode opnd = ParseAdditiveExpr(qyInput);

            do
            {
                Operator.Op op = (
                    this.scanner.Kind == XPathScanner.LexKind.Lt ? Operator.Op.LT :
                    this.scanner.Kind == XPathScanner.LexKind.Le ? Operator.Op.LE :
                    this.scanner.Kind == XPathScanner.LexKind.Gt ? Operator.Op.GT :
                    this.scanner.Kind == XPathScanner.LexKind.Ge ? Operator.Op.GE :
                    /*default :*/                                  Operator.Op.INVALID
                );
                if (op == Operator.Op.INVALID)
                {
                    return opnd;
                }
                NextLex();
                opnd = new Operator(op, opnd, ParseAdditiveExpr(qyInput));
            } while (true);
        }

        //>> AdditiveOp   ::= '+' | '-'
        //>> AdditiveExpr ::= ( AdditiveExpr AdditiveOp )? MultiplicativeExpr
        private AstNode ParseAdditiveExpr(AstNode qyInput)
        {
            AstNode opnd = ParseMultiplicativeExpr(qyInput);

            do
            {
                Operator.Op op = (
                    this.scanner.Kind == XPathScanner.LexKind.Plus ? Operator.Op.PLUS :
                    this.scanner.Kind == XPathScanner.LexKind.Minus ? Operator.Op.MINUS :
                    /*default :*/                                     Operator.Op.INVALID
                );
                if (op == Operator.Op.INVALID)
                {
                    return opnd;
                }
                NextLex();
                opnd = new Operator(op, opnd, ParseMultiplicativeExpr(qyInput));
            } while (true);
        }

        //>> MultiplicativeOp   ::= '*' | 'div' | 'mod'
        //>> MultiplicativeExpr ::= ( MultiplicativeExpr MultiplicativeOp )? UnaryExpr
        private AstNode ParseMultiplicativeExpr(AstNode qyInput)
        {
            AstNode opnd = ParseUnaryExpr(qyInput);

            do
            {
                Operator.Op op = (
                    this.scanner.Kind == XPathScanner.LexKind.Star ? Operator.Op.MUL :
                    TestOp("div") ? Operator.Op.DIV :
                    TestOp("mod") ? Operator.Op.MOD :
                    /*default :*/                                     Operator.Op.INVALID
                );
                if (op == Operator.Op.INVALID)
                {
                    return opnd;
                }
                NextLex();
                opnd = new Operator(op, opnd, ParseUnaryExpr(qyInput));
            } while (true);
        }

        //>> UnaryExpr    ::= UnionExpr | '-' UnaryExpr
        private AstNode ParseUnaryExpr(AstNode qyInput)
        {
            bool minus = false;
            while (this.scanner.Kind == XPathScanner.LexKind.Minus)
            {
                NextLex();
                minus = !minus;
            }

            if (minus)
            {
                return new Operator(Operator.Op.MUL, ParseUnionExpr(qyInput), new Operand(-1));
            }
            else
            {
                return ParseUnionExpr(qyInput);
            }
        }

        //>> UnionExpr ::= ( UnionExpr '|' )? PathExpr  
        private AstNode ParseUnionExpr(AstNode qyInput)
        {
            AstNode opnd = ParsePathExpr(qyInput);

            do
            {
                if (this.scanner.Kind != XPathScanner.LexKind.Union)
                {
                    return opnd;
                }
                NextLex();
                AstNode opnd2 = ParsePathExpr(qyInput);
                CheckNodeSet(opnd.ReturnType);
                CheckNodeSet(opnd2.ReturnType);
                opnd = new Operator(Operator.Op.UNION, opnd, opnd2);
            } while (true);
        }

        private static bool IsNodeType(XPathScanner scaner)
        {
            return (
                scaner.Prefix.Length == 0 && (
                    scaner.Name == "node" ||
                    scaner.Name == "text" ||
                    scaner.Name == "processing-instruction" ||
                    scaner.Name == "comment"
                )
            );
        }

        //>> PathOp   ::= '/' | '//'
        //>> PathExpr ::= LocationPath | 
        //>>              FilterExpr ( PathOp  RelativeLocationPath )?
        private AstNode ParsePathExpr(AstNode qyInput)
        {
            AstNode opnd;
            if (IsPrimaryExpr(this.scanner))
            { // in this moment we shoud distinct LocationPas vs FilterExpr (which starts from is PrimaryExpr)
                opnd = ParseFilterExpr(qyInput);
                if (this.scanner.Kind == XPathScanner.LexKind.Slash)
                {
                    NextLex();
                    opnd = ParseRelativeLocationPath(opnd);
                }
                else if (this.scanner.Kind == XPathScanner.LexKind.SlashSlash)
                {
                    NextLex();
                    opnd = ParseRelativeLocationPath(new Axis(Axis.AxisType.DescendantOrSelf, opnd));
                }
            }
            else
            {
                opnd = ParseLocationPath(null);
            }

            return opnd;
        }

        //>> FilterExpr ::= PrimaryExpr | FilterExpr Predicate 
        private AstNode ParseFilterExpr(AstNode qyInput)
        {
            AstNode opnd = ParsePrimaryExpr(qyInput);
            while (this.scanner.Kind == XPathScanner.LexKind.LBracket)
            {
                // opnd must be a query
                opnd = new Filter(opnd, ParsePredicate(opnd));
            }
            return opnd;
        }

        //>> Predicate ::= '[' Expr ']'
        private AstNode ParsePredicate(AstNode qyInput)
        {
            AstNode opnd;

            // we have predicates. Check that input type is NodeSet
            CheckNodeSet(qyInput.ReturnType);

            PassToken(XPathScanner.LexKind.LBracket);
            opnd = ParseExpresion(qyInput);
            PassToken(XPathScanner.LexKind.RBracket);

            return opnd;
        }

        //>> LocationPath ::= RelativeLocationPath | AbsoluteLocationPath
        private AstNode ParseLocationPath(AstNode qyInput)
        {
            if (this.scanner.Kind == XPathScanner.LexKind.Slash)
            {
                NextLex();
                AstNode opnd = new Root();

                if (IsStep(this.scanner.Kind))
                {
                    opnd = ParseRelativeLocationPath(opnd);
                }
                return opnd;
            }
            else if (this.scanner.Kind == XPathScanner.LexKind.SlashSlash)
            {
                NextLex();
                return ParseRelativeLocationPath(new Axis(Axis.AxisType.DescendantOrSelf, new Root()));
            }
            else
            {
                return ParseRelativeLocationPath(qyInput);
            }
        } // ParseLocationPath

        //>> PathOp   ::= '/' | '//'
        //>> RelativeLocationPath ::= ( RelativeLocationPath PathOp )? Step 
        private AstNode ParseRelativeLocationPath(AstNode qyInput)
        {
            AstNode opnd = qyInput;
            do
            {
                opnd = ParseStep(opnd);
                if (XPathScanner.LexKind.SlashSlash == this.scanner.Kind)
                {
                    NextLex();
                    opnd = new Axis(Axis.AxisType.DescendantOrSelf, opnd);
                }
                else if (XPathScanner.LexKind.Slash == this.scanner.Kind)
                {
                    NextLex();
                }
                else
                {
                    break;
                }
            }
            while (true);

            return opnd;
        }

        private static bool IsStep(XPathScanner.LexKind lexKind)
        {
            return (
                lexKind == XPathScanner.LexKind.Dot ||
                lexKind == XPathScanner.LexKind.DotDot ||
                lexKind == XPathScanner.LexKind.At ||
                lexKind == XPathScanner.LexKind.Axe ||
                lexKind == XPathScanner.LexKind.Star ||
                lexKind == XPathScanner.LexKind.Name          // NodeTest is also Name
            );
        }

        //>> Step ::= '.' | '..' | ( AxisName '::' | '@' )? NodeTest Predicate*
        private AstNode ParseStep(AstNode qyInput)
        {
            AstNode opnd;
            if (XPathScanner.LexKind.Dot == this.scanner.Kind)
            {         //>> '.'
                NextLex();
                opnd = new Axis(Axis.AxisType.Self, qyInput);
            }
            else if (XPathScanner.LexKind.DotDot == this.scanner.Kind)
            { //>> '..'
                NextLex();
                opnd = new Axis(Axis.AxisType.Parent, qyInput);
            }
            else
            {                                                          //>> ( AxisName '::' | '@' )? NodeTest Predicate*
                Axis.AxisType axisType = Axis.AxisType.Child;
                switch (this.scanner.Kind)
                {
                    case XPathScanner.LexKind.At:                               //>> '@'
                        axisType = Axis.AxisType.Attribute;
                        NextLex();
                        break;
                    case XPathScanner.LexKind.Axe:                              //>> AxisName '::'
                        axisType = GetAxis();
                        NextLex();
                        break;
                }
                XPathNodeType nodeType = (
                    axisType == Axis.AxisType.Attribute ? XPathNodeType.Attribute :
                    //                    axisType == Axis.AxisType.Namespace ? XPathNodeType.Namespace : // No Idea why it's this way but othervise Axes doesn't work
                    /* default: */                        XPathNodeType.Element
                );

                opnd = ParseNodeTest(qyInput, axisType, nodeType);

                while (XPathScanner.LexKind.LBracket == this.scanner.Kind)
                {
                    opnd = new Filter(opnd, ParsePredicate(opnd));
                }
            }
            return opnd;
        }

        //>> NodeTest ::= NameTest | 'comment ()' | 'text ()' | 'node ()' | 'processing-instruction ('  Literal ? ')'
        private AstNode ParseNodeTest(AstNode qyInput, Axis.AxisType axisType, XPathNodeType nodeType)
        {
            string nodeName, nodePrefix;

            switch (this.scanner.Kind)
            {
                case XPathScanner.LexKind.Name:
                    if (this.scanner.CanBeFunction && IsNodeType(this.scanner))
                    {
                        nodePrefix = string.Empty;
                        nodeName = string.Empty;
                        nodeType = (
                            this.scanner.Name == "comment" ? XPathNodeType.Comment :
                            this.scanner.Name == "text" ? XPathNodeType.Text :
                            this.scanner.Name == "node" ? XPathNodeType.All :
                            this.scanner.Name == "processing-instruction" ? XPathNodeType.ProcessingInstruction :
                            /* default: */ XPathNodeType.Root
                        );
                        Debug.Assert(nodeType != XPathNodeType.Root);
                        NextLex();

                        PassToken(XPathScanner.LexKind.LParens);

                        if (nodeType == XPathNodeType.ProcessingInstruction)
                        {
                            if (this.scanner.Kind != XPathScanner.LexKind.RParens)
                            { //>> 'processing-instruction (' Literal ')'
                                CheckToken(XPathScanner.LexKind.String);
                                nodeName = this.scanner.StringValue;
                                NextLex();
                            }
                        }

                        PassToken(XPathScanner.LexKind.RParens);
                    }
                    else
                    {
                        nodePrefix = this.scanner.Prefix;
                        nodeName = this.scanner.Name;
                        NextLex();
                        if (nodeName == "*")
                        {
                            nodeName = string.Empty;
                        }
                    }
                    break;
                case XPathScanner.LexKind.Star:
                    nodePrefix = string.Empty;
                    nodeName = string.Empty;
                    NextLex();
                    break;
                default:
                    throw XPathException.Create(SR.Xp_NodeSetExpected, this.scanner.SourceText);
            }
            return new Axis(axisType, qyInput, nodePrefix, nodeName, nodeType);
        }

        private static bool IsPrimaryExpr(XPathScanner scanner)
        {
            return (
                scanner.Kind == XPathScanner.LexKind.String ||
                scanner.Kind == XPathScanner.LexKind.Number ||
                scanner.Kind == XPathScanner.LexKind.Dollar ||
                scanner.Kind == XPathScanner.LexKind.LParens ||
                scanner.Kind == XPathScanner.LexKind.Name && scanner.CanBeFunction && !IsNodeType(scanner)
            );
        }

        //>> PrimaryExpr ::= Literal | Number | VariableReference | '(' Expr ')' | FunctionCall
        private AstNode ParsePrimaryExpr(AstNode qyInput)
        {
            Debug.Assert(IsPrimaryExpr(this.scanner));
            AstNode opnd = null;
            switch (this.scanner.Kind)
            {
                case XPathScanner.LexKind.String:
                    opnd = new Operand(this.scanner.StringValue);
                    NextLex();
                    break;
                case XPathScanner.LexKind.Number:
                    opnd = new Operand(this.scanner.NumberValue);
                    NextLex();
                    break;
                case XPathScanner.LexKind.Dollar:
                    NextLex();
                    CheckToken(XPathScanner.LexKind.Name);
                    opnd = new Variable(this.scanner.Name, this.scanner.Prefix);
                    NextLex();
                    break;
                case XPathScanner.LexKind.LParens:
                    NextLex();
                    opnd = ParseExpresion(qyInput);
                    if (opnd.Type != AstNode.AstType.ConstantOperand)
                    {
                        opnd = new Group(opnd);
                    }
                    PassToken(XPathScanner.LexKind.RParens);
                    break;
                case XPathScanner.LexKind.Name:
                    if (this.scanner.CanBeFunction && !IsNodeType(this.scanner))
                    {
                        opnd = ParseMethod(null);
                    }
                    break;
            }
            Debug.Assert(opnd != null, "IsPrimaryExpr() was true. We should recognize this lex.");
            return opnd;
        }

        private AstNode ParseMethod(AstNode qyInput)
        {
            List<AstNode> argList = new List<AstNode>();
            string name = this.scanner.Name;
            string prefix = this.scanner.Prefix;
            PassToken(XPathScanner.LexKind.Name);
            PassToken(XPathScanner.LexKind.LParens);
            if (this.scanner.Kind != XPathScanner.LexKind.RParens)
            {
                do
                {
                    argList.Add(ParseExpresion(qyInput));
                    if (this.scanner.Kind == XPathScanner.LexKind.RParens)
                    {
                        break;
                    }
                    PassToken(XPathScanner.LexKind.Comma);
                } while (true);
            }
            PassToken(XPathScanner.LexKind.RParens);
            if (prefix.Length == 0)
            {
                ParamInfo pi;
                if (functionTable.TryGetValue(name, out pi))
                {
                    int argCount = argList.Count;
                    if (argCount < pi.Minargs)
                    {
                        throw XPathException.Create(SR.Xp_InvalidNumArgs, name, this.scanner.SourceText);
                    }
                    if (pi.FType == Function.FunctionType.FuncConcat)
                    {
                        for (int i = 0; i < argCount; i++)
                        {
                            AstNode arg = (AstNode)argList[i];
                            if (arg.ReturnType != XPathResultType.String)
                            {
                                arg = new Function(Function.FunctionType.FuncString, arg);
                            }
                            argList[i] = arg;
                        }
                    }
                    else
                    {
                        if (pi.Maxargs < argCount)
                        {
                            throw XPathException.Create(SR.Xp_InvalidNumArgs, name, this.scanner.SourceText);
                        }
                        if (pi.ArgTypes.Length < argCount)
                        {
                            argCount = pi.ArgTypes.Length;    // argument we have the type specified (can be < pi.Minargs)
                        }
                        for (int i = 0; i < argCount; i++)
                        {
                            AstNode arg = (AstNode)argList[i];
                            if (
                                pi.ArgTypes[i] != XPathResultType.Any &&
                                pi.ArgTypes[i] != arg.ReturnType
                            )
                            {
                                switch (pi.ArgTypes[i])
                                {
                                    case XPathResultType.NodeSet:
                                        if (!(arg is Variable) && !(arg is Function && arg.ReturnType == XPathResultType.Any))
                                        {
                                            throw XPathException.Create(SR.Xp_InvalidArgumentType, name, this.scanner.SourceText);
                                        }
                                        break;
                                    case XPathResultType.String:
                                        arg = new Function(Function.FunctionType.FuncString, arg);
                                        break;
                                    case XPathResultType.Number:
                                        arg = new Function(Function.FunctionType.FuncNumber, arg);
                                        break;
                                    case XPathResultType.Boolean:
                                        arg = new Function(Function.FunctionType.FuncBoolean, arg);
                                        break;
                                }
                                argList[i] = arg;
                            }
                        }
                    }
                    return new Function(pi.FType, argList);
                }
            }
            return new Function(prefix, name, argList);
        }

        // --------------- Pattern Parsing ----------------------

        //>> Pattern ::= ( Pattern '|' )? LocationPathPattern
        private AstNode ParsePattern()
        {
            AstNode opnd = ParseLocationPathPattern();

            do
            {
                if (this.scanner.Kind != XPathScanner.LexKind.Union)
                {
                    return opnd;
                }
                NextLex();
                opnd = new Operator(Operator.Op.UNION, opnd, ParseLocationPathPattern());
            } while (true);
        }

        //>> LocationPathPattern ::= '/' | RelativePathPattern | '//' RelativePathPattern  |  '/' RelativePathPattern
        //>>                       | IdKeyPattern (('/' | '//') RelativePathPattern)?  
        private AstNode ParseLocationPathPattern()
        {
            AstNode opnd = null;
            switch (this.scanner.Kind)
            {
                case XPathScanner.LexKind.Slash:
                    NextLex();
                    opnd = new Root();
                    if (this.scanner.Kind == XPathScanner.LexKind.Eof || this.scanner.Kind == XPathScanner.LexKind.Union)
                    {
                        return opnd;
                    }
                    break;
                case XPathScanner.LexKind.SlashSlash:
                    NextLex();
                    opnd = new Axis(Axis.AxisType.DescendantOrSelf, new Root());
                    break;
                case XPathScanner.LexKind.Name:
                    if (this.scanner.CanBeFunction)
                    {
                        opnd = ParseIdKeyPattern();
                        if (opnd != null)
                        {
                            switch (this.scanner.Kind)
                            {
                                case XPathScanner.LexKind.Slash:
                                    NextLex();
                                    break;
                                case XPathScanner.LexKind.SlashSlash:
                                    NextLex();
                                    opnd = new Axis(Axis.AxisType.DescendantOrSelf, opnd);
                                    break;
                                default:
                                    return opnd;
                            }
                        }
                    }
                    break;
            }
            return ParseRelativePathPattern(opnd);
        }

        //>> IdKeyPattern ::= 'id' '(' Literal ')' | 'key' '(' Literal ',' Literal ')'  
        private AstNode ParseIdKeyPattern()
        {
            Debug.Assert(this.scanner.CanBeFunction);
            List<AstNode> argList = new List<AstNode>();
            if (this.scanner.Prefix.Length == 0)
            {
                if (this.scanner.Name == "id")
                {
                    ParamInfo pi = (ParamInfo)functionTable["id"];
                    NextLex();
                    PassToken(XPathScanner.LexKind.LParens);
                    CheckToken(XPathScanner.LexKind.String);
                    argList.Add(new Operand(this.scanner.StringValue));
                    NextLex();
                    PassToken(XPathScanner.LexKind.RParens);
                    return new Function(pi.FType, argList);
                }
                if (this.scanner.Name == "key")
                {
                    NextLex();
                    PassToken(XPathScanner.LexKind.LParens);
                    CheckToken(XPathScanner.LexKind.String);
                    argList.Add(new Operand(this.scanner.StringValue));
                    NextLex();
                    PassToken(XPathScanner.LexKind.Comma);
                    CheckToken(XPathScanner.LexKind.String);
                    argList.Add(new Operand(this.scanner.StringValue));
                    NextLex();
                    PassToken(XPathScanner.LexKind.RParens);
                    return new Function("", "key", argList);
                }
            }
            return null;
        }

        //>> PathOp   ::= '/' | '//'
        //>> RelativePathPattern ::= ( RelativePathPattern PathOp )? StepPattern
        private AstNode ParseRelativePathPattern(AstNode qyInput)
        {
            AstNode opnd = ParseStepPattern(qyInput);
            if (XPathScanner.LexKind.SlashSlash == this.scanner.Kind)
            {
                NextLex();
                opnd = ParseRelativePathPattern(new Axis(Axis.AxisType.DescendantOrSelf, opnd));
            }
            else if (XPathScanner.LexKind.Slash == this.scanner.Kind)
            {
                NextLex();
                opnd = ParseRelativePathPattern(opnd);
            }
            return opnd;
        }

        //>> StepPattern    ::=    ChildOrAttributeAxisSpecifier NodeTest Predicate*   
        //>> ChildOrAttributeAxisSpecifier    ::=    @ ? | ('child' | 'attribute') '::' 
        private AstNode ParseStepPattern(AstNode qyInput)
        {
            AstNode opnd;
            Axis.AxisType axisType = Axis.AxisType.Child;
            switch (this.scanner.Kind)
            {
                case XPathScanner.LexKind.At:                               //>> '@'
                    axisType = Axis.AxisType.Attribute;
                    NextLex();
                    break;
                case XPathScanner.LexKind.Axe:                              //>> AxisName '::'
                    axisType = GetAxis();
                    if (axisType != Axis.AxisType.Child && axisType != Axis.AxisType.Attribute)
                    {
                        throw XPathException.Create(SR.Xp_InvalidToken, scanner.SourceText);
                    }
                    NextLex();
                    break;
            }
            XPathNodeType nodeType = (
                axisType == Axis.AxisType.Attribute ? XPathNodeType.Attribute :
                /* default: */                        XPathNodeType.Element
            );

            opnd = ParseNodeTest(qyInput, axisType, nodeType);

            while (XPathScanner.LexKind.LBracket == this.scanner.Kind)
            {
                opnd = new Filter(opnd, ParsePredicate(opnd));
            }
            return opnd;
        }

        // --------------- Helper methods ----------------------

        void CheckToken(XPathScanner.LexKind t)
        {
            if (this.scanner.Kind != t)
            {
                throw XPathException.Create(SR.Xp_InvalidToken, this.scanner.SourceText);
            }
        }

        void PassToken(XPathScanner.LexKind t)
        {
            CheckToken(t);
            NextLex();
        }

        void NextLex()
        {
            this.scanner.NextLex();
        }

        private bool TestOp(string op)
        {
            return (
                this.scanner.Kind == XPathScanner.LexKind.Name &&
                this.scanner.Prefix.Length == 0 &&
                this.scanner.Name.Equals(op)
            );
        }

        void CheckNodeSet(XPathResultType t)
        {
            if (t != XPathResultType.NodeSet && t != XPathResultType.Any)
            {
                throw XPathException.Create(SR.Xp_NodeSetExpected, this.scanner.SourceText);
            }
        }

        // ----------------------------------------------------------------
        static readonly XPathResultType[] temparray1 = { };
        static readonly XPathResultType[] temparray2 = { XPathResultType.NodeSet };
        static readonly XPathResultType[] temparray3 = { XPathResultType.Any };
        static readonly XPathResultType[] temparray4 = { XPathResultType.String };
        static readonly XPathResultType[] temparray5 = { XPathResultType.String, XPathResultType.String };
        static readonly XPathResultType[] temparray6 = { XPathResultType.String, XPathResultType.Number, XPathResultType.Number };
        static readonly XPathResultType[] temparray7 = { XPathResultType.String, XPathResultType.String, XPathResultType.String };
        static readonly XPathResultType[] temparray8 = { XPathResultType.Boolean };
        static readonly XPathResultType[] temparray9 = { XPathResultType.Number };

        private class ParamInfo
        {
            private Function.FunctionType ftype;
            private int minargs;
            private int maxargs;
            private XPathResultType[] argTypes;

            public Function.FunctionType FType { get { return this.ftype; } }
            public int Minargs { get { return this.minargs; } }
            public int Maxargs { get { return this.maxargs; } }
            public XPathResultType[] ArgTypes { get { return this.argTypes; } }

            internal ParamInfo(Function.FunctionType ftype, int minargs, int maxargs, XPathResultType[] argTypes)
            {
                this.ftype = ftype;
                this.minargs = minargs;
                this.maxargs = maxargs;
                this.argTypes = argTypes;
            }
        } //ParamInfo

        private static Dictionary<string, ParamInfo> functionTable = CreateFunctionTable();
        private static Dictionary<string, ParamInfo> CreateFunctionTable()
        {
            Dictionary<string, ParamInfo> table = new Dictionary<string, ParamInfo>(36);
            table.Add("last", new ParamInfo(Function.FunctionType.FuncLast, 0, 0, temparray1));
            table.Add("position", new ParamInfo(Function.FunctionType.FuncPosition, 0, 0, temparray1));
            table.Add("name", new ParamInfo(Function.FunctionType.FuncName, 0, 1, temparray2));
            table.Add("namespace-uri", new ParamInfo(Function.FunctionType.FuncNameSpaceUri, 0, 1, temparray2));
            table.Add("local-name", new ParamInfo(Function.FunctionType.FuncLocalName, 0, 1, temparray2));
            table.Add("count", new ParamInfo(Function.FunctionType.FuncCount, 1, 1, temparray2));
            table.Add("id", new ParamInfo(Function.FunctionType.FuncID, 1, 1, temparray3));
            table.Add("string", new ParamInfo(Function.FunctionType.FuncString, 0, 1, temparray3));
            table.Add("concat", new ParamInfo(Function.FunctionType.FuncConcat, 2, 100, temparray4));
            table.Add("starts-with", new ParamInfo(Function.FunctionType.FuncStartsWith, 2, 2, temparray5));
            table.Add("contains", new ParamInfo(Function.FunctionType.FuncContains, 2, 2, temparray5));
            table.Add("substring-before", new ParamInfo(Function.FunctionType.FuncSubstringBefore, 2, 2, temparray5));
            table.Add("substring-after", new ParamInfo(Function.FunctionType.FuncSubstringAfter, 2, 2, temparray5));
            table.Add("substring", new ParamInfo(Function.FunctionType.FuncSubstring, 2, 3, temparray6));
            table.Add("string-length", new ParamInfo(Function.FunctionType.FuncStringLength, 0, 1, temparray4));
            table.Add("normalize-space", new ParamInfo(Function.FunctionType.FuncNormalize, 0, 1, temparray4));
            table.Add("translate", new ParamInfo(Function.FunctionType.FuncTranslate, 3, 3, temparray7));
            table.Add("boolean", new ParamInfo(Function.FunctionType.FuncBoolean, 1, 1, temparray3));
            table.Add("not", new ParamInfo(Function.FunctionType.FuncNot, 1, 1, temparray8));
            table.Add("true", new ParamInfo(Function.FunctionType.FuncTrue, 0, 0, temparray8));
            table.Add("false", new ParamInfo(Function.FunctionType.FuncFalse, 0, 0, temparray8));
            table.Add("lang", new ParamInfo(Function.FunctionType.FuncLang, 1, 1, temparray4));
            table.Add("number", new ParamInfo(Function.FunctionType.FuncNumber, 0, 1, temparray3));
            table.Add("sum", new ParamInfo(Function.FunctionType.FuncSum, 1, 1, temparray2));
            table.Add("floor", new ParamInfo(Function.FunctionType.FuncFloor, 1, 1, temparray9));
            table.Add("ceiling", new ParamInfo(Function.FunctionType.FuncCeiling, 1, 1, temparray9));
            table.Add("round", new ParamInfo(Function.FunctionType.FuncRound, 1, 1, temparray9));
            return table;
        }

        private static Dictionary<string, Axis.AxisType> AxesTable = CreateAxesTable();
        private static Dictionary<string, Axis.AxisType> CreateAxesTable()
        {
            Dictionary<string, Axis.AxisType> table = new Dictionary<string, Axis.AxisType>(13);
            table.Add("ancestor", Axis.AxisType.Ancestor);
            table.Add("ancestor-or-self", Axis.AxisType.AncestorOrSelf);
            table.Add("attribute", Axis.AxisType.Attribute);
            table.Add("child", Axis.AxisType.Child);
            table.Add("descendant", Axis.AxisType.Descendant);
            table.Add("descendant-or-self", Axis.AxisType.DescendantOrSelf);
            table.Add("following", Axis.AxisType.Following);
            table.Add("following-sibling", Axis.AxisType.FollowingSibling);
            table.Add("namespace", Axis.AxisType.Namespace);
            table.Add("parent", Axis.AxisType.Parent);
            table.Add("preceding", Axis.AxisType.Preceding);
            table.Add("preceding-sibling", Axis.AxisType.PrecedingSibling);
            table.Add("self", Axis.AxisType.Self);
            return table;
        }

        private Axis.AxisType GetAxis()
        {
            Debug.Assert(scanner.Kind == XPathScanner.LexKind.Axe);
            Axis.AxisType axis;
            if (!AxesTable.TryGetValue(scanner.Name, out axis))
            {
                throw XPathException.Create(SR.Xp_InvalidToken, scanner.SourceText);
            }
            return axis;
        }
    }
}
