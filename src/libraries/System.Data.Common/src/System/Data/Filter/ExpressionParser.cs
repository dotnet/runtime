// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data
{
    internal enum ValueType
    {
        Unknown = -1,
        Null = 0,
        Bool = 1,
        Numeric = 2,
        Str = 3,
        Float = 4,
        Decimal = 5,
        Object = 6,
        Date = 7,
    }
    /// <summary>
    ///     ExpressionParser: expression node types
    /// </summary>
    internal enum Nodes
    {
        Noop = 0,
        Unop = 1,       /* Unary operator */
        UnopSpec = 2,   /* Special unop: IFF does not eval args */
        Binop = 3,      /* Binary operator */
        BinopSpec = 4,  /* Special binop: BETWEEN, IN does not eval args */
        Zop = 5,        /* "0-ary operator" - intrinsic constant. */
        Call = 6,       /* Function call or rhs of IN or IFF */
        Const = 7,      /* Constant value */
        Name = 8,       /* Identifier */
        Paren = 9,      /* Parentheses */
        Conv = 10,      /* Type conversion */
    }

    internal sealed class ExpressionParser
    {
        /// <summary>
        ///     Operand situations for parser
        /// </summary>
        private const int Empty = 0;  /* There was no previous operand */
        private const int Scalar = 1; /* The previous operand was a constant or id */
        private const int Expr = 2;   /* The previous operand was a complex expression */


        private readonly struct ReservedWords
        {
            internal readonly string _word;      // the word
            internal readonly Tokens _token;
            internal readonly int _op;

            internal ReservedWords(string word, Tokens token, int op)
            {
                _word = word;
                _token = token;
                _op = op;
            }
        }

        // this should be maintained as a invariantculture sorted array for binary searching
        private static readonly ReservedWords[] s_reservedwords = new ReservedWords[] {
            new ReservedWords("And", Tokens.BinaryOp, Operators.And),
            /*
            the following operator is not implemented in the current version of the
            Expression language, but we need to add them to the Reserved words list
            to prevent future compatibility problems.
            */
            new ReservedWords("Between", Tokens.BinaryOp, Operators.Between),

            new ReservedWords("Child", Tokens.Child, Operators.Noop),
            new ReservedWords("False", Tokens.ZeroOp, Operators.False),
            new ReservedWords("In", Tokens.BinaryOp, Operators.In),
            new ReservedWords("Is", Tokens.BinaryOp, Operators.Is),
            new ReservedWords("Like", Tokens.BinaryOp, Operators.Like),
            new ReservedWords("Not", Tokens.UnaryOp, Operators.Not),
            new ReservedWords("Null", Tokens.ZeroOp, Operators.Null),
            new ReservedWords("Or", Tokens.BinaryOp, Operators.Or),
            new ReservedWords("Parent", Tokens.Parent, Operators.Noop),
            new ReservedWords("True", Tokens.ZeroOp, Operators.True),
        };


        /* the following is the Scanner local configuration, Default settings is US
         * CONSIDER: should we read the user (or system) local settings?
         * CONSIDER: make this configurable by the user, and system locale for the string compare
         */
        private readonly char _escape = '\\';
        private readonly char _decimalSeparator = '.';
        //not used: private char ThousandSeparator = ',';
        private readonly char _listSeparator = ',';
        //not used: private char DateSeparator = '/';
        private readonly char _exponentL = 'e';
        private readonly char _exponentU = 'E';

        internal char[] _text = default!; // Always initialized in LoadExpression before use
        internal int _pos;
        internal int _start;
        internal Tokens _token;
        internal int _op = Operators.Noop;

        internal OperatorInfo[] _ops = new OperatorInfo[MaxPredicates];
        internal int _topOperator;
        internal int _topNode;

        private readonly DataTable? _table;

        private const int MaxPredicates = 100;
        internal ExpressionNode[] _nodeStack = new ExpressionNode[MaxPredicates];

        internal int _prevOperand;

        internal ExpressionNode? _expression;

        internal ExpressionParser(DataTable? table)
        {
            _table = table;
        }

        internal void LoadExpression(string? data)
        {
            int length;

            if (data == null)
            {
                length = 0;
                _text = new char[length + 1];
            }
            else
            {
                length = data.Length;
                _text = new char[length + 1];
                data.CopyTo(0, _text, 0, length);
            }

            _text[length] = '\0';

            if (_expression != null)
            {
                // free all nodes
                _expression = null;
            }
        }

        internal void StartScan()
        {
            _op = Operators.Noop;
            _pos = 0;
            _start = 0;

            _topOperator = 0;
            _ops[_topOperator++] = new OperatorInfo(Nodes.Noop, Operators.Noop, Operators.priStart);
        }

        // CONSIDER: configure the scanner : local info

        internal ExpressionNode Parse()
        {
            // free all nodes
            _expression = null;

            StartScan();

            int cParens = 0;
            OperatorInfo opInfo;

            while (_token != Tokens.EOS)
            {
            loop:
                Scan();

                switch (_token)
                {
                    case Tokens.EOS:
                        // End of string: must be operand; force out expression;
                        // check for bomb; check nothing left on stack.

                        if (_prevOperand == Empty)
                        {
                            if (_topNode == 0)
                            {
                                // we have an empty expression
                                break;
                            }
                            // set error missing operator
                            // read the last operator info
                            opInfo = _ops[_topOperator - 1];

                            throw ExprException.MissingOperand(opInfo);
                        }
                        // collect all nodes
                        BuildExpression(Operators.priLow);
                        if (_topOperator != 1)
                        {
                            throw ExprException.MissingRightParen();
                        }
                        break;

                    case Tokens.Name:
                    case Tokens.Parent:
                    case Tokens.Numeric:
                    case Tokens.Decimal:
                    case Tokens.Float:
                    case Tokens.StringConst:
                    case Tokens.Date:
                        ExpressionNode? node = null;
                        string? str;

                        /* Constants and identifiers: create leaf node */

                        if (_prevOperand != Empty)
                        {
                            // set error missing operator
                            throw ExprException.MissingOperator(new string(_text, _start, _pos - _start));
                        }

                        if (_topOperator > 0)
                        {
                            // special check for IN without parentheses

                            opInfo = _ops[_topOperator - 1];

                            if (opInfo._type == Nodes.Binop && opInfo._op == Operators.In && _token != Tokens.Parent)
                            {
                                throw ExprException.InWithoutParentheses();
                            }
                        }

                        _prevOperand = Scalar;

                        switch (_token)
                        {
                            case Tokens.Parent:
                                string? relname;
                                string colname;

                                // parsing Parent[(relation_name)].column_name)
                                try
                                {
                                    // expecting an '(' or '.'
                                    Scan();
                                    if (_token == Tokens.LeftParen)
                                    {
                                        //read the relation name
                                        ScanToken(Tokens.Name);
                                        relname = NameNode.ParseName(_text, _start, _pos);
                                        ScanToken(Tokens.RightParen);
                                        ScanToken(Tokens.Dot);
                                    }
                                    else
                                    {
                                        relname = null;
                                        CheckToken(Tokens.Dot);
                                    }
                                }
                                catch (Exception e) when (Common.ADP.IsCatchableExceptionType(e))
                                {
                                    throw ExprException.LookupArgument();
                                }

                                ScanToken(Tokens.Name);
                                colname = NameNode.ParseName(_text, _start, _pos);

                                node = new LookupNode(_table, colname, relname);

                                break;

                            case Tokens.Name:
                                /* Qualify name now for nice error checking */


                                /* Create tree element -                */
                                // CONSIDER: Check for reserved proc names here
                                node = new NameNode(_table, _text, _start, _pos);

                                break;

                            case Tokens.Numeric:
                                str = new string(_text, _start, _pos - _start);
                                node = new ConstNode(_table, ValueType.Numeric, str);
                                break;
                            case Tokens.Decimal:
                                str = new string(_text, _start, _pos - _start);
                                node = new ConstNode(_table, ValueType.Decimal, str);
                                break;
                            case Tokens.Float:
                                str = new string(_text, _start, _pos - _start);
                                node = new ConstNode(_table, ValueType.Float, str);
                                break;
                            case Tokens.StringConst:
                                Debug.Assert(_text[_start] == '\'' && _text[_pos - 1] == '\'', "The expression contains an invalid string constant");
                                Debug.Assert(_pos - _start > 1, "The expression contains an invalid string constant");
                                // Store string without quotes..
                                str = new string(_text, _start + 1, _pos - _start - 2);
                                node = new ConstNode(_table, ValueType.Str, str);
                                break;
                            case Tokens.Date:
                                Debug.Assert(_text[_start] == '#' && _text[_pos - 1] == '#', "The expression contains invalid date constant.");
                                Debug.Assert(_pos - _start > 2, "The expression contains invalid date constant '{0}'.");
                                // Store date without delimiters(#s)..
                                str = new string(_text, _start + 1, _pos - _start - 2);
                                node = new ConstNode(_table, ValueType.Date, str);
                                break;
                            default:
                                Debug.Fail("unhandled token");
                                break;
                        }

                        NodePush(node);
                        goto loop;

                    case Tokens.LeftParen:
                        cParens++;
                        if (_prevOperand == Empty)
                        {
                            // Check for ( following IN/IFF. if not, we have a normal (.
                            // Peek: take a look at the operators stack

                            Debug.Assert(_topOperator > 0, "Empty operator stack!!");
                            opInfo = _ops[_topOperator - 1];

                            if (opInfo._type == Nodes.Binop && opInfo._op == Operators.In)
                            {
                                /* IN - handle as procedure call */

                                node = new FunctionNode(_table, "In");
                                NodePush(node);
                                /* Push operator decriptor */
                                _ops[_topOperator++] = new OperatorInfo(Nodes.Call, Operators.Noop, Operators.priParen);
                            }
                            else
                            {  /* Normal ( */
                                /* Push operator decriptor */
                                _ops[_topOperator++] = new OperatorInfo(Nodes.Paren, Operators.Noop, Operators.priParen);
                            }
                        }
                        else
                        {
                            // This is a procedure call or () qualification
                            // Force out any dot qualifiers; check for bomb

                            BuildExpression(Operators.priProc);
                            _prevOperand = Empty;
                            ExpressionNode? nodebefore = NodePeek();

                            if (nodebefore == null || nodebefore.GetType() != typeof(NameNode))
                            {
                                // this is more like an assert, so we not care about "nice" exception text..
                                throw ExprException.SyntaxError();
                            }

                            /* Get the proc name */
                            NameNode name = (NameNode)NodePop();

                            // Make sure that we can bind the name as a Function
                            // then get the argument count and types, and parse arguments..

                            node = new FunctionNode(_table, name._name);

                            // check to see if this is an aggregate function
                            Aggregate agg = (Aggregate)(int)((FunctionNode)node).Aggregate;
                            if (agg != Aggregate.None)
                            {
                                node = ParseAggregateArgument((FunctionId)(int)agg);
                                NodePush(node);
                                _prevOperand = Expr;
                                goto loop;
                            }

                            NodePush(node);
                            _ops[_topOperator++] = new OperatorInfo(Nodes.Call, Operators.Noop, Operators.priParen);
                        }
                        goto loop;

                    case Tokens.RightParen:
                        {
                            /* Right parentheses: Build expression if we have an operand. */
                            if (_prevOperand != Empty)
                            {
                                BuildExpression(Operators.priLow);
                            }

                            /* We must have Tokens.LeftParen on stack. If no operand, must be procedure call. */
                            if (_topOperator <= 1)
                            {
                                // set error, syntax: too many right parens..
                                throw ExprException.TooManyRightParentheses();
                            }

                            Debug.Assert(_topOperator > 1, "melformed operator stack.");
                            _topOperator--;
                            opInfo = _ops[_topOperator];

                            if (_prevOperand == Empty && opInfo._type != Nodes.Call)
                            {
                                // set error, syntax: missing operand.
                                throw ExprException.MissingOperand(opInfo);
                            }

                            Debug.Assert(opInfo._priority == Operators.priParen, "melformed operator stack.");

                            if (opInfo._type == Nodes.Call)
                            {
                                /* add argument to the function call. */

                                if (_prevOperand != Empty)
                                {
                                    // read last function argument
                                    ExpressionNode argument = NodePop();

                                    /* Get the procedure name and append argument */
                                    Debug.Assert(_topNode > 0 && NodePeek()!.GetType() == typeof(FunctionNode), "The function node should be created on '('");

                                    FunctionNode func = (FunctionNode)NodePop();
                                    func.AddArgument(argument);
                                    func.Check();
                                    NodePush(func);
                                }
                            }
                            else
                            {
                                /* Normal parentheses: create tree node */
                                // Construct & Put the Nodes.Paren node on node stack
                                node = NodePop();
                                node = new UnaryNode(_table, Operators.Noop, node);
                                NodePush(node);
                            }

                            _prevOperand = Expr;
                            cParens--;
                            goto loop;
                        }
                    case Tokens.ListSeparator:
                        {
                            /* Comma encountered: Must be operand; force out subexpression */

                            if (_prevOperand == Empty)
                            {
                                throw ExprException.MissingOperandBefore(",");
                            }

                            /* We are be in a procedure call */

                            /* build next argument */
                            BuildExpression(Operators.priLow);

                            opInfo = _ops[_topOperator - 1];

                            if (opInfo._type != Nodes.Call)
                                throw ExprException.SyntaxError();

                            ExpressionNode argument2 = NodePop();

                            /* Get the procedure name */

                            FunctionNode func = (FunctionNode)NodePop();

                            func.AddArgument(argument2);

                            NodePush(func);

                            _prevOperand = Empty;

                            goto loop;
                        }
                    case Tokens.BinaryOp:
                        if (_prevOperand == Empty)
                        {
                            /* Check for unary plus/minus */
                            if (_op == Operators.Plus)
                            {
                                _op = Operators.UnaryPlus;
                                // fall through to UnaryOperator;
                            }
                            else if (_op == Operators.Minus)
                            {
                                /* Unary minus */
                                _op = Operators.Negative;
                                // fall through to UnaryOperator;
                            }
                            else
                            {
                                // Error missing operand:
                                throw ExprException.MissingOperandBefore(Operators.ToString(_op));
                            }
                        }
                        else
                        {
                            _prevOperand = Empty;

                            /* CNSIDER: If we are going to support BETWEEN Translate AND to special BetweenAnd if it is. */

                            /* Force out to appropriate precedence; push operator. */

                            BuildExpression(Operators.Priority(_op));

                            // PushOperator descriptor
                            _ops[_topOperator++] = new OperatorInfo(Nodes.Binop, _op, Operators.Priority(_op));
                            goto loop;
                        }
                        goto
                    case Tokens.UnaryOp; // fall through to UnaryOperator;

                    case Tokens.UnaryOp:
                        /* Must be no operand. Push it. */
                        _ops[_topOperator++] = new OperatorInfo(Nodes.Unop, _op, Operators.Priority(_op));
                        goto loop;

                    case Tokens.ZeroOp:
                        // check the we have operator on the stack
                        if (_prevOperand != Empty)
                        {
                            // set error missing operator
                            throw ExprException.MissingOperator(new string(_text, _start, _pos - _start));
                        }

                        // PushOperator descriptor
                        _ops[_topOperator++] = new OperatorInfo(Nodes.Zop, _op, Operators.priMax);
                        _prevOperand = Expr;
                        goto loop;

                    case Tokens.Dot:
                        //if there is a name on the stack append it.
                        ExpressionNode? before = NodePeek();

                        if (before != null && before.GetType() == typeof(NameNode))
                        {
                            Scan();

                            if (_token == Tokens.Name)
                            {
                                NameNode nameBefore = (NameNode)NodePop();
                                string newName = nameBefore._name + "." + NameNode.ParseName(_text, _start, _pos);
                                NodePush(new NameNode(_table, newName));
                                goto loop;
                            }
                        }
                        // fall through to default
                        goto default;
                    default:
                        throw ExprException.UnknownToken(new string(_text, _start, _pos - _start), _start + 1);
                }
            }
            goto end_loop;
        end_loop:
            Debug.Assert(_topNode == 1 || _topNode == 0, "Invalid Node Stack");
            _expression = _nodeStack[0];

            return _expression;
        }

        /// <summary>
        /// Parse the argument to an Aggregate function. The syntax is
        ///      Func(child[(relation_name)].column_name)
        /// When the function is called we have already parsed the Aggregate name, and open paren
        /// </summary>
        private ExpressionNode ParseAggregateArgument(FunctionId aggregate)
        {
            Debug.Assert(_token == Tokens.LeftParen, "ParseAggregateArgument(): Invalid argument, token <> '('");

            bool child;
            string? relname;
            string colname;

            Scan();

            try
            {
                if (_token != Tokens.Child)
                {
                    if (_token != Tokens.Name)
                        throw ExprException.AggregateArgument();

                    colname = NameNode.ParseName(_text, _start, _pos);
                    ScanToken(Tokens.RightParen);
                    return new AggregateNode(_table, aggregate, colname);
                }

                child = (_token == Tokens.Child);
                _prevOperand = Scalar;

                // expecting an '(' or '.'
                Scan();

                if (_token == Tokens.LeftParen)
                {
                    //read the relation name
                    ScanToken(Tokens.Name);
                    relname = NameNode.ParseName(_text, _start, _pos);
                    ScanToken(Tokens.RightParen);
                    ScanToken(Tokens.Dot);
                }
                else
                {
                    relname = null;
                    CheckToken(Tokens.Dot);
                }

                ScanToken(Tokens.Name);
                colname = NameNode.ParseName(_text, _start, _pos);
                ScanToken(Tokens.RightParen);
            }
            catch (Exception e) when (Common.ADP.IsCatchableExceptionType(e))
            {
                throw ExprException.AggregateArgument();
            }
            return new AggregateNode(_table, aggregate, colname, !child, relname);
        }

        /// <summary>
        ///     NodePop - Pop an operand node from the node stack.
        /// </summary>
        private ExpressionNode NodePop()
        {
            Debug.Assert(_topNode > 0, "NodePop(): Corrupted node stack");
            ExpressionNode node = _nodeStack[--_topNode];
            Debug.Assert(null != node, "null NodePop");
            return node;
        }

        /// <summary>
        ///     NodePeek - Peek at the top node.
        /// </summary>
        private ExpressionNode? NodePeek()
        {
            if (_topNode <= 0)
                return null;

            return _nodeStack[_topNode - 1];
        }

        /// <summary>
        ///     Push an operand node onto the node stack
        /// </summary>
        private void NodePush(ExpressionNode node)
        {
            Debug.Assert(null != node, "null NodePush");

            if (_topNode >= MaxPredicates - 2)
            {
                throw ExprException.ExpressionTooComplex();
            }
            _nodeStack[_topNode++] = node;
        }

        /// <summary>
        ///     Builds expression tree for higher-precedence operator to be used as left
        ///     operand of current operator. May cause errors - always do ErrorCheck() upin return.
        /// </summary>

        private void BuildExpression(int pri)
        {
            ExpressionNode? expr;

            Debug.Assert(pri > Operators.priStart && pri <= Operators.priMax, "Invalid priority value");

            /* For all operators of higher or same precedence (we are always
            left-associative) */
            while (true)
            {
                Debug.Assert(_topOperator > 0, "Empty operator stack!!");
                OperatorInfo opInfo = _ops[_topOperator - 1];

                if (opInfo._priority < pri)
                    goto end_loop;

                Debug.Assert(opInfo._priority >= pri, "Invalid prioriry value");
                _topOperator--;

                ExpressionNode? nodeLeft;
                ExpressionNode nodeRight;
                switch (opInfo._type)
                {
                    case Nodes.Binop:
                        {
                            // get right, left operands. Bind them.

                            nodeRight = NodePop();
                            nodeLeft = NodePop();

                            /* This is the place to do type and other checks */

                            switch (opInfo._op)
                            {
                                case Operators.Between:
                                case Operators.BetweenAnd:
                                case Operators.BitwiseAnd:
                                case Operators.BitwiseOr:
                                case Operators.BitwiseXor:
                                case Operators.BitwiseNot:
                                    throw ExprException.UnsupportedOperator(opInfo._op);

                                case Operators.Is:
                                case Operators.Or:
                                case Operators.And:
                                case Operators.EqualTo:
                                case Operators.NotEqual:
                                case Operators.Like:
                                case Operators.LessThen:
                                case Operators.LessOrEqual:
                                case Operators.GreaterThen:
                                case Operators.GreaterOrEqual:
                                case Operators.In:
                                    break;

                                default:
                                    Debug.Assert(opInfo._op == Operators.Plus ||
                                                 opInfo._op == Operators.Minus ||
                                                 opInfo._op == Operators.Multiply ||
                                                 opInfo._op == Operators.Divide ||
                                                 opInfo._op == Operators.Modulo,
                                                 "Invalud Binary operation");

                                    break;
                            }
                            Debug.Assert(nodeLeft != null, "Invalid left operand");
                            Debug.Assert(nodeRight != null, "Invalid right operand");

                            if (opInfo._op == Operators.Like)
                            {
                                expr = new LikeNode(_table, opInfo._op, nodeLeft, nodeRight);
                            }
                            else
                            {
                                expr = new BinaryNode(_table, opInfo._op, nodeLeft, nodeRight);
                            }

                            break;
                        }
                    case Nodes.Unop:
                        /* Unary operator: Pop and bind right op. */
                        nodeLeft = null;
                        nodeRight = NodePop();

                        /* Check for special cases */
                        switch (opInfo._op)
                        {
                            case Operators.Not:
                                break;

                            case Operators.BitwiseNot:
                                throw ExprException.UnsupportedOperator(opInfo._op);

                            case Operators.Negative:
                                break;
                        }

                        Debug.Assert(nodeLeft == null, "Invalid left operand");
                        Debug.Assert(nodeRight != null, "Invalid right operand");

                        expr = new UnaryNode(_table, opInfo._op, nodeRight);
                        break;

                    case Nodes.Zop:
                        /* Intrinsic constant: just create node. */
                        expr = new ZeroOpNode(opInfo._op);
                        break;

                    default:
                        Debug.Fail("Unhandled operator type");
                        goto end_loop;
                }
                Debug.Assert(expr != null, "Failed to create expression");

                NodePush(expr);
                // countinue while loop;
            }
        end_loop:
            ;
        }


        internal void CheckToken(Tokens token)
        {
            if (_token != token)
            {
                throw ExprException.UnknownToken(token, _token, _pos);
            }
        }

        internal Tokens Scan()
        {
            char ch;
            char[] text = _text;

            _token = Tokens.None;

            while (true)
            {
            loop:
                _start = _pos;
                _op = Operators.Noop;
                ch = text[_pos++];
                switch (ch)
                {
                    case (char)0:
                        _token = Tokens.EOS;
                        goto end_loop;

                    case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
                        ScanWhite();
                        goto loop;

                    case '(':
                        _token = Tokens.LeftParen;
                        goto end_loop;

                    case ')':
                        _token = Tokens.RightParen;
                        goto end_loop;

                    case '#':
                        ScanDate();
                        CheckToken(Tokens.Date);
                        goto end_loop;

                    case '\'':
                        ScanString('\'');
                        CheckToken(Tokens.StringConst);
                        goto end_loop;

                    case '=':
                        _token = Tokens.BinaryOp;
                        _op = Operators.EqualTo;
                        goto end_loop;

                    case '>':
                        _token = Tokens.BinaryOp;
                        ScanWhite();
                        if (text[_pos] == '=')
                        {
                            _pos++;
                            _op = Operators.GreaterOrEqual;
                        }
                        else
                            _op = Operators.GreaterThen;
                        goto end_loop;
                    case '<':
                        _token = Tokens.BinaryOp;
                        ScanWhite();
                        if (text[_pos] == '=')
                        {
                            _pos++;
                            _op = Operators.LessOrEqual;
                        }
                        else if (text[_pos] == '>')
                        {
                            _pos++;
                            _op = Operators.NotEqual;
                        }
                        else
                            _op = Operators.LessThen;
                        goto end_loop;

                    case '+':
                        _token = Tokens.BinaryOp;
                        _op = Operators.Plus;
                        goto end_loop;

                    case '-':
                        _token = Tokens.BinaryOp;
                        _op = Operators.Minus;
                        goto end_loop;

                    case '*':
                        _token = Tokens.BinaryOp;
                        _op = Operators.Multiply;
                        goto end_loop;

                    case '/':
                        _token = Tokens.BinaryOp;
                        _op = Operators.Divide;
                        goto end_loop;

                    case '%':
                        _token = Tokens.BinaryOp;
                        _op = Operators.Modulo;
                        goto end_loop;

                    /* Beginning of bitwise operators */
                    case '&':
                        _token = Tokens.BinaryOp;
                        _op = Operators.BitwiseAnd;
                        goto end_loop;

                    case '|':
                        _token = Tokens.BinaryOp;
                        _op = Operators.BitwiseOr;
                        goto end_loop;
                    case '^':
                        _token = Tokens.BinaryOp;
                        _op = Operators.BitwiseXor;
                        goto end_loop;
                    case '~':
                        _token = Tokens.BinaryOp;
                        _op = Operators.BitwiseNot;
                        goto end_loop;

                    /* we have bracketed identifier */
                    case '[':
                        // BUG: special case
                        ScanName(']', _escape, "]\\");
                        CheckToken(Tokens.Name);
                        goto end_loop;

                    case '`':
                        ScanName('`', '`', "`");
                        CheckToken(Tokens.Name);
                        goto end_loop;

                    default:
                        /* Check for list separator */

                        if (ch == _listSeparator)
                        {
                            _token = Tokens.ListSeparator;
                            goto end_loop;
                        }

                        if (ch == '.')
                        {
                            if (_prevOperand == Empty)
                            {
                                ScanNumeric();
                            }
                            else
                            {
                                _token = Tokens.Dot;
                            }
                            goto end_loop;
                        }

                        /* Check for binary constant */
                        if (ch == '0' && (text[_pos] == 'x' || text[_pos] == 'X'))
                        {
                            _token = Tokens.BinaryConst;
                            goto end_loop;
                        }

                        /* Check for number: digit is always good; . or - only if osNil. */
                        if (IsDigit(ch))
                        {
                            ScanNumeric();
                            goto end_loop;
                        }

                        /* Check for reserved word */
                        ScanReserved();
                        if (_token != Tokens.None)
                        {
                            goto end_loop;
                        }

                        /* Alpha means identifier */

                        if (IsAlphaNumeric(ch))
                        {
                            ScanName();
                            if (_token != Tokens.None)
                            {
                                CheckToken(Tokens.Name);
                                goto end_loop;
                            }
                        }

                        /* Don't understand that banter at all. */
                        _token = Tokens.Unknown;
                        throw ExprException.UnknownToken(new string(text, _start, _pos - _start), _start + 1);
                }
            }
        end_loop:
            return _token;
        }

        /// <summary>
        ///     ScanNumeric - parse number.
        ///     In format: [digit|.]*{[e|E]{[+|-]}{digit*}}
        ///     Further checking is done by constant parser.
        /// </summary>
        private void ScanNumeric()
        {
            char[] text = _text;
            bool fDot = false;
            bool fSientific = false;

            Debug.Assert(_pos != 0, "We have at least one digit in the buffer, ScanNumeric()");
            Debug.Assert(IsDigit(text[_pos - 1]), "We have at least one digit in the buffer, ScanNumeric(), not a digit");

            while (IsDigit(text[_pos]))
            {
                _pos++;
            }

            if (text[_pos] == _decimalSeparator)
            {
                fDot = true;
                _pos++;
            }

            while (IsDigit(text[_pos]))
            {
                _pos++;
            }

            if (text[_pos] == _exponentL || text[_pos] == _exponentU)
            {
                fSientific = true;
                _pos++;

                if (text[_pos] == '-' || text[_pos] == '+')
                {
                    _pos++;
                }
                while (IsDigit(text[_pos]))
                {
                    _pos++;
                }
            }
            if (fSientific)
                _token = Tokens.Float;
            else if (fDot)
                _token = Tokens.Decimal;
            else
                _token = Tokens.Numeric;
        }
        /// <summary>
        ///     Just a string of alphanumeric characters.
        /// </summary>
        private void ScanName()
        {
            char[] text = _text;

            while (IsAlphaNumeric(text[_pos]))
                _pos++;

            _token = Tokens.Name;
        }

        /// <summary>
        ///      recognize bracketed identifiers.
        ///      Special case: we are using '\' character to escape '[' and ']' only, so '\' by itself  is not an escape
        /// </summary>
        private void ScanName(char chEnd, char esc, string charsToEscape)
        {
            char[] text = _text;

            Debug.Assert(chEnd != '\0', "Invalid bracket value");
            Debug.Assert(esc != '\0', "Invalid escape value");
            do
            {
                if (text[_pos] == esc)
                {
                    if (_pos + 1 < text.Length && charsToEscape.Contains(text[_pos + 1]))
                    {
                        _pos++;
                    }
                }
                _pos++;
            } while (_pos < text.Length && text[_pos] != chEnd);

            if (_pos >= text.Length)
            {
                throw ExprException.InvalidNameBracketing(new string(text, _start, (_pos - 1) - _start));
            }

            Debug.Assert(text[_pos] == chEnd, "Invalid bracket value");

            _pos++;

            _token = Tokens.Name;
        }

        /// <summary>
        ///     Just read the string between '#' signs, and parse it later
        /// </summary>
        private void ScanDate()
        {
            char[] text = _text;

            do _pos++; while (_pos < text.Length && text[_pos] != '#');

            if (_pos >= text.Length || text[_pos] != '#')
            {
                // Bad date constant
                if (_pos >= text.Length)
                    throw ExprException.InvalidDate(new string(text, _start, (_pos - 1) - _start));
                else
                    throw ExprException.InvalidDate(new string(text, _start, _pos - _start));
            }
            else
            {
                _token = Tokens.Date;
            }
            _pos++;
        }

        private void ScanReserved()
        {
            char[] text = _text;

            if (IsAlpha(text[_pos]))
            {
                ScanName();

                Debug.Assert(_token == Tokens.Name, "Exprecing an identifier.");
                Debug.Assert(_pos > _start, "Exprecing an identifier.");

                string name = new string(text, _start, _pos - _start);
                Debug.Assert(name != null, "Make sure the arguments for Compare method are OK");


                CompareInfo comparer = CultureInfo.InvariantCulture.CompareInfo;
                // binary search reserved words
                int lo = 0;
                int hi = s_reservedwords.Length - 1;
                do
                {
                    int i = (lo + hi) / 2;
                    Debug.Assert(s_reservedwords[i]._word != null, "Make sure the arguments for Compare method are OK");
                    int c = comparer.Compare(s_reservedwords[i]._word, name, CompareOptions.IgnoreCase);

                    if (c == 0)
                    {
                        // we found the reserved word..
                        _token = s_reservedwords[i]._token;
                        _op = s_reservedwords[i]._op;
                        return;
                    }
                    if (c < 0)
                    {
                        lo = i + 1;
                    }
                    else
                    {
                        hi = i - 1;
                    }
                } while (lo <= hi);

                Debug.Assert(_token == Tokens.Name, "Exprecing an identifier.");
            }
        }

        private void ScanString(char escape)
        {
            char[] text = _text;

            while (_pos < text.Length)
            {
                char ch = text[_pos++];

                if (ch == escape && _pos < text.Length && text[_pos] == escape)
                {
                    _pos++;
                }
                else if (ch == escape)
                    break;
            }

            if (_pos >= text.Length)
            {
                throw ExprException.InvalidString(new string(text, _start, (_pos - 1) - _start));
            }

            _token = Tokens.StringConst;
        }

        // scan the next token, and error if it doesn't match the requested token
        internal void ScanToken(Tokens token)
        {
            Scan();
            CheckToken(token);
        }

        private void ScanWhite()
        {
            char[] text = _text;

            while (_pos < text.Length && IsWhiteSpace(text[_pos]))
            {
                _pos++;
            }
        }

        /// <summary>
        ///     is the character a whitespace character?
        ///     Consider using CharacterInfo().IsWhiteSpace(ch) (System.Globalization)
        /// </summary>
        private static bool IsWhiteSpace(char ch)
        {
            return ch <= 32 && ch != '\0';
        }

        /// <summary>
        ///     is the character an alphanumeric?
        /// </summary>
        private static bool IsAlphaNumeric(char ch)
        {
            //single comparison
            switch (ch)
            {
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '_':
                case '$':
                    return true;
                default:
                    if (ch > 0x7f)
                        return true;

                    return false;
            }
        }

        private static bool IsDigit(char ch)
        {
            //single comparison
            switch (ch)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     is the character an alpha?
        /// </summary>
        private static bool IsAlpha(char ch)
        {
            //single comparison
            switch (ch)
            {
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                case '_':
                    return true;
                default:
                    return false;
            }
        }
    }

    internal enum Tokens
    {
        None = 0,
        Name = 1, /* Identifier */
        Numeric = 2,
        Decimal = 3,
        Float = 4,
        BinaryConst = 5, /* Binary Constant e.g. 0x12ef */
        StringConst = 6,
        Date = 7,
        ListSeparator = 8, /* List Tokens.ListSeparator/Comma */
        LeftParen = 9, /* '('; */
        RightParen = 10, /* ')'; */
        ZeroOp = 11, /* 0-array operator like "NULL" */
        UnaryOp = 12,
        BinaryOp = 13,
        Child = 14,
        Parent = 15,
        Dot = 16,
        Unknown = 17, /* do not understend the token */
        EOS = 18, /* End of string */
    }

    /// <summary>
    ///     Operator stack element
    /// </summary>
    internal sealed class OperatorInfo
    {
        internal Nodes _type;
        internal int _op;
        internal int _priority;

        internal OperatorInfo(Nodes type, int op, int pri)
        {
            _type = type;
            _op = op;
            _priority = pri;
        }
    }
}
