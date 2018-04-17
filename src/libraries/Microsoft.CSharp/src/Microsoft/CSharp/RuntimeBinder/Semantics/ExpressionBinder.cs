// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // Used by bindUserDefinedConversion
    internal readonly struct UdConvInfo
    {
        public readonly MethWithType Meth;
        public readonly bool SrcImplicit;
        public readonly bool DstImplicit;

        public UdConvInfo(MethWithType mwt, bool srcImplicit, bool dstImplicit)
        {
            Meth = mwt;
            SrcImplicit = srcImplicit;
            DstImplicit = dstImplicit;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    // Small wrapper for passing around argument information for the various BindGrpTo methods
    // It is used because most things only need the type, but in the case of METHGRPs and ANONMETHs
    // the expr is also needed to determine if a conversion is possible
    internal sealed class ArgInfos
    {
        public int carg;
        public TypeArray types;
        public List<Expr> prgexpr;
    }

    internal enum ConstCastResult
    {
        Success,      // Constant can be cast to type
        Failure,      // Constant cannot be cast to type
        CheckFailure  // Constant cannot be cast to type because of overflow in checked context
                      // (Note that this only happens when the conversion is explicit; implicit
                      // conversions never overflow, that's why they're implicit.)
    }

    internal enum AggCastResult
    {
        Success, // We found a conversion, stop looking
        Failure, // This conversion doesn't work, keep looking
        Abort    // No possible conversion can work, stop looking
    }

    internal enum UnaryOperatorSignatureFindResult
    {
        Match,
        Continue,
        Return
    }


    internal enum UnaOpKind
    {
        Plus,
        Minus,
        Tilde,
        Bang,
        IncDec,
        Lim
    }

    [Flags]
    internal enum UnaOpMask
    {
        None = 0,
        Plus = 1 << UnaOpKind.Plus,
        Minus = 1 << UnaOpKind.Minus,
        Tilde = 1 << UnaOpKind.Tilde,
        Bang = 1 << UnaOpKind.Bang,
        IncDec = 1 << UnaOpKind.IncDec,
        // The different combinations needed in operators.cs
        Signed = Plus | Minus | Tilde,
        Unsigned = Plus | Tilde,
        Real = Plus | Minus,
        Bool = Bang,
    }

    [Flags]
    internal enum OpSigFlags
    {
        None = 0,
        Convert = 0x01,    // Convert the operands before calling the bind method
        CanLift = 0x02,    // Operator has a lifted form
        AutoLift = 0x04,   // Standard nullable lifting
        // The different combinations needed in operators.cs
        Value = Convert | CanLift | AutoLift,
        Reference = Convert,
        BoolBit = Convert | CanLift,
    }

    [Flags]
    internal enum LiftFlags
    {
        None = 0,
        Lift1 = 0x01,
        Lift2 = 0x02,
        Convert1 = 0x04,
        Convert2 = 0x08,
    }

    internal enum CheckLvalueKind
    {
        Assignment,
        Increment
    }

    internal enum BinOpFuncKind
    {
        BoolBinOp,
        BoolBitwiseOp,
        DecBinOp,
        DelBinOp,
        EnumBinOp,
        IntBinOp,
        RealBinOp,
        RefCmpOp,
        ShiftOp,
        StrBinOp,
        StrCmpOp,
        None
    }

    internal enum UnaOpFuncKind
    {
        BoolUnaOp,
        DecUnaOp,
        EnumUnaOp,
        IntUnaOp,
        RealUnaOp,
        LiftedIncOpCore,
        None
    }

    internal readonly partial struct ExpressionBinder
    {
        // ExpressionBinder - General Rules
        // 
        // Express the Contract
        // 
        // Use assertions and naming guidelines to express the contract for methods. 
        // The most common issue is whether an argument may be null or not. If an 
        // argument may not be null, then the method must ASSERT that before any other 
        // code. If an argument may be null then the name of the argument should 
        // include 'Optional'. The exception to this rule is the input parse tree 
        // parameter. If the parse tree may be null, then the method name should 
        // include an 'Opt' suffix. For example bindArgumentList should really be 
        // named bindArgumentListOpt. Abbreviations should be avoided, but the 'Opt' 
        // suffix gets an exception because it is used consistently in the language 
        // spec.
        // 
        // 
        // Error Tolerant
        // 
        // Do not rely on the input parse tree being complete. Erroneous code may 
        // result in parse trees with required children missing, or with unexpected 
        // structure. Find out what the invariants are for the parse tree being 
        // consumed and code defensively.
        // 
        // Similarly, the result of binding children nodes may not be 'OK'. The child 
        // node may have contained some semantic errors and the binding code in the 
        // parent must cope gracefully with the result. For example, an EXPRMEMGRP may 
        // contain no members.
        // 
        // 
        // Error Recovery
        // 
        // Always attempt to bind children nodes even if errors have already been 
        // detected in other children. Always build a new node representing a 'best 
        // guess' at the semantics of the parse tree. Never discard the results of 
        // binding a child node even if the binding has errors. Since a new node is 
        // always produced there should always be a place to add bindings with errors 
        // to the result. 
        // 
        // This ensures that full semantic information for all nodes is produced - 
        // that the expression binder always produces a 'best guess' for every 
        // expression in source.
        // 
        // 
        // Error Reporting
        // 
        // If a child expression has an error, then no new error's for the parent 
        // should be reported, unless there is no way that the new error was caused 
        // by any child errors. If child nodes don't have any errors, and the new 
        // node does have an error, then at least one error must be reported. These 
        // rules ensure that an error is always reported for erroneous code, and that 
        // only the most meaningful error is reported from a set of cascading errors.
        // 
        // 
        // Map Back To the Source
        // 
        // When constructing new expression nodes, attach the appropriate parse tree 
        // node. The attached parse tree is used for:
        //             - error location reporting
        //             - debug sequence points 
        //                         - stepping
        //                         - local variable scopes
        //             - finding the most meaningful expression for a parse tree node
        // 
        // 
        // Meaning not Implementation
        // 
        // The expression trees resulting from the initial binding pass should 
        // represent the semantics of the input source code. There should be a 
        // direct mapping between the newly constructed expression and the input 
        // parse tree.
        // 
        // The Whidbey codebase had the habit of producing expressions in the initial 
        // binding which were closer in representation to the generated IL than the 
        // input source code. In Orcas all transformations which may lose semantic 
        // information about the source must be done after the initial expression 
        // binding phase. 
        // 
        // Special Cases
        // 
        //     -   Constant folding - Constant folding is a semantic losing 
        //         transformation which must be performed to complete expression 
        //         analysis. When creating a folded constant, create an expression 
        //         node representing the unfolded expression, then pass this as a 
        //         child expression of a new constant expression.
        //     -   Color Color - The new Type or Instance expression covers this case. 
        //         It is produced from bindSimpleName.
        //     -   Method Group - Method groups should be preserved in expression trees. 
        //         This includes as children of Call expressions, and delegate construction 
        //         nodes.
        //     -   Type Binding - This is a big one. Whenever a type is bound in an 
        //         expression, the binding of the component parts of the type must be 
        //         preserved. This includes every identifier in a dotted type or 
        //         namespace name, as well as the type binding information for the 
        //         type arguments of constructed types. The semantic information for 
        //         intermediate type binding results is represented by an 
        //         EXPRTYPEORNAMESPACE. Types can be bound in several places in 
        //         expressions:
        //             - Sizeof
        //             - Typeof
        //             - New
        //             - Is/As
        //             - Cast
        //             - Type Arguments supplied to generic method calls.
        //             - Left hand side of a dot operator.
        //             - Parameter types in anonymous methods and lambdas.
        //             - Local Variables
        // 
        // 
        // Want to eventually Have's
        // 
        // Only build the new node once all children have been built.
        // Factory should require all children as arguments.
        // Factory method sets the "Do Children have Errors?" bit - not done manually.
        // Once constructed Expression trees are not mutated - doesn't work easily for statements unfortunately.

        private delegate Expr PfnBindBinOp(ExpressionBinder binder, ExpressionKind ek, EXPRFLAG flags, Expr op1, Expr op2);
        private delegate Expr PfnBindUnaOp(ExpressionBinder binder, ExpressionKind ek, EXPRFLAG flags, Expr op);

        public BindingContext Context { get; }

        public ExpressionBinder(BindingContext context)
        {
            Context = context;
        }

        private static AggregateType GetPredefindType(PredefinedType pt)
        {
            Debug.Assert(pt != PredefinedType.PT_VOID); // use getVoidType()

            return SymbolLoader.GetPredefindType(pt);
        }

        private Expr GenerateAssignmentConversion(Expr op1, Expr op2, bool allowExplicit) =>
            allowExplicit ? mustCastCore(op2, op1.Type, 0) : mustConvertCore(op2, op1.Type);

        ////////////////////////////////////////////////////////////////////////////////
        // Bind the simple assignment operator =.

        public Expr BindAssignment(Expr op1, Expr op2, bool allowExplicit)
        {
            Debug.Assert(op1 is ExprCast
                || op1 is ExprArrayIndex
                || op1 is ExprCall
                || op1 is ExprProperty
                || op1 is ExprClass
                || op1 is ExprField);

            CheckLvalue(op1, CheckLvalueKind.Assignment);
            op2 = GenerateAssignmentConversion(op1, op2, allowExplicit);
            return GenerateOptimizedAssignment(op1, op2);
        }

        internal Expr BindArrayIndexCore(Expr pOp1, Expr pOp2)
        {
            CType pIntType = GetPredefindType(PredefinedType.PT_INT);

            ArrayType pArrayType = pOp1.Type as ArrayType;
            Debug.Assert(pArrayType != null);
            CType elementType = pArrayType.ElementType;
            CheckUnsafe(elementType); // added to the binder so we don't bind to pointer ops
            // Check the rank of the array against the number of indices provided, and
            // convert the indexes to ints

            CType pDestType = ChooseArrayIndexType(pOp2);
            ExpressionBinder binder = this;
            Expr transformedIndices = pOp2.Map(
                x =>
                {
                    Expr pTemp = binder.mustConvert(x, pDestType);
                    return pDestType == pIntType
                        ? pTemp
                        : ExprFactory.CreateCast(EXPRFLAG.EXF_INDEXEXPR, pDestType, pTemp);
                });

            // Allocate a new expression, the type is the element type of the array.
            // Array index operations are always lvalues.
            return ExprFactory.CreateArrayIndex(elementType, pOp1, transformedIndices);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create a cast node with the given expression flags. 
        private void bindSimpleCast(Expr exprSrc, CType typeDest, out Expr pexprDest) =>
            bindSimpleCast(exprSrc, typeDest, out pexprDest, 0);

        private void bindSimpleCast(Expr exprSrc, CType typeDest, out Expr pexprDest, EXPRFLAG exprFlags)
        {
            Debug.Assert(typeDest != null);
            // If the source is a constant, and cast is really simple (no change in fundamental
            // type, no flags), then create a new constant node with the new type instead of
            // creating a cast node. This allows compile-time constants to be easily recognized.
            Expr exprConst = exprSrc.GetConst();

            // Make the cast expr anyway, and if we find that we have a constant, then set the cast expr
            // as the original tree for the constant. Otherwise, return the cast expr.

            ExprCast exprCast = ExprFactory.CreateCast(exprFlags, typeDest, exprSrc);
            if (Context.Checked)
            {
                exprCast.Flags |= EXPRFLAG.EXF_CHECKOVERFLOW;
            }

            // Check if we have a compile time constant. If we do, create a constant for it and set the
            // original tree to the cast.

            if (exprConst is ExprConstant constant && exprFlags == 0 &&
                exprSrc.Type.FundamentalType == typeDest.FundamentalType &&
                (!exprSrc.Type.IsPredefType(PredefinedType.PT_STRING) || constant.Val.IsNullRef))
            {
                ExprConstant expr = ExprFactory.CreateConstant(typeDest, constant.Val);
                pexprDest = expr;
                return;
            }

            pexprDest = exprCast;
            Debug.Assert(exprCast.Argument != null);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Binds a call to a method, return type is an error or an EXPRCALL.
        //
        // tree      - ParseTree for error messages
        // pObject    - pObject to call method on
        // pmwi      - Meth to bind to. This will be morphed when we remap to an override.
        // args      - arguments
        // exprFlags - Flags to put on the generated expr

        private ExprCall BindToMethod(MethWithInst mwi, Expr pArguments, ExprMemberGroup pMemGroup, MemLookFlags flags)
        {
            Debug.Assert(mwi.Sym is MethodSymbol && (!mwi.Meth().isOverride || mwi.Meth().isHideByName));
            Debug.Assert(pMemGroup != null);

            Expr pObject = pMemGroup.OptionalObject;
            CType callingObjectType = pObject?.Type;
            PostBindMethod(mwi);
            pObject = AdjustMemberObject(mwi, pObject);
            pMemGroup.OptionalObject = pObject;

            CType pReturnType;
            if ((flags & (MemLookFlags.Ctor | MemLookFlags.NewObj)) == (MemLookFlags.Ctor | MemLookFlags.NewObj))
            {
                pReturnType = mwi.Ats;
            }
            else
            {
                pReturnType = TypeManager.SubstType(mwi.Meth().RetType, mwi.GetType(), mwi.TypeArgs);
            }

            ExprCall pResult = ExprFactory.CreateCall(0, pReturnType, pArguments, pMemGroup, mwi);

            // Set the return type and flags for constructors.
            if ((flags & MemLookFlags.Ctor) != 0)
            {
                if ((flags & MemLookFlags.NewObj) != 0)
                {
                    pResult.Flags |= EXPRFLAG.EXF_NEWOBJCALL | EXPRFLAG.EXF_CANTBENULL;
                }
                else
                {
                    Debug.Assert(pResult.Type == VoidType.Instance);
                }
            }

            verifyMethodArgs(pResult, callingObjectType);
            return pResult;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Construct the Expr node which corresponds to a field expression
        // for a given field and pObject pointer.

        internal Expr BindToField(Expr pOptionalObject, FieldWithType fwt, BindingFlag bindFlags)
        {
            Debug.Assert(fwt.GetType() != null && fwt.Field().getClass() == fwt.GetType().OwningAggregate);

            CType pFieldType = TypeManager.SubstType(fwt.Field().GetType(), fwt.GetType());
            pOptionalObject = AdjustMemberObject(fwt, pOptionalObject);

            CheckUnsafe(pFieldType); // added to the binder so we don't bind to pointer ops

            // lvalue if the object is an lvalue (or it's static) and the field is not readonly.
            // Since dynamic objects for fields come from locals or casts/conversions on locals
            // (never properties) and hence always have EXF_LVALUE set, the first part of this is
            // always true, leaving the rest to be determined by the field ctor
            AssertObjectIsLvalue(pOptionalObject);

            AggregateType fieldType = null;
            // If this field is the backing field of a WindowsRuntime event then we need to bind to its
            // invocationlist property which is a delegate containing all the handlers.
            if (fwt.Field().isEvent && fwt.Field().getEvent() != null
                && fwt.Field().getEvent().IsWindowsRuntimeEvent)
            {
                fieldType = fwt.Field().GetType() as AggregateType;
                if (fieldType != null)
                {
                    // Access event backing field (EventRegistrationTokenTable<T>) using
                    // EventRegistrationTokenTable<T>.GetOrCreateEventRegistrationTokenTable()
                    // to ensure non-null
                    pFieldType = TypeManager.GetParameterModifier(pFieldType, false);
                }
            }

            ExprField pResult = ExprFactory.CreateField(pFieldType, pOptionalObject, fwt);

            Debug.Assert(BindingFlag.BIND_MEMBERSET == (BindingFlag)EXPRFLAG.EXF_MEMBERSET);
            pResult.Flags |= (EXPRFLAG)(bindFlags & BindingFlag.BIND_MEMBERSET);

            if (fieldType != null)
            {
                Name getOrCreateMethodName =
                    NameManager.GetPredefinedName(PredefinedName.PN_GETORCREATEEVENTREGISTRATIONTOKENTABLE);
                SymbolTable.PopulateSymbolTableWithName(
                    getOrCreateMethodName.Text, null, fieldType.AssociatedSystemType);
                MethodSymbol getOrCreateMethod =
                    SymbolLoader.LookupAggMember(getOrCreateMethodName, fieldType.OwningAggregate, symbmask_t.MASK_MethodSymbol)
                         as MethodSymbol;

                MethPropWithInst getOrCreatempwi = new MethPropWithInst(getOrCreateMethod, fieldType);
                ExprMemberGroup getOrCreateGrp = ExprFactory.CreateMemGroup(null, getOrCreatempwi);

                Expr getOrCreateCall = BindToMethod(
                    new MethWithInst(getOrCreatempwi), pResult, getOrCreateGrp, (MemLookFlags)MemLookFlags.None);

                AggregateSymbol fieldTypeSymbol = fieldType.OwningAggregate;
                Name invocationListName = NameManager.GetPredefinedName(PredefinedName.PN_INVOCATIONLIST);

                // InvocationList might not be populated in the symbol table as no one would have called it.
                SymbolTable.PopulateSymbolTableWithName(invocationListName.Text, null, fieldType.AssociatedSystemType);
                PropertySymbol invocationList =
                    SymbolLoader.LookupAggMember(invocationListName, fieldTypeSymbol, symbmask_t.MASK_PropertySymbol)
                         as PropertySymbol;

                MethPropWithInst mpwi = new MethPropWithInst(invocationList, fieldType);
                ExprMemberGroup memGroup = ExprFactory.CreateMemGroup(getOrCreateCall, mpwi);

                PropWithType pwt = new PropWithType(invocationList, fieldType);
                Expr propertyExpr = BindToProperty(getOrCreateCall, pwt, bindFlags, null, memGroup);
                return propertyExpr;
            }

            return pResult;
        }

        ////////////////////////////////////////////////////////////////////////////////

        internal ExprProperty BindToProperty(Expr pObject, PropWithType pwt, BindingFlag bindFlags, Expr args, ExprMemberGroup pMemGroup)
        {
            Debug.Assert(pwt.Sym is PropertySymbol &&
                    pwt.GetType() != null &&
                    pwt.Prop().getClass() == pwt.GetType().OwningAggregate);
            Debug.Assert(pwt.Prop().Params.Count == 0 || pwt.Prop() is IndexerSymbol);

            // We keep track of the type of the pObject which we're doing the call through so that we can report 
            // protection access errors later, either below when binding the get, or later when checking that
            // the setter is actually an lvalue.
            Expr pObjectThrough = pObject;

            PostBindProperty(pwt, out MethWithType mwtGet, out MethWithType mwtSet);

            if (mwtGet &&
                    (!mwtSet ||
                     mwtSet.GetType() == mwtGet.GetType() ||
                     SymbolLoader.HasBaseConversion(mwtGet.GetType(), mwtSet.GetType())
                     )
                )
            {
                pObject = AdjustMemberObject(mwtGet, pObject);
            }
            else if (mwtSet)
            {
                pObject = AdjustMemberObject(mwtSet, pObject);
            }
            else
            {
                pObject = AdjustMemberObject(pwt, pObject);
            }

            pMemGroup.OptionalObject = pObject;
            CType pReturnType = TypeManager.SubstType(pwt.Prop().RetType, pwt.GetType());

            // if we are doing a get on this thing, and there is no get, and
            // most importantly, we are not leaving the arguments to be bound by the array index
            // then error...
            if ((bindFlags & BindingFlag.BIND_RVALUEREQUIRED) != 0)
            {
                if (!mwtGet)
                {
                    throw ErrorHandling.Error(ErrorCode.ERR_PropertyLacksGet, pwt);
                }

                CType type = null;
                if (pObjectThrough != null)
                {
                    type = pObjectThrough.Type;
                }

                ACCESSERROR error = CSemanticChecker.CheckAccess2(mwtGet.Meth(), mwtGet.GetType(), ContextForMemberLookup, type);
                if (error != ACCESSERROR.ACCESSERROR_NOERROR)
                {
                    // if the get exists, but is not accessible, give an error.
                    if (error == ACCESSERROR.ACCESSERROR_NOACCESSTHRU)
                    {
                        throw ErrorHandling.Error(ErrorCode.ERR_BadProtectedAccess, pwt, type, ContextForMemberLookup);
                    }

                    throw ErrorHandling.Error(ErrorCode.ERR_InaccessibleGetter, pwt);
                }
            }

            ExprProperty result = ExprFactory.CreateProperty(pReturnType, pObjectThrough, args, pMemGroup, pwt, mwtSet);
            if (result.OptionalArguments != null)
            {
                verifyMethodArgs(result, pObjectThrough?.Type);
            }

            AssertObjectIsLvalue(result.MemberGroup.OptionalObject);

            return result;
        }

        internal Expr bindUDUnop(ExpressionKind ek, Expr arg)
        {
            Name pName = ExpressionKindName(ek);
            Debug.Assert(pName != null);

            CType typeSrc = arg.Type;

        LAgain:
            switch (typeSrc.TypeKind)
            {
                case TypeKind.TK_NullableType:
                    typeSrc = typeSrc.StripNubs();
                    goto LAgain;
                case TypeKind.TK_AggregateType:
                    if (!typeSrc.IsClassType && !typeSrc.IsStructType || ((AggregateType)typeSrc).OwningAggregate.IsSkipUDOps())
                        return null;
                    break;
                default:
                    return null;
            }

            ArgInfos info = new ArgInfos();

            info.carg = 1;
            FillInArgInfoFromArgList(info, arg);

            List<CandidateFunctionMember> methFirstList = new List<CandidateFunctionMember>();
            MethodSymbol methCur = null;
            AggregateType atsCur = (AggregateType)typeSrc;

            for (;;)
            {
                // Find the next operator.
                methCur = (methCur == null
                    ? SymbolLoader.LookupAggMember(pName, atsCur.OwningAggregate, symbmask_t.MASK_MethodSymbol)
                    : methCur.LookupNext(symbmask_t.MASK_MethodSymbol)) as MethodSymbol;

                if (methCur == null)
                {
                    // Find the next type.
                    // If we've found some applicable methods in a class then we don't need to look any further.
                    if (!methFirstList.IsEmpty())
                    {
                        break;
                    }

                    atsCur = atsCur.BaseClass;
                    if (atsCur == null)
                    {
                        break;
                    }

                    continue;
                }

                // Only look at operators with 1 args.
                if (!methCur.isOperator || methCur.Params.Count != 1)
                {
                    continue;
                }

                Debug.Assert(methCur.typeVars.Count == 0);

                TypeArray paramsCur = TypeManager.SubstTypeArray(methCur.Params, atsCur);
                CType typeParam = paramsCur[0];
                NullableType nubParam;

                if (canConvert(arg, typeParam))
                {
                    methFirstList.Add(new CandidateFunctionMember(
                                    new MethPropWithInst(methCur, atsCur, TypeArray.Empty),
                                    paramsCur,
                                    0,
                                    false));
                }
                else if (typeParam.IsNonNullableValueType &&
                         TypeManager.SubstType(methCur.RetType, atsCur).IsNonNullableValueType &&
                         canConvert(arg, nubParam = TypeManager.GetNullable(typeParam)))
                {
                    methFirstList.Add(new CandidateFunctionMember(
                                    new MethPropWithInst(methCur, atsCur, TypeArray.Empty),
                                    TypeArray.Allocate(nubParam),
                                    1,
                                    false));
                }
            }

            if (methFirstList.IsEmpty())
                return null;

            CandidateFunctionMember pmethAmbig1;
            CandidateFunctionMember pmethAmbig2;
            CandidateFunctionMember pmethBest = FindBestMethod(methFirstList, null, info, out pmethAmbig1, out pmethAmbig2);

            if (pmethBest == null)
            {
                // No winner, so its an ambiguous call...
                throw ErrorHandling.Error(ErrorCode.ERR_AmbigCall, pmethAmbig1.mpwi, pmethAmbig2.mpwi);
            }

            ExprCall call;

            if (pmethBest.ctypeLift != 0)
            {
                call = BindLiftedUDUnop(arg, pmethBest.@params[0], pmethBest.mpwi);
            }
            else
            {
                call = BindUDUnopCall(arg, pmethBest.@params[0], pmethBest.mpwi);
            }

            return ExprFactory.CreateUserDefinedUnaryOperator(ek, call.Type, arg, call, pmethBest.mpwi);
        }

        private ExprCall BindLiftedUDUnop(Expr arg, CType typeArg, MethPropWithInst mpwi)
        {
            CType typeRaw = typeArg.StripNubs();
            if (!(arg.Type is NullableType) || !canConvert(arg.Type.StripNubs(), typeRaw, CONVERTTYPE.NOUDC))
            {
                // Convert then lift.
                arg = mustConvert(arg, typeArg);
            }
            Debug.Assert(arg.Type is NullableType);

            CType typeRet = TypeManager.SubstType(mpwi.Meth().RetType, mpwi.GetType());
            if (!(typeRet is NullableType))
            {
                typeRet = TypeManager.GetNullable(typeRet);
            }

            // First bind the non-lifted version for errors.
            Expr nonLiftedArg = mustCast(arg, typeRaw);
            ExprCall nonLiftedResult = BindUDUnopCall(nonLiftedArg, typeRaw, mpwi);

            ExprMemberGroup pMemGroup = ExprFactory.CreateMemGroup(null, mpwi);
            ExprCall call = ExprFactory.CreateCall(0, typeRet, arg, pMemGroup, null);
            call.MethWithInst = new MethWithInst(mpwi);
            call.CastOfNonLiftedResultToLiftedType = mustCast(nonLiftedResult, typeRet, 0);
            call.NullableCallLiftKind = NullableCallLiftKind.Operator;
            return call;
        }

        private ExprCall BindUDUnopCall(Expr arg, CType typeArg, MethPropWithInst mpwi)
        {
            CType typeRet = TypeManager.SubstType(mpwi.Meth().RetType, mpwi.GetType());
            CheckUnsafe(typeRet); // added to the binder so we don't bind to pointer ops
            ExprMemberGroup pMemGroup = ExprFactory.CreateMemGroup(null, mpwi);
            ExprCall call = ExprFactory.CreateCall(0, typeRet, mustConvert(arg, typeArg), pMemGroup, null);
            call.MethWithInst = new MethWithInst(mpwi);
            verifyMethodArgs(call, mpwi.GetType());
            return call;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Given a method group or indexer group, bind it to the arguments for an 
        // invocation.
        private GroupToArgsBinderResult BindMethodGroupToArgumentsCore(BindingFlag bindFlags, ExprMemberGroup grp, Expr args, int carg, NamedArgumentsKind namedArgumentsKind)
        {
            ArgInfos pargInfo = new ArgInfos {carg = carg};
            FillInArgInfoFromArgList(pargInfo, args);

            ArgInfos pOriginalArgInfo = new ArgInfos {carg = carg};
            FillInArgInfoFromArgList(pOriginalArgInfo, args);

            GroupToArgsBinder binder = new GroupToArgsBinder(this, bindFlags, grp, pargInfo, pOriginalArgInfo, namedArgumentsKind);
            binder.Bind();

            return binder.GetResultsOfBind();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Given a method group or indexer group, bind it to the arguments for an 
        // invocation.
        internal ExprWithArgs BindMethodGroupToArguments(BindingFlag bindFlags, ExprMemberGroup grp, Expr args)
        {
            Debug.Assert(grp.SymKind == SYMKIND.SK_MethodSymbol || grp.SymKind == SYMKIND.SK_PropertySymbol && ((grp.Flags & EXPRFLAG.EXF_INDEXER) != 0));

            // Count the args.
            int carg = CountArguments(args);

            Debug.Assert(grp.Name != null);

            // Do we have named arguments specified, are they after fixed arguments (can position) or
            // non-trailing (can rule out methods only).
            NamedArgumentsKind namedKind = FindNamedArgumentsType(args);

            MethPropWithInst mpwiBest = BindMethodGroupToArgumentsCore(bindFlags, grp, args, carg, namedKind).BestResult;
            if (grp.SymKind == SYMKIND.SK_PropertySymbol)
            {
                Debug.Assert((grp.Flags & EXPRFLAG.EXF_INDEXER) != 0);
                return BindToProperty(grp.OptionalObject, new PropWithType(mpwiBest), bindFlags, args, grp);
            }

            return BindToMethod(new MethWithInst(mpwiBest), args, grp, (MemLookFlags)grp.Flags);
        }

        /////////////////////////////////////////////////////////////////////////////////

        public enum NamedArgumentsKind
        {
            None,
            Positioning,
            NonTrailing
        }

        private static NamedArgumentsKind FindNamedArgumentsType(Expr args)
        {
            Expr list = args;
            while (list != null)
            {
                Expr arg;
                if (list is ExprList next)
                {
                    arg = next.OptionalElement;
                    list = next.OptionalNextListNode;
                }
                else
                {
                    arg = list;
                    list = null;
                }

                Debug.Assert(arg != null);
                if (arg is ExprNamedArgumentSpecification)
                {
                    while (list != null)
                    {
                        if (list is ExprList nextList)
                        {
                            arg = nextList.OptionalElement;
                            list = nextList.OptionalNextListNode;
                        }
                        else
                        {
                            arg = list;
                            list = null;
                        }

                        if (!(arg is ExprNamedArgumentSpecification))
                        {
                            return NamedArgumentsKind.NonTrailing;
                        }
                    }

                    return NamedArgumentsKind.Positioning;
                }
            }

            return NamedArgumentsKind.None;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Report a bad operator types error to the user.
        private static RuntimeBinderException BadOperatorTypesError(Expr pOperand1, Expr pOperand2)
        {
            // This is a hack, but we need to store the operation somewhere... the first argument's as
            // good a place as any.
            string strOp = pOperand1.ErrorString;

            Debug.Assert(pOperand1 != null);
            Debug.Assert(pOperand1.Type != null);

            if (pOperand2 != null)
            {
                Debug.Assert(pOperand2.Type != null);
                return ErrorHandling.Error(ErrorCode.ERR_BadBinaryOps, strOp, pOperand1.Type, pOperand2.Type);
            }

            return ErrorHandling.Error(ErrorCode.ERR_BadUnaryOp, strOp, pOperand1.Type);
        }

        private static ErrorCode GetStandardLvalueError(CheckLvalueKind kind)
        {
            Debug.Assert(kind >= CheckLvalueKind.Assignment && kind <= CheckLvalueKind.Increment);
            return kind == CheckLvalueKind.Increment
                ? ErrorCode.ERR_IncrementLvalueExpected
                : ErrorCode.ERR_AssgLvalueExpected;
        }

        private void CheckLvalueProp(ExprProperty prop)
        {
            Debug.Assert(prop != null);
            Debug.Assert(prop.isLvalue());

            // We have an lvalue property.  Give an error if this is an inaccessible property.

            CType type = null;
            if (prop.OptionalObjectThrough != null)
            {
                type = prop.OptionalObjectThrough.Type;
            }

            CheckPropertyAccess(prop.MethWithTypeSet, prop.PropWithTypeSlot, type);
        }

        private void CheckPropertyAccess(MethWithType mwt, PropWithType pwtSlot, CType type)
        {
            switch (CSemanticChecker.CheckAccess2(mwt.Meth(), mwt.GetType(), ContextForMemberLookup, type))
            {
                case ACCESSERROR.ACCESSERROR_NOACCESSTHRU:
                    throw ErrorHandling.Error(ErrorCode.ERR_BadProtectedAccess, pwtSlot, type, ContextForMemberLookup);
                case ACCESSERROR.ACCESSERROR_NOACCESS:
                    throw ErrorHandling.Error(mwt.Meth().isSetAccessor() ? ErrorCode.ERR_InaccessibleSetter : ErrorCode.ERR_InaccessibleGetter, pwtSlot);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // A false return means not to process the expr any further - it's totally out
        // of place. For example - a method group or an anonymous method.
        private void CheckLvalue(Expr expr, CheckLvalueKind kind)
        {
            if (expr.isLvalue())
            {
                if (expr is ExprProperty prop)
                {
                    CheckLvalueProp(prop);
                }

                return;
            }

            Debug.Assert(!(expr is ExprLocal));
            Debug.Assert(!(expr is ExprMemberGroup));

            switch (expr.Kind)
            {
                case ExpressionKind.Property:
                    ExprProperty prop = (ExprProperty)expr;
                    Debug.Assert(!prop.MethWithTypeSet);
                    // Assigning to a property without a setter.
                    // If we have
                    // bool? b = true; (bool)b = false;
                    // then this is realized immediately as
                    // b.Value = false;
                    // and no ExpressionKind.EK_CAST is generated. We'd rather not give a "you're writing
                    // to a read-only property" error in the case where the property access
                    // is not explicit in the source code.  Fortunately in this case the
                    // cast is still hanging around in the parse tree, so we can look for it.

                    // POSSIBLE ERROR: It would be nice to also give this error for other situations
                    // POSSIBLE ERROR: in which the user is attempting to assign to a value, such as
                    // POSSIBLE ERROR: an explicit (bool)b.Value = false;
                    // POSSIBLE ERROR: Unfortunately we cannot use this trick in that situation because
                    // POSSIBLE ERROR: we've already discarded the OperatorKind.OP_CAST node.  (This is an SyntaxKind.Dot).

                    // SPEC VIOLATION: More generally:
                    // SPEC VIOLATION: The spec states that the result of any cast is a value, not a
                    // SPEC VIOLATION: variable. Unfortunately we do not correctly implement this
                    // SPEC VIOLATION: and we probably should not start implementing it because this
                    // SPEC VIOLATION: would be a breaking change.  We currently discard "no op" casts
                    // SPEC VIOLATION: very aggressively rather than generating an ExpressionKind.EK_CAST node.

                    throw ErrorHandling.Error(ErrorCode.ERR_AssgReadonlyProp, prop.PropWithTypeSlot);

                case ExpressionKind.Field:
                    ExprField field = (ExprField)expr;
                    Debug.Assert(field.FieldWithType.Field().isReadOnly);
                    throw ErrorHandling.Error(
                        field.FieldWithType.Field().isStatic
                            ? ErrorCode.ERR_AssgReadonlyStatic
                            : ErrorCode.ERR_AssgReadonly);

                default:
                    throw ErrorHandling.Error(GetStandardLvalueError(kind));
            }
        }

        private static void PostBindMethod(MethWithInst pMWI)
        {
            MethodSymbol meth = pMWI.Meth();
            if (meth.RetType != null)
            {
                CheckUnsafe(meth.RetType);

                // We need to check unsafe on the parameters as well, since we cannot check in conversion.
                foreach (CType type in meth.Params.Items)
                {
                    CheckUnsafe(type);
                }
            }
        }

        private static void PostBindProperty(PropWithType pwt, out MethWithType pmwtGet, out MethWithType pmwtSet)
        {
            PropertySymbol prop = pwt.Prop();
            Debug.Assert(prop != null);
            // Get the accessors.
            pmwtGet = prop.GetterMethod != null
                ? new MethWithType(prop.GetterMethod, pwt.GetType())
                : new MethWithType();
            pmwtSet = prop.SetterMethod != null
                ? new MethWithType(prop.SetterMethod, pwt.GetType())
                : new MethWithType();

            if (prop.RetType != null)
            {
                CheckUnsafe(prop.RetType);
            }
        }

        private Expr AdjustMemberObject(SymWithType swt, Expr pObject)
        {
            // Assert that the type is present and is an instantiation of the member's parent.
            Debug.Assert(swt.GetType() != null && swt.GetType().OwningAggregate == swt.Sym.parent as AggregateSymbol);
            bool bIsMatchingStatic = IsMatchingStatic(swt, pObject);
            bool isStatic = swt.Sym.isStatic;

            // If our static doesn't match, bail out of here.
            if (!bIsMatchingStatic)
            {
                if (isStatic)
                {
                    // If we have a mismatched static, a static method, and the binding flag
                    // that tells us we're binding simple names, then insert a type here instead.
                    if ((pObject.Flags & EXPRFLAG.EXF_SIMPLENAME) != 0)
                    {
                        // We've made the static match now.
                        return null;
                    }

                    throw ErrorHandling.Error(ErrorCode.ERR_ObjectProhibited, swt);
                }

                throw ErrorHandling.Error(ErrorCode.ERR_ObjectRequired, swt);
            }

            // At this point, all errors for static invocations have been reported, and
            // the object has been nulled out. So return out of here.
            if (isStatic)
            {
                return null;
            }

            // If we're in a constructor, then bail.
            if ((swt.Sym is MethodSymbol) && swt.Meth().IsConstructor())
            {
                return pObject;
            }

            if (pObject == null)
            {
                return null;
            }

            CType typeObj = pObject.Type;
            CType typeTmp;

            if (typeObj is NullableType nubTypeObj && (typeTmp = nubTypeObj.GetAts()) != swt.GetType())
            {
                typeObj = typeTmp;
            }

            if (typeObj is TypeParameterType || typeObj is AggregateType)
            {
                AggregateSymbol aggCalled = swt.Sym.parent as AggregateSymbol;
                Debug.Assert(swt.GetType().OwningAggregate == aggCalled);

                pObject = tryConvert(pObject, swt.GetType(), CONVERTTYPE.NOUDC);
                Debug.Assert(pObject != null);
            }

            return pObject;
        }
        /////////////////////////////////////////////////////////////////////////////////

        private static bool IsMatchingStatic(SymWithType swt, Expr pObject)
        {
            Symbol pSym = swt.Sym;

            // Instance constructors are always ok, static constructors are never ok.
            if (pSym is MethodSymbol meth && meth.IsConstructor())
            {
                return !meth.isStatic;
            }

            bool isStatic = swt.Sym.isStatic;

            if (isStatic)
            {
                // If we're static and we don't have an object then we're ok.
                if (pObject == null)
                {
                    return true;
                }

                if ((pObject.Flags & EXPRFLAG.EXF_SAMENAMETYPE) == 0)
                {
                    return false;
                }
            }
            else if (pObject == null)
            {
                // We're not static, and we don't have an object. This is ok in certain scenarios:
                return false;
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // this determines whether the expression as an pObject of a prop or field is an
        // lvalue
        [Conditional("DEBUG")]
        private static void AssertObjectIsLvalue(Expr pObject)
        {
            Debug.Assert (
                       pObject == null ||  // statics are always lvalues
                       (((pObject.Flags & EXPRFLAG.EXF_LVALUE) != 0) && (pObject.Kind != ExpressionKind.Property)) ||
                       // things marked as lvalues have props/fields which are lvalues, with one exception:  props of structs
                       // do not have fields/structs as lvalues

                       !pObject.Type.IsStructOrEnum
                   // non-struct types are lvalues (such as non-struct method returns)
                   );
        }

        private void verifyMethodArgs(ExprWithArgs call, CType callingObjectType)
        {
            Debug.Assert(call != null);
            Expr argsPtr = call.OptionalArguments;
            SymWithType swt = call.GetSymWithType();
            MethodOrPropertySymbol mp = swt.Sym as MethodOrPropertySymbol;
            TypeArray pTypeArgs = (call as ExprCall)?.MethWithInst.TypeArgs;
            Expr newArgs;
            AdjustCallArgumentsForParams(callingObjectType, swt.GetType(), mp, pTypeArgs, argsPtr, out newArgs);
            call.OptionalArguments = newArgs;
        }

        private void AdjustCallArgumentsForParams(CType callingObjectType, CType type, MethodOrPropertySymbol mp, TypeArray pTypeArgs, Expr argsPtr, out Expr newArgs)
        {
            Debug.Assert(mp != null);
            Debug.Assert(mp.Params != null);
            newArgs = null;
            Expr newArgsTail = null;

            MethodOrPropertySymbol mostDerivedMethod = GroupToArgsBinder.FindMostDerivedMethod(mp, callingObjectType);

            int paramCount = mp.Params.Count;
            TypeArray @params = mp.Params;
            int iDst = 0;
            MethodSymbol m = mp as MethodSymbol;
            int argCount = ExpressionIterator.Count(argsPtr);

            Debug.Assert(!@params.Items.Any(p => p is ArgumentListType)); // We should never have picked a varargs method to bind to.

            bool bDontFixParamArray = false;

            ExpressionIterator it = new ExpressionIterator(argsPtr);

            if (argsPtr == null)
            {
                if (mp.isParamArray)
                    goto FIXUPPARAMLIST;
                return;
            }
            for (; !it.AtEnd(); it.MoveNext())
            {
                Expr indir = it.Current();
                // this will splice the optional arguments into the list

                if (indir.Type is ParameterModifierType)
                {
                    if (paramCount != 0)
                        paramCount--;
                    ExprFactory.AppendItemToList(indir, ref newArgs, ref newArgsTail);
                }
                else if (paramCount != 0)
                {
                    if (paramCount == 1 && mp.isParamArray && argCount > mp.Params.Count)
                    {
                        // we arrived at the last formal, and we have more than one actual, so
                        // we need to put the rest in an array...
                        goto FIXUPPARAMLIST;
                    }

                    Expr argument = indir;
                    Expr rval;
                    if (argument is ExprNamedArgumentSpecification named)
                    {
                        int index = 0;
                        // If we're named, look for the type of the matching name.
                        foreach (Name i in mostDerivedMethod.ParameterNames)
                        {
                            if (i == named.Name)
                            {
                                break;
                            }
                            index++;
                        }

                        Debug.Assert(index != mp.Params.Count);
                        CType substDestType = TypeManager.SubstType(@params[index], type, pTypeArgs);

                        // If we cant convert the argument and we're the param array argument, then deal with it.
                        if (!canConvert(named.Value, substDestType) &&
                            mp.isParamArray && index == mp.Params.Count - 1)
                        {
                            // We have a param array, but we're not at the end yet. This will happen
                            // with named arguments when the user specifies a name for the param array,
                            // and its not an actual array.
                            // 
                            // For example:
                            // void Foo(int y, params int[] x);
                            // ...
                            // Foo(x:1, y:1);
                            CType arrayType = (ArrayType)TypeManager.SubstType(mp.Params[mp.Params.Count - 1], type, pTypeArgs);

                            // Use an EK_ARRINIT even in the empty case so empty param arrays in attributes work.
                            ExprArrayInit arrayInit = ExprFactory.CreateArrayInit(arrayType, null, null, new[] { 0 });
                            arrayInit.GeneratedForParamArray = true;
                            arrayInit.OptionalArguments = named.Value;

                            named.Value = arrayInit;
                            bDontFixParamArray = true;
                        }
                        else
                        {
                            // Otherwise, force the conversion and get errors if needed.
                            named.Value = tryConvert(named.Value, substDestType);
                        }
                        rval = argument;
                    }
                    else
                    {
                        CType substDestType = TypeManager.SubstType(@params[iDst], type, pTypeArgs);
                        rval = tryConvert(indir, substDestType);
                    }

                    if (rval == null)
                    {
                        // the last arg failed to fix up, so it must fixup into the array element
                        // if we have a param array (we will be passing a 1 element array...)
                        if (mp.isParamArray && paramCount == 1 && argCount >= mp.Params.Count)
                        {
                            goto FIXUPPARAMLIST;
                        }
                        else
                        {
                            // This is either the error case that the args are of the wrong type, 
                            // or that we have some optional arguments being used. Either way,
                            // we won't need to expand the param array.
                            return;
                        }
                    }
                    Debug.Assert(rval != null);
                    indir = rval;
                    ExprFactory.AppendItemToList(rval, ref newArgs, ref newArgsTail);
                    paramCount--;
                }
                // note that destype might not be valid if we are in varargs, but then we won't ever use it...
                iDst++;

                if (paramCount != 0 && mp.isParamArray && iDst == argCount)
                {
                    // we run out of actuals, but we still have formals, so this is an empty array being passed
                    // into the last param...
                    indir = null;
                    it.MoveNext();
                    goto FIXUPPARAMLIST;
                }
            }

            return;

        FIXUPPARAMLIST:
            if (bDontFixParamArray)
            {
                // We've already fixed the param array for named arguments.
                return;
            }

            // we need to create an array and put it as the last arg...
            CType substitutedArrayType = TypeManager.SubstType(mp.Params[mp.Params.Count - 1], type, pTypeArgs);
            if (!(substitutedArrayType is ArrayType subArr) || !subArr.IsSZArray)
            {
                // Invalid type for params array parameter. Happens in LAF scenarios, e.g.
                //
                // void Foo(int i, params int ar = null) { }
                // ...
                // Foo(1);
                return;
            }

            CType elementType = subArr.ElementType;

            // Use an EK_ARRINIT even in the empty case so empty param arrays in attributes work.
            ExprArrayInit exprArrayInit = ExprFactory.CreateArrayInit(substitutedArrayType, null, null, new[] { 0 });
            exprArrayInit.GeneratedForParamArray = true;

            if (it.AtEnd())
            {
                exprArrayInit.DimensionSizes[0] = 0;
                exprArrayInit.OptionalArguments = null;
                if (argsPtr == null)
                {
                    argsPtr = exprArrayInit;
                }
                else
                {
                    argsPtr = ExprFactory.CreateList(argsPtr, exprArrayInit);
                }
                ExprFactory.AppendItemToList(exprArrayInit, ref newArgs, ref newArgsTail);
            }
            else
            {
                // Go through the list - for each argument, do the conversion and append it to the new list.
                Expr newList = null;
                Expr newListTail = null;
                int count = 0;

                for (; !it.AtEnd(); it.MoveNext())
                {
                    Expr expr = it.Current();
                    count++;

                    if (expr is ExprNamedArgumentSpecification named)
                    {
                        named.Value = tryConvert(named.Value, elementType);
                    }
                    else
                    {
                        expr = tryConvert(expr, elementType);
                    }
                    ExprFactory.AppendItemToList(expr, ref newList, ref newListTail);
                }

                exprArrayInit.DimensionSizes[0] = count;
                exprArrayInit.OptionalArguments = newList;
                ExprFactory.AppendItemToList(exprArrayInit, ref newArgs, ref newArgsTail);
            }
        }

        private static readonly PredefinedType[] s_rgptIntOp =
        {
            PredefinedType.PT_INT,
            PredefinedType.PT_UINT,
            PredefinedType.PT_LONG,
            PredefinedType.PT_ULONG
        };

        internal CType ChooseArrayIndexType(Expr args)
        {
            // first, select the allowable types
            foreach (PredefinedType predef in s_rgptIntOp)
            {
                CType type = GetPredefindType(predef);
                foreach (Expr arg in args.ToEnumerable())
                {
                    if (!canConvert(arg, type))
                    {
                        goto NEXTI;
                    }
                }

                return type;

             NEXTI:
                ;
            }

            // Provide better error message in attempting cast to int.
            return GetPredefindType(PredefinedType.PT_INT);
        }

        internal static void FillInArgInfoFromArgList(ArgInfos argInfo, Expr args)
        {
            CType[] prgtype = new CType[argInfo.carg];
            argInfo.prgexpr = new List<Expr>();

            int iarg = 0;
            for (Expr list = args; list != null; iarg++)
            {
                Expr arg;
                if (list is ExprList next)
                {
                    arg = next.OptionalElement;
                    list = next.OptionalNextListNode;
                }
                else
                {
                    arg = list;
                    list = null;
                }

                Debug.Assert(arg != null);
                Debug.Assert(arg.Type != null);

                prgtype[iarg] = arg.Type;
                argInfo.prgexpr.Add(arg);
            }

            Debug.Assert(iarg <= argInfo.carg);
            argInfo.types = TypeArray.Allocate(prgtype);
        }

        private static bool TryGetExpandedParams(TypeArray @params, int count, out TypeArray ppExpandedParams)
        {
            CType[] prgtype;
            if (count < @params.Count - 1)
            {
                // The user has specified less arguments than our parameters, but we still
                // need to return our set of types without the param array. This is in the 
                // case that all the parameters are optional.
                prgtype = new CType[@params.Count - 1];
                @params.CopyItems(0, @params.Count - 1, prgtype);
                ppExpandedParams = TypeArray.Allocate(prgtype);
                return true;
            }

            prgtype = new CType[count];
            @params.CopyItems(0, @params.Count - 1, prgtype);

            CType type = @params[@params.Count - 1];

            if (!(type is ArrayType arr))
            {
                ppExpandedParams = null;
                // If we don't have an array sym, we don't have expanded parameters.
                return false;
            }

            // At this point, we have an array sym.
            CType elementType = arr.ElementType;

            for (int itype = @params.Count - 1; itype < count; itype++)
            {
                prgtype[itype] = elementType;
            }

            ppExpandedParams = TypeArray.Allocate(prgtype);

            return true;
        }

        // Is the method/property callable. Not if it's an override or not user-callable.
        public static bool IsMethPropCallable(MethodOrPropertySymbol sym, bool requireUC)
        {
            // The hide-by-pName option for binding other languages takes precedence over general
            // rules of not binding to overrides.
            return (!sym.isOverride || sym.isHideByName) && (!requireUC || sym.isUserCallable());
        }

        private static bool IsConvInTable(List<UdConvInfo> convTable, MethodSymbol meth, AggregateType ats, bool fSrc, bool fDst)
        {
            foreach (UdConvInfo conv in convTable)
            {
                if (conv.Meth.Meth() == meth &&
                    conv.Meth.GetType() == ats &&
                    conv.SrcImplicit == fSrc &&
                    conv.DstImplicit == fDst)
                {
                    return true;
                }
            }

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Check to see if an integral constant is within range of a integral 
        // destination type.
        private static bool isConstantInRange(ExprConstant exprSrc, CType typeDest)
        {
            return isConstantInRange(exprSrc, typeDest, false);
        }

        private static bool isConstantInRange(ExprConstant exprSrc, CType typeDest, bool realsOk)
        {
            FUNDTYPE ftSrc = exprSrc.Type.FundamentalType;
            FUNDTYPE ftDest = typeDest.FundamentalType;

            if (ftSrc > FUNDTYPE.FT_LASTINTEGRAL || ftDest > FUNDTYPE.FT_LASTINTEGRAL)
            {
                if (!realsOk)
                {
                    return false;
                }
                else if (ftSrc > FUNDTYPE.FT_LASTNUMERIC || ftDest > FUNDTYPE.FT_LASTNUMERIC)
                {
                    return false;
                }
            }

            // if converting to a float type, this always succeeds...
            if (ftDest > FUNDTYPE.FT_LASTINTEGRAL)
            {
                return true;
            }

            // if converting from float to an integral type, we need to check whether it fits
            if (ftSrc > FUNDTYPE.FT_LASTINTEGRAL)
            {
                double dvalue = exprSrc.Val.DoubleVal;

                switch (ftDest)
                {
                    case FUNDTYPE.FT_I1:
                        if (dvalue > -0x81 && dvalue < 0x80)
                            return true;
                        break;
                    case FUNDTYPE.FT_I2:
                        if (dvalue > -0x8001 && dvalue < 0x8000)
                            return true;
                        break;
                    case FUNDTYPE.FT_I4:
                        if (dvalue > I64(-0x80000001) && dvalue < I64(0x80000000))
                            return true;
                        break;
                    case FUNDTYPE.FT_I8:
                        // 0x7FFFFFFFFFFFFFFFFFFF is rounded to 0x800000000000000000000 in 64 bit double precision
                        // floating point representation. The conversion back to ulong is not possible.
                        if (dvalue >= -9223372036854775808.0 && dvalue < 9223372036854775808.0)
                        {
                            return true;
                        }
                        break;
                    case FUNDTYPE.FT_U1:
                        if (dvalue > -1 && dvalue < 0x100)
                            return true;
                        break;
                    case FUNDTYPE.FT_U2:
                        if (dvalue > -1 && dvalue < 0x10000)
                            return true;
                        break;
                    case FUNDTYPE.FT_U4:
                        if (dvalue > -1 && dvalue < I64(0x100000000))
                            return true;
                        break;
                    case FUNDTYPE.FT_U8:
                        // 0xFFFFFFFFFFFFFFFFFFFF is rounded to 0x100000000000000000000 in 64 bit double precision
                        // floating point representation. The conversion back to ulong is not possible.
                        if (dvalue > -1.0 && dvalue < 18446744073709551616.0)
                        {
                            return true;
                        }
                        break;
                    default:
                        break;
                }
                return false;
            }

            // U8 src is unsigned, so deal with values > MAX_LONG here.
            if (ftSrc == FUNDTYPE.FT_U8)
            {
                ulong value = exprSrc.UInt64Value;

                switch (ftDest)
                {
                    case FUNDTYPE.FT_I1:
                        if (value <= (ulong)sbyte.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_I2:
                        if (value <= (ulong)short.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_I4:
                        if (value <= int.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_I8:
                        if (value <= long.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_U1:
                        if (value <= byte.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_U2:
                        if (value <= ushort.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_U4:
                        if (value <= uint.MaxValue)
                            return true;
                        break;
                    case FUNDTYPE.FT_U8:
                        return true;
                    default:
                        break;
                }
            }
            else
            {
                long value = exprSrc.Int64Value;

                switch (ftDest)
                {
                    case FUNDTYPE.FT_I1:
                        if (value >= -128 && value <= 127)
                            return true;
                        break;
                    case FUNDTYPE.FT_I2:
                        if (value >= -0x8000 && value <= 0x7fff)
                            return true;
                        break;
                    case FUNDTYPE.FT_I4:
                        if (value >= I64(-0x80000000) && value <= I64(0x7fffffff))
                            return true;
                        break;
                    case FUNDTYPE.FT_I8:
                        return true;
                    case FUNDTYPE.FT_U1:
                        if (value >= 0 && value <= 0xff)
                            return true;
                        break;
                    case FUNDTYPE.FT_U2:
                        if (value >= 0 && value <= 0xffff)
                            return true;
                        break;
                    case FUNDTYPE.FT_U4:
                        if (value >= 0 && value <= I64(0xffffffff))
                            return true;
                        break;
                    case FUNDTYPE.FT_U8:
                        if (value >= 0)
                            return true;
                        break;
                    default:
                        break;
                }
            }
            return false;
        }

        private static readonly PredefinedName[] s_EK2NAME =
        {
            PredefinedName.PN_OPEQUALS,
            PredefinedName.PN_OPCOMPARE,
            PredefinedName.PN_OPTRUE,
            PredefinedName.PN_OPFALSE,
            PredefinedName.PN_OPINCREMENT,
            PredefinedName.PN_OPDECREMENT,
            PredefinedName.PN_OPNEGATION,
            PredefinedName.PN_OPEQUALITY,
            PredefinedName.PN_OPINEQUALITY,
            PredefinedName.PN_OPLESSTHAN,
            PredefinedName.PN_OPLESSTHANOREQUAL,
            PredefinedName.PN_OPGREATERTHAN,
            PredefinedName.PN_OPGREATERTHANOREQUAL,
            PredefinedName.PN_OPPLUS,
            PredefinedName.PN_OPMINUS,
            PredefinedName.PN_OPMULTIPLY,
            PredefinedName.PN_OPDIVISION,
            PredefinedName.PN_OPMODULUS,
            PredefinedName.PN_OPUNARYMINUS,
            PredefinedName.PN_OPUNARYPLUS,
            PredefinedName.PN_OPBITWISEAND,
            PredefinedName.PN_OPBITWISEOR,
            PredefinedName.PN_OPXOR,
            PredefinedName.PN_OPCOMPLEMENT,
            PredefinedName.PN_OPLEFTSHIFT,
            PredefinedName.PN_OPRIGHTSHIFT,
        };

        private static Name ExpressionKindName(ExpressionKind ek)
        {
            Debug.Assert(ek >= ExpressionKind.FirstOp && (ek - ExpressionKind.FirstOp) < (int)s_EK2NAME.Length);
            return NameManager.GetPredefinedName(s_EK2NAME[ek - ExpressionKind.FirstOp]);
        }

        private static void CheckUnsafe(CType type)
        {
            if (type == null || type.IsUnsafe())
            {
                throw ErrorHandling.Error(ErrorCode.ERR_UnsafeNeeded);
            }
        }

        private AggregateSymbol ContextForMemberLookup => Context.ContextForMemberLookup;

        private static ExprWrap WrapShortLivedExpression(Expr expr) => ExprFactory.CreateWrap(expr);

        private static ExprAssignment GenerateOptimizedAssignment(Expr op1, Expr op2) => ExprFactory.CreateAssignment(op1, op2);

        internal static int CountArguments(Expr args)
        {
            int carg = 0;
            for (Expr list = args; list != null; carg++)
            {
                Expr arg;

                if (list is ExprList next)
                {
                    arg = next.OptionalElement;
                    list = next.OptionalNextListNode;
                }
                else
                {
                    arg = list;
                    list = null;
                }

                Debug.Assert(arg != null);
            }

            return carg;
        }
    }
}

