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

        private readonly TypeFactory _typeFactory;
        private readonly TypeTable _typeTable;
        private SymbolTable _symbolTable;

        // Special types
        private readonly VoidType _voidType;
        private readonly NullType _nullType;
        private readonly MethodGroupType _typeMethGrp;
        private readonly ArgumentListType _argListType;

        private readonly StdTypeVarColl _stvcMethod;
        private readonly StdTypeVarColl _stvcClass;

        public TypeManager(BSYMMGR bsymmgr, PredefinedTypes predefTypes)
        {
            _typeFactory = new TypeFactory();
            _typeTable = new TypeTable();

            // special types with their own symbol kind.
            _voidType = _typeFactory.CreateVoid();
            _nullType = _typeFactory.CreateNull();
            _typeMethGrp = _typeFactory.CreateMethodGroup();
            _argListType = _typeFactory.CreateArgList();

            _stvcMethod = new StdTypeVarColl();
            _stvcClass = new StdTypeVarColl();
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
            Name name;

            Debug.Assert(args > 0 && args < 32767);
            Debug.Assert(args == 1 || !isSZArray);

            switch (args)
            {
                case 1:
                    if (isSZArray)
                    {
                        goto case 2;
                    }
                    else
                    {
                        goto default;
                    }
                case 2:
                    name = NameManager.GetPredefinedName(PredefinedName.PN_ARRAY0 + args);
                    break;
                default:
                    name = NameManager.Add("[X" + args + 1);
                    break;
            }

            // See if we already have an array type of this element type and rank.
            ArrayType pArray = _typeTable.LookupArray(name, elementType);
            if (pArray == null)
            {
                // No existing array symbol. Create a new one.
                pArray = _typeFactory.CreateArray(name, elementType, args, isSZArray);
                _typeTable.InsertArray(name, elementType, pArray);
            }

            Debug.Assert(pArray.rank == args);
            Debug.Assert(pArray.GetElementType() == elementType);

            return pArray;
        }

        public AggregateType GetAggregate(AggregateSymbol agg, AggregateType atsOuter, TypeArray typeArgs)
        {
            Debug.Assert(agg.GetTypeManager() == this);
            Debug.Assert(atsOuter == null || atsOuter.getAggregate() == agg.Parent, "");

            if (typeArgs == null)
            {
                typeArgs = BSYMMGR.EmptyTypeArray();
            }

            Debug.Assert(agg.GetTypeVars().Count == typeArgs.Count);
            AggregateType pAggregate = _typeTable.LookupAggregate(agg, atsOuter, typeArgs);
            if (pAggregate == null)
            {
                pAggregate = _typeFactory.CreateAggregateType(
                          agg,
                          typeArgs,
                          atsOuter
                      );

                Debug.Assert(!pAggregate.fConstraintsChecked && !pAggregate.fConstraintError);

                _typeTable.InsertAggregate(agg, atsOuter, typeArgs, pAggregate);
            }

            Debug.Assert(pAggregate.getAggregate() == agg);
            Debug.Assert(pAggregate.GetTypeArgsThis() != null && pAggregate.GetTypeArgsAll() != null);
            Debug.Assert(pAggregate.GetTypeArgsThis() == typeArgs);

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
                Name namePtr = NameManager.GetPredefinedName(PredefinedName.PN_PTR);

                pPointer = _typeFactory.CreatePointer(namePtr, baseType);
                _typeTable.InsertPointer(baseType, pPointer);
            }

            Debug.Assert(pPointer.GetReferentType() == baseType);

            return pPointer;
        }

        public NullableType GetNullable(CType pUnderlyingType)
        {
            if (pUnderlyingType is NullableType nt)
            {
                Debug.Fail("Attempt to make nullable of nullable");
                return nt;
            }

            NullableType pNullableType = _typeTable.LookupNullable(pUnderlyingType);
            if (pNullableType == null)
            {
                Name pName = NameManager.GetPredefinedName(PredefinedName.PN_NUB);

                pNullableType = _typeFactory.CreateNullable(pName, pUnderlyingType, _BSymmgr, this);
                _typeTable.InsertNullable(pUnderlyingType, pNullableType);
            }

            return pNullableType;
        }

        public NullableType GetNubFromNullable(AggregateType ats)
        {
            Debug.Assert(ats.isPredefType(PredefinedType.PT_G_OPTIONAL));
            return GetNullable(ats.GetTypeArgsAll()[0]);
        }

        public ParameterModifierType GetParameterModifier(CType paramType, bool isOut)
        {
            Name name = NameManager.GetPredefinedName(isOut ? PredefinedName.PN_OUTPARAM : PredefinedName.PN_REFPARAM);
            ParameterModifierType pParamModifier = _typeTable.LookupParameterModifier(name, paramType);

            if (pParamModifier == null)
            {
                // No existing parammod symbol. Create a new one.
                pParamModifier = _typeFactory.CreateParameterModifier(name, paramType);
                pParamModifier.isOut = isOut;
                _typeTable.InsertParameterModifier(name, paramType, pParamModifier);
            }

            Debug.Assert(pParamModifier.GetParameterType() == paramType);

            return pParamModifier;
        }

        public VoidType GetVoid()
        {
            return _voidType;
        }

        public NullType GetNullType()
        {
            return _nullType;
        }

        public MethodGroupType GetMethGrpType()
        {
            return _typeMethGrp;
        }

        public ArgumentListType GetArgListType()
        {
            return _argListType;
        }

        public AggregateSymbol GetNullable() => GetPredefAgg(PredefinedType.PT_G_OPTIONAL);

        private CType SubstType(CType typeSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth, SubstTypeFlags grfst)
        {
            if (typeSrc == null)
                return null;

            var ctx = new SubstContext(typeArgsCls, typeArgsMeth, grfst);
            return ctx.FNop() ? typeSrc : SubstTypeCore(typeSrc, ctx);
        }

        public AggregateType SubstType(AggregateType typeSrc, TypeArray typeArgsCls)
        {
            if (typeSrc != null)
            {
                SubstContext ctx = new SubstContext(typeArgsCls, null, SubstTypeFlags.NormNone);
                if (!ctx.FNop())
                {
                    return SubstTypeCore(typeSrc, ctx);
                }
            }

            return typeSrc;
        }

        private CType SubstType(CType typeSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth)
        {
            return SubstType(typeSrc, typeArgsCls, typeArgsMeth, SubstTypeFlags.NormNone);
        }

        public TypeArray SubstTypeArray(TypeArray taSrc, SubstContext ctx)
        {
            if (taSrc != null && taSrc.Count != 0 && ctx != null && !ctx.FNop())
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
            : SubstTypeArray(taSrc, new SubstContext(typeArgsCls, typeArgsMeth, SubstTypeFlags.NormNone));

        public TypeArray SubstTypeArray(TypeArray taSrc, TypeArray typeArgsCls) => SubstTypeArray(taSrc, typeArgsCls, null);

        private AggregateType SubstTypeCore(AggregateType type, SubstContext ctx)
        {
            TypeArray args = type.GetTypeArgsAll();
            if (args.Count > 0)
            {
                TypeArray typeArgs = SubstTypeArray(args, ctx);
                if (args != typeArgs)
                {
                    return GetAggregate(type.getAggregate(), typeArgs);
                }
            }

            return type;
        }

        private CType SubstTypeCore(CType type, SubstContext pctx)
        {
            CType typeSrc;
            CType typeDst;

            switch (type.GetTypeKind())
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
                    typeDst = SubstTypeCore(typeSrc = mod.GetParameterType(), pctx);
                    return (typeDst == typeSrc) ? type : GetParameterModifier(typeDst, mod.isOut);

                case TypeKind.TK_ArrayType:
                    var arr = (ArrayType)type;
                    typeDst = SubstTypeCore(typeSrc = arr.GetElementType(), pctx);
                    return (typeDst == typeSrc) ? type : GetArray(typeDst, arr.rank, arr.IsSZArray);

                case TypeKind.TK_PointerType:
                    typeDst = SubstTypeCore(typeSrc = ((PointerType)type).GetReferentType(), pctx);
                    return (typeDst == typeSrc) ? type : GetPointer(typeDst);

                case TypeKind.TK_NullableType:
                    typeDst = SubstTypeCore(typeSrc = ((NullableType)type).GetUnderlyingType(), pctx);
                    return (typeDst == typeSrc) ? type : GetNullable(typeDst);

                case TypeKind.TK_AggregateType:
                    return SubstTypeCore((AggregateType)type, pctx);

                case TypeKind.TK_TypeParameterType:
                    {
                        TypeParameterSymbol tvs = ((TypeParameterType)type).GetTypeParameterSymbol();
                        int index = tvs.GetIndexInTotalParameters();
                        if (tvs.IsMethodTypeParameter())
                        {
                            if ((pctx.grfst & SubstTypeFlags.DenormMeth) != 0 && tvs.parent != null)
                                return type;
                            Debug.Assert(tvs.GetIndexInOwnParameters() == tvs.GetIndexInTotalParameters());
                            if (index < pctx.ctypeMeth)
                            {
                                Debug.Assert(pctx.prgtypeMeth != null);
                                return pctx.prgtypeMeth[index];
                            }
                            else
                            {
                                return ((pctx.grfst & SubstTypeFlags.NormMeth) != 0 ? GetStdMethTypeVar(index) : type);
                            }
                        }
                        if ((pctx.grfst & SubstTypeFlags.DenormClass) != 0 && tvs.parent != null)
                            return type;
                        return index < pctx.ctypeCls ? pctx.prgtypeCls[index] :
                               ((pctx.grfst & SubstTypeFlags.NormClass) != 0 ? GetStdClsTypeVar(index) : type);
                    }
            }
        }

        public bool SubstEqualTypes(CType typeDst, CType typeSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth, SubstTypeFlags grfst)
        {
            if (typeDst.Equals(typeSrc))
            {
                Debug.Assert(typeDst.Equals(SubstType(typeSrc, typeArgsCls, typeArgsMeth, grfst)));
                return true;
            }

            var ctx = new SubstContext(typeArgsCls, typeArgsMeth, grfst);

            return !ctx.FNop() && SubstEqualTypesCore(typeDst, typeSrc, ctx);
        }

        public bool SubstEqualTypeArrays(TypeArray taDst, TypeArray taSrc, TypeArray typeArgsCls, TypeArray typeArgsMeth, SubstTypeFlags grfst)
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

            var ctx = new SubstContext(typeArgsCls, typeArgsMeth, grfst);

            if (ctx.FNop())
                return false;

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

            switch (typeSrc.GetTypeKind())
            {
                default:
                    Debug.Assert(false, "Bad Symbol kind in SubstEqualTypesCore");
                    return false;

                case TypeKind.TK_NullType:
                case TypeKind.TK_VoidType:
                    // There should only be a single instance of these.
                    Debug.Assert(typeDst.GetTypeKind() != typeSrc.GetTypeKind());
                    return false;

                case TypeKind.TK_ArrayType:
                    ArrayType arrSrc = (ArrayType)typeSrc;
                    if (!(typeDst is ArrayType arrDst) || arrDst.rank != arrSrc.rank || arrDst.IsSZArray != arrSrc.IsSZArray)
                        return false;
                    goto LCheckBases;

                case TypeKind.TK_ParameterModifierType:
                    if (!(typeDst is ParameterModifierType modDest) ||
                        ((pctx.grfst & SubstTypeFlags.NoRefOutDifference) == 0 &&
                         modDest.isOut != ((ParameterModifierType)typeSrc).isOut))
                        return false;
                    goto LCheckBases;

                case TypeKind.TK_PointerType:
                case TypeKind.TK_NullableType:
                    if (typeDst.GetTypeKind() != typeSrc.GetTypeKind())
                        return false;
                    LCheckBases:
                    typeSrc = typeSrc.GetBaseOrParameterOrElementType();
                    typeDst = typeDst.GetBaseOrParameterOrElementType();
                    goto LRecurse;

                case TypeKind.TK_AggregateType:
                    if (!(typeDst is AggregateType atsDst))
                        return false;
                    { // BLOCK
                        AggregateType atsSrc = (AggregateType)typeSrc;

                        if (atsSrc.getAggregate() != atsDst.getAggregate())
                            return false;

                        Debug.Assert(atsSrc.GetTypeArgsAll().Count == atsDst.GetTypeArgsAll().Count);

                        // All the args must unify.
                        for (int i = 0; i < atsSrc.GetTypeArgsAll().Count; i++)
                        {
                            if (!SubstEqualTypesCore(atsDst.GetTypeArgsAll()[i], atsSrc.GetTypeArgsAll()[i], pctx))
                                return false;
                        }
                    }
                    return true;

                case TypeKind.TK_TypeParameterType:
                    { // BLOCK
                        TypeParameterSymbol tvs = ((TypeParameterType)typeSrc).GetTypeParameterSymbol();
                        int index = tvs.GetIndexInTotalParameters();

                        if (tvs.IsMethodTypeParameter())
                        {
                            if ((pctx.grfst & SubstTypeFlags.DenormMeth) != 0 && tvs.parent != null)
                            {
                                // typeDst == typeSrc was handled above.
                                Debug.Assert(typeDst != typeSrc);
                                return false;
                            }
                            Debug.Assert(tvs.GetIndexInOwnParameters() == tvs.GetIndexInTotalParameters());
                            Debug.Assert(pctx.prgtypeMeth == null || tvs.GetIndexInTotalParameters() < pctx.ctypeMeth);
                            if (index < pctx.ctypeMeth && pctx.prgtypeMeth != null)
                            {
                                return typeDst == pctx.prgtypeMeth[index];
                            }
                            if ((pctx.grfst & SubstTypeFlags.NormMeth) != 0)
                            {
                                return typeDst == GetStdMethTypeVar(index);
                            }
                        }
                        else
                        {
                            if ((pctx.grfst & SubstTypeFlags.DenormClass) != 0 && tvs.parent != null)
                            {
                                // typeDst == typeSrc was handled above.
                                Debug.Assert(typeDst != typeSrc);
                                return false;
                            }
                            Debug.Assert(pctx.prgtypeCls == null || tvs.GetIndexInTotalParameters() < pctx.ctypeCls);
                            if (index < pctx.ctypeCls)
                                return typeDst == pctx.prgtypeCls[index];
                            if ((pctx.grfst & SubstTypeFlags.NormClass) != 0)
                                return typeDst == GetStdClsTypeVar(index);
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

            switch (type.GetTypeKind())
            {
                default:
                    Debug.Assert(false, "Bad Symbol kind in TypeContainsType");
                    return false;

                case TypeKind.TK_NullType:
                case TypeKind.TK_VoidType:
                    // There should only be a single instance of these.
                    Debug.Assert(typeFind.GetTypeKind() != type.GetTypeKind());
                    return false;

                case TypeKind.TK_ArrayType:
                case TypeKind.TK_NullableType:
                case TypeKind.TK_ParameterModifierType:
                case TypeKind.TK_PointerType:
                    type = type.GetBaseOrParameterOrElementType();
                    goto LRecurse;

                case TypeKind.TK_AggregateType:
                    { // BLOCK
                        AggregateType ats = (AggregateType)type;

                        for (int i = 0; i < ats.GetTypeArgsAll().Count; i++)
                        {
                            if (TypeContainsType(ats.GetTypeArgsAll()[i], typeFind))
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
            switch (type.GetTypeKind())
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
                    type = type.GetBaseOrParameterOrElementType();
                    goto LRecurse;

                case TypeKind.TK_AggregateType:
                    { // BLOCK
                        AggregateType ats = (AggregateType)type;

                        for (int i = 0; i < ats.GetTypeArgsAll().Count; i++)
                        {
                            if (TypeContainsTyVars(ats.GetTypeArgsAll()[i], typeVars))
                            {
                                return true;
                            }
                        }
                    }
                    return false;

                case TypeKind.TK_TypeParameterType:
                    if (typeVars != null && typeVars.Count > 0)
                    {
                        int ivar = ((TypeParameterType)type).GetIndexInTotalParameters();
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
            ctx == null || ctx.FNop() ? typeSrc : SubstTypeCore(typeSrc, ctx);

        public CType SubstType(CType typeSrc, SubstContext pctx)
        {
            return (pctx == null || pctx.FNop()) ? typeSrc : SubstTypeCore(typeSrc, pctx);
        }

        public CType SubstType(CType typeSrc, AggregateType atsCls)
        {
            return SubstType(typeSrc, atsCls, (TypeArray)null);
        }

        public CType SubstType(CType typeSrc, AggregateType atsCls, TypeArray typeArgsMeth)
        {
            return SubstType(typeSrc, atsCls?.GetTypeArgsAll(), typeArgsMeth);
        }

        public CType SubstType(CType typeSrc, CType typeCls, TypeArray typeArgsMeth)
        {
            return SubstType(typeSrc, (typeCls as AggregateType)?.GetTypeArgsAll(), typeArgsMeth);
        }

        public TypeArray SubstTypeArray(TypeArray taSrc, AggregateType atsCls, TypeArray typeArgsMeth)
        {
            return SubstTypeArray(taSrc, atsCls?.GetTypeArgsAll(), typeArgsMeth);
        }

        public TypeArray SubstTypeArray(TypeArray taSrc, AggregateType atsCls)
        {
            return this.SubstTypeArray(taSrc, atsCls, (TypeArray)null);
        }

        private bool SubstEqualTypes(CType typeDst, CType typeSrc, CType typeCls, TypeArray typeArgsMeth)
        {
            return SubstEqualTypes(typeDst, typeSrc, (typeCls as AggregateType)?.GetTypeArgsAll(), typeArgsMeth, SubstTypeFlags.NormNone);
        }

        public bool SubstEqualTypes(CType typeDst, CType typeSrc, CType typeCls)
        {
            return SubstEqualTypes(typeDst, typeSrc, typeCls, (TypeArray)null);
        }

        //public bool SubstEqualTypeArrays(TypeArray taDst, TypeArray taSrc, AggregateType atsCls, TypeArray typeArgsMeth)
        //{
        //    return SubstEqualTypeArrays(taDst, taSrc, atsCls != null ? atsCls.GetTypeArgsAll() : (TypeArray)null, typeArgsMeth, SubstTypeFlags.NormNone);
        //}

        public TypeParameterType GetStdMethTypeVar(int iv)
        {
            return _stvcMethod.GetTypeVarSym(iv, this, true);
        }

        private TypeParameterType GetStdClsTypeVar(int iv)
        {
            return _stvcClass.GetTypeVarSym(iv, this, false);
        }

        // These are singletons for each.
        public TypeParameterType GetTypeParameter(TypeParameterSymbol pSymbol)
        {
            Debug.Assert(pSymbol.GetTypeParameterType() == null); // Should have been checked first before creating
            return _typeFactory.CreateTypeParameter(pSymbol);
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
            if (typeSrc is AggregateType aggSrc && (aggSrc.isInterfaceType() || aggSrc.isDelegateType()) && TryVarianceAdjustmentToGetAccessibleType(semanticChecker, bindingContext, aggSrc, out intermediateType))
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
                AggregateType baseType = aggType.GetBaseClass();

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
            Debug.Assert(typeSrc.isInterfaceType() || typeSrc.isDelegateType());

            typeDst = null;

            AggregateSymbol aggSym = typeSrc.GetOwningAggregate();
            AggregateType aggOpenType = aggSym.getThisType();

            if (!semanticChecker.CheckTypeAccess(aggOpenType, bindingContext.ContextForMemberLookup))
            {
                // if the aggregate symbol itself is not accessible, then forget it, there is no
                // variance that will help us arrive at an accessible type.
                return false;
            }

            TypeArray typeArgs = typeSrc.GetTypeArgsThis();
            TypeArray typeParams = aggOpenType.GetTypeArgsThis();
            CType[] newTypeArgsTemp = new CType[typeArgs.Count];

            for (int i = 0; i < typeArgs.Count; i++)
            {
                if (semanticChecker.CheckTypeAccess(typeArgs[i], bindingContext.ContextForMemberLookup))
                {
                    // we have an accessible argument, this position is not a problem.
                    newTypeArgsTemp[i] = typeArgs[i];
                    continue;
                }

                if (!typeArgs[i].IsRefType() || !((TypeParameterType)typeParams[i]).Covariant)
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
            CType intermediateType = this.GetAggregate(aggSym, typeSrc.outerType, newTypeArgs);

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

            CType elementType = typeSrc.GetElementType();
            if (!elementType.IsRefType())
            {
                // Covariant array conversions exist for reference types only.
                return false;
            }

            CType intermediateType;
            if (GetBestAccessibleType(semanticChecker, bindingContext, elementType, out intermediateType))
            {
                typeDst = this.GetArray(intermediateType, typeSrc.rank, typeSrc.IsSZArray);

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
