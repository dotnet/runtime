// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal abstract class ExprVisitorBase
    {
        public EXPR Visit(EXPR pExpr)
        {
            if (pExpr == null)
            {
                return null;
            }

            EXPR pResult;
            if (IsCachedExpr(pExpr, out pResult))
            {
                return pResult;
            }

            if (pExpr.isSTMT())
            {
                return CacheExprMapping(pExpr, DispatchStatementList(pExpr.asSTMT()));
            }

            return CacheExprMapping(pExpr, Dispatch(pExpr));
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPRSTMT DispatchStatementList(EXPRSTMT expr)
        {
            Debug.Assert(expr != null);

            EXPRSTMT first = expr;
            EXPRSTMT pexpr = first;

            while (pexpr != null)
            {
                // If the processor replaces the statement -- potentially with
                // null, another statement, or a list of statements -- then we
                // make sure that the statement list is hooked together correctly.

                EXPRSTMT next = pexpr.GetOptionalNextStatement();
                EXPRSTMT old = pexpr;

                // Unhook the next one.
                pexpr.SetOptionalNextStatement(null);

                EXPR result = Dispatch(pexpr);
                Debug.Assert(result == null || result.isSTMT());

                if (pexpr == first)
                {
                    first = (result == null) ? null : result.asSTMT();
                }
                else
                {
                    pexpr.SetOptionalNextStatement((result == null) ? null : result.asSTMT());
                }

                // A transformation may return back a list of statements (or
                // if the statements have been determined to be unnecessary,
                // perhaps it has simply returned null.)
                //
                // Skip visiting the new list, then hook the tail of the old list
                // up to the end of the new list.

                while (pexpr.GetOptionalNextStatement() != null)
                {
                    pexpr = pexpr.GetOptionalNextStatement();
                }

                // Re-hook the next pointer.
                pexpr.SetOptionalNextStatement(next);
            }
            return first;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private bool IsCachedExpr(EXPR pExpr, out EXPR pTransformedExpr)
        {
            pTransformedExpr = null;
            return false;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR CacheExprMapping(EXPR pExpr, EXPR pTransformedExpr)
        {
            return pTransformedExpr;
        }

        protected virtual EXPR Dispatch(EXPR pExpr)
        {
            switch (pExpr.kind)
            {
                case ExpressionKind.EK_BLOCK:
                    return VisitBLOCK(pExpr as EXPRBLOCK);
                case ExpressionKind.EK_RETURN:
                    return VisitRETURN(pExpr as EXPRRETURN);
                case ExpressionKind.EK_BINOP:
                    return VisitBINOP(pExpr as EXPRBINOP);
                case ExpressionKind.EK_UNARYOP:
                    return VisitUNARYOP(pExpr as EXPRUNARYOP);
                case ExpressionKind.EK_ASSIGNMENT:
                    return VisitASSIGNMENT(pExpr as EXPRASSIGNMENT);
                case ExpressionKind.EK_LIST:
                    return VisitLIST(pExpr as EXPRLIST);
                case ExpressionKind.EK_QUESTIONMARK:
                    return VisitQUESTIONMARK(pExpr as EXPRQUESTIONMARK);
                case ExpressionKind.EK_ARRAYINDEX:
                    return VisitARRAYINDEX(pExpr as EXPRARRAYINDEX);
                case ExpressionKind.EK_ARRAYLENGTH:
                    return VisitARRAYLENGTH(pExpr as EXPRARRAYLENGTH);
                case ExpressionKind.EK_CALL:
                    return VisitCALL(pExpr as EXPRCALL);
                case ExpressionKind.EK_EVENT:
                    return VisitEVENT(pExpr as EXPREVENT);
                case ExpressionKind.EK_FIELD:
                    return VisitFIELD(pExpr as EXPRFIELD);
                case ExpressionKind.EK_LOCAL:
                    return VisitLOCAL(pExpr as EXPRLOCAL);
                case ExpressionKind.EK_THISPOINTER:
                    return VisitTHISPOINTER(pExpr as EXPRTHISPOINTER);
                case ExpressionKind.EK_CONSTANT:
                    return VisitCONSTANT(pExpr as EXPRCONSTANT);
                case ExpressionKind.EK_TYPEARGUMENTS:
                    return VisitTYPEARGUMENTS(pExpr as EXPRTYPEARGUMENTS);
                case ExpressionKind.EK_TYPEORNAMESPACE:
                    return VisitTYPEORNAMESPACE(pExpr as EXPRTYPEORNAMESPACE);
                case ExpressionKind.EK_CLASS:
                    return VisitCLASS(pExpr as EXPRCLASS);
                case ExpressionKind.EK_FUNCPTR:
                    return VisitFUNCPTR(pExpr as EXPRFUNCPTR);
                case ExpressionKind.EK_PROP:
                    return VisitPROP(pExpr as EXPRPROP);
                case ExpressionKind.EK_MULTI:
                    return VisitMULTI(pExpr as EXPRMULTI);
                case ExpressionKind.EK_MULTIGET:
                    return VisitMULTIGET(pExpr as EXPRMULTIGET);
                case ExpressionKind.EK_WRAP:
                    return VisitWRAP(pExpr as EXPRWRAP);
                case ExpressionKind.EK_CONCAT:
                    return VisitCONCAT(pExpr as EXPRCONCAT);
                case ExpressionKind.EK_ARRINIT:
                    return VisitARRINIT(pExpr as EXPRARRINIT);
                case ExpressionKind.EK_CAST:
                    return VisitCAST(pExpr as EXPRCAST);
                case ExpressionKind.EK_USERDEFINEDCONVERSION:
                    return VisitUSERDEFINEDCONVERSION(pExpr as EXPRUSERDEFINEDCONVERSION);
                case ExpressionKind.EK_TYPEOF:
                    return VisitTYPEOF(pExpr as EXPRTYPEOF);
                case ExpressionKind.EK_ZEROINIT:
                    return VisitZEROINIT(pExpr as EXPRZEROINIT);
                case ExpressionKind.EK_USERLOGOP:
                    return VisitUSERLOGOP(pExpr as EXPRUSERLOGOP);
                case ExpressionKind.EK_MEMGRP:
                    return VisitMEMGRP(pExpr as EXPRMEMGRP);
                case ExpressionKind.EK_BOUNDLAMBDA:
                    return VisitBOUNDLAMBDA(pExpr as EXPRBOUNDLAMBDA);
                case ExpressionKind.EK_UNBOUNDLAMBDA:
                    return VisitUNBOUNDLAMBDA(pExpr as EXPRUNBOUNDLAMBDA);
                case ExpressionKind.EK_HOISTEDLOCALEXPR:
                    return VisitHOISTEDLOCALEXPR(pExpr as EXPRHOISTEDLOCALEXPR);
                case ExpressionKind.EK_FIELDINFO:
                    return VisitFIELDINFO(pExpr as EXPRFIELDINFO);
                case ExpressionKind.EK_METHODINFO:
                    return VisitMETHODINFO(pExpr as EXPRMETHODINFO);

                // Binary operators
                case ExpressionKind.EK_EQUALS:
                    return VisitEQUALS(pExpr.asBIN());
                case ExpressionKind.EK_COMPARE:
                    return VisitCOMPARE(pExpr.asBIN());
                case ExpressionKind.EK_NE:
                    return VisitNE(pExpr.asBIN());
                case ExpressionKind.EK_LT:
                    return VisitLT(pExpr.asBIN());
                case ExpressionKind.EK_LE:
                    return VisitLE(pExpr.asBIN());
                case ExpressionKind.EK_GT:
                    return VisitGT(pExpr.asBIN());
                case ExpressionKind.EK_GE:
                    return VisitGE(pExpr.asBIN());
                case ExpressionKind.EK_ADD:
                    return VisitADD(pExpr.asBIN());
                case ExpressionKind.EK_SUB:
                    return VisitSUB(pExpr.asBIN());
                case ExpressionKind.EK_MUL:
                    return VisitMUL(pExpr.asBIN());
                case ExpressionKind.EK_DIV:
                    return VisitDIV(pExpr.asBIN());
                case ExpressionKind.EK_MOD:
                    return VisitMOD(pExpr.asBIN());
                case ExpressionKind.EK_BITAND:
                    return VisitBITAND(pExpr.asBIN());
                case ExpressionKind.EK_BITOR:
                    return VisitBITOR(pExpr.asBIN());
                case ExpressionKind.EK_BITXOR:
                    return VisitBITXOR(pExpr.asBIN());
                case ExpressionKind.EK_LSHIFT:
                    return VisitLSHIFT(pExpr.asBIN());
                case ExpressionKind.EK_RSHIFT:
                    return VisitRSHIFT(pExpr.asBIN());
                case ExpressionKind.EK_LOGAND:
                    return VisitLOGAND(pExpr.asBIN());
                case ExpressionKind.EK_LOGOR:
                    return VisitLOGOR(pExpr.asBIN());
                case ExpressionKind.EK_SEQUENCE:
                    return VisitSEQUENCE(pExpr.asBIN());
                case ExpressionKind.EK_SEQREV:
                    return VisitSEQREV(pExpr.asBIN());
                case ExpressionKind.EK_SAVE:
                    return VisitSAVE(pExpr.asBIN());
                case ExpressionKind.EK_SWAP:
                    return VisitSWAP(pExpr.asBIN());
                case ExpressionKind.EK_INDIR:
                    return VisitINDIR(pExpr.asBIN());
                case ExpressionKind.EK_STRINGEQ:
                    return VisitSTRINGEQ(pExpr.asBIN());
                case ExpressionKind.EK_STRINGNE:
                    return VisitSTRINGNE(pExpr.asBIN());
                case ExpressionKind.EK_DELEGATEEQ:
                    return VisitDELEGATEEQ(pExpr.asBIN());
                case ExpressionKind.EK_DELEGATENE:
                    return VisitDELEGATENE(pExpr.asBIN());
                case ExpressionKind.EK_DELEGATEADD:
                    return VisitDELEGATEADD(pExpr.asBIN());
                case ExpressionKind.EK_DELEGATESUB:
                    return VisitDELEGATESUB(pExpr.asBIN());
                case ExpressionKind.EK_EQ:
                    return VisitEQ(pExpr.asBIN());

                // Unary operators
                case ExpressionKind.EK_TRUE:
                    return VisitTRUE(pExpr.asUnaryOperator());
                case ExpressionKind.EK_FALSE:
                    return VisitFALSE(pExpr.asUnaryOperator());
                case ExpressionKind.EK_INC:
                    return VisitINC(pExpr.asUnaryOperator());
                case ExpressionKind.EK_DEC:
                    return VisitDEC(pExpr.asUnaryOperator());
                case ExpressionKind.EK_LOGNOT:
                    return VisitLOGNOT(pExpr.asUnaryOperator());
                case ExpressionKind.EK_NEG:
                    return VisitNEG(pExpr.asUnaryOperator());
                case ExpressionKind.EK_UPLUS:
                    return VisitUPLUS(pExpr.asUnaryOperator());
                case ExpressionKind.EK_BITNOT:
                    return VisitBITNOT(pExpr.asUnaryOperator());
                case ExpressionKind.EK_ADDR:
                    return VisitADDR(pExpr.asUnaryOperator());
                case ExpressionKind.EK_DECIMALNEG:
                    return VisitDECIMALNEG(pExpr.asUnaryOperator());
                case ExpressionKind.EK_DECIMALINC:
                    return VisitDECIMALINC(pExpr.asUnaryOperator());
                case ExpressionKind.EK_DECIMALDEC:
                    return VisitDECIMALDEC(pExpr.asUnaryOperator());
                default:
                    throw Error.InternalCompilerError();
            }
        }
        private void VisitChildren(EXPR pExpr)
        {
            Debug.Assert(pExpr != null);

            EXPR exprRet = null;

            // Lists are a special case.  We treat a list not as a
            // binary node but rather as a node with n children.
            if (pExpr.isLIST())
            {
                EXPRLIST list = pExpr.asLIST();
                while (true)
                {
                    list.SetOptionalElement(Visit(list.GetOptionalElement()));
                    if (list.GetOptionalNextListNode() == null)
                    {
                        return;
                    }
                    if (!list.GetOptionalNextListNode().isLIST())
                    {
                        list.SetOptionalNextListNode(Visit(list.GetOptionalNextListNode()));
                        return;
                    }
                    list = list.GetOptionalNextListNode().asLIST();
                }
            }

            switch (pExpr.kind)
            {
                default:
                    if (pExpr.isUnaryOperator())
                    {
                        goto VISIT_EXPRUNARYOP;
                    }
                    Debug.Assert(pExpr.isBIN());
                    goto VISIT_EXPRBINOP;

                VISIT_EXPR:
                    break;
                VISIT_BASE_EXPRSTMT:
                    goto VISIT_EXPR;
                VISIT_EXPRSTMT:
                    goto VISIT_BASE_EXPRSTMT;

                case ExpressionKind.EK_BINOP:
                    goto VISIT_EXPRBINOP;
                VISIT_BASE_EXPRBINOP:
                    goto VISIT_EXPR;
                VISIT_EXPRBINOP:
                    exprRet = Visit((pExpr as EXPRBINOP).GetOptionalLeftChild());
                    (pExpr as EXPRBINOP).SetOptionalLeftChild(exprRet as EXPR);
                    exprRet = Visit((pExpr as EXPRBINOP).GetOptionalRightChild());
                    (pExpr as EXPRBINOP).SetOptionalRightChild(exprRet as EXPR);
                    goto VISIT_BASE_EXPRBINOP;

                case ExpressionKind.EK_LIST:
                    goto VISIT_EXPRLIST;
                VISIT_BASE_EXPRLIST:
                    goto VISIT_EXPR;
                VISIT_EXPRLIST:
                    exprRet = Visit((pExpr as EXPRLIST).GetOptionalElement());
                    (pExpr as EXPRLIST).SetOptionalElement(exprRet as EXPR);
                    exprRet = Visit((pExpr as EXPRLIST).GetOptionalNextListNode());
                    (pExpr as EXPRLIST).SetOptionalNextListNode(exprRet as EXPR);
                    goto VISIT_BASE_EXPRLIST;

                case ExpressionKind.EK_ASSIGNMENT:
                    goto VISIT_EXPRASSIGNMENT;
                VISIT_BASE_EXPRASSIGNMENT:
                    goto VISIT_EXPR;
                VISIT_EXPRASSIGNMENT:
                    exprRet = Visit((pExpr as EXPRASSIGNMENT).GetLHS());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRASSIGNMENT).SetLHS(exprRet as EXPR);
                    exprRet = Visit((pExpr as EXPRASSIGNMENT).GetRHS());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRASSIGNMENT).SetRHS(exprRet as EXPR);
                    goto VISIT_BASE_EXPRASSIGNMENT;

                case ExpressionKind.EK_QUESTIONMARK:
                    goto VISIT_EXPRQUESTIONMARK;
                VISIT_BASE_EXPRQUESTIONMARK:
                    goto VISIT_EXPR;
                VISIT_EXPRQUESTIONMARK:
                    exprRet = Visit((pExpr as EXPRQUESTIONMARK).GetTestExpression());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRQUESTIONMARK).SetTestExpression(exprRet as EXPR);
                    exprRet = Visit((pExpr as EXPRQUESTIONMARK).GetConsequence());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRQUESTIONMARK).SetConsequence(exprRet as EXPRBINOP);
                    goto VISIT_BASE_EXPRQUESTIONMARK;

                case ExpressionKind.EK_ARRAYINDEX:
                    goto VISIT_EXPRARRAYINDEX;
                VISIT_BASE_EXPRARRAYINDEX:
                    goto VISIT_EXPR;
                VISIT_EXPRARRAYINDEX:
                    exprRet = Visit((pExpr as EXPRARRAYINDEX).GetArray());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRARRAYINDEX).SetArray(exprRet as EXPR);
                    exprRet = Visit((pExpr as EXPRARRAYINDEX).GetIndex());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRARRAYINDEX).SetIndex(exprRet as EXPR);
                    goto VISIT_BASE_EXPRARRAYINDEX;

                case ExpressionKind.EK_ARRAYLENGTH:
                    goto VISIT_EXPRARRAYLENGTH;
                VISIT_BASE_EXPRARRAYLENGTH:
                    goto VISIT_EXPR;
                VISIT_EXPRARRAYLENGTH:
                    exprRet = Visit((pExpr as EXPRARRAYLENGTH).GetArray());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRARRAYLENGTH).SetArray(exprRet as EXPR);
                    goto VISIT_BASE_EXPRARRAYLENGTH;

                case ExpressionKind.EK_UNARYOP:
                    goto VISIT_EXPRUNARYOP;
                VISIT_BASE_EXPRUNARYOP:
                    goto VISIT_EXPR;
                VISIT_EXPRUNARYOP:
                    exprRet = Visit((pExpr as EXPRUNARYOP).Child);
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRUNARYOP).Child = exprRet as EXPR;
                    goto VISIT_BASE_EXPRUNARYOP;

                case ExpressionKind.EK_USERLOGOP:
                    goto VISIT_EXPRUSERLOGOP;
                VISIT_BASE_EXPRUSERLOGOP:
                    goto VISIT_EXPR;
                VISIT_EXPRUSERLOGOP:
                    exprRet = Visit((pExpr as EXPRUSERLOGOP).TrueFalseCall);
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRUSERLOGOP).TrueFalseCall = exprRet as EXPR;
                    exprRet = Visit((pExpr as EXPRUSERLOGOP).OperatorCall);
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRUSERLOGOP).OperatorCall = exprRet as EXPRCALL;
                    exprRet = Visit((pExpr as EXPRUSERLOGOP).FirstOperandToExamine);
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRUSERLOGOP).FirstOperandToExamine = exprRet as EXPR;
                    goto VISIT_BASE_EXPRUSERLOGOP;

                case ExpressionKind.EK_TYPEOF:
                    goto VISIT_EXPRTYPEOF;
                VISIT_BASE_EXPRTYPEOF:
                    goto VISIT_EXPR;
                VISIT_EXPRTYPEOF:
                    exprRet = Visit((pExpr as EXPRTYPEOF).GetSourceType());
                    (pExpr as EXPRTYPEOF).SetSourceType(exprRet as EXPRTYPEORNAMESPACE);
                    goto VISIT_BASE_EXPRTYPEOF;

                case ExpressionKind.EK_CAST:
                    goto VISIT_EXPRCAST;
                VISIT_BASE_EXPRCAST:
                    goto VISIT_EXPR;
                VISIT_EXPRCAST:
                    exprRet = Visit((pExpr as EXPRCAST).GetArgument());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRCAST).SetArgument(exprRet as EXPR);
                    exprRet = Visit((pExpr as EXPRCAST).GetDestinationType());
                    (pExpr as EXPRCAST).SetDestinationType(exprRet as EXPRTYPEORNAMESPACE);
                    goto VISIT_BASE_EXPRCAST;

                case ExpressionKind.EK_USERDEFINEDCONVERSION:
                    goto VISIT_EXPRUSERDEFINEDCONVERSION;
                VISIT_BASE_EXPRUSERDEFINEDCONVERSION:
                    goto VISIT_EXPR;
                VISIT_EXPRUSERDEFINEDCONVERSION:
                    exprRet = Visit((pExpr as EXPRUSERDEFINEDCONVERSION).UserDefinedCall);
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRUSERDEFINEDCONVERSION).UserDefinedCall = exprRet as EXPR;
                    goto VISIT_BASE_EXPRUSERDEFINEDCONVERSION;

                case ExpressionKind.EK_ZEROINIT:
                    goto VISIT_EXPRZEROINIT;
                VISIT_BASE_EXPRZEROINIT:
                    goto VISIT_EXPR;
                VISIT_EXPRZEROINIT:
                    exprRet = Visit((pExpr as EXPRZEROINIT).OptionalArgument);
                    (pExpr as EXPRZEROINIT).OptionalArgument = exprRet as EXPR;
                    // Used for when we zeroinit 0 parameter constructors for structs/enums.
                    exprRet = Visit((pExpr as EXPRZEROINIT).OptionalConstructorCall);
                    (pExpr as EXPRZEROINIT).OptionalConstructorCall = exprRet as EXPR;
                    goto VISIT_BASE_EXPRZEROINIT;

                case ExpressionKind.EK_BLOCK:
                    goto VISIT_EXPRBLOCK;
                VISIT_BASE_EXPRBLOCK:
                    goto VISIT_EXPRSTMT;
                VISIT_EXPRBLOCK:
                    exprRet = Visit((pExpr as EXPRBLOCK).GetOptionalStatements());
                    (pExpr as EXPRBLOCK).SetOptionalStatements(exprRet as EXPRSTMT);
                    goto VISIT_BASE_EXPRBLOCK;

                case ExpressionKind.EK_MEMGRP:
                    goto VISIT_EXPRMEMGRP;
                VISIT_BASE_EXPRMEMGRP:
                    goto VISIT_EXPR;
                VISIT_EXPRMEMGRP:
                    // The object expression. NULL for a static invocation.
                    exprRet = Visit((pExpr as EXPRMEMGRP).GetOptionalObject());
                    (pExpr as EXPRMEMGRP).SetOptionalObject(exprRet as EXPR);
                    goto VISIT_BASE_EXPRMEMGRP;

                case ExpressionKind.EK_CALL:
                    goto VISIT_EXPRCALL;
                VISIT_BASE_EXPRCALL:
                    goto VISIT_EXPR;
                VISIT_EXPRCALL:
                    exprRet = Visit((pExpr as EXPRCALL).GetOptionalArguments());
                    (pExpr as EXPRCALL).SetOptionalArguments(exprRet as EXPR);
                    exprRet = Visit((pExpr as EXPRCALL).GetMemberGroup());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRCALL).SetMemberGroup(exprRet as EXPRMEMGRP);
                    goto VISIT_BASE_EXPRCALL;


                case ExpressionKind.EK_PROP:
                    goto VISIT_EXPRPROP;
                VISIT_BASE_EXPRPROP:
                    goto VISIT_EXPR;
                VISIT_EXPRPROP:
                    exprRet = Visit((pExpr as EXPRPROP).GetOptionalArguments());
                    (pExpr as EXPRPROP).SetOptionalArguments(exprRet as EXPR);
                    exprRet = Visit((pExpr as EXPRPROP).GetMemberGroup());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRPROP).SetMemberGroup(exprRet as EXPRMEMGRP);
                    goto VISIT_BASE_EXPRPROP;

                case ExpressionKind.EK_FIELD:
                    goto VISIT_EXPRFIELD;
                VISIT_BASE_EXPRFIELD:
                    goto VISIT_EXPR;
                VISIT_EXPRFIELD:
                    exprRet = Visit((pExpr as EXPRFIELD).GetOptionalObject());
                    (pExpr as EXPRFIELD).SetOptionalObject(exprRet as EXPR);
                    goto VISIT_BASE_EXPRFIELD;

                case ExpressionKind.EK_EVENT:
                    goto VISIT_EXPREVENT;
                VISIT_BASE_EXPREVENT:
                    goto VISIT_EXPR;
                VISIT_EXPREVENT:
                    exprRet = Visit((pExpr as EXPREVENT).OptionalObject);
                    (pExpr as EXPREVENT).OptionalObject = exprRet as EXPR;
                    goto VISIT_BASE_EXPREVENT;

                case ExpressionKind.EK_LOCAL:
                    goto VISIT_EXPRLOCAL;
                VISIT_BASE_EXPRLOCAL:
                    goto VISIT_EXPR;
                VISIT_EXPRLOCAL:
                    goto VISIT_BASE_EXPRLOCAL;

                case ExpressionKind.EK_THISPOINTER:
                    goto VISIT_EXPRTHISPOINTER;
                VISIT_BASE_EXPRTHISPOINTER:
                    goto VISIT_EXPRLOCAL;
                VISIT_EXPRTHISPOINTER:
                    goto VISIT_BASE_EXPRTHISPOINTER;

                case ExpressionKind.EK_RETURN:
                    goto VISIT_EXPRRETURN;
                VISIT_BASE_EXPRRETURN:
                    goto VISIT_EXPRSTMT;
                VISIT_EXPRRETURN:
                    exprRet = Visit((pExpr as EXPRRETURN).GetOptionalObject());
                    (pExpr as EXPRRETURN).SetOptionalObject(exprRet as EXPR);
                    goto VISIT_BASE_EXPRRETURN;

                case ExpressionKind.EK_CONSTANT:
                    goto VISIT_EXPRCONSTANT;
                VISIT_BASE_EXPRCONSTANT:
                    goto VISIT_EXPR;
                VISIT_EXPRCONSTANT:
                    // Used for when we zeroinit 0 parameter constructors for structs/enums.
                    exprRet = Visit((pExpr as EXPRCONSTANT).GetOptionalConstructorCall());
                    (pExpr as EXPRCONSTANT).SetOptionalConstructorCall(exprRet as EXPR);
                    goto VISIT_BASE_EXPRCONSTANT;

                /*************************************************************************************************
                  TYPEEXPRs defined:

                  The following exprs are used to represent the results of type binding, and are defined as follows:

                  TYPEARGUMENTS - This wraps the type arguments for a class. It contains the TypeArray* which is
                    associated with the AggregateType for the instantiation of the class. 

                  TYPEORNAMESPACE - This is the base class for this set of EXPRs. When binding a type, the result
                    must be a type or a namespace. This EXPR encapsulates that fact. The lhs member is the EXPR 
                    tree that was bound to resolve the type or namespace.

                  TYPEORNAMESPACEERROR - This is the error class for the type or namespace exprs when we don't know
                    what to bind it to.

                  The following three exprs all have a TYPEORNAMESPACE child, which is their fundamental type:
                    POINTERTYPE - This wraps the sym for the pointer type.
                    NULLABLETYPE - This wraps the sym for the nullable type.

                  CLASS - This represents an instantiation of a class.

                  NSPACE - This represents a namespace, which is the intermediate step when attempting to bind
                    a qualified name.

                  ALIAS - This represents an alias

                *************************************************************************************************/

                case ExpressionKind.EK_TYPEARGUMENTS:
                    goto VISIT_EXPRTYPEARGUMENTS;
                VISIT_BASE_EXPRTYPEARGUMENTS:
                    goto VISIT_EXPR;
                VISIT_EXPRTYPEARGUMENTS:
                    exprRet = Visit((pExpr as EXPRTYPEARGUMENTS).GetOptionalElements());
                    (pExpr as EXPRTYPEARGUMENTS).SetOptionalElements(exprRet as EXPR);
                    goto VISIT_BASE_EXPRTYPEARGUMENTS;

                case ExpressionKind.EK_TYPEORNAMESPACE:
                    goto VISIT_EXPRTYPEORNAMESPACE;
                VISIT_BASE_EXPRTYPEORNAMESPACE:
                    goto VISIT_EXPR;
                VISIT_EXPRTYPEORNAMESPACE:
                    goto VISIT_BASE_EXPRTYPEORNAMESPACE;

                case ExpressionKind.EK_CLASS:
                    goto VISIT_EXPRCLASS;
                VISIT_BASE_EXPRCLASS:
                    goto VISIT_EXPRTYPEORNAMESPACE;
                VISIT_EXPRCLASS:
                    goto VISIT_BASE_EXPRCLASS;

                case ExpressionKind.EK_FUNCPTR:
                    goto VISIT_EXPRFUNCPTR;
                VISIT_BASE_EXPRFUNCPTR:
                    goto VISIT_EXPR;
                VISIT_EXPRFUNCPTR:
                    goto VISIT_BASE_EXPRFUNCPTR;

                case ExpressionKind.EK_MULTIGET:
                    goto VISIT_EXPRMULTIGET;
                VISIT_BASE_EXPRMULTIGET:
                    goto VISIT_EXPR;
                VISIT_EXPRMULTIGET:
                    goto VISIT_BASE_EXPRMULTIGET;

                case ExpressionKind.EK_MULTI:
                    goto VISIT_EXPRMULTI;
                VISIT_BASE_EXPRMULTI:
                    goto VISIT_EXPR;
                VISIT_EXPRMULTI:
                    exprRet = Visit((pExpr as EXPRMULTI).GetLeft());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRMULTI).SetLeft(exprRet as EXPR);
                    exprRet = Visit((pExpr as EXPRMULTI).GetOperator());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRMULTI).SetOperator(exprRet as EXPR);
                    goto VISIT_BASE_EXPRMULTI;

                case ExpressionKind.EK_WRAP:
                    goto VISIT_EXPRWRAP;
                VISIT_BASE_EXPRWRAP:
                    goto VISIT_EXPR;
                VISIT_EXPRWRAP:
                    goto VISIT_BASE_EXPRWRAP;

                case ExpressionKind.EK_CONCAT:
                    goto VISIT_EXPRCONCAT;
                VISIT_BASE_EXPRCONCAT:
                    goto VISIT_EXPR;
                VISIT_EXPRCONCAT:
                    exprRet = Visit((pExpr as EXPRCONCAT).GetFirstArgument());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRCONCAT).SetFirstArgument(exprRet as EXPR);
                    exprRet = Visit((pExpr as EXPRCONCAT).GetSecondArgument());
                    Debug.Assert(exprRet != null);
                    (pExpr as EXPRCONCAT).SetSecondArgument(exprRet as EXPR);
                    goto VISIT_BASE_EXPRCONCAT;

                case ExpressionKind.EK_ARRINIT:
                    goto VISIT_EXPRARRINIT;
                VISIT_BASE_EXPRARRINIT:
                    goto VISIT_EXPR;
                VISIT_EXPRARRINIT:
                    exprRet = Visit((pExpr as EXPRARRINIT).GetOptionalArguments());
                    (pExpr as EXPRARRINIT).SetOptionalArguments(exprRet as EXPR);
                    exprRet = Visit((pExpr as EXPRARRINIT).GetOptionalArgumentDimensions());
                    (pExpr as EXPRARRINIT).SetOptionalArgumentDimensions(exprRet as EXPR);
                    goto VISIT_BASE_EXPRARRINIT;

                case ExpressionKind.EK_NOOP:
                    goto VISIT_EXPRNOOP;
                VISIT_BASE_EXPRNOOP:
                    goto VISIT_EXPRSTMT;
                VISIT_EXPRNOOP:
                    goto VISIT_BASE_EXPRNOOP;

                case ExpressionKind.EK_BOUNDLAMBDA:
                    goto VISIT_EXPRBOUNDLAMBDA;
                VISIT_BASE_EXPRBOUNDLAMBDA:
                    goto VISIT_EXPR;
                VISIT_EXPRBOUNDLAMBDA:
                    exprRet = Visit((pExpr as EXPRBOUNDLAMBDA).OptionalBody);
                    (pExpr as EXPRBOUNDLAMBDA).OptionalBody = exprRet as EXPRBLOCK;
                    goto VISIT_BASE_EXPRBOUNDLAMBDA;

                case ExpressionKind.EK_UNBOUNDLAMBDA:
                    goto VISIT_EXPRUNBOUNDLAMBDA;
                VISIT_BASE_EXPRUNBOUNDLAMBDA:
                    goto VISIT_EXPR;
                VISIT_EXPRUNBOUNDLAMBDA:
                    goto VISIT_BASE_EXPRUNBOUNDLAMBDA;

                case ExpressionKind.EK_HOISTEDLOCALEXPR:
                    goto VISIT_EXPRHOISTEDLOCALEXPR;
                VISIT_BASE_EXPRHOISTEDLOCALEXPR:
                    goto VISIT_EXPR;
                VISIT_EXPRHOISTEDLOCALEXPR:
                    goto VISIT_BASE_EXPRHOISTEDLOCALEXPR;

                case ExpressionKind.EK_FIELDINFO:
                    goto VISIT_EXPRFIELDINFO;
                VISIT_BASE_EXPRFIELDINFO:
                    goto VISIT_EXPR;
                VISIT_EXPRFIELDINFO:
                    goto VISIT_BASE_EXPRFIELDINFO;

                case ExpressionKind.EK_METHODINFO:
                    goto VISIT_EXPRMETHODINFO;
                VISIT_BASE_EXPRMETHODINFO:
                    goto VISIT_EXPR;
                VISIT_EXPRMETHODINFO:
                    goto VISIT_BASE_EXPRMETHODINFO;
            }
        }
        protected virtual EXPR VisitEXPR(EXPR pExpr)
        {
            VisitChildren(pExpr);
            return pExpr;
        }
        protected virtual EXPR VisitBLOCK(EXPRBLOCK pExpr)
        {
            return VisitSTMT(pExpr);
        }
        protected virtual EXPR VisitTHISPOINTER(EXPRTHISPOINTER pExpr)
        {
            return VisitLOCAL(pExpr);
        }
        protected virtual EXPR VisitRETURN(EXPRRETURN pExpr)
        {
            return VisitSTMT(pExpr);
        }
        protected virtual EXPR VisitCLASS(EXPRCLASS pExpr)
        {
            return VisitTYPEORNAMESPACE(pExpr);
        }
        protected virtual EXPR VisitSTMT(EXPRSTMT pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitBINOP(EXPRBINOP pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitLIST(EXPRLIST pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitASSIGNMENT(EXPRASSIGNMENT pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitQUESTIONMARK(EXPRQUESTIONMARK pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitARRAYINDEX(EXPRARRAYINDEX pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitARRAYLENGTH(EXPRARRAYLENGTH pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitUNARYOP(EXPRUNARYOP pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitUSERLOGOP(EXPRUSERLOGOP pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitTYPEOF(EXPRTYPEOF pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitCAST(EXPRCAST pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitUSERDEFINEDCONVERSION(EXPRUSERDEFINEDCONVERSION pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitZEROINIT(EXPRZEROINIT pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitMEMGRP(EXPRMEMGRP pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitCALL(EXPRCALL pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitPROP(EXPRPROP pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitFIELD(EXPRFIELD pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitEVENT(EXPREVENT pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitLOCAL(EXPRLOCAL pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitCONSTANT(EXPRCONSTANT pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitTYPEARGUMENTS(EXPRTYPEARGUMENTS pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitTYPEORNAMESPACE(EXPRTYPEORNAMESPACE pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitFUNCPTR(EXPRFUNCPTR pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitMULTIGET(EXPRMULTIGET pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitMULTI(EXPRMULTI pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitWRAP(EXPRWRAP pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitCONCAT(EXPRCONCAT pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitARRINIT(EXPRARRINIT pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitBOUNDLAMBDA(EXPRBOUNDLAMBDA pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitUNBOUNDLAMBDA(EXPRUNBOUNDLAMBDA pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitHOISTEDLOCALEXPR(EXPRHOISTEDLOCALEXPR pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitFIELDINFO(EXPRFIELDINFO pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitMETHODINFO(EXPRMETHODINFO pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual EXPR VisitEQUALS(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitCOMPARE(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitEQ(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitNE(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitLE(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitGE(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitADD(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitSUB(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitDIV(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitBITAND(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitBITOR(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitLSHIFT(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitLOGAND(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitSEQUENCE(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitSAVE(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitINDIR(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitSTRINGEQ(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitDELEGATEEQ(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitDELEGATEADD(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitRANGE(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitLT(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitMUL(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitBITXOR(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitRSHIFT(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitLOGOR(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitSEQREV(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitSTRINGNE(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitDELEGATENE(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitGT(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitMOD(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitSWAP(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitDELEGATESUB(EXPRBINOP pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual EXPR VisitTRUE(EXPRUNARYOP pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual EXPR VisitINC(EXPRUNARYOP pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual EXPR VisitLOGNOT(EXPRUNARYOP pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual EXPR VisitNEG(EXPRUNARYOP pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual EXPR VisitBITNOT(EXPRUNARYOP pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual EXPR VisitADDR(EXPRUNARYOP pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual EXPR VisitDECIMALNEG(EXPRUNARYOP pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual EXPR VisitDECIMALDEC(EXPRUNARYOP pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual EXPR VisitFALSE(EXPRUNARYOP pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual EXPR VisitDEC(EXPRUNARYOP pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual EXPR VisitUPLUS(EXPRUNARYOP pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual EXPR VisitDECIMALINC(EXPRUNARYOP pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
    }
}
