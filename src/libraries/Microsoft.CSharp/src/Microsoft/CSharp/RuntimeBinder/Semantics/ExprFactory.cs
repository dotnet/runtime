// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprFactory
    {
        private readonly GlobalSymbolContext _globalSymbolContext;
        private readonly ConstValFactory _constants;

        public ExprFactory(GlobalSymbolContext globalSymbolContext)
        {
            Debug.Assert(globalSymbolContext != null);
            _globalSymbolContext = globalSymbolContext;
            _constants = new ConstValFactory();
        }
        public ConstValFactory GetExprConstants()
        {
            return _constants;
        }
        private TypeManager GetTypes()
        {
            return _globalSymbolContext.GetTypes();
        }
        private BSYMMGR GetGlobalSymbols()
        {
            return _globalSymbolContext.GetGlobalSymbols();
        }

        public EXPRCALL CreateCall(EXPRFLAG nFlags, CType pType, EXPR pOptionalArguments, EXPRMEMGRP pMemberGroup, MethWithInst MWI)
        {
            Debug.Assert(0 == (nFlags &
               ~(
                   EXPRFLAG.EXF_NEWOBJCALL | EXPRFLAG.EXF_CONSTRAINED | EXPRFLAG.EXF_BASECALL |
                   EXPRFLAG.EXF_NEWSTRUCTASSG |
                   EXPRFLAG.EXF_IMPLICITSTRUCTASSG | EXPRFLAG.EXF_MASK_ANY
               )
              ));

            EXPRCALL rval = new EXPRCALL();
            rval.kind = ExpressionKind.EK_CALL;
            rval.type = pType;
            rval.flags = nFlags;
            rval.SetOptionalArguments(pOptionalArguments);
            rval.SetMemberGroup(pMemberGroup);
            rval.nubLiftKind = NullableCallLiftKind.NotLifted;
            rval.castOfNonLiftedResultToLiftedType = null;

            rval.mwi = MWI;
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRFIELD CreateField(EXPRFLAG nFlags, CType pType, EXPR pOptionalObject, uint nOffset, FieldWithType FWT, EXPR pOptionalLHS)
        {
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_MEMBERSET | EXPRFLAG.EXF_MASK_ANY)));
            EXPRFIELD rval = new EXPRFIELD();
            rval.kind = ExpressionKind.EK_FIELD;
            rval.type = pType;
            rval.flags = nFlags;
            rval.SetOptionalObject(pOptionalObject);
            if (FWT != null)
            {
                rval.fwt = FWT;
            }
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRFUNCPTR CreateFunctionPointer(EXPRFLAG nFlags, CType pType, EXPR pObject, MethWithInst MWI)
        {
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_BASECALL)));
            EXPRFUNCPTR rval = new EXPRFUNCPTR();
            rval.kind = ExpressionKind.EK_FUNCPTR;
            rval.type = pType;
            rval.flags = nFlags;
            rval.OptionalObject = pObject;
            rval.mwi = new MethWithInst(MWI);
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRARRINIT CreateArrayInit(EXPRFLAG nFlags, CType pType, EXPR pOptionalArguments, EXPR pOptionalArgumentDimensions, int[] pDimSizes)
        {
            Debug.Assert(0 == (nFlags &
                       ~(EXPRFLAG.EXF_MASK_ANY | EXPRFLAG.EXF_ARRAYCONST | EXPRFLAG.EXF_ARRAYALLCONST)));
            EXPRARRINIT rval = new EXPRARRINIT();
            rval.kind = ExpressionKind.EK_ARRINIT;
            rval.type = pType;
            rval.SetOptionalArguments(pOptionalArguments);
            rval.SetOptionalArgumentDimensions(pOptionalArgumentDimensions);
            rval.dimSizes = pDimSizes;
            rval.dimSize = pDimSizes != null ? pDimSizes.Length : 0;
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRPROP CreateProperty(CType pType, EXPR pOptionalObject)
        {
            MethPropWithInst mwi = new MethPropWithInst();
            EXPRMEMGRP pMemGroup = CreateMemGroup(pOptionalObject, mwi);
            return CreateProperty(pType, null, null, pMemGroup, null, null, null);
        }

        public EXPRPROP CreateProperty(CType pType, EXPR pOptionalObjectThrough, EXPR pOptionalArguments, EXPRMEMGRP pMemberGroup, PropWithType pwtSlot, MethWithType mwtGet, MethWithType mwtSet)
        {
            EXPRPROP rval = new EXPRPROP();
            rval.kind = ExpressionKind.EK_PROP;
            rval.type = pType;
            rval.flags = 0;
            rval.SetOptionalObjectThrough(pOptionalObjectThrough);
            rval.SetOptionalArguments(pOptionalArguments);
            rval.SetMemberGroup(pMemberGroup);

            if (pwtSlot != null)
            {
                rval.pwtSlot = pwtSlot;
            }
            if (mwtSet != null)
            {
                rval.mwtSet = mwtSet;
            }
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPREVENT CreateEvent(CType pType, EXPR pOptionalObject, EventWithType EWT)
        {
            EXPREVENT rval = new EXPREVENT();
            rval.kind = ExpressionKind.EK_EVENT;
            rval.type = pType;
            rval.flags = 0;
            rval.OptionalObject = pOptionalObject;
            if (EWT != null)
            {
                rval.ewt = EWT;
            }
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRMEMGRP CreateMemGroup(EXPRFLAG nFlags, Name pName, TypeArray pTypeArgs, SYMKIND symKind, CType pTypePar, MethodOrPropertySymbol pMPS, EXPR pObject, CMemberLookupResults memberLookupResults)
        {
            Debug.Assert(0 == (nFlags & ~(
                           EXPRFLAG.EXF_CTOR | EXPRFLAG.EXF_INDEXER | EXPRFLAG.EXF_OPERATOR | EXPRFLAG.EXF_NEWOBJCALL |
                           EXPRFLAG.EXF_BASECALL | EXPRFLAG.EXF_DELEGATE | EXPRFLAG.EXF_USERCALLABLE | EXPRFLAG.EXF_MASK_ANY
                       )
                      ));
            EXPRMEMGRP rval = new EXPRMEMGRP();
            rval.kind = ExpressionKind.EK_MEMGRP;
            rval.type = GetTypes().GetMethGrpType();
            rval.flags = nFlags;
            rval.name = pName;
            rval.typeArgs = pTypeArgs;
            rval.sk = symKind;
            rval.SetParentType(pTypePar);
            rval.SetOptionalObject(pObject);
            rval.SetMemberLookupResults(memberLookupResults);
            rval.SetOptionalLHS(null);
            if (rval.typeArgs == null)
            {
                rval.typeArgs = BSYMMGR.EmptyTypeArray();
            }
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRMEMGRP CreateMemGroup(
                EXPR pObject,
                MethPropWithInst mwi)
        {
            Name pName = mwi.Sym != null ? mwi.Sym.name : null;
            MethodOrPropertySymbol methProp = mwi.MethProp();

            CType pType = mwi.GetType();
            if (pType == null)
            {
                pType = GetTypes().GetErrorSym();
            }

            return CreateMemGroup(0, pName, mwi.TypeArgs, methProp != null ? methProp.getKind() : SYMKIND.SK_MethodSymbol, mwi.GetType(), methProp, pObject, new CMemberLookupResults(GetGlobalSymbols().AllocParams(1, new CType[] { pType }), pName));
        }

        public EXPRUSERDEFINEDCONVERSION CreateUserDefinedConversion(EXPR arg, EXPR call, MethWithInst mwi)
        {
            Debug.Assert(arg != null);
            Debug.Assert(call != null);
            EXPRUSERDEFINEDCONVERSION rval = new EXPRUSERDEFINEDCONVERSION();
            rval.kind = ExpressionKind.EK_USERDEFINEDCONVERSION;
            rval.type = call.type;
            rval.flags = 0;
            rval.Argument = arg;
            rval.UserDefinedCall = call;
            rval.UserDefinedCallMethod = mwi;
            if (call.HasError())
            {
                rval.SetError();
            }
            Debug.Assert(rval != null);
            return rval;
        }

        public EXPRCAST CreateCast(EXPRFLAG nFlags, CType pType, EXPR pArg)
        {
            return CreateCast(nFlags, CreateClass(pType, null, null), pArg);
        }

        public EXPRCAST CreateCast(EXPRFLAG nFlags, EXPRTYPEORNAMESPACE pType, EXPR pArg)
        {
            Debug.Assert(pArg != null);
            Debug.Assert(pType != null);
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_CAST_ALL | EXPRFLAG.EXF_MASK_ANY)));
            EXPRCAST rval = new EXPRCAST();
            rval.type = pType.TypeOrNamespace as CType;
            rval.kind = ExpressionKind.EK_CAST;
            rval.Argument = pArg;
            rval.flags = nFlags;
            rval.DestinationType = pType;
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRRETURN CreateReturn(EXPRFLAG nFlags, Scope pCurrentScope, EXPR pOptionalObject)
        {
            return CreateReturn(nFlags, pCurrentScope, pOptionalObject, pOptionalObject);
        }

        private EXPRRETURN CreateReturn(EXPRFLAG nFlags, Scope pCurrentScope, EXPR pOptionalObject, EXPR pOptionalOriginalObject)
        {
            Debug.Assert(0 == (nFlags &
                       ~(EXPRFLAG.EXF_ASLEAVE | EXPRFLAG.EXF_FINALLYBLOCKED | EXPRFLAG.EXF_RETURNISYIELD |
                          EXPRFLAG.EXF_ASFINALLYLEAVE | EXPRFLAG.EXF_GENERATEDSTMT | EXPRFLAG.EXF_MARKING |
                          EXPRFLAG.EXF_MASK_ANY
                        )
                      ));
            EXPRRETURN rval = new EXPRRETURN();
            rval.kind = ExpressionKind.EK_RETURN;
            rval.type = null;
            rval.flags = nFlags;
            rval.SetOptionalObject(pOptionalObject);
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRLOCAL CreateLocal(EXPRFLAG nFlags, LocalVariableSymbol pLocal)
        {
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_MASK_ANY)));

            CType type = null;
            if (pLocal != null)
            {
                type = pLocal.GetType();
            }

            EXPRLOCAL rval = new EXPRLOCAL();
            rval.kind = ExpressionKind.EK_LOCAL;
            rval.type = type;
            rval.flags = nFlags;
            rval.local = pLocal;
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRTHISPOINTER CreateThis(LocalVariableSymbol pLocal, bool fImplicit)
        {
            Debug.Assert(pLocal == null || pLocal.isThis);

            CType type = null;
            if (pLocal != null)
            {
                type = pLocal.GetType();
            }

            EXPRFLAG flags = EXPRFLAG.EXF_CANTBENULL;
            if (fImplicit)
            {
                flags |= EXPRFLAG.EXF_IMPLICITTHIS;
            }
            if (type != null && type.isStructType())
            {
                flags |= EXPRFLAG.EXF_LVALUE;
            }

            EXPRTHISPOINTER rval = new EXPRTHISPOINTER();
            rval.kind = ExpressionKind.EK_THISPOINTER;
            rval.type = type;
            rval.flags = flags;
            rval.local = pLocal;
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRBOUNDLAMBDA CreateAnonymousMethod(AggregateType delegateType)
        {
            Debug.Assert(delegateType == null || delegateType.isDelegateType());
            EXPRBOUNDLAMBDA rval = new EXPRBOUNDLAMBDA();
            rval.kind = ExpressionKind.EK_BOUNDLAMBDA;
            rval.type = delegateType;
            rval.flags = 0;
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRUNBOUNDLAMBDA CreateLambda()
        {
            CType type = GetTypes().GetAnonMethType();


            EXPRUNBOUNDLAMBDA rval = new EXPRUNBOUNDLAMBDA();
            rval.kind = ExpressionKind.EK_UNBOUNDLAMBDA;
            rval.type = type;
            rval.flags = 0;
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRHOISTEDLOCALEXPR CreateHoistedLocalInExpression(EXPRLOCAL localToHoist)
        {
            Debug.Assert(localToHoist != null);
            EXPRHOISTEDLOCALEXPR rval = new EXPRHOISTEDLOCALEXPR();
            rval.kind = ExpressionKind.EK_HOISTEDLOCALEXPR;
            rval.type = GetTypes().GetOptPredefAgg(PredefinedType.PT_EXPRESSION).getThisType();
            rval.flags = 0;
            return rval;
        }

        public EXPRMETHODINFO CreateMethodInfo(MethPropWithInst mwi)
        {
            return CreateMethodInfo(mwi.Meth(), mwi.GetType(), mwi.TypeArgs);
        }

        public EXPRMETHODINFO CreateMethodInfo(MethodSymbol method, AggregateType methodType, TypeArray methodParameters)
        {
            Debug.Assert(method != null);
            Debug.Assert(methodType != null);
            EXPRMETHODINFO methodInfo = new EXPRMETHODINFO();
            CType type;
            if (method.IsConstructor())
            {
                type = GetTypes().GetOptPredefAgg(PredefinedType.PT_CONSTRUCTORINFO).getThisType();
            }
            else
            {
                type = GetTypes().GetOptPredefAgg(PredefinedType.PT_METHODINFO).getThisType();
            }

            methodInfo.kind = ExpressionKind.EK_METHODINFO;
            methodInfo.type = type;
            methodInfo.flags = 0;
            methodInfo.Method = new MethWithInst(method, methodType, methodParameters);
            return methodInfo;
        }

        public EXPRPropertyInfo CreatePropertyInfo(PropertySymbol prop, AggregateType propertyType)
        {
            Debug.Assert(prop != null);
            Debug.Assert(propertyType != null);
            EXPRPropertyInfo propInfo = new EXPRPropertyInfo();

            propInfo.kind = ExpressionKind.EK_PROPERTYINFO;
            propInfo.type = GetTypes().GetOptPredefAgg(PredefinedType.PT_PROPERTYINFO).getThisType();
            propInfo.flags = 0;
            propInfo.Property = new PropWithType(prop, propertyType);

            return propInfo;
        }

        public EXPRFIELDINFO CreateFieldInfo(FieldSymbol field, AggregateType fieldType)
        {
            Debug.Assert(field != null);
            Debug.Assert(fieldType != null);
            EXPRFIELDINFO rval = new EXPRFIELDINFO();
            rval.kind = ExpressionKind.EK_FIELDINFO;
            rval.type = GetTypes().GetOptPredefAgg(PredefinedType.PT_FIELDINFO).getThisType();
            rval.flags = 0;
            rval.Init(field, fieldType);
            return rval;
        }

        private EXPRTYPEOF CreateTypeOf(EXPRTYPEORNAMESPACE pSourceType)
        {
            EXPRTYPEOF rval = new EXPRTYPEOF();
            rval.kind = ExpressionKind.EK_TYPEOF;
            rval.type = GetTypes().GetReqPredefAgg(PredefinedType.PT_TYPE).getThisType();
            rval.flags = EXPRFLAG.EXF_CANTBENULL;
            rval.SetSourceType(pSourceType);
            Debug.Assert(rval != null);
            return (rval);
        }
        public EXPRTYPEOF CreateTypeOf(CType pSourceType)
        {
            return CreateTypeOf(MakeClass(pSourceType));
        }

        public EXPRUSERLOGOP CreateUserLogOp(CType pType, EXPR pCallTF, EXPRCALL pCallOp)
        {
            Debug.Assert(pCallTF != null);
            Debug.Assert(pCallOp != null);
            Debug.Assert(pCallOp.GetOptionalArguments() != null);
            Debug.Assert(pCallOp.GetOptionalArguments().isLIST());
            Debug.Assert(pCallOp.GetOptionalArguments().asLIST().GetOptionalElement() != null);
            EXPRUSERLOGOP rval = new EXPRUSERLOGOP();
            EXPR leftChild = pCallOp.GetOptionalArguments().asLIST().GetOptionalElement();
            Debug.Assert(leftChild != null);
            if (leftChild.isWRAP())
            {
                // In the EE case, we don't create WRAPEXPRs.
                leftChild = leftChild.asWRAP().GetOptionalExpression();
                Debug.Assert(leftChild != null);
            }
            rval.kind = ExpressionKind.EK_USERLOGOP;
            rval.type = pType;
            rval.flags = EXPRFLAG.EXF_ASSGOP;
            rval.TrueFalseCall = pCallTF;
            rval.OperatorCall = pCallOp;
            rval.FirstOperandToExamine = leftChild;
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRUSERLOGOP CreateUserLogOpError(CType pType, EXPR pCallTF, EXPRCALL pCallOp)
        {
            EXPRUSERLOGOP rval = CreateUserLogOp(pType, pCallTF, pCallOp);
            rval.SetError();
            return rval;
        }

        public EXPRCONCAT CreateConcat(EXPR op1, EXPR op2)
        {
            Debug.Assert(op1 != null && op1.type != null);
            Debug.Assert(op2 != null && op2.type != null);
            Debug.Assert(op1.type.isPredefType(PredefinedType.PT_STRING) || op2.type.isPredefType(PredefinedType.PT_STRING));

            CType type = op1.type;
            if (!type.isPredefType(PredefinedType.PT_STRING))
            {
                type = op2.type;
            }

            Debug.Assert(type.isPredefType(PredefinedType.PT_STRING));

            EXPRCONCAT rval = new EXPRCONCAT();
            rval.kind = ExpressionKind.EK_CONCAT;
            rval.type = type;
            rval.flags = 0;
            rval.SetFirstArgument(op1);
            rval.SetSecondArgument(op2);
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRCONSTANT CreateStringConstant(string str)
        {
            return CreateConstant(GetTypes().GetReqPredefAgg(PredefinedType.PT_STRING).getThisType(), _constants.Create(str));
        }

        public EXPRMULTIGET CreateMultiGet(EXPRFLAG nFlags, CType pType, EXPRMULTI pOptionalMulti)
        {
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_MASK_ANY)));
            EXPRMULTIGET rval = new EXPRMULTIGET();

            rval.kind = ExpressionKind.EK_MULTIGET;
            rval.type = pType;
            rval.flags = nFlags;
            rval.SetOptionalMulti(pOptionalMulti);
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRMULTI CreateMulti(EXPRFLAG nFlags, CType pType, EXPR pLeft, EXPR pOp)
        {
            Debug.Assert(pLeft != null);
            Debug.Assert(pOp != null);
            EXPRMULTI rval = new EXPRMULTI();

            rval.kind = ExpressionKind.EK_MULTI;
            rval.type = pType;
            rval.flags = nFlags;
            rval.SetLeft(pLeft);
            rval.SetOperator(pOp);
            Debug.Assert(rval != null);
            return (rval);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // Precondition:
        //
        // pType - Non-null
        //
        // This returns a null for reference types and an EXPRZEROINIT for all others.

        public EXPR CreateZeroInit(CType pType)
        {
            EXPRCLASS exprClass = MakeClass(pType);
            return CreateZeroInit(exprClass);
        }

        private EXPR CreateZeroInit(EXPRTYPEORNAMESPACE pTypeExpr)
        {
            return CreateZeroInit(pTypeExpr, null, false);
        }

        private EXPR CreateZeroInit(EXPRTYPEORNAMESPACE pTypeExpr, EXPR pOptionalOriginalConstructorCall, bool isConstructor)
        {
            Debug.Assert(pTypeExpr != null);
            CType pType = pTypeExpr.TypeOrNamespace.AsType();
            bool bIsError = false;

            if (pType.isEnumType())
            {
                // For enum types, we create a constant that has the default value
                // as an object pointer.
                ConstValFactory factory = new ConstValFactory();
                EXPRCONSTANT expr = CreateConstant(pType, factory.Create(Activator.CreateInstance(pType.AssociatedSystemType)));
                return expr;
            }

            switch (pType.fundType())
            {
                default:
                    bIsError = true;
                    break;

                case FUNDTYPE.FT_PTR:
                    {
                        CType nullType = GetTypes().GetNullType();

                        // It looks like this if is always false ...
                        if (nullType.fundType() == pType.fundType())
                        {
                            // Create a constant here.

                            EXPRCONSTANT expr = CreateConstant(pType, ConstValFactory.GetDefaultValue(ConstValKind.IntPtr));
                            return (expr);
                        }

                        // Just allocate a new node and fill it in.

                        EXPRCAST cast = CreateCast(0, pTypeExpr, CreateNull());
                        return (cast);
                    }

                case FUNDTYPE.FT_REF:
                case FUNDTYPE.FT_I1:
                case FUNDTYPE.FT_U1:
                case FUNDTYPE.FT_I2:
                case FUNDTYPE.FT_U2:
                case FUNDTYPE.FT_I4:
                case FUNDTYPE.FT_U4:
                case FUNDTYPE.FT_I8:
                case FUNDTYPE.FT_U8:
                case FUNDTYPE.FT_R4:
                case FUNDTYPE.FT_R8:
                    {
                        EXPRCONSTANT expr = CreateConstant(pType, ConstValFactory.GetDefaultValue(pType.constValKind()));
                        EXPRCONSTANT exprInOriginal = CreateConstant(pType, ConstValFactory.GetDefaultValue(pType.constValKind()));
                        exprInOriginal.SetOptionalConstructorCall(pOptionalOriginalConstructorCall);
                        return expr;
                    }
                case FUNDTYPE.FT_STRUCT:
                    if (pType.isPredefType(PredefinedType.PT_DECIMAL))
                    {
                        EXPRCONSTANT expr = CreateConstant(pType, ConstValFactory.GetDefaultValue(pType.constValKind()));
                        EXPRCONSTANT exprOriginal = CreateConstant(pType, ConstValFactory.GetDefaultValue(pType.constValKind()));
                        exprOriginal.SetOptionalConstructorCall(pOptionalOriginalConstructorCall);
                        return expr;
                    }
                    break;

                case FUNDTYPE.FT_VAR:
                    break;
            }

            EXPRZEROINIT rval = new EXPRZEROINIT();
            rval.kind = ExpressionKind.EK_ZEROINIT;
            rval.type = pType;
            rval.flags = 0;
            rval.OptionalConstructorCall = pOptionalOriginalConstructorCall;
            rval.IsConstructor = isConstructor;

            if (bIsError)
            {
                rval.SetError();
            }

            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRCONSTANT CreateConstant(CType pType, CONSTVAL constVal)
        {
            return CreateConstant(pType, constVal, null);
        }

        private EXPRCONSTANT CreateConstant(CType pType, CONSTVAL constVal, EXPR pOriginal)
        {
            EXPRCONSTANT rval = CreateConstant(pType);
            rval.setVal(constVal);
            Debug.Assert(rval != null);
            return (rval);
        }

        private EXPRCONSTANT CreateConstant(CType pType)
        {
            EXPRCONSTANT rval = new EXPRCONSTANT();
            rval.kind = ExpressionKind.EK_CONSTANT;
            rval.type = pType;
            rval.flags = 0;
            return rval;
        }

        public EXPRCONSTANT CreateIntegerConstant(int x)
        {
            return CreateConstant(GetTypes().GetReqPredefAgg(PredefinedType.PT_INT).getThisType(), ConstValFactory.GetInt(x));
        }
        public EXPRCONSTANT CreateBoolConstant(bool b)
        {
            return CreateConstant(GetTypes().GetReqPredefAgg(PredefinedType.PT_BOOL).getThisType(), ConstValFactory.GetBool(b));
        }
        public EXPRBLOCK CreateBlock(EXPRBLOCK pOptionalCurrentBlock, EXPRSTMT pOptionalStatements, Scope pOptionalScope)
        {
            EXPRBLOCK rval = new EXPRBLOCK();
            rval.kind = ExpressionKind.EK_BLOCK;
            rval.type = null;
            rval.flags = 0;
            rval.SetOptionalStatements(pOptionalStatements);
            rval.OptionalScopeSymbol = pOptionalScope;
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRQUESTIONMARK CreateQuestionMark(EXPR pTestExpression, EXPRBINOP pConsequence)
        {
            Debug.Assert(pTestExpression != null);
            Debug.Assert(pConsequence != null);

            CType pType = pConsequence.type;
            if (pType == null)
            {
                Debug.Assert(pConsequence.GetOptionalLeftChild() != null);
                pType = pConsequence.GetOptionalLeftChild().type;
                Debug.Assert(pType != null);
            }
            EXPRQUESTIONMARK pResult = new EXPRQUESTIONMARK();
            pResult.kind = ExpressionKind.EK_QUESTIONMARK;
            pResult.type = pType;
            pResult.flags = 0;
            pResult.SetTestExpression(pTestExpression);
            pResult.SetConsequence(pConsequence);
            Debug.Assert(pResult != null);
            return pResult;
        }

        public EXPRARRAYINDEX CreateArrayIndex(EXPR pArray, EXPR pIndex)
        {
            CType pType = pArray.type;

            if (pType != null && pType.IsArrayType())
            {
                pType = pType.AsArrayType().GetElementType();
            }
            else if (pType == null)
            {
                pType = GetTypes().GetReqPredefAgg(PredefinedType.PT_INT).getThisType();
            }
            EXPRARRAYINDEX pResult = new EXPRARRAYINDEX();
            pResult.kind = ExpressionKind.EK_ARRAYINDEX;
            pResult.type = pType;
            pResult.flags = 0;
            pResult.SetArray(pArray);
            pResult.SetIndex(pIndex);
            return pResult;
        }

        public EXPRARRAYLENGTH CreateArrayLength(EXPR pArray)
        {
            EXPRARRAYLENGTH pResult = new EXPRARRAYLENGTH();
            pResult.kind = ExpressionKind.EK_ARRAYLENGTH;
            pResult.type = GetTypes().GetReqPredefAgg(PredefinedType.PT_INT).getThisType();
            pResult.flags = 0;
            pResult.SetArray(pArray);
            return pResult;
        }

        public EXPRBINOP CreateBinop(ExpressionKind exprKind, CType pType, EXPR p1, EXPR p2)
        {
            //Debug.Assert(exprKind.isBinaryOperator());
            EXPRBINOP rval = new EXPRBINOP();
            rval.kind = exprKind;
            rval.type = pType;
            rval.flags = EXPRFLAG.EXF_BINOP;
            rval.SetOptionalLeftChild(p1);
            rval.SetOptionalRightChild(p2);
            rval.isLifted = false;
            rval.SetOptionalUserDefinedCall(null);
            rval.SetUserDefinedCallMethod(null);
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRUNARYOP CreateUnaryOp(ExpressionKind exprKind, CType pType, EXPR pOperand)
        {
            Debug.Assert(exprKind.isUnaryOperator());
            Debug.Assert(pOperand != null);
            EXPRUNARYOP rval = new EXPRUNARYOP();
            rval.kind = exprKind;
            rval.type = pType;
            rval.flags = 0;
            rval.Child = pOperand;
            rval.OptionalUserDefinedCall = null;
            rval.UserDefinedCallMethod = null;
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPR CreateOperator(ExpressionKind exprKind, CType pType, EXPR pArg1, EXPR pOptionalArg2)
        {
            Debug.Assert(pArg1 != null);
            EXPR rval = null;
            if (exprKind.isUnaryOperator())
            {
                Debug.Assert(pOptionalArg2 == null);
                rval = CreateUnaryOp(exprKind, pType, pArg1);
            }
            else
                rval = CreateBinop(exprKind, pType, pArg1, pOptionalArg2);
            Debug.Assert(rval != null);
            return rval;
        }


        public EXPRBINOP CreateUserDefinedBinop(ExpressionKind exprKind, CType pType, EXPR p1, EXPR p2, EXPR call, MethPropWithInst pmpwi)
        {
            Debug.Assert(p1 != null);
            Debug.Assert(p2 != null);
            Debug.Assert(call != null);
            EXPRBINOP rval = new EXPRBINOP();
            rval.kind = exprKind;
            rval.type = pType;
            rval.flags = EXPRFLAG.EXF_BINOP;
            rval.SetOptionalLeftChild(p1);
            rval.SetOptionalRightChild(p2);
            // The call may be lifted, but we do not mark the outer binop as lifted.
            rval.isLifted = false;
            rval.SetOptionalUserDefinedCall(call);
            rval.SetUserDefinedCallMethod(pmpwi);
            if (call.HasError())
            {
                rval.SetError();
            }
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRUNARYOP CreateUserDefinedUnaryOperator(ExpressionKind exprKind, CType pType, EXPR pOperand, EXPR call, MethPropWithInst pmpwi)
        {
            Debug.Assert(pType != null);
            Debug.Assert(pOperand != null);
            Debug.Assert(call != null);
            Debug.Assert(pmpwi != null);
            EXPRUNARYOP rval = new EXPRUNARYOP();
            rval.kind = exprKind;
            rval.type = pType;
            rval.flags = 0;
            rval.Child = pOperand;
            // The call may be lifted, but we do not mark the outer binop as lifted.
            rval.OptionalUserDefinedCall = call;
            rval.UserDefinedCallMethod = pmpwi;
            if (call.HasError())
            {
                rval.SetError();
            }
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRUNARYOP CreateNeg(EXPRFLAG nFlags, EXPR pOperand)
        {
            Debug.Assert(pOperand != null);
            EXPRUNARYOP pUnaryOp = CreateUnaryOp(ExpressionKind.EK_NEG, pOperand.type, pOperand);
            pUnaryOp.flags |= nFlags;
            return pUnaryOp;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create a node that evaluates the first, evaluates the second, results in the second.

        public EXPRBINOP CreateSequence(EXPR p1, EXPR p2)
        {
            Debug.Assert(p1 != null);
            Debug.Assert(p2 != null);
            return CreateBinop(ExpressionKind.EK_SEQUENCE, p2.type, p1, p2);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create a node that evaluates the first, evaluates the second, results in the first.

        public EXPRBINOP CreateReverseSequence(EXPR p1, EXPR p2)
        {
            Debug.Assert(p1 != null);
            Debug.Assert(p2 != null);
            return CreateBinop(ExpressionKind.EK_SEQREV, p1.type, p1, p2);
        }

        public EXPRASSIGNMENT CreateAssignment(EXPR pLHS, EXPR pRHS)
        {
            EXPRASSIGNMENT pAssignment = new EXPRASSIGNMENT();
            pAssignment.kind = ExpressionKind.EK_ASSIGNMENT;
            pAssignment.type = pLHS.type;
            pAssignment.flags = EXPRFLAG.EXF_ASSGOP;
            pAssignment.SetLHS(pLHS);
            pAssignment.SetRHS(pRHS);
            return pAssignment;
        }

        ////////////////////////////////////////////////////////////////////////////////

        public EXPRNamedArgumentSpecification CreateNamedArgumentSpecification(Name pName, EXPR pValue)
        {
            EXPRNamedArgumentSpecification pResult = new EXPRNamedArgumentSpecification();

            pResult.kind = ExpressionKind.EK_NamedArgumentSpecification;
            pResult.type = pValue.type;
            pResult.flags = 0;
            pResult.Value = pValue;
            pResult.Name = pName;

            return pResult;
        }


        public EXPRWRAP CreateWrap(
            Scope pCurrentScope,
            EXPR pOptionalExpression
        )
        {
            EXPRWRAP rval = new EXPRWRAP();
            rval.kind = ExpressionKind.EK_WRAP;
            rval.type = null;
            rval.flags = 0;
            rval.SetOptionalExpression(pOptionalExpression);
            if (pOptionalExpression != null)
            {
                rval.setType(pOptionalExpression.type);
            }
            rval.flags |= EXPRFLAG.EXF_LVALUE;

            Debug.Assert(rval != null);
            return (rval);
        }
        public EXPRWRAP CreateWrapNoAutoFree(Scope pCurrentScope, EXPR pOptionalWrap)
        {
            EXPRWRAP rval = CreateWrap(pCurrentScope, pOptionalWrap);
            return rval;
        }
        public EXPRBINOP CreateSave(EXPRWRAP wrap)
        {
            Debug.Assert(wrap != null);
            EXPRBINOP expr = CreateBinop(ExpressionKind.EK_SAVE, wrap.type, wrap.GetOptionalExpression(), wrap);
            expr.setAssignment();
            return expr;
        }

        public EXPR CreateNull()
        {
            return CreateConstant(GetTypes().GetNullType(), ConstValFactory.GetNullRef());
        }

        public void AppendItemToList(
            EXPR newItem,
            ref EXPR first,
            ref EXPR last
        )
        {
            if (newItem == null)
            {
                // Nothing changes.
                return;
            }
            if (first == null)
            {
                Debug.Assert(last == first);
                first = newItem;
                last = newItem;
                return;
            }
            if (first.kind != ExpressionKind.EK_LIST)
            {
                Debug.Assert(last == first);
                first = CreateList(first, newItem);
                last = first;
                return;
            }
            Debug.Assert(last.kind == ExpressionKind.EK_LIST);
            Debug.Assert(last.asLIST().OptionalNextListNode != null);
            Debug.Assert(last.asLIST().OptionalNextListNode.kind != ExpressionKind.EK_LIST);
            last.asLIST().OptionalNextListNode = CreateList(last.asLIST().OptionalNextListNode, newItem);
            last = last.asLIST().OptionalNextListNode;
        }

        public EXPRLIST CreateList(EXPR op1, EXPR op2)
        {
            EXPRLIST rval = new EXPRLIST();
            rval.kind = ExpressionKind.EK_LIST;
            rval.type = null;
            rval.flags = 0;
            rval.SetOptionalElement(op1);
            rval.SetOptionalNextListNode(op2);
            Debug.Assert(rval != null);
            return (rval);
        }
        public EXPRLIST CreateList(EXPR op1, EXPR op2, EXPR op3)
        {
            return CreateList(op1, CreateList(op2, op3));
        }
        public EXPRLIST CreateList(EXPR op1, EXPR op2, EXPR op3, EXPR op4)
        {
            return CreateList(op1, CreateList(op2, CreateList(op3, op4)));
        }
        public EXPRTYPEARGUMENTS CreateTypeArguments(TypeArray pTypeArray, EXPR pOptionalElements)
        {
            Debug.Assert(pTypeArray != null);
            EXPRTYPEARGUMENTS rval = new EXPRTYPEARGUMENTS();
            rval.kind = ExpressionKind.EK_TYPEARGUMENTS;
            rval.type = null;
            rval.flags = 0;
            rval.SetOptionalElements(pOptionalElements);
            return rval;
        }

        public EXPRCLASS CreateClass(CType pType, EXPR pOptionalLHS, EXPRTYPEARGUMENTS pOptionalTypeArguments)
        {
            Debug.Assert(pType != null);
            EXPRCLASS rval = new EXPRCLASS();
            rval.kind = ExpressionKind.EK_CLASS;
            rval.type = pType;
            rval.TypeOrNamespace = pType;
            Debug.Assert(rval != null);
            return (rval);
        }

        public EXPRCLASS MakeClass(CType pType)
        {
            Debug.Assert(pType != null);
            return CreateClass(pType, null/* LHS */, null/* type arguments */);
        }
    }
}

