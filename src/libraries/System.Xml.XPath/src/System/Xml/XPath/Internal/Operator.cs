// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        static Op[] invertOp = {
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

        static public Operator.Op InvertOperator(Operator.Op op)
        {
            Debug.Assert(Op.EQ <= op && op <= Op.GE);
            return invertOp[(int)op];
        }

        private Op opType;
        private AstNode opnd1;
        private AstNode opnd2;

        public Operator(Op op, AstNode opnd1, AstNode opnd2)
        {
            this.opType = op;
            this.opnd1 = opnd1;
            this.opnd2 = opnd2;
        }

        public override AstType Type { get { return AstType.Operator; } }
        public override XPathResultType ReturnType
        {
            get
            {
                if (opType <= Op.GE)
                {
                    return XPathResultType.Boolean;
                }
                if (opType <= Op.MOD)
                {
                    return XPathResultType.Number;
                }
                return XPathResultType.NodeSet;
            }
        }

        public Op OperatorType { get { return opType; } }
        public AstNode Operand1 { get { return opnd1; } }
        public AstNode Operand2 { get { return opnd2; } }
    }
}
