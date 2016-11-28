// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class ExpressionTreeRewriter : ExprVisitorBase
    {
        public static EXPR Rewrite(EXPR expr, ExprFactory expressionFactory, SymbolLoader symbolLoader)
        {
            ExpressionTreeRewriter rewriter = new ExpressionTreeRewriter(expressionFactory, symbolLoader);
            rewriter.alwaysRewrite = true;
            return rewriter.Visit(expr);
        }

        protected ExprFactory expressionFactory;
        protected SymbolLoader symbolLoader;
        protected EXPRBOUNDLAMBDA currentAnonMeth;
        protected bool alwaysRewrite;

        protected ExprFactory GetExprFactory() { return expressionFactory; }
        protected SymbolLoader GetSymbolLoader() { return symbolLoader; }

        protected ExpressionTreeRewriter(ExprFactory expressionFactory, SymbolLoader symbolLoader)
        {
            this.expressionFactory = expressionFactory;
            this.symbolLoader = symbolLoader;
            this.alwaysRewrite = false;
        }

        protected override EXPR Dispatch(EXPR expr)
        {
            Debug.Assert(expr != null);

            EXPR result = base.Dispatch(expr);
            if (result == expr)
            {
                throw Error.InternalCompilerError();
            }
            return result;
        }

        /////////////////////////////////////////////////////////////////////////////////
        // Statement types.
        protected override EXPR VisitASSIGNMENT(EXPRASSIGNMENT assignment)
        {
            Debug.Assert(assignment != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);

            // For assignments, we either have a member assignment or an indexed assignment.
            //Debug.Assert(assignment.GetLHS().isPROP() || assignment.GetLHS().isFIELD() || assignment.GetLHS().isARRAYINDEX() || assignment.GetLHS().isLOCAL());
            EXPR lhs;
            if (assignment.GetLHS().isPROP())
            {
                EXPRPROP prop = assignment.GetLHS().asPROP();

                if (prop.GetOptionalArguments() == null)
                {
                    // Regular property.
                    lhs = Visit(prop);
                }
                else
                {
                    // Indexed assignment. Here we need to find the instance of the object, create the 
                    // PropInfo for the thing, and get the array of expressions that make up the index arguments.
                    //
                    // The LHS becomes Expression.Property(instance, indexerInfo, arguments).
                    EXPR instance = Visit(prop.GetMemberGroup().GetOptionalObject());
                    EXPR propInfo = GetExprFactory().CreatePropertyInfo(prop.pwtSlot.Prop(), prop.pwtSlot.Ats);
                    EXPR arguments = GenerateParamsArray(
                        GenerateArgsList(prop.GetOptionalArguments()),
                        PredefinedType.PT_EXPRESSION);

                    lhs = GenerateCall(PREDEFMETH.PM_EXPRESSION_PROPERTY, instance, propInfo, arguments);
                }
            }
            else
            {
                lhs = Visit(assignment.GetLHS());
            }

            EXPR rhs = Visit(assignment.GetRHS());
            return GenerateCall(PREDEFMETH.PM_EXPRESSION_ASSIGN, lhs, rhs);
        }
        protected override EXPR VisitMULTIGET(EXPRMULTIGET pExpr)
        {
            return Visit(pExpr.GetOptionalMulti().Left);
        }
        protected override EXPR VisitMULTI(EXPRMULTI pExpr)
        {
            EXPR rhs = Visit(pExpr.Operator);
            EXPR lhs = Visit(pExpr.Left);
            return GenerateCall(PREDEFMETH.PM_EXPRESSION_ASSIGN, lhs, rhs);
        }

        /////////////////////////////////////////////////////////////////////////////////
        // Expression types.

        protected override EXPR VisitBOUNDLAMBDA(EXPRBOUNDLAMBDA anonmeth)
        {
            Debug.Assert(anonmeth != null);

            EXPRBOUNDLAMBDA prevAnonMeth = currentAnonMeth;
            currentAnonMeth = anonmeth;
            MethodSymbol lambdaMethod = GetPreDefMethod(PREDEFMETH.PM_EXPRESSION_LAMBDA);

            CType delegateType = anonmeth.DelegateType();
            TypeArray lambdaTypeParams = GetSymbolLoader().getBSymmgr().AllocParams(1, new CType[] { delegateType });
            AggregateType expressionType = GetSymbolLoader().GetOptPredefTypeErr(PredefinedType.PT_EXPRESSION, true);
            MethWithInst mwi = new MethWithInst(lambdaMethod, expressionType, lambdaTypeParams);
            EXPR createParameters = CreateWraps(anonmeth);
            EXPR body = RewriteLambdaBody(anonmeth);
            EXPR parameters = RewriteLambdaParameters(anonmeth);
            EXPR args = GetExprFactory().CreateList(body, parameters);
            CType typeRet = GetSymbolLoader().GetTypeManager().SubstType(mwi.Meth().RetType, mwi.GetType(), mwi.TypeArgs);
            EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, mwi);
            EXPR callLambda = GetExprFactory().CreateCall(0, typeRet, args, pMemGroup, mwi);

            callLambda.asCALL().PredefinedMethod = PREDEFMETH.PM_EXPRESSION_LAMBDA;

            currentAnonMeth = prevAnonMeth;
            if (createParameters != null)
            {
                callLambda = GetExprFactory().CreateSequence(createParameters, callLambda);
            }
            EXPR expr = DestroyWraps(anonmeth, callLambda);
            // If we are already inside an expression tree rewrite and this is an expression tree lambda
            // then it needs to be quoted.
            if (currentAnonMeth != null)
            {
                expr = GenerateCall(PREDEFMETH.PM_EXPRESSION_QUOTE, expr);
            }
            return expr;
        }
        protected override EXPR VisitCONSTANT(EXPRCONSTANT expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            return GenerateConstant(expr);
        }
        protected override EXPR VisitLOCAL(EXPRLOCAL local)
        {
            Debug.Assert(local != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            Debug.Assert(!local.local.isThis);
            // this is true for all parameters of an expression lambda
            if (local.local.wrap != null)
            {
                return local.local.wrap;
            }
            Debug.Assert(local.local.fUsedInAnonMeth);
            return GetExprFactory().CreateHoistedLocalInExpression(local);
        }
        protected override EXPR VisitTHISPOINTER(EXPRTHISPOINTER expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            Debug.Assert(expr.local.isThis);
            return GenerateConstant(expr);
        }
        protected override EXPR VisitFIELD(EXPRFIELD expr)
        {
            Debug.Assert(expr != null);
            EXPR pObject;
            if (expr.GetOptionalObject() == null)
            {
                pObject = GetExprFactory().CreateNull();
            }
            else
            {
                pObject = Visit(expr.GetOptionalObject());
            }
            EXPRFIELDINFO pFieldInfo = GetExprFactory().CreateFieldInfo(expr.fwt.Field(), expr.fwt.GetType());
            return GenerateCall(PREDEFMETH.PM_EXPRESSION_FIELD, pObject, pFieldInfo);
        }
        protected override EXPR VisitUSERDEFINEDCONVERSION(EXPRUSERDEFINEDCONVERSION expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            return GenerateUserDefinedConversion(expr, expr.Argument);
        }
        protected override EXPR VisitCAST(EXPRCAST pExpr)
        {
            Debug.Assert(pExpr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);

            EXPR pArgument = pExpr.GetArgument();

            // If we have generated an identity cast or reference cast to a base class
            // we can omit the cast.
            if (pArgument.type == pExpr.type ||
                    GetSymbolLoader().IsBaseClassOfClass(pArgument.type, pExpr.type) ||
                    CConversions.FImpRefConv(GetSymbolLoader(), pArgument.type, pExpr.type))
            {
                return Visit(pArgument);
            }

            // If we have a cast to PredefinedType.PT_G_EXPRESSION and the thing that we're casting is 
            // a EXPRBOUNDLAMBDA that is an expression tree, then just visit the expression tree.
            if (pExpr.type != null &&
                    pExpr.type.isPredefType(PredefinedType.PT_G_EXPRESSION) &&
                    pArgument.isBOUNDLAMBDA())
            {
                return Visit(pArgument);
            }

            EXPR result = GenerateConversion(pArgument, pExpr.type, pExpr.isChecked());
            if ((pExpr.flags & EXPRFLAG.EXF_UNBOXRUNTIME) != 0)
            {
                // Propagate the unbox flag to the call for the ExpressionTreeCallRewriter.
                result.flags |= EXPRFLAG.EXF_UNBOXRUNTIME;
            }
            return result;
        }
        protected override EXPR VisitCONCAT(EXPRCONCAT expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            PREDEFMETH pdm;
            if (expr.GetFirstArgument().type.isPredefType(PredefinedType.PT_STRING) && expr.GetSecondArgument().type.isPredefType(PredefinedType.PT_STRING))
            {
                pdm = PREDEFMETH.PM_STRING_CONCAT_STRING_2;
            }
            else
            {
                pdm = PREDEFMETH.PM_STRING_CONCAT_OBJECT_2;
            }
            EXPR p1 = Visit(expr.GetFirstArgument());
            EXPR p2 = Visit(expr.GetSecondArgument());
            MethodSymbol method = GetPreDefMethod(pdm);
            EXPR methodInfo = GetExprFactory().CreateMethodInfo(method, GetSymbolLoader().GetReqPredefType(PredefinedType.PT_STRING), null);
            return GenerateCall(PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED, p1, p2, methodInfo);
        }
        protected override EXPR VisitBINOP(EXPRBINOP expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            if (expr.GetUserDefinedCallMethod() != null)
            {
                return GenerateUserDefinedBinaryOperator(expr);
            }
            else
            {
                return GenerateBuiltInBinaryOperator(expr);
            }
        }
        protected override EXPR VisitUNARYOP(EXPRUNARYOP pExpr)
        {
            Debug.Assert(pExpr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            if (pExpr.UserDefinedCallMethod != null)
            {
                return GenerateUserDefinedUnaryOperator(pExpr);
            }
            else
            {
                return GenerateBuiltInUnaryOperator(pExpr);
            }
        }
        protected override EXPR VisitARRAYINDEX(EXPRARRAYINDEX pExpr)
        {
            Debug.Assert(pExpr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            EXPR arr = Visit(pExpr.GetArray());
            EXPR args = GenerateIndexList(pExpr.GetIndex());
            if (args.isLIST())
            {
                EXPR Params = GenerateParamsArray(args, PredefinedType.PT_EXPRESSION);
                return GenerateCall(PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2, arr, Params);
            }
            return GenerateCall(PREDEFMETH.PM_EXPRESSION_ARRAYINDEX, arr, args);
        }
        protected override EXPR VisitARRAYLENGTH(EXPRARRAYLENGTH pExpr)
        {
            return GenerateBuiltInUnaryOperator(PREDEFMETH.PM_EXPRESSION_ARRAYLENGTH, pExpr.GetArray(), pExpr);
        }
        protected override EXPR VisitQUESTIONMARK(EXPRQUESTIONMARK pExpr)
        {
            Debug.Assert(pExpr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            EXPR p1 = Visit(pExpr.GetTestExpression());
            EXPR p2 = GenerateQuestionMarkOperand(pExpr.GetConsequence().asBINOP().GetOptionalLeftChild());
            EXPR p3 = GenerateQuestionMarkOperand(pExpr.GetConsequence().asBINOP().GetOptionalRightChild());
            return GenerateCall(PREDEFMETH.PM_EXPRESSION_CONDITION, p1, p2, p3);
        }
        protected override EXPR VisitCALL(EXPRCALL expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);

            switch (expr.nubLiftKind)
            {
                default:
                    break;
                case NullableCallLiftKind.NullableIntermediateConversion:
                case NullableCallLiftKind.NullableConversion:
                case NullableCallLiftKind.NullableConversionConstructor:
                    return GenerateConversion(expr.GetOptionalArguments(), expr.type, expr.isChecked());
                case NullableCallLiftKind.NotLiftedIntermediateConversion:
                case NullableCallLiftKind.UserDefinedConversion:
                    return GenerateUserDefinedConversion(expr.GetOptionalArguments(), expr.type, expr.mwi);
            }

            if (expr.mwi.Meth().IsConstructor())
            {
                return GenerateConstructor(expr);
            }

            EXPRMEMGRP memberGroup = expr.GetMemberGroup();
            if (memberGroup.isDelegate())
            {
                return GenerateDelegateInvoke(expr);
            }

            EXPR pObject;
            if (expr.mwi.Meth().isStatic || expr.GetMemberGroup().GetOptionalObject() == null)
            {
                pObject = GetExprFactory().CreateNull();
            }
            else
            {
                pObject = expr.GetMemberGroup().GetOptionalObject();

                // If we have, say, an int? which is the object of a call to ToString
                // then we do NOT want to generate ((object)i).ToString() because that
                // will convert a null-valued int? to a null object.  Rather what we want
                // to do is box it to a ValueType and call ValueType.ToString.
                //
                // To implement this we say that if the object of the call is an implicit boxing cast
                // then just generate the object, not the cast.  If the cast is explicit in the
                // source code then it will be an EXPLICITCAST and we will visit it normally.
                //
                // It might be better to rewrite the expression tree API so that it
                // can handle in the general case all implicit boxing conversions. Right now it 
                // requires that all arguments to a call that need to be boxed be explicitly boxed.

                if (pObject != null && pObject.isCAST() && pObject.asCAST().IsBoxingCast())
                {
                    pObject = pObject.asCAST().GetArgument();
                }
                pObject = Visit(pObject);
            }
            EXPR methodInfo = GetExprFactory().CreateMethodInfo(expr.mwi);
            EXPR args = GenerateArgsList(expr.GetOptionalArguments());
            EXPR Params = GenerateParamsArray(args, PredefinedType.PT_EXPRESSION);
            PREDEFMETH pdm = PREDEFMETH.PM_EXPRESSION_CALL;
            Debug.Assert(!expr.mwi.Meth().isVirtual || expr.GetMemberGroup().GetOptionalObject() != null);

            return GenerateCall(pdm, pObject, methodInfo, Params);
        }
        protected override EXPR VisitPROP(EXPRPROP expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            EXPR pObject;
            if (expr.pwtSlot.Prop().isStatic || expr.GetMemberGroup().GetOptionalObject() == null)
            {
                pObject = GetExprFactory().CreateNull();
            }
            else
            {
                pObject = Visit(expr.GetMemberGroup().GetOptionalObject());
            }
            EXPR propInfo = GetExprFactory().CreatePropertyInfo(expr.pwtSlot.Prop(), expr.pwtSlot.GetType());
            if (expr.GetOptionalArguments() != null)
            {
                // It is an indexer property.  Turn it into a virtual method call.
                EXPR args = GenerateArgsList(expr.GetOptionalArguments());
                EXPR Params = GenerateParamsArray(args, PredefinedType.PT_EXPRESSION);
                return GenerateCall(PREDEFMETH.PM_EXPRESSION_PROPERTY, pObject, propInfo, Params);
            }
            return GenerateCall(PREDEFMETH.PM_EXPRESSION_PROPERTY, pObject, propInfo);
        }
        protected override EXPR VisitARRINIT(EXPRARRINIT expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            // POSSIBLE ERROR: Multi-d should be an error?
            EXPR pTypeOf = CreateTypeOf(expr.type.AsArrayType().GetElementType());
            EXPR args = GenerateArgsList(expr.GetOptionalArguments());
            EXPR Params = GenerateParamsArray(args, PredefinedType.PT_EXPRESSION);
            return GenerateCall(PREDEFMETH.PM_EXPRESSION_NEWARRAYINIT, pTypeOf, Params);
        }
        protected override EXPR VisitZEROINIT(EXPRZEROINIT expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            Debug.Assert(expr.OptionalArgument == null);

            if (expr.IsConstructor)
            {
                // We have a parameterless "new MyStruct()" which has been realized as a zero init.
                EXPRTYPEOF pTypeOf = CreateTypeOf(expr.type);
                return GenerateCall(PREDEFMETH.PM_EXPRESSION_NEW_TYPE, pTypeOf);
            }
            return GenerateConstant(expr);
        }
        protected override EXPR VisitTYPEOF(EXPRTYPEOF expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            return GenerateConstant(expr);
        }

        protected virtual EXPR GenerateQuestionMarkOperand(EXPR pExpr)
        {
            Debug.Assert(pExpr != null);
            // We must not optimize away compiler-generated reference casts because
            // the expression tree API insists that the CType of both sides be identical.
            if (pExpr.isCAST())
            {
                return GenerateConversion(pExpr.asCAST().GetArgument(), pExpr.type, pExpr.isChecked());
            }
            return Visit(pExpr);
        }
        protected virtual EXPR GenerateDelegateInvoke(EXPRCALL expr)
        {
            Debug.Assert(expr != null);
            EXPRMEMGRP memberGroup = expr.GetMemberGroup();
            Debug.Assert(memberGroup.isDelegate());
            EXPR oldObject = memberGroup.GetOptionalObject();
            Debug.Assert(oldObject != null);
            EXPR pObject = Visit(oldObject);
            EXPR args = GenerateArgsList(expr.GetOptionalArguments());
            EXPR Params = GenerateParamsArray(args, PredefinedType.PT_EXPRESSION);
            return GenerateCall(PREDEFMETH.PM_EXPRESSION_INVOKE, pObject, Params);
        }
        protected virtual EXPR GenerateBuiltInBinaryOperator(EXPRBINOP expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            PREDEFMETH pdm;

            switch (expr.kind)
            {
                case ExpressionKind.EK_LSHIFT: pdm = PREDEFMETH.PM_EXPRESSION_LEFTSHIFT; break;
                case ExpressionKind.EK_RSHIFT: pdm = PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT; break;
                case ExpressionKind.EK_BITXOR: pdm = PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR; break;
                case ExpressionKind.EK_BITOR: pdm = PREDEFMETH.PM_EXPRESSION_OR; break;
                case ExpressionKind.EK_BITAND: pdm = PREDEFMETH.PM_EXPRESSION_AND; break;
                case ExpressionKind.EK_LOGAND: pdm = PREDEFMETH.PM_EXPRESSION_ANDALSO; break;
                case ExpressionKind.EK_LOGOR: pdm = PREDEFMETH.PM_EXPRESSION_ORELSE; break;
                case ExpressionKind.EK_STRINGEQ: pdm = PREDEFMETH.PM_EXPRESSION_EQUAL; break;
                case ExpressionKind.EK_EQ: pdm = PREDEFMETH.PM_EXPRESSION_EQUAL; break;
                case ExpressionKind.EK_STRINGNE: pdm = PREDEFMETH.PM_EXPRESSION_NOTEQUAL; break;
                case ExpressionKind.EK_NE: pdm = PREDEFMETH.PM_EXPRESSION_NOTEQUAL; break;
                case ExpressionKind.EK_GE: pdm = PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL; break;
                case ExpressionKind.EK_LE: pdm = PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL; break;
                case ExpressionKind.EK_LT: pdm = PREDEFMETH.PM_EXPRESSION_LESSTHAN; break;
                case ExpressionKind.EK_GT: pdm = PREDEFMETH.PM_EXPRESSION_GREATERTHAN; break;
                case ExpressionKind.EK_MOD: pdm = PREDEFMETH.PM_EXPRESSION_MODULO; break;
                case ExpressionKind.EK_DIV: pdm = PREDEFMETH.PM_EXPRESSION_DIVIDE; break;
                case ExpressionKind.EK_MUL:
                    pdm = expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED : PREDEFMETH.PM_EXPRESSION_MULTIPLY;
                    break;
                case ExpressionKind.EK_SUB:
                    pdm = expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED : PREDEFMETH.PM_EXPRESSION_SUBTRACT;
                    break;
                case ExpressionKind.EK_ADD:
                    pdm = expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_ADDCHECKED : PREDEFMETH.PM_EXPRESSION_ADD;
                    break;

                default:
                    throw Error.InternalCompilerError();
            }
            EXPR origL = expr.GetOptionalLeftChild();
            EXPR origR = expr.GetOptionalRightChild();
            Debug.Assert(origL != null);
            Debug.Assert(origR != null);
            CType typeL = origL.type;
            CType typeR = origR.type;

            EXPR newL = Visit(origL);
            EXPR newR = Visit(origR);

            bool didEnumConversion = false;
            CType convertL = null;
            CType convertR = null;

            if (typeL.isEnumType())
            {
                // We have already inserted casts if not lifted, so we should never see an enum.
                Debug.Assert(expr.isLifted);
                convertL = GetSymbolLoader().GetTypeManager().GetNullable(typeL.underlyingEnumType());
                typeL = convertL;
                didEnumConversion = true;
            }
            else if (typeL.IsNullableType() && typeL.StripNubs().isEnumType())
            {
                Debug.Assert(expr.isLifted);
                convertL = GetSymbolLoader().GetTypeManager().GetNullable(typeL.StripNubs().underlyingEnumType());
                typeL = convertL;
                didEnumConversion = true;
            }
            if (typeR.isEnumType())
            {
                Debug.Assert(expr.isLifted);
                convertR = GetSymbolLoader().GetTypeManager().GetNullable(typeR.underlyingEnumType());
                typeR = convertR;
                didEnumConversion = true;
            }
            else if (typeR.IsNullableType() && typeR.StripNubs().isEnumType())
            {
                Debug.Assert(expr.isLifted);
                convertR = GetSymbolLoader().GetTypeManager().GetNullable(typeR.StripNubs().underlyingEnumType());
                typeR = convertR;
                didEnumConversion = true;
            }
            if (typeL.IsNullableType() && typeL.StripNubs() == typeR)
            {
                convertR = typeL;
            }
            if (typeR.IsNullableType() && typeR.StripNubs() == typeL)
            {
                convertL = typeR;
            }

            if (convertL != null)
            {
                newL = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, newL, CreateTypeOf(convertL));
            }
            if (convertR != null)
            {
                newR = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, newR, CreateTypeOf(convertR));
            }

            EXPR call = GenerateCall(pdm, newL, newR);

            if (didEnumConversion && expr.type.StripNubs().isEnumType())
            {
                call = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, call, CreateTypeOf(expr.type));
            }

            return call;
        }
        protected virtual EXPR GenerateBuiltInUnaryOperator(EXPRUNARYOP expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            PREDEFMETH pdm;
            switch (expr.kind)
            {
                case ExpressionKind.EK_UPLUS:
                    return Visit(expr.Child);
                case ExpressionKind.EK_BITNOT: pdm = PREDEFMETH.PM_EXPRESSION_NOT; break;
                case ExpressionKind.EK_LOGNOT: pdm = PREDEFMETH.PM_EXPRESSION_NOT; break;
                case ExpressionKind.EK_NEG:
                    pdm = expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_NEGATECHECKED : PREDEFMETH.PM_EXPRESSION_NEGATE;
                    break;
                default:
                    throw Error.InternalCompilerError();
            }
            EXPR origOp = expr.Child;

            return GenerateBuiltInUnaryOperator(pdm, origOp, expr);
        }
        protected virtual EXPR GenerateBuiltInUnaryOperator(PREDEFMETH pdm, EXPR pOriginalOperator, EXPR pOperator)
        {
            EXPR op = Visit(pOriginalOperator);
            if (pOriginalOperator.type.IsNullableType() && pOriginalOperator.type.StripNubs().isEnumType())
            {
                Debug.Assert(pOperator.kind == ExpressionKind.EK_BITNOT); // The only built-in unary operator defined on nullable enum.
                CType underlyingType = pOriginalOperator.type.StripNubs().underlyingEnumType();
                CType nullableType = GetSymbolLoader().GetTypeManager().GetNullable(underlyingType);
                op = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, op, CreateTypeOf(nullableType));
            }
            EXPR call = GenerateCall(pdm, op);
            if (pOriginalOperator.type.IsNullableType() && pOriginalOperator.type.StripNubs().isEnumType())
            {
                call = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, call, CreateTypeOf(pOperator.type));
            }

            return call;
        }
        protected virtual EXPR GenerateUserDefinedBinaryOperator(EXPRBINOP expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            PREDEFMETH pdm;

            switch (expr.kind)
            {
                case ExpressionKind.EK_LOGOR: pdm = PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED; break;
                case ExpressionKind.EK_LOGAND: pdm = PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED; break;
                case ExpressionKind.EK_LSHIFT: pdm = PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED; break;
                case ExpressionKind.EK_RSHIFT: pdm = PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED; break;
                case ExpressionKind.EK_BITXOR: pdm = PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED; break;
                case ExpressionKind.EK_BITOR: pdm = PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED; break;
                case ExpressionKind.EK_BITAND: pdm = PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED; break;
                case ExpressionKind.EK_MOD: pdm = PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED; break;
                case ExpressionKind.EK_DIV: pdm = PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED; break;
                case ExpressionKind.EK_STRINGEQ:
                case ExpressionKind.EK_STRINGNE:
                case ExpressionKind.EK_DELEGATEEQ:
                case ExpressionKind.EK_DELEGATENE:
                case ExpressionKind.EK_EQ:
                case ExpressionKind.EK_NE:
                case ExpressionKind.EK_GE:
                case ExpressionKind.EK_GT:
                case ExpressionKind.EK_LE:
                case ExpressionKind.EK_LT:
                    return GenerateUserDefinedComparisonOperator(expr);
                case ExpressionKind.EK_DELEGATESUB:
                case ExpressionKind.EK_SUB:
                    pdm = expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED : PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED;
                    break;
                case ExpressionKind.EK_DELEGATEADD:
                case ExpressionKind.EK_ADD:
                    pdm = expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED : PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED;
                    break;
                case ExpressionKind.EK_MUL:
                    pdm = expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED : PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED;
                    break;
                default:
                    throw Error.InternalCompilerError();
            }
            EXPR p1 = expr.GetOptionalLeftChild();
            EXPR p2 = expr.GetOptionalRightChild();
            EXPR udcall = expr.GetOptionalUserDefinedCall();
            if (udcall != null)
            {
                Debug.Assert(udcall.kind == ExpressionKind.EK_CALL || udcall.kind == ExpressionKind.EK_USERLOGOP);
                if (udcall.kind == ExpressionKind.EK_CALL)
                {
                    EXPRLIST args = udcall.asCALL().GetOptionalArguments().asLIST();
                    Debug.Assert(args.GetOptionalNextListNode().kind != ExpressionKind.EK_LIST);
                    p1 = args.GetOptionalElement();
                    p2 = args.GetOptionalNextListNode();
                }
                else
                {
                    EXPRLIST args = udcall.asUSERLOGOP().OperatorCall.GetOptionalArguments().asLIST();
                    Debug.Assert(args.GetOptionalNextListNode().kind != ExpressionKind.EK_LIST);
                    p1 = args.GetOptionalElement().asWRAP().GetOptionalExpression();
                    p2 = args.GetOptionalNextListNode();
                }
            }
            p1 = Visit(p1);
            p2 = Visit(p2);
            FixLiftedUserDefinedBinaryOperators(expr, ref p1, ref p2);
            EXPR methodInfo = GetExprFactory().CreateMethodInfo(expr.GetUserDefinedCallMethod());
            EXPR call = GenerateCall(pdm, p1, p2, methodInfo);
            // Delegate add/subtract generates a call to Combine/Remove, which returns System.Delegate,
            // not the operand delegate CType.  We must cast to the delegate CType.
            if (expr.kind == ExpressionKind.EK_DELEGATESUB || expr.kind == ExpressionKind.EK_DELEGATEADD)
            {
                EXPR pTypeOf = CreateTypeOf(expr.type);
                return GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, call, pTypeOf);
            }
            return call;
        }
        protected virtual EXPR GenerateUserDefinedUnaryOperator(EXPRUNARYOP expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            PREDEFMETH pdm;
            EXPR arg = expr.Child;
            EXPRCALL call = expr.OptionalUserDefinedCall.asCALL();
            if (call != null)
            {
                // Use the actual argument of the call; it may contain user-defined
                // conversions or be a bound lambda, and that will not be in the original
                // argument stashed away in the left child of the operator.
                arg = call.GetOptionalArguments();
            }
            Debug.Assert(arg != null && arg.kind != ExpressionKind.EK_LIST);
            switch (expr.kind)
            {
                case ExpressionKind.EK_TRUE:
                case ExpressionKind.EK_FALSE:
                    return Visit(call);
                case ExpressionKind.EK_UPLUS:
                    pdm = PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED;
                    break;
                case ExpressionKind.EK_BITNOT: pdm = PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED; break;
                case ExpressionKind.EK_LOGNOT: pdm = PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED; break;
                case ExpressionKind.EK_DECIMALNEG:
                case ExpressionKind.EK_NEG:
                    pdm = expr.isChecked() ? PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED : PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED;
                    break;

                case ExpressionKind.EK_INC:
                case ExpressionKind.EK_DEC:
                case ExpressionKind.EK_DECIMALINC:
                case ExpressionKind.EK_DECIMALDEC:
                    pdm = PREDEFMETH.PM_EXPRESSION_CALL;
                    break;

                default:
                    throw Error.InternalCompilerError();
            }
            EXPR op = Visit(arg);
            EXPR methodInfo = GetExprFactory().CreateMethodInfo(expr.UserDefinedCallMethod);

            if (expr.kind == ExpressionKind.EK_INC || expr.kind == ExpressionKind.EK_DEC ||
                expr.kind == ExpressionKind.EK_DECIMALINC || expr.kind == ExpressionKind.EK_DECIMALDEC)
            {
                return GenerateCall(pdm, null, methodInfo, GenerateParamsArray(op, PredefinedType.PT_EXPRESSION));
            }
            return GenerateCall(pdm, op, methodInfo);
        }
        protected virtual EXPR GenerateUserDefinedComparisonOperator(EXPRBINOP expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(alwaysRewrite || currentAnonMeth != null);
            PREDEFMETH pdm;

            switch (expr.kind)
            {
                case ExpressionKind.EK_STRINGEQ: pdm = PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED; break;
                case ExpressionKind.EK_STRINGNE: pdm = PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED; break;
                case ExpressionKind.EK_DELEGATEEQ: pdm = PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED; break;
                case ExpressionKind.EK_DELEGATENE: pdm = PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED; break;
                case ExpressionKind.EK_EQ: pdm = PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED; break;
                case ExpressionKind.EK_NE: pdm = PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED; break;
                case ExpressionKind.EK_LE: pdm = PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED; break;
                case ExpressionKind.EK_LT: pdm = PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED; break;
                case ExpressionKind.EK_GE: pdm = PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED; break;
                case ExpressionKind.EK_GT: pdm = PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED; break;
                default:
                    throw Error.InternalCompilerError();
            }
            EXPR p1 = expr.GetOptionalLeftChild();
            EXPR p2 = expr.GetOptionalRightChild();
            if (expr.GetOptionalUserDefinedCall() != null)
            {
                EXPRCALL udcall = expr.GetOptionalUserDefinedCall().asCALL();
                EXPRLIST args = udcall.GetOptionalArguments().asLIST();
                Debug.Assert(args.GetOptionalNextListNode().kind != ExpressionKind.EK_LIST);

                p1 = args.GetOptionalElement();
                p2 = args.GetOptionalNextListNode();
            }
            p1 = Visit(p1);
            p2 = Visit(p2);
            FixLiftedUserDefinedBinaryOperators(expr, ref p1, ref p2);
            EXPR lift = GetExprFactory().CreateBoolConstant(false); // We never lift to null in C#.
            EXPR methodInfo = GetExprFactory().CreateMethodInfo(expr.GetUserDefinedCallMethod());
            return GenerateCall(pdm, p1, p2, lift, methodInfo);
        }
        protected EXPR RewriteLambdaBody(EXPRBOUNDLAMBDA anonmeth)
        {
            Debug.Assert(anonmeth != null);
            Debug.Assert(anonmeth.OptionalBody != null);
            Debug.Assert(anonmeth.OptionalBody.GetOptionalStatements() != null);
            // There ought to be no way to get an empty statement block successfully converted into an expression tree.
            Debug.Assert(anonmeth.OptionalBody.GetOptionalStatements().GetOptionalNextStatement() == null);

            EXPRBLOCK body = anonmeth.OptionalBody;

            // The most likely case:
            if (body.GetOptionalStatements().isRETURN())
            {
                Debug.Assert(body.GetOptionalStatements().asRETURN().GetOptionalObject() != null);
                return Visit(body.GetOptionalStatements().asRETURN().GetOptionalObject());
            }
            // This can only if it is a void delegate and this is a void expression, such as a call to a void method
            // or something like Expression<Action<Foo>> e = (Foo f) => f.MyEvent += MyDelegate;

            throw Error.InternalCompilerError();
        }
        protected EXPR RewriteLambdaParameters(EXPRBOUNDLAMBDA anonmeth)
        {
            Debug.Assert(anonmeth != null);

            // new ParameterExpression[2] {Parameter(typeof(type1), name1), Parameter(typeof(type2), name2)}

            EXPR paramArrayInitializerArgs = null;
            EXPR paramArrayInitializerArgsTail = paramArrayInitializerArgs;

            for (Symbol sym = anonmeth.ArgumentScope(); sym != null; sym = sym.nextChild)
            {
                if (!sym.IsLocalVariableSymbol())
                {
                    continue;
                }
                LocalVariableSymbol local = sym.AsLocalVariableSymbol();
                if (local.isThis)
                {
                    continue;
                }
                GetExprFactory().AppendItemToList(local.wrap, ref paramArrayInitializerArgs, ref paramArrayInitializerArgsTail);
            }

            return GenerateParamsArray(paramArrayInitializerArgs, PredefinedType.PT_PARAMETEREXPRESSION);
        }
        protected virtual EXPR GenerateConversion(EXPR arg, CType CType, bool bChecked)
        {
            return GenerateConversionWithSource(Visit(arg), CType, bChecked || arg.isChecked());
        }
        protected virtual EXPR GenerateConversionWithSource(EXPR pTarget, CType pType, bool bChecked)
        {
            PREDEFMETH pdm = bChecked ? PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED : PREDEFMETH.PM_EXPRESSION_CONVERT;
            EXPR pTypeOf = CreateTypeOf(pType);
            return GenerateCall(pdm, pTarget, pTypeOf);
        }
        protected virtual EXPR GenerateValueAccessConversion(EXPR pArgument)
        {
            Debug.Assert(pArgument != null);
            CType pStrippedTypeOfArgument = pArgument.type.StripNubs();
            EXPR pStrippedTypeExpr = CreateTypeOf(pStrippedTypeOfArgument);
            return GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, Visit(pArgument), pStrippedTypeExpr);
        }
        protected virtual EXPR GenerateUserDefinedConversion(EXPR arg, CType type, MethWithInst method)
        {
            EXPR target = Visit(arg);
            return GenerateUserDefinedConversion(arg, type, target, method);
        }
        protected virtual EXPR GenerateUserDefinedConversion(EXPR arg, CType CType, EXPR target, MethWithInst method)
        {
            // The user-defined explicit conversion from enum? to decimal or decimal? requires
            // that we convert the enum? to its nullable underlying CType.
            if (isEnumToDecimalConversion(arg.type, CType))
            {
                // Special case: If we have enum? to decimal? then we need to emit 
                // a conversion from enum? to its nullable underlying CType first.
                // This is unfortunate; we ought to reorganize how conversions are 
                // represented in the EXPR tree so that this is more transparent.

                // converting an enum to its underlying CType never fails, so no need to check it.
                CType underlyingType = arg.type.StripNubs().underlyingEnumType();
                CType nullableType = GetSymbolLoader().GetTypeManager().GetNullable(underlyingType);
                EXPR typeofNubEnum = CreateTypeOf(nullableType);
                target = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, target, typeofNubEnum);
            }

            // If the methodinfo does not return the target CType AND this is not a lifted conversion
            // from one value CType to another, then we need to wrap the whole thing in another conversion,
            // e.g. if we have a user-defined conversion from int to S? and we have (S)myint, then we need to generate 
            // Convert(Convert(myint, typeof(S?), op_implicit), typeof(S))

            CType pMethodReturnType = GetSymbolLoader().GetTypeManager().SubstType(method.Meth().RetType,
                method.GetType(), method.TypeArgs);
            bool fDontLiftReturnType = (pMethodReturnType == CType || (IsNullableValueType(arg.type) && IsNullableValueType(CType)));

            EXPR typeofInner = CreateTypeOf(fDontLiftReturnType ? CType : pMethodReturnType);
            EXPR methodInfo = GetExprFactory().CreateMethodInfo(method);
            PREDEFMETH pdmInner = arg.isChecked() ? PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED : PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED;
            EXPR callUserDefinedConversion = GenerateCall(pdmInner, target, typeofInner, methodInfo);

            if (fDontLiftReturnType)
            {
                return callUserDefinedConversion;
            }

            PREDEFMETH pdmOuter = arg.isChecked() ? PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED : PREDEFMETH.PM_EXPRESSION_CONVERT;
            EXPR typeofOuter = CreateTypeOf(CType);
            return GenerateCall(pdmOuter, callUserDefinedConversion, typeofOuter);
        }
        protected virtual EXPR GenerateUserDefinedConversion(EXPRUSERDEFINEDCONVERSION pExpr, EXPR pArgument)
        {
            EXPR pCastCall = pExpr.UserDefinedCall;
            EXPR pCastArgument = pExpr.Argument;
            EXPR pConversionSource = null;

            if (!isEnumToDecimalConversion(pArgument.type, pExpr.type) && IsNullableValueAccess(pCastArgument, pArgument))
            {
                // We have an implicit conversion of nullable CType to the value CType, generate a convert node for it.
                pConversionSource = GenerateValueAccessConversion(pArgument);
            }
            else if (pCastCall.isCALL() && pCastCall.asCALL().pConversions != null)
            {
                EXPR pUDConversion = pCastCall.asCALL().pConversions;
                if (pUDConversion.isCALL())
                {
                    EXPR pUDConversionArgument = pUDConversion.asCALL().GetOptionalArguments();
                    if (IsNullableValueAccess(pUDConversionArgument, pArgument))
                    {
                        pConversionSource = GenerateValueAccessConversion(pArgument);
                    }
                    else
                    {
                        pConversionSource = Visit(pUDConversionArgument);
                    }
                    return GenerateConversionWithSource(pConversionSource, pCastCall.type, pCastCall.asCALL().isChecked());
                }
                else
                {
                    // This can happen if we have a UD conversion from C to, say, int, 
                    // and we have an explicit cast to decimal?. The conversion should
                    // then be bound as two chained user-defined conversions.
                    Debug.Assert(pUDConversion.isUSERDEFINEDCONVERSION());
                    // Just recurse.
                    return GenerateUserDefinedConversion(pUDConversion.asUSERDEFINEDCONVERSION(), pArgument);
                }
            }
            else
            {
                pConversionSource = Visit(pCastArgument);
            }
            return GenerateUserDefinedConversion(pCastArgument, pExpr.type, pConversionSource, pExpr.UserDefinedCallMethod);
        }
        protected virtual EXPR GenerateParameter(string name, CType CType)
        {
            GetSymbolLoader().GetReqPredefType(PredefinedType.PT_STRING);  // force an ensure state
            EXPRCONSTANT nameString = GetExprFactory().CreateStringConstant(name);
            EXPRTYPEOF pTypeOf = CreateTypeOf(CType);
            return GenerateCall(PREDEFMETH.PM_EXPRESSION_PARAMETER, pTypeOf, nameString);
        }
        protected MethodSymbol GetPreDefMethod(PREDEFMETH pdm)
        {
            return GetSymbolLoader().getPredefinedMembers().GetMethod(pdm);
        }
        protected EXPRTYPEOF CreateTypeOf(CType CType)
        {
            return GetExprFactory().CreateTypeOf(CType);
        }
        protected EXPR CreateWraps(EXPRBOUNDLAMBDA anonmeth)
        {
            EXPR sequence = null;
            for (Symbol sym = anonmeth.ArgumentScope().firstChild; sym != null; sym = sym.nextChild)
            {
                if (!sym.IsLocalVariableSymbol())
                {
                    continue;
                }
                LocalVariableSymbol local = sym.AsLocalVariableSymbol();
                if (local.isThis)
                {
                    continue;
                }
                Debug.Assert(anonmeth.OptionalBody != null);
                EXPR create = GenerateParameter(local.name.Text, local.GetType());
                local.wrap = GetExprFactory().CreateWrapNoAutoFree(anonmeth.OptionalBody.OptionalScopeSymbol, create);
                EXPR save = GetExprFactory().CreateSave(local.wrap);
                if (sequence == null)
                {
                    sequence = save;
                }
                else
                {
                    sequence = GetExprFactory().CreateSequence(sequence, save);
                }
            }

            return sequence;
        }
        protected EXPR DestroyWraps(EXPRBOUNDLAMBDA anonmeth, EXPR sequence)
        {
            for (Symbol sym = anonmeth.ArgumentScope(); sym != null; sym = sym.nextChild)
            {
                if (!sym.IsLocalVariableSymbol())
                {
                    continue;
                }
                LocalVariableSymbol local = sym.AsLocalVariableSymbol();
                if (local.isThis)
                {
                    continue;
                }
                Debug.Assert(local.wrap != null);
                Debug.Assert(anonmeth.OptionalBody != null);
                EXPR freeWrap = GetExprFactory().CreateWrap(anonmeth.OptionalBody.OptionalScopeSymbol, local.wrap);
                sequence = GetExprFactory().CreateReverseSequence(sequence, freeWrap);
            }
            return sequence;
        }
        protected virtual EXPR GenerateConstructor(EXPRCALL expr)
        {
            Debug.Assert(expr != null);
            Debug.Assert(expr.mwi.Meth().IsConstructor());

            // Realize a call to new DELEGATE(obj, FUNCPTR) as though it actually was
            // (DELEGATE)CreateDelegate(typeof(DELEGATE), obj, GetMethInfoFromHandle(FUNCPTR))

            if (IsDelegateConstructorCall(expr))
            {
                return GenerateDelegateConstructor(expr);
            }
            EXPR constructorInfo = GetExprFactory().CreateMethodInfo(expr.mwi);
            EXPR args = GenerateArgsList(expr.GetOptionalArguments());
            EXPR Params = GenerateParamsArray(args, PredefinedType.PT_EXPRESSION);
            if (expr.type.IsAggregateType() && expr.type.AsAggregateType().getAggregate().IsAnonymousType())
            {
                EXPR members = GenerateMembersArray(expr.type.AsAggregateType(), PredefinedType.PT_METHODINFO);
                return GenerateCall(PREDEFMETH.PM_EXPRESSION_NEW_MEMBERS, constructorInfo, Params, members);
            }
            else
            {
                return GenerateCall(PREDEFMETH.PM_EXPRESSION_NEW, constructorInfo, Params);
            }
        }
        protected virtual EXPR GenerateDelegateConstructor(EXPRCALL expr)
        {
            // In:
            //
            // new DELEGATE(obj, &FUNC)
            //
            // Out:
            //
            //  Cast(
            //      Call(
            //          null,
            //          (MethodInfo)GetMethodFromHandle(&CreateDelegate),
            //          new Expression[3]{
            //              Constant(typeof(DELEGATE)),
            //              transformed-object,
            //              Constant((MethodInfo)GetMethodFromHandle(&FUNC)}),
            //      typeof(DELEGATE))
            //

            Debug.Assert(expr != null);
            Debug.Assert(expr.mwi.Meth().IsConstructor());
            Debug.Assert(expr.type.isDelegateType());
            Debug.Assert(expr.GetOptionalArguments() != null);
            Debug.Assert(expr.GetOptionalArguments().isLIST());
            EXPRLIST origArgs = expr.GetOptionalArguments().asLIST();
            EXPR target = origArgs.GetOptionalElement();
            Debug.Assert(origArgs.GetOptionalNextListNode().kind == ExpressionKind.EK_FUNCPTR);
            EXPRFUNCPTR funcptr = origArgs.GetOptionalNextListNode().asFUNCPTR();
            MethodSymbol createDelegateMethod = GetPreDefMethod(PREDEFMETH.PM_METHODINFO_CREATEDELEGATE_TYPE_OBJECT);
            AggregateType delegateType = GetSymbolLoader().GetOptPredefTypeErr(PredefinedType.PT_DELEGATE, true);
            MethWithInst mwi = new MethWithInst(createDelegateMethod, delegateType);

            EXPR instance = GenerateConstant(GetExprFactory().CreateMethodInfo(funcptr.mwi));
            EXPR methinfo = GetExprFactory().CreateMethodInfo(mwi);
            EXPR param1 = GenerateConstant(CreateTypeOf(expr.type));
            EXPR param2 = Visit(target);
            EXPR paramsList = GetExprFactory().CreateList(param1, param2);
            EXPR Params = GenerateParamsArray(paramsList, PredefinedType.PT_EXPRESSION);
            EXPR call = GenerateCall(PREDEFMETH.PM_EXPRESSION_CALL, instance, methinfo, Params);
            EXPR pTypeOf = CreateTypeOf(expr.type);
            return GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, call, pTypeOf);
        }
        protected virtual EXPR GenerateArgsList(EXPR oldArgs)
        {
            EXPR newArgs = null;
            EXPR newArgsTail = newArgs;
            for (ExpressionIterator it = new ExpressionIterator(oldArgs); !it.AtEnd(); it.MoveNext())
            {
                EXPR oldArg = it.Current();
                GetExprFactory().AppendItemToList(Visit(oldArg), ref newArgs, ref newArgsTail);
            }
            return newArgs;
        }
        protected virtual EXPR GenerateIndexList(EXPR oldIndices)
        {
            CType intType = symbolLoader.GetReqPredefType(PredefinedType.PT_INT, true);

            EXPR newIndices = null;
            EXPR newIndicesTail = newIndices;
            for (ExpressionIterator it = new ExpressionIterator(oldIndices); !it.AtEnd(); it.MoveNext())
            {
                EXPR newIndex = it.Current();
                if (newIndex.type != intType)
                {
                    EXPRCLASS exprType = expressionFactory.CreateClass(intType, null, null);
                    newIndex = expressionFactory.CreateCast(EXPRFLAG.EXF_INDEXEXPR, exprType, newIndex);
                    newIndex.flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
                }
                EXPR rewrittenIndex = Visit(newIndex);
                expressionFactory.AppendItemToList(rewrittenIndex, ref newIndices, ref newIndicesTail);
            }
            return newIndices;
        }
        protected virtual EXPR GenerateConstant(EXPR expr)
        {
            EXPRFLAG flags = 0;

            AggregateType pObject = GetSymbolLoader().GetReqPredefType(PredefinedType.PT_OBJECT, true);

            if (expr.type.IsNullType())
            {
                EXPRTYPEOF pTypeOf = CreateTypeOf(pObject);
                return GenerateCall(PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE, expr, pTypeOf);
            }

            AggregateType stringType = GetSymbolLoader().GetReqPredefType(PredefinedType.PT_STRING, true);
            if (expr.type != stringType)
            {
                flags = EXPRFLAG.EXF_BOX;
            }

            EXPRCLASS objectType = GetExprFactory().MakeClass(pObject);
            EXPRCAST cast = GetExprFactory().CreateCast(flags, objectType, expr);
            EXPRTYPEOF pTypeOf2 = CreateTypeOf(expr.type);

            return GenerateCall(PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE, cast, pTypeOf2);
        }
        protected EXPRCALL GenerateCall(PREDEFMETH pdm, EXPR arg1)
        {
            MethodSymbol method = GetPreDefMethod(pdm);
            // this should be enforced in an earlier pass and the transform pass should not 
            // be handling this error
            if (method == null)
                return null;
            AggregateType expressionType = GetSymbolLoader().GetOptPredefTypeErr(PredefinedType.PT_EXPRESSION, true);
            MethWithInst mwi = new MethWithInst(method, expressionType);
            EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, mwi);
            EXPRCALL call = GetExprFactory().CreateCall(0, mwi.Meth().RetType, arg1, pMemGroup, mwi);
            call.PredefinedMethod = pdm;
            return call;
        }
        protected EXPRCALL GenerateCall(PREDEFMETH pdm, EXPR arg1, EXPR arg2)
        {
            MethodSymbol method = GetPreDefMethod(pdm);
            if (method == null)
                return null;
            AggregateType expressionType = GetSymbolLoader().GetOptPredefTypeErr(PredefinedType.PT_EXPRESSION, true);
            EXPR args = GetExprFactory().CreateList(arg1, arg2);
            MethWithInst mwi = new MethWithInst(method, expressionType);
            EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, mwi);
            EXPRCALL call = GetExprFactory().CreateCall(0, mwi.Meth().RetType, args, pMemGroup, mwi);
            call.PredefinedMethod = pdm;
            return call;
        }
        protected EXPRCALL GenerateCall(PREDEFMETH pdm, EXPR arg1, EXPR arg2, EXPR arg3)
        {
            MethodSymbol method = GetPreDefMethod(pdm);
            if (method == null)
                return null;
            AggregateType expressionType = GetSymbolLoader().GetOptPredefTypeErr(PredefinedType.PT_EXPRESSION, true);
            EXPR args = GetExprFactory().CreateList(arg1, arg2, arg3);
            MethWithInst mwi = new MethWithInst(method, expressionType);
            EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, mwi);
            EXPRCALL call = GetExprFactory().CreateCall(0, mwi.Meth().RetType, args, pMemGroup, mwi);
            call.PredefinedMethod = pdm;
            return call;
        }
        protected EXPRCALL GenerateCall(PREDEFMETH pdm, EXPR arg1, EXPR arg2, EXPR arg3, EXPR arg4)
        {
            MethodSymbol method = GetPreDefMethod(pdm);
            if (method == null)
                return null;
            AggregateType expressionType = GetSymbolLoader().GetOptPredefTypeErr(PredefinedType.PT_EXPRESSION, true);
            EXPR args = GetExprFactory().CreateList(arg1, arg2, arg3, arg4);
            MethWithInst mwi = new MethWithInst(method, expressionType);
            EXPRMEMGRP pMemGroup = GetExprFactory().CreateMemGroup(null, mwi);
            EXPRCALL call = GetExprFactory().CreateCall(0, mwi.Meth().RetType, args, pMemGroup, mwi);
            call.PredefinedMethod = pdm;
            return call;
        }
        protected virtual EXPRARRINIT GenerateParamsArray(EXPR args, PredefinedType pt)
        {
            int parameterCount = ExpressionIterator.Count(args);
            AggregateType paramsArrayElementType = GetSymbolLoader().GetOptPredefTypeErr(pt, true);
            ArrayType paramsArrayType = GetSymbolLoader().GetTypeManager().GetArray(paramsArrayElementType, 1);
            EXPRCONSTANT paramsArrayArg = GetExprFactory().CreateIntegerConstant(parameterCount);
            EXPRARRINIT arrayInit = GetExprFactory().CreateArrayInit(EXPRFLAG.EXF_CANTBENULL, paramsArrayType, args, paramsArrayArg, null);
            arrayInit.dimSize = parameterCount;
            arrayInit.dimSizes = new int[] { arrayInit.dimSize }; // CLEANUP: Why isn't this done by the factory?
            return arrayInit;
        }
        protected virtual EXPRARRINIT GenerateMembersArray(AggregateType anonymousType, PredefinedType pt)
        {
            EXPR newArgs = null;
            EXPR newArgsTail = newArgs;
            int methodCount = 0;
            AggregateSymbol aggSym = anonymousType.getAggregate();

            for (Symbol member = aggSym.firstChild; member != null; member = member.nextChild)
            {
                if (member.IsMethodSymbol())
                {
                    MethodSymbol method = member.AsMethodSymbol();
                    if (method.MethKind() == MethodKindEnum.PropAccessor)
                    {
                        EXPRMETHODINFO methodInfo = GetExprFactory().CreateMethodInfo(method, anonymousType, method.Params);
                        GetExprFactory().AppendItemToList(methodInfo, ref newArgs, ref newArgsTail);
                        methodCount++;
                    }
                }
            }

            AggregateType paramsArrayElementType = GetSymbolLoader().GetOptPredefTypeErr(pt, true);
            ArrayType paramsArrayType = GetSymbolLoader().GetTypeManager().GetArray(paramsArrayElementType, 1);
            EXPRCONSTANT paramsArrayArg = GetExprFactory().CreateIntegerConstant(methodCount);
            EXPRARRINIT arrayInit = GetExprFactory().CreateArrayInit(EXPRFLAG.EXF_CANTBENULL, paramsArrayType, newArgs, paramsArrayArg, null);
            arrayInit.dimSize = methodCount;
            arrayInit.dimSizes = new int[] { arrayInit.dimSize }; // CLEANUP: Why isn't this done by the factory?
            return arrayInit;
        }
        protected void FixLiftedUserDefinedBinaryOperators(EXPRBINOP expr, ref EXPR pp1, ref EXPR pp2)
        {
            // If we have lifted T1 op T2 to T1? op T2?, and we have an expression T1 op T2? or T1? op T2 then
            // we need to ensure that the unlifted actual arguments are promoted to their nullable CType.
            Debug.Assert(expr != null);
            Debug.Assert(pp1 != null);
            Debug.Assert(pp1 != null);
            Debug.Assert(pp2 != null);
            Debug.Assert(pp2 != null);
            MethodSymbol method = expr.GetUserDefinedCallMethod().Meth();
            EXPR orig1 = expr.GetOptionalLeftChild();
            EXPR orig2 = expr.GetOptionalRightChild();
            Debug.Assert(orig1 != null && orig2 != null);
            EXPR new1 = pp1;
            EXPR new2 = pp2;
            CType fptype1 = method.Params.Item(0);
            CType fptype2 = method.Params.Item(1);
            CType aatype1 = orig1.type;
            CType aatype2 = orig2.type;
            // Is the operator even a candidate for lifting?
            if (fptype1.IsNullableType() || fptype2.IsNullableType() ||
                !fptype1.IsAggregateType() || !fptype2.IsAggregateType() ||
                !fptype1.AsAggregateType().getAggregate().IsValueType() ||
                !fptype2.AsAggregateType().getAggregate().IsValueType())
            {
                return;
            }
            CType nubfptype1 = GetSymbolLoader().GetTypeManager().GetNullable(fptype1);
            CType nubfptype2 = GetSymbolLoader().GetTypeManager().GetNullable(fptype2);
            // If we have null op X, or T1 op T2?, or T1 op null, lift first arg to T1?
            if (aatype1.IsNullType() || aatype1 == fptype1 && (aatype2 == nubfptype2 || aatype2.IsNullType()))
            {
                new1 = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, new1, CreateTypeOf(nubfptype1));
            }

            // If we have X op null, or T1? op T2, or null op T2, lift second arg to T2?
            if (aatype2.IsNullType() || aatype2 == fptype2 && (aatype1 == nubfptype1 || aatype1.IsNullType()))
            {
                new2 = GenerateCall(PREDEFMETH.PM_EXPRESSION_CONVERT, new2, CreateTypeOf(nubfptype2));
            }
            pp1 = new1;
            pp2 = new2;
        }
        protected bool IsNullableValueType(CType pType)
        {
            if (pType.IsNullableType())
            {
                CType pStrippedType = pType.StripNubs();
                return pStrippedType.IsAggregateType() && pStrippedType.AsAggregateType().getAggregate().IsValueType();
            }
            return false;
        }
        protected bool IsNullableValueAccess(EXPR pExpr, EXPR pObject)
        {
            Debug.Assert(pExpr != null);
            return pExpr.isPROP() && (pExpr.asPROP().GetMemberGroup().GetOptionalObject() == pObject) && pObject.type.IsNullableType();
        }
        protected bool IsDelegateConstructorCall(EXPR pExpr)
        {
            Debug.Assert(pExpr != null);
            if (!pExpr.isCALL())
            {
                return false;
            }
            EXPRCALL pCall = pExpr.asCALL();
            return pCall.mwi.Meth() != null &&
                pCall.mwi.Meth().IsConstructor() &&
                pCall.type.isDelegateType() &&
                pCall.GetOptionalArguments() != null &&
                pCall.GetOptionalArguments().isLIST() &&
                pCall.GetOptionalArguments().asLIST().GetOptionalNextListNode().kind == ExpressionKind.EK_FUNCPTR;
        }
        private static bool isEnumToDecimalConversion(CType argtype, CType desttype)
        {
            CType strippedArgType = argtype.IsNullableType() ? argtype.StripNubs() : argtype;
            CType strippedDestType = desttype.IsNullableType() ? desttype.StripNubs() : desttype;
            return strippedArgType.isEnumType() && strippedDestType.isPredefType(PredefinedType.PT_DECIMAL);
        }
    }
}
