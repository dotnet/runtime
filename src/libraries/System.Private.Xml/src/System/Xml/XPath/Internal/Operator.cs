// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal class Operator : AstNode
    {
        public enum Op
        { // order is aligned with XPathOperator
            INVALID,
            /*Logical   */
            OR,
            AND,
            /*Equality  */
            EQ,
            NE,
            /*Relational*/
            LT,
            LE,
            GT,
            GE,
            /*Arithmetic*/
            PLUS,
            MINUS,
            MUL,
            DIV,
            MOD,
            /*Union     */
            UNION,
        };

        private static readonly Op[] s_invertOp = {
            /*INVALID*/ Op.INVALID,
            /*OR     */ Op.INVALID,
            /*END    */ Op.INVALID,
            /*EQ     */ Op.EQ,
            /*NE     */ Op.NE,
            /*LT     */ Op.GT,
            /*LE     */ Op.GE,
            /*GT     */ Op.LT,
            /*GE     */ Op.LE,
        };

        public static Operator.Op InvertOperator(Operator.Op op)
        {
            Debug.Assert(Op.EQ <= op && op <= Op.GE);
            return s_invertOp[(int)op];
        }

        private readonly Op _opType;
        private readonly AstNode _opnd1;
        private readonly AstNode _opnd2;

        public Operator(Op op, AstNode opnd1, AstNode opnd2)
        {
            _opType = op;
            _opnd1 = opnd1;
            _opnd2 = opnd2;
        }

        public override AstType Type { get { return AstType.Operator; } }
        public override XPathResultType ReturnType
        {
            get
            {
                if (_opType <= Op.GE)
                {
                    return XPathResultType.Boolean;
                }
                if (_opType <= Op.MOD)
                {
                    return XPathResultType.Number;
                }
                return XPathResultType.NodeSet;
            }
        }

        public Op OperatorType { get { return _opType; } }
        public AstNode Operand1 { get { return _opnd1; } }
        public AstNode Operand2 { get { return _opnd2; } }
    }
}
