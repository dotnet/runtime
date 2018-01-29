// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class TypeManager
    {
        private BSYMMGR _BSymmgr;
        private PredefinedTypes _predefTypes;

        private readonly TypeTable _typeTable;
        private SymbolTable _symbolTable;

        private readonly StdTypeVarColl _stvcMethod;

        public TypeManager(BSYMMGR bsymmgr, PredefinedTypes predefTypes)
        {
            _typeTable = new TypeTable();

            _stvcMethod = new StdTypeVarColl();
            _BSymmgr = bsymmgr;
            _predefTypes = predefTypes;
        }

        public void InitTypeFactory(SymbolTable table)
        {
            _symbolTable = table;
        }

        public SymbolTable SymbolTable => _symbolTable;

        private sealed class StdTypeVarColl
        {
            private readonly List<TypeParameterType> prgptvs;

            public StdTypeVarColl()
            {
                prgptvs = new List<TypeParameterType>();
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Get the standard type variable (eg, !0, !1, or !!0, !!1).
            //
            //      iv is the index.
            //      pbsm is the containing symbol manager
            //      fMeth designates whether this is a method type var or class type var
            //
            // The standard class type variables are useful during emit, but not for type 
            // comparison when binding. The standard method type variables are useful during
            // binding for signature comparison.

            public TypeParameterType GetTypeVarSym(int iv, TypeManager pTypeManager, bool fMeth)
            {
                Debug.Assert(iv >= 0);

                TypeParameterType tpt;
                if (iv >= this.prgptvs.Count)
                {
                    TypeParameterSymbol pTypeParameter = new TypeParameterSymbol();
                    pTypeParameter.SetIsMethodTypeParameter(fMeth);
                    pTypeParameter.SetIndexInOwnParameters(iv);
                    pTypeParameter.SetIndexInTotalParameters(iv);
                    pTypeParameter.SetAccess(ACCESS.ACC_PRIVATE);
                    tpt = pTypeManager.GetTypeParameter(pTypeParameter);
                    this.prgptvs.Add(tpt);
                }
                else
                {
                    tpt = this.prgptvs[iv];
                }
                Debug.Assert(tpt != null);
                return tpt;
            }
        }

        public ArrayType GetArray(CType elementType, int args, bool isSZArray)
        {
            Debug.Assert(args > 0 && args < 32767);
            Debug.Assert(args == 1 || !isSZArray);

            int rankNum = isSZArray ? 0 : args;

            // See if we already have an array type of this element type and rank.
            ArrayType pArray = _typeTable.LookupArray(elementType, rankNum);
            if (pArray == null)
            {
                // No existing array symbol. Create a new one.
                pArray = new ArrayType(elementType, args, isSZArray);
                _typeTable.InsertArray(elementType, rankNum, pArray);
            }

            Debug.Assert(pArray.Rank == args);
            Debug.Assert(pArray.ElementType == elementType);

            return pArray;
        }

        public AggregateType GetAggregate(AggregateSymbol agg, AggregateType atsOuter, TypeArray typeArgs)
        {
            Debug.Assert(agg.GetTypeManager() == this);
            Debug.Assert(atsOuter == null || atsOuter.OwningAggregate == agg.Parent, "");

            if (typeArgs == null)
            {
                typeArgs = BSYMMGR.EmptyTypeArray();
            }

            Debug.Assert(agg.GetTypeVars().Count == typeArgs.Count);
            AggregateType pAggregate = _typeTable.LookupAggregate(agg, atsOuter, typeArgs);
            if (pAggregate == null)
            {
                pAggregate = new AggregateType(agg, typeArgs, atsOuter);

                Debug.Assert(!pAggregate.ConstraintError.HasValue);

                _typeTable.InsertAggregate(agg, atsOuter, typeArgs, pAggregate);
            }

            Debug.Assert(pAggregate.OwningAggregate == agg);
            Debug.Assert(pAggregate.TypeArgsThis != null && pAggregate.TypeArgsAll != null);
            Debug.Assert(pAggregate.TypeArgsThis == typeArgs);

            return pAggregate;
        }

        public AggregateType GetAggregate(AggregateSymbol agg, TypeArray typeArgsAll)
        {
            Debug.Assert(typeArgsAll != null && typeArgsAll.Count == agg.GetTypeVarsAll().Count);

            if (typeArgsAll.Count == 0)
                return agg.getThisType();

            AggregateSymbol aggOuter = agg.GetOuterAgg();

            if (aggOuter == null)
                return GetAggregate(agg, null, typeArgsAll);

            int cvarOuter = aggOuter.GetTypeVarsAll().Count;
            Debug.Assert(cvarOuter <= typeArgsAll.Count);

            TypeArray typeArgsOuter = _BSymmgr.AllocParams(cvarOuter, typeArgsAll, 0);
            TypeArray typeArgsInner = _BSymmgr.AllocParams(agg.GetTypeVars().Count, typeArgsAll, cvarOuter);
            AggregateType atsOuter = GetAggregate(aggOuter, typeArgsOuter);

            return GetAggregate(agg, atsOuter, typeArgsInner);
        }

        public PointerType GetPointer(CType baseType)
        {
            PointerType pPointer = _typeTable.LookupPointer(baseType);
            if (pPointer == null)
            {
                // No existing type. Create a new one.
                pPointer = new PointerType(baseType);
                _typeTable.InsertPointer(baseType, pPointer);
            }

            Debug.Assert(pPointer.ReferentType == baseType);

            return pPointer;
        }

        public NullableType GetNullable(CType pUnderlyingType)
        {
            Debug.Assert(!(pUnderlyingType is NullableType), "Attempt to make nullable of nullable");

            NullableType pNullableType = _typeTable.LookupNullable(pUnderlyingType);
            if (pNullableType == null)
            {
                pNullableType = new NullableType(pUnderlyingType, _BSymmgr, this);
                _typeTable.InsertNullable(pUnderlyingType, pNullableType);
            }

            return pNullableType;
        }

        public ParameterModifierType GetParameterModifier(CType paramType, bool isOut)
        {
            ParameterModifierType pParamModifier = _typeTable.LookupParameterModifier(paramType, isOut);
            if (pParamModifier == null)
            {
                // No existing parammod symbol. Create a new one.
                pParamModifier = new ParameterModifierType(paramType, isOut);
                _typeTable.InsertParameterModifier(paramType, isOut, pParamModifier);
            }

            Debug.Assert(pParamModifier.ParameterType == paramType);

            return pParamModifier;
        }

        public AggregateSymbol GetNullable() => GetPredefAgg(PredefinedType.PT_G_OPTIONAL);

        private CType SubstType(CType typeSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth, bool denormMeth)
        {
            Debug.Assert(typeSrc != null);
            SubstContext ctx = new SubstContext(typeArgsCls, typeArgsMeth, denormMeth);
            return ctx.IsNop ? typeSrc : SubstTypeCore(typeSrc, ctx);
        }

        public AggregateType SubstType(AggregateType typeSrc, TypeArray typeArgsCls)
        {
            Debug.Assert(typeSrc != null);

            SubstContext ctx = new SubstContext(typeArgsCls, null, false);
            return ctx.IsNop ? typeSrc : SubstTypeCore(typeSrc, ctx);
        }

        private CType SubstType(CType typeSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth) =>
            SubstType(typeSrc, typeArgsCls, typeArgsMeth, false);

        public TypeArray SubstTypeArray(TypeArray taSrc, SubstContext ctx)
        {
            if (taSrc != null && taSrc.Count != 0 && ctx != null && !ctx.IsNop)
            {
                CType[] srcs = taSrc.Items;
                for (int i = 0; i < srcs.Length; i++)
                {
                    CType src = srcs[i];
                    CType dst = SubstTypeCore(src, ctx);
                    if (src != dst)
                    {
                        CType[] dsts = new CType[srcs.Length];
                        Array.Copy(srcs, dsts, i);
                        dsts[i] = dst;
                        while (++i < srcs.Length)
                        {
                            dsts[i] = SubstTypeCore(srcs[i], ctx);
                        }

                        return _BSymmgr.AllocParams(dsts);
                    }
                }
            }

            return taSrc;
        }

        public TypeArray SubstTypeArray(TypeArray taSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth)
            => taSrc == null || taSrc.Count == 0
            ? taSrc
            : SubstTypeArray(taSrc, new SubstContext(typeArgsCls, typeArgsMeth, false));

        public TypeArray SubstTypeArray(TypeArray taSrc, TypeArray typeArgsCls) => SubstTypeArray(taSrc, typeArgsCls, null);

        private AggregateType SubstTypeCore(AggregateType type, SubstContext ctx)
        {
            TypeArray args = type.TypeArgsAll;
            if (args.Count > 0)
            {
                TypeArray typeArgs = SubstTypeArray(args, ctx);
                if (args != typeArgs)
                {
                    return GetAggregate(type.OwningAggregate, typeArgs);
                }
            }

            return type;
        }

        private CType SubstTypeCore(CType type, SubstContext pctx)
        {
            CType typeSrc;
            CType typeDst;

            switch (type.TypeKind)
            {
                default:
                    Debug.Assert(false);
                    return type;

                case TypeKind.TK_NullType:
                case TypeKind.TK_VoidType:
                case TypeKind.TK_MethodGroupType:
                case TypeKind.TK_ArgumentListType:
                    return type;

                case TypeKind.TK_ParameterModifierType:
                    ParameterModifierType mod = (ParameterModifierType)type;
                    typeDst = SubstTypeCore(typeSrc = mod.ParameterType, pctx);
                    return (typeDst == typeSrc) ? type : GetParameterModifier(typeDst, mod.IsOut);

                case TypeKind.TK_ArrayType:
                    var arr = (ArrayType)type;
                    typeDst = SubstTypeCore(typeSrc = arr.ElementType, pctx);
                    return (typeDst == typeSrc) ? type : GetArray(typeDst, arr.Rank, arr.IsSZArray);

                case TypeKind.TK_PointerType:
                    typeDst = SubstTypeCore(typeSrc = ((PointerType)type).ReferentType, pctx);
                    return (typeDst == typeSrc) ? type : GetPointer(typeDst);

                case TypeKind.TK_NullableType:
                    typeDst = SubstTypeCore(typeSrc = ((NullableType)type).UnderlyingType, pctx);
                    return (typeDst == typeSrc) ? type : GetNullable(typeDst);

                case TypeKind.TK_AggregateType:
                    return SubstTypeCore((AggregateType)type, pctx);

                case TypeKind.TK_TypeParameterType:
                    {
                        TypeParameterSymbol tvs = ((TypeParameterType)type).Symbol;
                        int index = tvs.GetIndexInTotalParameters();
                        if (tvs.IsMethodTypeParameter())
                        {
                            if (pctx.DenormMeth && tvs.parent != null)
                            {
                                return type;
                            }

                            Debug.Assert(tvs.GetIndexInOwnParameters() == tvs.GetIndexInTotalParameters());
                            if (index < pctx.MethodTypes.Length)
                            {
                                Debug.Assert(pctx.MethodTypes != null);
                                return pctx.MethodTypes[index];
                            }

                            return type;
                        }

                        return index < pctx.ClassTypes.Length ? pctx.ClassTypes[index] : type;
                    }
            }
        }

        public bool SubstEqualTypes(CType typeDst, CType typeSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth, bool denormMeth)
        {
            if (typeDst.Equals(typeSrc))
            {
                Debug.Assert(typeDst.Equals(SubstType(typeSrc, typeArgsCls, typeArgsMeth, denormMeth)));
                return true;
            }

            SubstContext ctx = new SubstContext(typeArgsCls, typeArgsMeth, denormMeth);

            return !ctx.IsNop && SubstEqualTypesCore(typeDst, typeSrc, ctx);
        }

        public bool SubstEqualTypeArrays(TypeArray taDst, TypeArray taSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth)
        {
            // Handle the simple common cases first.
            if (taDst == taSrc || (taDst != null && taDst.Equals(taSrc)))
            {
                // The following assertion is not always true and indicates a problem where
                // the signature of override method does not match the one inherited from
                // the base class. The method match we have found does not take the type 
                // arguments of the base class into account. So actually we are not overriding
                // the method that we "intend" to. 
                // Debug.Assert(taDst == SubstTypeArray(taSrc, typeArgsCls, typeArgsMeth, grfst));
                return true;
            }
            if (taDst.Count != taSrc.Count)
                return false;
            if (taDst.Count == 0)
                return true;

            var ctx = new SubstContext(typeArgsCls, typeArgsMeth, true);

            if (ctx.IsNop)
            {
                return false;
            }

            for (int i = 0; i < taDst.Count; i++)
            {
                if (!SubstEqualTypesCore(taDst[i], taSrc[i], ctx))
                    return false;
            }

            return true;
        }

        private bool SubstEqualTypesCore(CType typeDst, CType typeSrc, SubstContext pctx)
        {
        LRecurse:  // Label used for "tail" recursion.

            if (typeDst == typeSrc || typeDst.Equals(typeSrc))
            {
                return true;
            }

            switch (typeSrc.TypeKind)
            {
                default:
                    Debug.Assert(false, "Bad Symbol kind in SubstEqualTypesCore");
                    return false;

                case TypeKind.TK_NullType:
                case TypeKind.TK_VoidType:
                    // There should only be a single instance of these.
                    Debug.Assert(typeDst.TypeKind != typeSrc.TypeKind);
                    return false;

                case TypeKind.TK_ArrayType:
                    ArrayType arrSrc = (ArrayType)typeSrc;
                    if (!(typeDst is ArrayType arrDst) || arrDst.Rank != arrSrc.Rank || arrDst.IsSZArray != arrSrc.IsSZArray)
                        return false;
                    goto LCheckBases;

                case TypeKind.TK_ParameterModifierType:
                    if (!(typeDst is ParameterModifierType modDest) || modDest.IsOut != ((ParameterModifierType)typeSrc).IsOut)
                    {
                        return false;
                    }

                    goto LCheckBases;

                case TypeKind.TK_PointerType:
                case TypeKind.TK_NullableType:
                    if (typeDst.TypeKind != typeSrc.TypeKind)
                        return false;
                    LCheckBases:
                    typeSrc = typeSrc.BaseOrParameterOrElementType;
                    typeDst = typeDst.BaseOrParameterOrElementType;
                    goto LRecurse;

                case TypeKind.TK_AggregateType:
                    if (!(typeDst is AggregateType atsDst))
                        return false;
                    { // BLOCK
                        AggregateType atsSrc = (AggregateType)typeSrc;

                        if (atsSrc.OwningAggregate != atsDst.OwningAggregate)
                            return false;

                        Debug.Assert(atsSrc.TypeArgsAll.Count == atsDst.TypeArgsAll.Count);

                        // All the args must unify.
                        for (int i = 0; i < atsSrc.TypeArgsAll.Count; i++)
                        {
                            if (!SubstEqualTypesCore(atsDst.TypeArgsAll[i], atsSrc.TypeArgsAll[i], pctx))
                                return false;
                        }
                    }
                    return true;

                case TypeKind.TK_TypeParameterType:
                    { // BLOCK
                        TypeParameterSymbol tvs = ((TypeParameterType)typeSrc).Symbol;
                        int index = tvs.GetIndexInTotalParameters();

                        if (tvs.IsMethodTypeParameter())
                        {
                            if (pctx.DenormMeth && tvs.parent != null)
                            {
                                // typeDst == typeSrc was handled above.
                                Debug.Assert(typeDst != typeSrc);
                                return false;
                            }

                            Debug.Assert(tvs.GetIndexInOwnParameters() == index);
                            Debug.Assert(tvs.GetIndexInTotalParameters() < pctx.MethodTypes.Length);
                            if (index < pctx.MethodTypes.Length)
                            {
                                return typeDst == pctx.MethodTypes[index];
                            }
                        }
                        else
                        {
                            Debug.Assert(index < pctx.ClassTypes.Length);
                            if (index < pctx.ClassTypes.Length)
                            {
                                return typeDst == pctx.ClassTypes[index];
                            }
                        }
                    }
                    return false;
            }
        }

        public static bool TypeContainsType(CType type, CType typeFind)
        {
        LRecurse:  // Label used for "tail" recursion.

            if (type == typeFind || type.Equals(typeFind))
                return true;

            switch (type.TypeKind)
            {
                default:
                    Debug.Assert(false, "Bad Symbol kind in TypeContainsType");
                    return false;

                case TypeKind.TK_NullType:
                case TypeKind.TK_VoidType:
                    // There should only be a single instance of these.
                    Debug.Assert(typeFind.TypeKind != type.TypeKind);
                    return false;

                case TypeKind.TK_ArrayType:
                case TypeKind.TK_NullableType:
                case TypeKind.TK_ParameterModifierType:
                case TypeKind.TK_PointerType:
                    type = type.BaseOrParameterOrElementType;
                    goto LRecurse;

                case TypeKind.TK_AggregateType:
                    { // BLOCK
                        AggregateType ats = (AggregateType)type;

                        for (int i = 0; i < ats.TypeArgsAll.Count; i++)
                        {
                            if (TypeContainsType(ats.TypeArgsAll[i], typeFind))
                                return true;
                        }
                    }
                    return false;

                case TypeKind.TK_TypeParameterType:
                    return false;
            }
        }

        public static bool TypeContainsTyVars(CType type, TypeArray typeVars)
        {
        LRecurse:  // Label used for "tail" recursion.
            switch (type.TypeKind)
            {
                default:
                    Debug.Assert(false, "Bad Symbol kind in TypeContainsTyVars");
                    return false;

                case TypeKind.TK_NullType:
                case TypeKind.TK_VoidType:
                case TypeKind.TK_MethodGroupType:
                    return false;

                case TypeKind.TK_ArrayType:
                case TypeKind.TK_NullableType:
                case TypeKind.TK_ParameterModifierType:
                case TypeKind.TK_PointerType:
                    type = type.BaseOrParameterOrElementType;
                    goto LRecurse;

                case TypeKind.TK_AggregateType:
                    { // BLOCK
                        AggregateType ats = (AggregateType)type;

                        for (int i = 0; i < ats.TypeArgsAll.Count; i++)
                        {
                            if (TypeContainsTyVars(ats.TypeArgsAll[i], typeVars))
                            {
                                return true;
                            }
                        }
                    }
                    return false;

                case TypeKind.TK_TypeParameterType:
                    if (typeVars != null && typeVars.Count > 0)
                    {
                        int ivar = ((TypeParameterType)type).IndexInTotalParameters;
                        return ivar < typeVars.Count && type == typeVars[ivar];
                    }
                    return true;
            }
        }

        public static bool ParametersContainTyVar(TypeArray @params, TypeParameterType typeFind)
        {
            Debug.Assert(@params != null);
            Debug.Assert(typeFind != null);
            for (int p = 0; p < @params.Count; p++)
            {
                CType sym = @params[p];
                if (TypeContainsType(sym, typeFind))
                {
                    return true;
                }
            }
            return false;
        }

        public AggregateSymbol GetPredefAgg(PredefinedType pt) => _predefTypes.GetPredefinedAggregate(pt);

        public TypeArray ConcatenateTypeArrays(TypeArray pTypeArray1, TypeArray pTypeArray2)
        {
            return _BSymmgr.ConcatParams(pTypeArray1, pTypeArray2);
        }

        public TypeArray GetStdMethTyVarArray(int cTyVars)
        {
            TypeParameterType[] prgvar = new TypeParameterType[cTyVars];

            for (int ivar = 0; ivar < cTyVars; ivar++)
            {
                prgvar[ivar] = GetStdMethTypeVar(ivar);
            }

            return _BSymmgr.AllocParams(cTyVars, (CType[])prgvar);
        }

        public AggregateType SubstType(AggregateType typeSrc, SubstContext ctx) =>
            ctx == null || ctx.IsNop ? typeSrc : SubstTypeCore(typeSrc, ctx);

        public CType SubstType(CType typeSrc, SubstContext pctx) =>
            pctx == null || pctx.IsNop ? typeSrc : SubstTypeCore(typeSrc, pctx);

        public CType SubstType(CType typeSrc, AggregateType atsCls)
        {
            return SubstType(typeSrc, atsCls, (TypeArray)null);
        }

        public CType SubstType(CType typeSrc, AggregateType atsCls, TypeArray typeArgsMeth)
        {
            return SubstType(typeSrc, atsCls?.TypeArgsAll, typeArgsMeth);
        }

        public CType SubstType(CType typeSrc, CType typeCls, TypeArray typeArgsMeth)
        {
            return SubstType(typeSrc, (typeCls as AggregateType)?.TypeArgsAll, typeArgsMeth);
        }

        public TypeArray SubstTypeArray(TypeArray taSrc, AggregateType atsCls, TypeArray typeArgsMeth)
        {
            return SubstTypeArray(taSrc, atsCls?.TypeArgsAll, typeArgsMeth);
        }

        public TypeArray SubstTypeArray(TypeArray taSrc, AggregateType atsCls)
        {
            return this.SubstTypeArray(taSrc, atsCls, (TypeArray)null);
        }

        private bool SubstEqualTypes(CType typeDst, CType typeSrc, CType typeCls, TypeArray typeArgsMeth) =>
            SubstEqualTypes(typeDst, typeSrc, (typeCls as AggregateType)?.TypeArgsAll, typeArgsMeth, false);

        public bool SubstEqualTypes(CType typeDst, CType typeSrc, CType typeCls)
        {
            return SubstEqualTypes(typeDst, typeSrc, typeCls, (TypeArray)null);
        }

        public TypeParameterType GetStdMethTypeVar(int iv)
        {
            return _stvcMethod.GetTypeVarSym(iv, this, true);
        }

        // These are singletons for each.
        public TypeParameterType GetTypeParameter(TypeParameterSymbol pSymbol)
        {
            Debug.Assert(pSymbol.GetTypeParameterType() == null); // Should have been checked first before creating
            return new TypeParameterType(pSymbol);
        }

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // RUNTIME BINDER ONLY CHANGE
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        internal bool GetBestAccessibleType(CSemanticChecker semanticChecker, BindingContext bindingContext, CType typeSrc, out CType typeDst)
        {
            // This method implements the "best accessible type" algorithm for determining the type
            // of untyped arguments in the runtime binder. It is also used in method type inference
            // to fix type arguments to types that are accessible.

            // The new type is returned in an out parameter. The result will be true (and the out param
            // non-null) only when the algorithm could find a suitable accessible type.

            Debug.Assert(semanticChecker != null);
            Debug.Assert(bindingContext != null);
            Debug.Assert(typeSrc != null);

            typeDst = null;

            if (semanticChecker.CheckTypeAccess(typeSrc, bindingContext.ContextForMemberLookup))
            {
                // If we already have an accessible type, then use it. This is the terminal point of the recursion.
                typeDst = typeSrc;
                return true;
            }

            // These guys have no accessibility concerns.
            Debug.Assert(!(typeSrc is VoidType) && !(typeSrc is TypeParameterType));

            if (typeSrc is ParameterModifierType || typeSrc is PointerType)
            {
                // We cannot vary these.
                return false;
            }

            CType intermediateType;
            if (typeSrc is AggregateType aggSrc && (aggSrc.IsInterfaceType || aggSrc.IsDelegateType) && TryVarianceAdjustmentToGetAccessibleType(semanticChecker, bindingContext, aggSrc, out intermediateType))
            {
                // If we have an interface or delegate type, then it can potentially be varied by its type arguments
                // to produce an accessible type, and if that's the case, then return that.
                // Example: IEnumerable<PrivateConcreteFoo> --> IEnumerable<PublicAbstractFoo>
                typeDst = intermediateType;

                Debug.Assert(semanticChecker.CheckTypeAccess(typeDst, bindingContext.ContextForMemberLookup));
                return true;
            }

            if (typeSrc is ArrayType arrSrc && TryArrayVarianceAdjustmentToGetAccessibleType(semanticChecker, bindingContext, arrSrc, out intermediateType))
            {
                // Similarly to the interface and delegate case, arrays are covariant in their element type and
                // so we can potentially produce an array type that is accessible.
                // Example: PrivateConcreteFoo[] --> PublicAbstractFoo[]
                typeDst = intermediateType;

                Debug.Assert(semanticChecker.CheckTypeAccess(typeDst, bindingContext.ContextForMemberLookup));
                return true;
            }

            if (typeSrc is NullableType)
            {
                // We have an inaccessible nullable type, which means that the best we can do is System.ValueType.
                typeDst = GetPredefAgg(PredefinedType.PT_VALUE).getThisType();

                Debug.Assert(semanticChecker.CheckTypeAccess(typeDst, bindingContext.ContextForMemberLookup));
                return true;
            }

            if (typeSrc is ArrayType)
            {
                // We have an inaccessible array type for which we could not earlier find a better array type
                // with a covariant conversion, so the best we can do is System.Array.
                typeDst = GetPredefAgg(PredefinedType.PT_ARRAY).getThisType();

                Debug.Assert(semanticChecker.CheckTypeAccess(typeDst, bindingContext.ContextForMemberLookup));
                return true;
            }

            Debug.Assert(typeSrc is AggregateType);

            if (typeSrc is AggregateType aggType)
            {
                // We have an AggregateType, so recurse on its base class.
                AggregateType baseType = aggType.BaseClass;

                if (baseType == null)
                {
                    // This happens with interfaces, for instance. But in that case, the
                    // conversion to object does exist, is an implicit reference conversion,
                    // and so we will use it.
                    baseType = GetPredefAgg(PredefinedType.PT_OBJECT).getThisType();
                }

                return GetBestAccessibleType(semanticChecker, bindingContext, baseType, out typeDst);
            }

            return false;
        }

        private bool TryVarianceAdjustmentToGetAccessibleType(CSemanticChecker semanticChecker, BindingContext bindingContext, AggregateType typeSrc, out CType typeDst)
        {
            Debug.Assert(typeSrc != null);
            Debug.Assert(typeSrc.IsInterfaceType || typeSrc.IsDelegateType);

            typeDst = null;

            AggregateSymbol aggSym = typeSrc.OwningAggregate;
            AggregateType aggOpenType = aggSym.getThisType();

            if (!semanticChecker.CheckTypeAccess(aggOpenType, bindingContext.ContextForMemberLookup))
            {
                // if the aggregate symbol itself is not accessible, then forget it, there is no
                // variance that will help us arrive at an accessible type.
                return false;
            }

            TypeArray typeArgs = typeSrc.TypeArgsThis;
            TypeArray typeParams = aggOpenType.TypeArgsThis;
            CType[] newTypeArgsTemp = new CType[typeArgs.Count];

            for (int i = 0; i < typeArgs.Count; i++)
            {
                if (semanticChecker.CheckTypeAccess(typeArgs[i], bindingContext.ContextForMemberLookup))
                {
                    // we have an accessible argument, this position is not a problem.
                    newTypeArgsTemp[i] = typeArgs[i];
                    continue;
                }

                if (!typeArgs[i].IsReferenceType || !((TypeParameterType)typeParams[i]).Covariant)
                {
                    // This guy is inaccessible, and we are not going to be able to vary him, so we need to fail.
                    return false;
                }

                CType intermediateTypeArg;
                if (GetBestAccessibleType(semanticChecker, bindingContext, typeArgs[i], out intermediateTypeArg))
                {
                    // now we either have a value type (which must be accessible due to the above
                    // check, OR we have an inaccessible type (which must be a ref type). In either
                    // case, the recursion worked out and we are OK to vary this argument.
                    newTypeArgsTemp[i] = intermediateTypeArg;
                    continue;
                }
                else
                {
                    Debug.Assert(false, "GetBestAccessibleType unexpectedly failed on a type that was used as a type parameter");
                    return false;
                }
            }

            TypeArray newTypeArgs = semanticChecker.getBSymmgr().AllocParams(typeArgs.Count, newTypeArgsTemp);
            CType intermediateType = this.GetAggregate(aggSym, typeSrc.OuterType, newTypeArgs);

            // All type arguments were varied successfully, which means now we must be accessible. But we could
            // have violated constraints. Let's check that out.

            if (!TypeBind.CheckConstraints(semanticChecker, null/*ErrorHandling*/, intermediateType, CheckConstraintsFlags.NoErrors))
            {
                return false;
            }

            typeDst = intermediateType;
            Debug.Assert(semanticChecker.CheckTypeAccess(typeDst, bindingContext.ContextForMemberLookup));
            return true;
        }

        private bool TryArrayVarianceAdjustmentToGetAccessibleType(CSemanticChecker semanticChecker, BindingContext bindingContext, ArrayType typeSrc, out CType typeDst)
        {
            Debug.Assert(typeSrc != null);

            typeDst = null;

            // We are here because we have an array type with an inaccessible element type. If possible,
            // we should create a new array type that has an accessible element type for which a
            // conversion exists.

            CType elementType = typeSrc.ElementType;
            if (!elementType.IsReferenceType)
            {
                // Covariant array conversions exist for reference types only.
                return false;
            }

            if (GetBestAccessibleType(semanticChecker, bindingContext, elementType, out CType intermediateType))
            {
                typeDst = GetArray(intermediateType, typeSrc.Rank, typeSrc.IsSZArray);

                Debug.Assert(semanticChecker.CheckTypeAccess(typeDst, bindingContext.ContextForMemberLookup));
                return true;
            }

            return false;
        }

        public AggregateType ObjectAggregateType => (AggregateType)_symbolTable.GetCTypeFromType(typeof(object));

        private readonly Dictionary<Tuple<Assembly, Assembly>, bool> _internalsVisibleToCalculated
            = new Dictionary<Tuple<Assembly, Assembly>, bool>();

        internal bool InternalsVisibleTo(Assembly assemblyThatDefinesAttribute, Assembly assemblyToCheck)
        {
            bool result;

            var key = Tuple.Create(assemblyThatDefinesAttribute, assemblyToCheck);
            if (!_internalsVisibleToCalculated.TryGetValue(key, out result))
            {
                AssemblyName assyName;

                // Assembly.GetName() requires FileIOPermission to FileIOPermissionAccess.PathDiscovery.
                // If we don't have that (we're in low trust), then we are going to effectively turn off
                // InternalsVisibleTo. The alternative is to crash when this happens. 

                try
                {
                    assyName = assemblyToCheck.GetName();
                }
                catch (System.Security.SecurityException)
                {
                    result = false;
                    goto SetMemo;
                }

                result = assemblyThatDefinesAttribute.GetCustomAttributes()
                    .OfType<InternalsVisibleToAttribute>()
                    .Select(ivta => new AssemblyName(ivta.AssemblyName))
                    .Any(an => AssemblyName.ReferenceMatchesDefinition(an, assyName));

            SetMemo:
                _internalsVisibleToCalculated[key] = result;
            }

            return result;
        }
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // END RUNTIME BINDER ONLY CHANGE
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    }
}
