// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // This class takes an EXPRMEMGRP and a set of arguments and binds the arguments
    // to the best applicable method in the group.
    // ----------------------------------------------------------------------------

    internal partial class ExpressionBinder
    {
        internal class GroupToArgsBinder
        {
            private enum Result
            {
                Success,
                Failure_SearchForExpanded,
                Failure_NoSearchForExpanded
            }

            private readonly ExpressionBinder _pExprBinder;
            private bool _fCandidatesUnsupported;
            private readonly BindingFlag _fBindFlags;
            private readonly EXPRMEMGRP _pGroup;
            private readonly ArgInfos _pArguments;
            private readonly ArgInfos _pOriginalArguments;
            private readonly bool _bHasNamedArguments;
            private readonly AggregateType _pDelegate;
            private AggregateType _pCurrentType;
            private MethodOrPropertySymbol _pCurrentSym;
            private TypeArray _pCurrentTypeArgs;
            private TypeArray _pCurrentParameters;
            private TypeArray _pBestParameters;
            private int _nArgBest;
            // Keep track of the first 20 or so syms with the wrong arg count.
            private readonly SymWithType[] _swtWrongCount = new SymWithType[20];
            private int _nWrongCount;
            private bool _bIterateToEndOfNsList;               // we have found an appliacable extension method only itereate to 
            // end of current namespaces extension method list
            private bool _bBindingCollectionAddArgs;           // Report parameter modifiers as error 
            private readonly GroupToArgsBinderResult _results;
            private readonly List<CandidateFunctionMember> _methList;
            private readonly MethPropWithInst _mpwiParamTypeConstraints;
            private readonly MethPropWithInst _mpwiBogus;
            private readonly MethPropWithInst _mpwiCantInferInstArg;
            private readonly MethWithType _mwtBadArity;
            private Name _pInvalidSpecifiedName;
            private Name _pNameUsedInPositionalArgument;
            private Name _pDuplicateSpecifiedName;
            // When we find a type with an interface, then we want to mark all other interfaces that it 
            // implements as being hidden. We also want to mark object as being hidden. So stick them
            // all in this list, and then for subsequent types, if they're in this list, then we
            // ignore them.
            private readonly List<CType> _HiddenTypes;
            private bool _bArgumentsChangedForNamedOrOptionalArguments;

            public GroupToArgsBinder(ExpressionBinder exprBinder, BindingFlag bindFlags, EXPRMEMGRP grp, ArgInfos args, ArgInfos originalArgs, bool bHasNamedArguments, AggregateType atsDelegate)
            {
                Debug.Assert(grp != null);
                Debug.Assert(exprBinder != null);
                Debug.Assert(args != null);

                _pExprBinder = exprBinder;
                _fCandidatesUnsupported = false;
                _fBindFlags = bindFlags;
                _pGroup = grp;
                _pArguments = args;
                _pOriginalArguments = originalArgs;
                _bHasNamedArguments = bHasNamedArguments;
                _pDelegate = atsDelegate;
                _pCurrentType = null;
                _pCurrentSym = null;
                _pCurrentTypeArgs = null;
                _pCurrentParameters = null;
                _pBestParameters = null;
                _nArgBest = -1;
                _nWrongCount = 0;
                _bIterateToEndOfNsList = false;
                _bBindingCollectionAddArgs = false;
                _results = new GroupToArgsBinderResult();
                _methList = new List<CandidateFunctionMember>();
                _mpwiParamTypeConstraints = new MethPropWithInst();
                _mpwiBogus = new MethPropWithInst();
                _mpwiCantInferInstArg = new MethPropWithInst();
                _mwtBadArity = new MethWithType();
                _HiddenTypes = new List<CType>();
            }

            // ----------------------------------------------------------------------------
            // This method does the actual binding.
            // ----------------------------------------------------------------------------

            public bool Bind(bool bReportErrors)
            {
                Debug.Assert(_pGroup.sk == SYMKIND.SK_MethodSymbol || _pGroup.sk == SYMKIND.SK_PropertySymbol && 0 != (_pGroup.flags & EXPRFLAG.EXF_INDEXER));

                // We need the EXPRs for error reporting for non-delegates
                Debug.Assert(_pDelegate != null || _pArguments.fHasExprs);

                LookForCandidates();
                if (!GetResultOfBind(bReportErrors))
                {
                    if (bReportErrors)
                    {
                        ReportErrorsOnFailure();
                    }
                    return false;
                }
                return true;
            }

            public GroupToArgsBinderResult GetResultsOfBind()
            {
                return _results;
            }

            public bool BindCollectionAddArgs()
            {
                _bBindingCollectionAddArgs = true;
                return Bind(true /* bReportErrors */);
            }
            private SymbolLoader GetSymbolLoader()
            {
                return _pExprBinder.GetSymbolLoader();
            }
            private CSemanticChecker GetSemanticChecker()
            {
                return _pExprBinder.GetSemanticChecker();
            }
            private ErrorHandling GetErrorContext()
            {
                return _pExprBinder.GetErrorContext();
            }
            public static CType GetTypeQualifier(EXPRMEMGRP pGroup)
            {
                Debug.Assert(pGroup != null);

                CType rval = null;

                if (0 != (pGroup.flags & EXPRFLAG.EXF_BASECALL))
                {
                    rval = null;
                }
                else if (0 != (pGroup.flags & EXPRFLAG.EXF_CTOR))
                {
                    rval = pGroup.GetParentType();
                }
                else if (pGroup.GetOptionalObject() != null)
                {
                    rval = pGroup.GetOptionalObject().type;
                }
                else
                {
                    rval = null;
                }
                return rval;
            }

            private void LookForCandidates()
            {
                bool fExpanded = false;
                bool bSearchForExpanded = true;
                int cswtMaxWrongCount = _swtWrongCount.Length;
                bool allCandidatesUnsupported = true;
                bool lookedAtCandidates = false;

                // Calculate the mask based on the type of the sym we've found so far.  This
                // is to ensure that if we found a propsym (or methsym, or whatever) the 
                // iterator will only return propsyms (or methsyms, or whatever)
                symbmask_t mask = (symbmask_t)(1 << (int)_pGroup.sk);

                CType pTypeThrough = _pGroup.GetOptionalObject() != null ? _pGroup.GetOptionalObject().type : null;
                CMemberLookupResults.CMethodIterator iterator = _pGroup.GetMemberLookupResults().GetMethodIterator(GetSemanticChecker(), GetSymbolLoader(), pTypeThrough, GetTypeQualifier(_pGroup), _pExprBinder.ContextForMemberLookup(), true, // AllowBogusAndInaccessible
                    false, _pGroup.typeArgs.size, _pGroup.flags, mask);
                while (true)
                {
                    bool bFoundExpanded;

                    bFoundExpanded = false;
                    if (bSearchForExpanded && !fExpanded)
                    {
                        bFoundExpanded = fExpanded = ConstructExpandedParameters();
                    }

                    // Get the next sym to search for.
                    if (!bFoundExpanded)
                    {
                        fExpanded = false;

                        if (!GetNextSym(iterator))
                        {
                            break;
                        }

                        // Get the parameters.
                        _pCurrentParameters = _pCurrentSym.Params;
                        bSearchForExpanded = true;
                    }

                    if (_bArgumentsChangedForNamedOrOptionalArguments)
                    {
                        // If we changed them last time, then we need to reset them.
                        _bArgumentsChangedForNamedOrOptionalArguments = false;
                        CopyArgInfos(_pOriginalArguments, _pArguments);
                    }

                    // If we have named arguments, reorder them for this method.
                    if (_pArguments.fHasExprs)
                    {
                        // If we don't have EXPRs, its because we're doing a method group conversion.
                        // In those scenarios, we never want to add named arguments or optional arguments.
                        if (_bHasNamedArguments)
                        {
                            if (!ReOrderArgsForNamedArguments())
                            {
                                continue;
                            }
                        }
                        else if (HasOptionalParameters())
                        {
                            if (!AddArgumentsForOptionalParameters())
                            {
                                continue;
                            }
                        }
                    }

                    if (!bFoundExpanded)
                    {
                        lookedAtCandidates = true;
                        allCandidatesUnsupported &= _pCurrentSym.getBogus();

                        // If we have the wrong number of arguments and still have room in our cache of 20,
                        // then store it in our cache and go to the next sym.
                        if (_pCurrentParameters.size != _pArguments.carg)
                        {
                            if (_nWrongCount < cswtMaxWrongCount &&
                                    (!_pCurrentSym.isParamArray || _pArguments.carg < _pCurrentParameters.size - 1))
                            {
                                _swtWrongCount[_nWrongCount++] = new SymWithType(_pCurrentSym, _pCurrentType);
                            }
                            bSearchForExpanded = true;
                            continue;
                        }
                    }

                    // If we cant use the current symbol, then we've filtered it, so get the next one.

                    if (!iterator.CanUseCurrentSymbol())
                    {
                        continue;
                    }

                    // Get the current type args.
                    Result currentTypeArgsResult = DetermineCurrentTypeArgs();
                    if (currentTypeArgsResult != Result.Success)
                    {
                        bSearchForExpanded = (currentTypeArgsResult == Result.Failure_SearchForExpanded);
                        continue;
                    }

                    // Check access.
                    bool fCanAccess = !iterator.IsCurrentSymbolInaccessible();
                    if (!fCanAccess && (!_methList.IsEmpty() || _results.GetInaccessibleResult()))
                    {
                        // We'll never use this one for error reporting anyway, so just skip it.
                        bSearchForExpanded = false;
                        continue;
                    }

                    // Check bogus.
                    bool fBogus = fCanAccess && iterator.IsCurrentSymbolBogus();
                    if (fBogus && (!_methList.IsEmpty() || _results.GetInaccessibleResult() || _mpwiBogus))
                    {
                        // We'll never use this one for error reporting anyway, so just skip it.
                        bSearchForExpanded = false;
                        continue;
                    }

                    // Check convertibility of arguments.
                    if (!ArgumentsAreConvertible())
                    {
                        bSearchForExpanded = true;
                        continue;
                    }

                    // We know we have the right number of arguments and they are all convertible.
                    if (!fCanAccess)
                    {
                        // In case we never get an accessible method, this will allow us to give
                        // a better error...
                        Debug.Assert(!_results.GetInaccessibleResult());
                        _results.GetInaccessibleResult().Set(_pCurrentSym, _pCurrentType, _pCurrentTypeArgs);
                    }
                    else if (fBogus)
                    {
                        // In case we never get a good method, this will allow us to give
                        // a better error...
                        Debug.Assert(!_mpwiBogus);
                        _mpwiBogus.Set(_pCurrentSym, _pCurrentType, _pCurrentTypeArgs);
                    }
                    else
                    {
                        // This is a plausible method / property to call.
                        // Link it in at the end of the list.
                        _methList.Add(new CandidateFunctionMember(
                                    new MethPropWithInst(_pCurrentSym, _pCurrentType, _pCurrentTypeArgs),
                                    _pCurrentParameters,
                                    0,
                                    fExpanded));

                        // When we find a method, we check if the type has interfaces. If so, mark the other interfaces
                        // as hidden, and object as well.

                        if (_pCurrentType.isInterfaceType())
                        {
                            TypeArray ifaces = _pCurrentType.GetIfacesAll();
                            for (int i = 0; i < ifaces.size; i++)
                            {
                                AggregateType type = ifaces.Item(i).AsAggregateType();

                                Debug.Assert(type.isInterfaceType());
                                _HiddenTypes.Add(type);
                            }

                            // Mark object.
                            AggregateType typeObject = GetSymbolLoader().GetReqPredefType(PredefinedType.PT_OBJECT, true);
                            _HiddenTypes.Add(typeObject);
                        }
                    }

                    // Don't look at the expanded form.
                    bSearchForExpanded = false;
                }
                _fCandidatesUnsupported = allCandidatesUnsupported && lookedAtCandidates;

                // Restore the arguments to their original state if we changed them for named/optional arguments.
                // ILGen will take care of putting the real arguments in there.
                if (_bArgumentsChangedForNamedOrOptionalArguments)
                {
                    // If we changed them last time, then we need to reset them.
                    CopyArgInfos(_pOriginalArguments, _pArguments);
                }
            }

            private void CopyArgInfos(ArgInfos src, ArgInfos dst)
            {
                dst.carg = src.carg;
                dst.types = src.types;
                dst.fHasExprs = src.fHasExprs;

                dst.prgexpr.Clear();
                for (int i = 0; i < src.prgexpr.Count; i++)
                {
                    dst.prgexpr.Add(src.prgexpr[i]);
                }
            }

            private bool GetResultOfBind(bool bReportErrors)
            {
                // We looked at all the evidence, and we come to render the verdict:

                if (!_methList.IsEmpty())
                {
                    CandidateFunctionMember pmethBest;
                    if (_methList.Count == 1)
                    {
                        // We found the single best method to call.
                        pmethBest = _methList.Head();
                    }
                    else
                    {
                        // We have some ambiguities, lets sort them out.
                        CandidateFunctionMember pAmbig1 = null;
                        CandidateFunctionMember pAmbig2 = null;

                        CType pTypeThrough = _pGroup.GetOptionalObject() != null ? _pGroup.GetOptionalObject().type : null;
                        pmethBest = _pExprBinder.FindBestMethod(_methList, pTypeThrough, _pArguments, out pAmbig1, out pAmbig2);

                        if (null == pmethBest)
                        {
                            // Arbitrarily use the first one, but make sure to report errors or give the ambiguous one
                            // back to the caller.
                            pmethBest = pAmbig1;
                            _results.AmbiguousResult = pAmbig2.mpwi;

                            if (bReportErrors)
                            {
                                if (pAmbig1.@params != pAmbig2.@params ||
                                    pAmbig1.mpwi.MethProp().Params.size != pAmbig2.mpwi.MethProp().Params.size ||
                                    pAmbig1.mpwi.TypeArgs != pAmbig2.mpwi.TypeArgs ||
                                    pAmbig1.mpwi.GetType() != pAmbig2.mpwi.GetType() ||
                                    pAmbig1.mpwi.MethProp().Params == pAmbig2.mpwi.MethProp().Params)
                                {
                                    GetErrorContext().Error(ErrorCode.ERR_AmbigCall, pAmbig1.mpwi, pAmbig2.mpwi);
                                }
                                else
                                {
                                    // The two signatures are identical so don't use the type args in the error message.
                                    GetErrorContext().Error(ErrorCode.ERR_AmbigCall, pAmbig1.mpwi.MethProp(), pAmbig2.mpwi.MethProp());
                                }
                            }
                        }
                    }

                    // This is the "success" exit path.
                    Debug.Assert(pmethBest != null);
                    _results.BestResult = pmethBest.mpwi;

                    // Record our best match in the memgroup as well. This is temporary.

                    if (bReportErrors)
                    {
                        ReportErrorsOnSuccess();
                    }
                    return true;
                }

                return false;
            }

            /////////////////////////////////////////////////////////////////////////////////
            // This method returns true if we're able to match arguments to their names.
            // If we either have too many arguments, or we cannot match their names, then
            // we return false. 
            //
            // Note that if we have not enough arguments, we still return true as long as
            // we can find matching parameters for each named arguments, and all parameters
            // that do not have a matching argument are optional parameters.

            private bool ReOrderArgsForNamedArguments()
            {
                // First we need to find the method that we're actually trying to call. 
                MethodOrPropertySymbol methprop = FindMostDerivedMethod(_pCurrentSym, _pGroup.GetOptionalObject());
                if (methprop == null)
                {
                    return false;
                }

                int numParameters = _pCurrentParameters.size;

                // If we have no parameters, or fewer parameters than we have arguments, bail.
                if (numParameters == 0 || numParameters < _pArguments.carg)
                {
                    return false;
                }

                // Make sure all the names we specified are in the list and we don't have duplicates.
                if (!NamedArgumentNamesAppearInParameterList(methprop))
                {
                    return false;
                }

                _bArgumentsChangedForNamedOrOptionalArguments = ReOrderArgsForNamedArguments(
                        methprop,
                        _pCurrentParameters,
                        _pCurrentType,
                        _pGroup,
                        _pArguments,
                        _pExprBinder.GetTypes(),
                        _pExprBinder.GetExprFactory(),
                        GetSymbolLoader());
                return _bArgumentsChangedForNamedOrOptionalArguments;
            }

            internal static bool ReOrderArgsForNamedArguments(
                    MethodOrPropertySymbol methprop,
                    TypeArray pCurrentParameters,
                    AggregateType pCurrentType,
                    EXPRMEMGRP pGroup,
                    ArgInfos pArguments,
                    TypeManager typeManager,
                    ExprFactory exprFactory,
                    SymbolLoader symbolLoader)
            {
                // We use the param count from pCurrentParameters because they may have been resized 
                // for param arrays.
                int numParameters = pCurrentParameters.size;

                EXPR[] pExprArguments = new EXPR[numParameters];

                // Now go through the parameters. First set all positional arguments in the new argument
                // set, then for the remainder, look for a named argument with a matching name.
                int index = 0;
                EXPR paramArrayArgument = null;
                TypeArray @params = typeManager.SubstTypeArray(
                    pCurrentParameters,
                    pCurrentType,
                    pGroup.typeArgs);
                foreach (Name name in methprop.ParameterNames)
                {
                    // This can happen if we had expanded our param array to size 0.
                    if (index >= pCurrentParameters.size)
                    {
                        break;
                    }

                    // If:
                    // (1) we have a param array method
                    // (2) we're on the last arg
                    // (3) the thing we have is an array init thats generated for param array
                    // then let us through.
                    if (methprop.isParamArray &&
                        index < pArguments.carg &&
                        pArguments.prgexpr[index].isARRINIT() && pArguments.prgexpr[index].asARRINIT().GeneratedForParamArray)
                    {
                        paramArrayArgument = pArguments.prgexpr[index];
                    }

                    // Positional.
                    if (index < pArguments.carg &&
                        !pArguments.prgexpr[index].isNamedArgumentSpecification() &&
                        !(pArguments.prgexpr[index].isARRINIT() && pArguments.prgexpr[index].asARRINIT().GeneratedForParamArray))
                    {
                        pExprArguments[index] = pArguments.prgexpr[index++];
                        continue;
                    }

                    // Look for names.
                    EXPR pNewArg = FindArgumentWithName(pArguments, name);
                    if (pNewArg == null)
                    {
                        if (methprop.IsParameterOptional(index))
                        {
                            pNewArg = GenerateOptionalArgument(symbolLoader, exprFactory, methprop, @params.Item(index), index);
                        }
                        else if (paramArrayArgument != null && index == methprop.Params.Count - 1)
                        {
                            // If we have a param array argument and we're on the last one, then use it.
                            pNewArg = paramArrayArgument;
                        }
                        else
                        {
                            // No name and no default value.
                            return false;
                        }
                    }
                    pExprArguments[index++] = pNewArg;
                }

                // Here we've found all the arguments, or have default values for them.
                CType[] prgTypes = new CType[pCurrentParameters.size];
                for (int i = 0; i < numParameters; i++)
                {
                    if (i < pArguments.prgexpr.Count)
                    {
                        pArguments.prgexpr[i] = pExprArguments[i];
                    }
                    else
                    {
                        pArguments.prgexpr.Add(pExprArguments[i]);
                    }
                    prgTypes[i] = pArguments.prgexpr[i].type;
                }
                pArguments.carg = pCurrentParameters.size;
                pArguments.types = symbolLoader.getBSymmgr().AllocParams(pCurrentParameters.size, prgTypes);
                return true;
            }

            /////////////////////////////////////////////////////////////////////////////////

            private static EXPR GenerateOptionalArgument(
                    SymbolLoader symbolLoader,
                    ExprFactory exprFactory,
                    MethodOrPropertySymbol methprop,
                    CType type,
                    int index)
            {
                CType pParamType = type;
                CType pRawParamType = type.IsNullableType() ? type.AsNullableType().GetUnderlyingType() : type;

                EXPR optionalArgument = null;
                if (methprop.HasDefaultParameterValue(index))
                {
                    CType pConstValType = methprop.GetDefaultParameterValueConstValType(index);
                    CONSTVAL cv = methprop.GetDefaultParameterValue(index);

                    if (pConstValType.isPredefType(PredefinedType.PT_DATETIME) &&
                        (pRawParamType.isPredefType(PredefinedType.PT_DATETIME) || pRawParamType.isPredefType(PredefinedType.PT_OBJECT) || pRawParamType.isPredefType(PredefinedType.PT_VALUE)))
                    {
                        // This is the specific case where we want to create a DateTime
                        // but the constval that stores it is a long.

                        AggregateType dateTimeType = symbolLoader.GetReqPredefType(PredefinedType.PT_DATETIME);
                        optionalArgument = exprFactory.CreateConstant(dateTimeType, new CONSTVAL(DateTime.FromBinary(cv.longVal)));
                    }
                    else if (pConstValType.isSimpleOrEnumOrString())
                    {
                        // In this case, the constval is a simple type (all the numerics, including
                        // decimal), or an enum or a string. This covers all the substantial values,
                        // and everything else that can be encoded is just null or default(something).

                        // For enum parameters, we create a constant of the enum type. For everything
                        // else, we create the appropriate constant.

                        if (pRawParamType.isEnumType() && pConstValType == pRawParamType.underlyingType())
                        {
                            optionalArgument = exprFactory.CreateConstant(pRawParamType, cv);
                        }
                        else
                        {
                            optionalArgument = exprFactory.CreateConstant(pConstValType, cv);
                        }
                    }
                    else if ((pParamType.IsRefType() || pParamType.IsNullableType()) && cv.IsNullRef())
                    {
                        // We have an "= null" default value with a reference type or a nullable type.

                        optionalArgument = exprFactory.CreateNull();
                    }
                    else
                    {
                        // We have a default value that is encoded as a nullref, and that nullref is
                        // interpreted as default(something). For instance, the pParamType could be
                        // a type parameter type or a non-simple value type.

                        optionalArgument = exprFactory.CreateZeroInit(pParamType);
                    }
                }
                else
                {
                    // There was no default parameter specified, so generally use default(T),
                    // except for some cases when the parameter type in metatdata is object.

                    if (pParamType.isPredefType(PredefinedType.PT_OBJECT))
                    {
                        if (methprop.MarshalAsObject(index))
                        {
                            // For [opt] parameters of type object, if we have marshal(iunknown),
                            // marshal(idispatch), or marshal(interface), then we emit a null.

                            optionalArgument = exprFactory.CreateNull();
                        }
                        else
                        {
                            // Otherwise, we generate Type.Missing

                            AggregateSymbol agg = symbolLoader.GetOptPredefAgg(PredefinedType.PT_MISSING);
                            Name name = symbolLoader.GetNameManager().GetPredefinedName(PredefinedName.PN_CAP_VALUE);
                            FieldSymbol field = symbolLoader.LookupAggMember(name, agg, symbmask_t.MASK_FieldSymbol).AsFieldSymbol();
                            FieldWithType fwt = new FieldWithType(field, agg.getThisType());
                            EXPRFIELD exprField = exprFactory.CreateField(0, agg.getThisType(), null, 0, fwt, null);

                            if (agg.getThisType() != type)
                            {
                                optionalArgument = exprFactory.CreateCast(0, type, exprField);
                            }
                            else
                            {
                                optionalArgument = exprField;
                            }
                        }
                    }
                    else
                    {
                        // Every type aside from object that doesn't have a default value gets
                        // its default value.

                        optionalArgument = exprFactory.CreateZeroInit(pParamType);
                    }
                }

                Debug.Assert(optionalArgument != null);
                optionalArgument.IsOptionalArgument = true;
                return optionalArgument;
            }

            /////////////////////////////////////////////////////////////////////////////////

            private MethodOrPropertySymbol FindMostDerivedMethod(
                    MethodOrPropertySymbol pMethProp,
                    EXPR pObject)
            {
                return FindMostDerivedMethod(GetSymbolLoader(), pMethProp, pObject != null ? pObject.type : null);
            }

            /////////////////////////////////////////////////////////////////////////////////

            public static MethodOrPropertySymbol FindMostDerivedMethod(
                    SymbolLoader symbolLoader,
                    MethodOrPropertySymbol pMethProp,
                    CType pType)
            {
                MethodSymbol method;
                bool bIsIndexer = false;

                if (pMethProp.IsMethodSymbol())
                {
                    method = pMethProp.AsMethodSymbol();
                }
                else
                {
                    PropertySymbol prop = pMethProp.AsPropertySymbol();
                    method = prop.methGet != null ? prop.methGet : prop.methSet;
                    if (method == null)
                    {
                        return null;
                    }
                    bIsIndexer = prop.isIndexer();
                }

                if (!method.isVirtual)
                {
                    return method;
                }

                if (pType == null)
                {
                    // This must be a static call.
                    return method;
                }

                // Now get the slot method.
                if (method.swtSlot != null && method.swtSlot.Meth() != null)
                {
                    method = method.swtSlot.Meth();
                }

                if (!pType.IsAggregateType())
                {
                    // Not something that can have overrides anyway.
                    return method;
                }

                for (AggregateSymbol pAggregate = pType.AsAggregateType().GetOwningAggregate();
                        pAggregate != null && pAggregate.GetBaseAgg() != null;
                        pAggregate = pAggregate.GetBaseAgg())
                {
                    for (MethodOrPropertySymbol meth = symbolLoader.LookupAggMember(method.name, pAggregate, symbmask_t.MASK_MethodSymbol | symbmask_t.MASK_PropertySymbol).AsMethodOrPropertySymbol();
                            meth != null;
                            meth = symbolLoader.LookupNextSym(meth, pAggregate, symbmask_t.MASK_MethodSymbol | symbmask_t.MASK_PropertySymbol).AsMethodOrPropertySymbol())
                    {
                        if (!meth.isOverride)
                        {
                            continue;
                        }
                        if (meth.swtSlot.Sym != null && meth.swtSlot.Sym == method)
                        {
                            if (bIsIndexer)
                            {
                                Debug.Assert(meth.IsMethodSymbol());
                                return meth.AsMethodSymbol().getProperty();
                            }
                            else
                            {
                                return meth;
                            }
                        }
                    }
                }

                // If we get here, it means we can have two cases: one is that we have 
                // a delegate. This is because the delegate invoke method is virtual and is 
                // an override, but we wont have the slots set up correctly, and will 
                // not find the base type in the inheritance hierarchy. The second is that
                // we're calling off of the base itself.
                Debug.Assert(method.parent.IsAggregateSymbol());
                return method;
            }


            /////////////////////////////////////////////////////////////////////////////////

            private bool HasOptionalParameters()
            {
                MethodOrPropertySymbol methprop = FindMostDerivedMethod(_pCurrentSym, _pGroup.GetOptionalObject());
                return methprop != null ? methprop.HasOptionalParameters() : false;
            }

            /////////////////////////////////////////////////////////////////////////////////
            // Returns true if we can either add enough optional parameters to make the 
            // argument list match, or if we don't need to at all.

            private bool AddArgumentsForOptionalParameters()
            {
                if (_pCurrentParameters.size <= _pArguments.carg)
                {
                    // If we have enough arguments, or too many, no need to add any optionals here.
                    return true;
                }

                // First we need to find the method that we're actually trying to call. 
                MethodOrPropertySymbol methprop = FindMostDerivedMethod(_pCurrentSym, _pGroup.GetOptionalObject());
                if (methprop == null)
                {
                    return false;
                }

                // If we're here, we know we're not in a named argument case. As such, we can
                // just generate defaults for every missing argument.
                int i = _pArguments.carg;
                int index = 0;
                TypeArray @params = _pExprBinder.GetTypes().SubstTypeArray(
                    _pCurrentParameters,
                    _pCurrentType,
                    _pGroup.typeArgs);
                EXPR[] pArguments = new EXPR[_pCurrentParameters.size - i];
                for (; i < @params.size; i++, index++)
                {
                    if (!methprop.IsParameterOptional(i))
                    {
                        // We don't have an optional here, but we need to fill it in.
                        return false;
                    }

                    pArguments[index] = GenerateOptionalArgument(GetSymbolLoader(), _pExprBinder.GetExprFactory(), methprop, @params.Item(i), i);
                }

                // Success. Lets copy them in now.
                for (int n = 0; n < index; n++)
                {
                    _pArguments.prgexpr.Add(pArguments[n]);
                }
                CType[] prgTypes = new CType[@params.size];
                for (int n = 0; n < @params.size; n++)
                {
                    prgTypes[n] = _pArguments.prgexpr[n].type;
                }
                _pArguments.types = GetSymbolLoader().getBSymmgr().AllocParams(@params.size, prgTypes);
                _pArguments.carg = @params.size;
                _bArgumentsChangedForNamedOrOptionalArguments = true;
                return true;
            }

            /////////////////////////////////////////////////////////////////////////////////

            private static EXPR FindArgumentWithName(ArgInfos pArguments, Name pName)
            {
                for (int i = 0; i < pArguments.carg; i++)
                {
                    if (pArguments.prgexpr[i].isNamedArgumentSpecification() &&
                            pArguments.prgexpr[i].asNamedArgumentSpecification().Name == pName)
                    {
                        return pArguments.prgexpr[i];
                    }
                }
                return null;
            }

            /////////////////////////////////////////////////////////////////////////////////

            private bool NamedArgumentNamesAppearInParameterList(
                    MethodOrPropertySymbol methprop)
            {
                // Keep track of the current position in the parameter list so that we can check
                // containment from this point onwards as well as complete containment. This is 
                // for error reporting. The user cannot specify a named argument for a parameter
                // that has a fixed argument value.
                List<Name> currentPosition = methprop.ParameterNames;
                HashSet<Name> names = new HashSet<Name>();
                for (int i = 0; i < _pArguments.carg; i++)
                {
                    if (!_pArguments.prgexpr[i].isNamedArgumentSpecification())
                    {
                        if (!currentPosition.IsEmpty())
                        {
                            currentPosition = currentPosition.Tail();
                        }
                        continue;
                    }

                    Name name = _pArguments.prgexpr[i].asNamedArgumentSpecification().Name;
                    if (!methprop.ParameterNames.Contains(name))
                    {
                        if (_pInvalidSpecifiedName == null)
                        {
                            _pInvalidSpecifiedName = name;
                        }
                        return false;
                    }
                    else if (!currentPosition.Contains(name))
                    {
                        if (_pNameUsedInPositionalArgument == null)
                        {
                            _pNameUsedInPositionalArgument = name;
                        }
                        return false;
                    }
                    if (names.Contains(name))
                    {
                        if (_pDuplicateSpecifiedName == null)
                        {
                            _pDuplicateSpecifiedName = name;
                        }
                        return false;
                    }
                    names.Add(name);
                }
                return true;
            }

            // This method returns true if we have another sym to consider.
            // If we've found a match in the current type, and have no more syms to consider in this type, then we
            // return false.
            private bool GetNextSym(CMemberLookupResults.CMethodIterator iterator)
            {
                if (!iterator.MoveNext(_methList.IsEmpty(), _bIterateToEndOfNsList))
                {
                    return false;
                }
                _pCurrentSym = iterator.GetCurrentSymbol();
                AggregateType type = iterator.GetCurrentType();

                // If our current type is null, this is our first iteration, so set the type.
                // If our current type is not null, and we've got a new type now, and we've already matched
                // a symbol, then bail out.

                if (_pCurrentType != type &&
                        _pCurrentType != null &&
                        !_methList.IsEmpty() &&
                        !_methList.Head().mpwi.GetType().isInterfaceType() &&
                        (!_methList.Head().mpwi.Sym.IsMethodSymbol() || !_methList.Head().mpwi.Meth().IsExtension()))
                {
                    return false;
                }
                else if (_pCurrentType != type &&
                        _pCurrentType != null &&
                        !_methList.IsEmpty() &&
                        !_methList.Head().mpwi.GetType().isInterfaceType() &&
                        _methList.Head().mpwi.Sym.IsMethodSymbol() &&
                        _methList.Head().mpwi.Meth().IsExtension())
                {
                    // we have found a applicable method that is an extension now we must move to the end of the NS list before quiting
                    if (_pGroup.GetOptionalObject() != null)
                    {
                        // if we find this while looking for static methods we should ignore it
                        _bIterateToEndOfNsList = true;
                    }
                }

                _pCurrentType = type;

                // We have a new type. If this type is hidden, we need another type.

                while (_HiddenTypes.Contains(_pCurrentType))
                {
                    // Move through this type and get the next one.
                    for (; iterator.GetCurrentType() == _pCurrentType; iterator.MoveNext(_methList.IsEmpty(), _bIterateToEndOfNsList)) ;
                    _pCurrentSym = iterator.GetCurrentSymbol();
                    _pCurrentType = iterator.GetCurrentType();

                    if (iterator.AtEnd())
                    {
                        return false;
                    }
                }
                return true;
            }

            private bool ConstructExpandedParameters()
            {
                // Deal with params.
                if (_pCurrentSym == null || _pArguments == null || _pCurrentParameters == null)
                {
                    return false;
                }
                if (0 != (_fBindFlags & BindingFlag.BIND_NOPARAMS))
                {
                    return false;
                }
                if (!_pCurrentSym.isParamArray)
                {
                    return false;
                }

                // Count the number of optionals in the method. If there are enough optionals
                // and actual arguments, then proceed.
                {
                    int numOptionals = 0;
                    for (int i = _pArguments.carg; i < _pCurrentSym.Params.size; i++)
                    {
                        if (_pCurrentSym.IsParameterOptional(i))
                        {
                            numOptionals++;
                        }
                    }
                    if (_pArguments.carg + numOptionals < _pCurrentParameters.size - 1)
                    {
                        return false;
                    }
                }

                Debug.Assert(_methList.IsEmpty() || _methList.Head().mpwi.MethProp() != _pCurrentSym);
                // Construct the expanded params.
                return _pExprBinder.TryGetExpandedParams(_pCurrentSym.Params, _pArguments.carg, out _pCurrentParameters);
            }

            private Result DetermineCurrentTypeArgs()
            {
                TypeArray typeArgs = _pGroup.typeArgs;

                // Get the type args.
                if (_pCurrentSym.IsMethodSymbol() && _pCurrentSym.AsMethodSymbol().typeVars.size != typeArgs.size)
                {
                    MethodSymbol methSym = _pCurrentSym.AsMethodSymbol();
                    // Can't infer if some type args are specified.
                    if (typeArgs.size > 0)
                    {
                        if (!_mwtBadArity)
                        {
                            _mwtBadArity.Set(methSym, _pCurrentType);
                        }
                        return Result.Failure_NoSearchForExpanded;
                    }
                    Debug.Assert(methSym.typeVars.size > 0);

                    // Try to infer. If we have an errorsym in the type arguments, we know we cant infer,
                    // but we want to attempt it anyway. We'll mark this as "cant infer" so that we can
                    // report the appropriate error, but we'll continue inferring, since we want 
                    // error sym to go to any type.

                    bool inferenceSucceeded;

                    inferenceSucceeded = MethodTypeInferrer.Infer(
                                _pExprBinder, GetSymbolLoader(),
                                methSym, _pCurrentType.GetTypeArgsAll(), _pCurrentParameters,
                                _pArguments, out _pCurrentTypeArgs);

                    if (!inferenceSucceeded)
                    {
                        if (_results.IsBetterUninferableResult(_pCurrentTypeArgs))
                        {
                            TypeArray pTypeVars = methSym.typeVars;
                            if (pTypeVars != null && _pCurrentTypeArgs != null && pTypeVars.size == _pCurrentTypeArgs.size)
                            {
                                _mpwiCantInferInstArg.Set(_pCurrentSym.AsMethodSymbol(), _pCurrentType, _pCurrentTypeArgs);
                            }
                            else
                            {
                                _mpwiCantInferInstArg.Set(_pCurrentSym.AsMethodSymbol(), _pCurrentType, pTypeVars);
                            }
                        }
                        return Result.Failure_SearchForExpanded;
                    }
                }
                else
                {
                    _pCurrentTypeArgs = typeArgs;
                }
                return Result.Success;
            }

            private bool ArgumentsAreConvertible()
            {
                bool containsErrorSym = false;
                bool bIsInstanceParameterConvertible = false;
                if (_pArguments.carg != 0)
                {
                    UpdateArguments();
                    for (int ivar = 0; ivar < _pArguments.carg; ivar++)
                    {
                        CType var = _pCurrentParameters.Item(ivar);
                        bool constraintErrors = !TypeBind.CheckConstraints(GetSemanticChecker(), GetErrorContext(), var, CheckConstraintsFlags.NoErrors);
                        if (constraintErrors && !DoesTypeArgumentsContainErrorSym(var))
                        {
                            _mpwiParamTypeConstraints.Set(_pCurrentSym, _pCurrentType, _pCurrentTypeArgs);
                            return false;
                        }
                    }

                    for (int ivar = 0; ivar < _pArguments.carg; ivar++)
                    {
                        CType var = _pCurrentParameters.Item(ivar);
                        containsErrorSym |= DoesTypeArgumentsContainErrorSym(var);
                        bool fresult;

                        if (_pArguments.fHasExprs)
                        {
                            EXPR pArgument = _pArguments.prgexpr[ivar];

                            // If we have a named argument, strip it to do the conversion.
                            if (pArgument.isNamedArgumentSpecification())
                            {
                                pArgument = pArgument.asNamedArgumentSpecification().Value;
                            }

                            fresult = _pExprBinder.canConvert(pArgument, var);
                        }
                        else
                        {
                            fresult = _pExprBinder.canConvert(_pArguments.types.Item(ivar), var);
                        }

                        // Mark this as a legitimate error if we didn't have any error syms.
                        if (!fresult && !containsErrorSym)
                        {
                            if (ivar > _nArgBest)
                            {
                                _nArgBest = ivar;

                                // If we already have best method for instance methods don't overwrite with extensions
                                if (!_results.GetBestResult())
                                {
                                    _results.GetBestResult().Set(_pCurrentSym, _pCurrentType, _pCurrentTypeArgs);
                                    _pBestParameters = _pCurrentParameters;
                                }
                            }
                            else if (ivar == _nArgBest && _pArguments.types.Item(ivar) != var)
                            {
                                // this is to eliminate the paranoid case of types that are equal but can't convert 
                                // (think ErrorType != ErrorType)
                                // See if they just differ in out / ref.
                                CType argStripped = _pArguments.types.Item(ivar).IsParameterModifierType() ?
                                    _pArguments.types.Item(ivar).AsParameterModifierType().GetParameterType() : _pArguments.types.Item(ivar);
                                CType varStripped = var.IsParameterModifierType() ? var.AsParameterModifierType().GetParameterType() : var;

                                if (argStripped == varStripped)
                                {
                                    // If we already have best method for instance methods don't overwrite with extensions
                                    if (!_results.GetBestResult())
                                    {
                                        _results.GetBestResult().Set(_pCurrentSym, _pCurrentType, _pCurrentTypeArgs);
                                        _pBestParameters = _pCurrentParameters;
                                    }
                                }
                            }

                            if (_pCurrentSym.IsMethodSymbol())
                            {
                                // Do not store the result if we have an extension method and the instance 
                                // parameter isn't convertible.

                                if (!_pCurrentSym.AsMethodSymbol().IsExtension() || bIsInstanceParameterConvertible)
                                {
                                    _results.AddInconvertibleResult(
                                        _pCurrentSym.AsMethodSymbol(),
                                        _pCurrentType,
                                        _pCurrentTypeArgs);
                                }
                            }
                            return false;
                        }
                    }
                }

                if (containsErrorSym)
                {
                    if (_results.IsBetterUninferableResult(_pCurrentTypeArgs) && _pCurrentSym.IsMethodSymbol())
                    {
                        // If we're an instance method or we're an extension that has an inferable instance argument,
                        // then mark us down. Note that the extension may not need to infer type args,
                        // so check if we have any type variables at all to begin with.
                        if (!_pCurrentSym.AsMethodSymbol().IsExtension() ||
                            _pCurrentSym.AsMethodSymbol().typeVars.size == 0 ||
                                MethodTypeInferrer.CanObjectOfExtensionBeInferred(
                                    _pExprBinder,
                                    GetSymbolLoader(),
                                    _pCurrentSym.AsMethodSymbol(),
                                    _pCurrentType.GetTypeArgsAll(),
                                    _pCurrentSym.AsMethodSymbol().Params,
                                    _pArguments))
                        {
                            _results.GetUninferableResult().Set(
                                    _pCurrentSym.AsMethodSymbol(),
                                    _pCurrentType,
                                    _pCurrentTypeArgs);
                        }
                    }
                }
                else
                {
                    if (_pCurrentSym.IsMethodSymbol())
                    {
                        // Do not store the result if we have an extension method and the instance 
                        // parameter isn't convertible.

                        if (!_pCurrentSym.AsMethodSymbol().IsExtension() || bIsInstanceParameterConvertible)
                        {
                            _results.AddInconvertibleResult(
                                    _pCurrentSym.AsMethodSymbol(),
                                    _pCurrentType,
                                    _pCurrentTypeArgs);
                        }
                    }
                }
                return !containsErrorSym;
            }

            private void UpdateArguments()
            {
                // Parameter types might have changed as a result of
                // method type inference. 

                _pCurrentParameters = _pExprBinder.GetTypes().SubstTypeArray(
                        _pCurrentParameters, _pCurrentType, _pCurrentTypeArgs);

                // It is also possible that an optional argument has changed its value
                // as a result of method type inference. For example, when inferring
                // from Foo(10) to Foo<T>(T t1, T t2 = default(T)), the fabricated
                // argument list starts off as being (10, default(T)). After type
                // inference has successfully inferred T as int, it needs to be 
                // transformed into (10, default(int)) before applicability checking
                // notices that default(T) is not assignable to int.

                if (_pArguments.prgexpr == null || _pArguments.prgexpr.Count == 0)
                {
                    return;
                }

                MethodOrPropertySymbol pMethod = null;
                for (int iParam = 0; iParam < _pCurrentParameters.size; ++iParam)
                {
                    EXPR pArgument = _pArguments.prgexpr[iParam];
                    if (!pArgument.IsOptionalArgument)
                    {
                        continue;
                    }
                    CType pType = _pCurrentParameters.Item(iParam);

                    if (pType == pArgument.type)
                    {
                        continue;
                    }

                    // Argument has changed its type because of method type inference. Recompute it.
                    if (pMethod == null)
                    {
                        pMethod = FindMostDerivedMethod(_pCurrentSym, _pGroup.GetOptionalObject());
                        Debug.Assert(pMethod != null);
                    }
                    Debug.Assert(pMethod.IsParameterOptional(iParam));
                    EXPR pArgumentNew = GenerateOptionalArgument(GetSymbolLoader(), _pExprBinder.GetExprFactory(), pMethod, _pCurrentParameters[iParam], iParam);
                    _pArguments.prgexpr[iParam] = pArgumentNew;
                }
            }

            private bool DoesTypeArgumentsContainErrorSym(CType var)
            {
                if (!var.IsAggregateType())
                {
                    return false;
                }

                TypeArray typeVars = var.AsAggregateType().GetTypeArgsAll();
                for (int i = 0; i < typeVars.size; i++)
                {
                    CType type = typeVars.Item(i);
                    if (type.IsErrorType())
                    {
                        return true;
                    }
                    else if (type.IsAggregateType())
                    {
                        // If we have an agg type sym, check if its type args have errors.
                        if (DoesTypeArgumentsContainErrorSym(type))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            // ----------------------------------------------------------------------------

            private void ReportErrorsOnSuccess()
            {
                // used for Methods and Indexers
                Debug.Assert(_pGroup.sk == SYMKIND.SK_MethodSymbol || _pGroup.sk == SYMKIND.SK_PropertySymbol && 0 != (_pGroup.flags & EXPRFLAG.EXF_INDEXER));
                Debug.Assert(_pGroup.typeArgs.size == 0 || _pGroup.sk == SYMKIND.SK_MethodSymbol);

                // if this is a binding to finalize on object, then complain:
                if (_results.GetBestResult().MethProp().name == GetSymbolLoader().GetNameManager().GetPredefName(PredefinedName.PN_DTOR) &&
                    _results.GetBestResult().MethProp().getClass().isPredefAgg(PredefinedType.PT_OBJECT))
                {
                    if (0 != (_pGroup.flags & EXPRFLAG.EXF_BASECALL))
                    {
                        GetErrorContext().Error(ErrorCode.ERR_CallingBaseFinalizeDeprecated);
                    }
                    else
                    {
                        GetErrorContext().Error(ErrorCode.ERR_CallingFinalizeDepracated);
                    }
                }

                Debug.Assert(0 == (_pGroup.flags & EXPRFLAG.EXF_USERCALLABLE) || _results.GetBestResult().MethProp().isUserCallable());

                if (_pGroup.sk == SYMKIND.SK_MethodSymbol)
                {
                    Debug.Assert(_results.GetBestResult().MethProp().IsMethodSymbol());

                    if (_results.GetBestResult().TypeArgs.size > 0)
                    {
                        // Check method type variable constraints.
                        TypeBind.CheckMethConstraints(GetSemanticChecker(), GetErrorContext(), new MethWithInst(_results.GetBestResult()));
                    }
                }
            }

            private void ReportErrorsOnFailure()
            {
                // First and foremost, report if the user specified a name more than once.
                if (_pDuplicateSpecifiedName != null)
                {
                    GetErrorContext().Error(ErrorCode.ERR_DuplicateNamedArgument, _pDuplicateSpecifiedName);
                    return;
                }

                Debug.Assert(_methList.IsEmpty());
                // Report inaccessible.
                if (_results.GetInaccessibleResult())
                {
                    // We might have called this, but it is inaccessible...
                    GetSemanticChecker().ReportAccessError(_results.GetInaccessibleResult(), _pExprBinder.ContextForMemberLookup(), GetTypeQualifier(_pGroup));
                    return;
                }

                // Report bogus.
                if (_mpwiBogus)
                {
                    // We might have called this, but it is bogus...
                    GetErrorContext().ErrorRef(ErrorCode.ERR_BindToBogus, _mpwiBogus);
                    return;
                }

                bool bUseDelegateErrors = false;
                Name nameErr = _pGroup.name;

                // Check for an invoke.
                if (_pGroup.GetOptionalObject() != null &&
                        _pGroup.GetOptionalObject().type != null &&
                        _pGroup.GetOptionalObject().type.isDelegateType() &&
                        _pGroup.name == GetSymbolLoader().GetNameManager().GetPredefName(PredefinedName.PN_INVOKE))
                {
                    Debug.Assert(!_results.GetBestResult() || _results.GetBestResult().MethProp().getClass().IsDelegate());
                    Debug.Assert(!_results.GetBestResult() || _results.GetBestResult().GetType().getAggregate().IsDelegate());
                    bUseDelegateErrors = true;
                    nameErr = _pGroup.GetOptionalObject().type.getAggregate().name;
                }

                if (_results.GetBestResult())
                {
                    // If we had some invalid arguments for best matching.
                    ReportErrorsForBestMatching(bUseDelegateErrors, nameErr);
                }
                else if (_results.GetUninferableResult() || _mpwiCantInferInstArg)
                {
                    if (!_results.GetUninferableResult())
                    {
                        //copy the extension method for which instance argument type inference failed
                        _results.GetUninferableResult().Set(_mpwiCantInferInstArg.Sym.AsMethodSymbol(), _mpwiCantInferInstArg.GetType(), _mpwiCantInferInstArg.TypeArgs);
                    }
                    Debug.Assert(_results.GetUninferableResult().Sym.IsMethodSymbol());

                    MethodSymbol sym = _results.GetUninferableResult().Meth();
                    TypeArray pCurrentParameters = sym.Params;
                    // if we tried to bind to an extensionmethod and the instance argument Type Inference failed then the method does not exist
                    // on the type at all. this is treated as a lookup error
                    CType type = null;
                    if (_pGroup.GetOptionalObject() != null)
                    {
                        type = _pGroup.GetOptionalObject().type;
                    }
                    else if (_pGroup.GetOptionalLHS() != null)
                    {
                        type = _pGroup.GetOptionalLHS().type;
                    }

                    MethWithType mwtCantInfer = new MethWithType();
                    mwtCantInfer.Set(_results.GetUninferableResult().Meth(), _results.GetUninferableResult().GetType());
                    GetErrorContext().Error(ErrorCode.ERR_CantInferMethTypeArgs, mwtCantInfer);
                }
                else if (_mwtBadArity)
                {
                    int cvar = _mwtBadArity.Meth().typeVars.size;
                    GetErrorContext().ErrorRef(cvar > 0 ? ErrorCode.ERR_BadArity : ErrorCode.ERR_HasNoTypeVars, _mwtBadArity, new ErrArgSymKind(_mwtBadArity.Meth()), _pArguments.carg);
                }
                else if (_mpwiParamTypeConstraints)
                {
                    // This will always report an error
                    TypeBind.CheckMethConstraints(GetSemanticChecker(), GetErrorContext(), new MethWithInst(_mpwiParamTypeConstraints));
                }
                else if (_pInvalidSpecifiedName != null)
                {
                    // Give a better message for delegate invoke.
                    if (_pGroup.GetOptionalObject() != null &&
                            _pGroup.GetOptionalObject().type.IsAggregateType() &&
                            _pGroup.GetOptionalObject().type.AsAggregateType().GetOwningAggregate().IsDelegate())
                    {
                        GetErrorContext().Error(ErrorCode.ERR_BadNamedArgumentForDelegateInvoke, _pGroup.GetOptionalObject().type.AsAggregateType().GetOwningAggregate().name, _pInvalidSpecifiedName);
                    }
                    else
                    {
                        GetErrorContext().Error(ErrorCode.ERR_BadNamedArgument, _pGroup.name, _pInvalidSpecifiedName);
                    }
                }
                else if (_pNameUsedInPositionalArgument != null)
                {
                    GetErrorContext().Error(ErrorCode.ERR_NamedArgumentUsedInPositional, _pNameUsedInPositionalArgument);
                }
                else
                {
                    CParameterizedError error;

                    if (_pDelegate != null)
                    {
                        GetErrorContext().MakeError(out error, ErrorCode.ERR_MethDelegateMismatch, nameErr, _pDelegate);
                        GetErrorContext().AddRelatedTypeLoc(error, _pDelegate);
                    }
                    else
                    {
                        // The number of arguments must be wrong.

                        if (_fCandidatesUnsupported)
                        {
                            GetErrorContext().MakeError(out error, ErrorCode.ERR_BindToBogus, nameErr);
                        }
                        else if (bUseDelegateErrors)
                        {
                            Debug.Assert(0 == (_pGroup.flags & EXPRFLAG.EXF_CTOR));
                            GetErrorContext().MakeError(out error, ErrorCode.ERR_BadDelArgCount, nameErr, _pArguments.carg);
                        }
                        else
                        {
                            if (0 != (_pGroup.flags & EXPRFLAG.EXF_CTOR))
                            {
                                Debug.Assert(!_pGroup.GetParentType().IsTypeParameterType());
                                GetErrorContext().MakeError(out error, ErrorCode.ERR_BadCtorArgCount, _pGroup.GetParentType(), _pArguments.carg);
                            }
                            else
                            {
                                GetErrorContext().MakeError(out error, ErrorCode.ERR_BadArgCount, nameErr, _pArguments.carg);
                            }
                        }
                    }

                    // Report possible matches (same name and is accessible). We stored these in m_swtWrongCount.
                    for (int i = 0; i < _nWrongCount; i++)
                    {
                        if (GetSemanticChecker().CheckAccess(
                                    _swtWrongCount[i].Sym,
                                    _swtWrongCount[i].GetType(),
                                    _pExprBinder.ContextForMemberLookup(),
                                    GetTypeQualifier(_pGroup)))
                        {
                            GetErrorContext().AddRelatedSymLoc(error, _swtWrongCount[i].Sym);
                        }
                    }
                    GetErrorContext().SubmitError(error);
                }
            }
            private void ReportErrorsForBestMatching(bool bUseDelegateErrors, Name nameErr)
            {
                // Best matching overloaded method 'name' had some invalid arguments.
                if (_pDelegate != null)
                {
                    GetErrorContext().ErrorRef(ErrorCode.ERR_MethDelegateMismatch, nameErr, _pDelegate, _results.GetBestResult());
                    return;
                }

                if (_bBindingCollectionAddArgs)
                {
                    if (ReportErrorsForCollectionAdd())
                    {
                        return;
                    }
                }

                if (bUseDelegateErrors)
                {
                    // Point to the Delegate, not the Invoke method
                    GetErrorContext().Error(ErrorCode.ERR_BadDelArgTypes, _results.GetBestResult().GetType());
                }
                else
                {
                    if (_results.GetBestResult().Sym.IsMethodSymbol() && _results.GetBestResult().Sym.AsMethodSymbol().IsExtension() && _pGroup.GetOptionalObject() != null)
                    {
                        GetErrorContext().Error(ErrorCode.ERR_BadExtensionArgTypes, _pGroup.GetOptionalObject().type, _pGroup.name, _results.GetBestResult().Sym);
                    }
                    else if (_bBindingCollectionAddArgs)
                    {
                        GetErrorContext().Error(ErrorCode.ERR_BadArgTypesForCollectionAdd, _results.GetBestResult());
                    }
                    else
                    {
                        GetErrorContext().Error(ErrorCode.ERR_BadArgTypes, _results.GetBestResult());
                    }
                }

                // Argument X: cannot convert type 'Y' to type 'Z'
                for (int ivar = 0; ivar < _pArguments.carg; ivar++)
                {
                    CType var = _pBestParameters.Item(ivar);

                    if (!_pExprBinder.canConvert(_pArguments.prgexpr[ivar], var))
                    {
                        // See if they just differ in out / ref.
                        CType argStripped = _pArguments.types.Item(ivar).IsParameterModifierType() ?
                            _pArguments.types.Item(ivar).AsParameterModifierType().GetParameterType() : _pArguments.types.Item(ivar);
                        CType varStripped = var.IsParameterModifierType() ? var.AsParameterModifierType().GetParameterType() : var;
                        if (argStripped == varStripped)
                        {
                            if (varStripped != var)
                            {
                                // The argument is wrong in ref / out-ness.
                                GetErrorContext().Error(ErrorCode.ERR_BadArgRef, ivar + 1, (var.IsParameterModifierType() && var.AsParameterModifierType().isOut) ? "out" : "ref");
                            }
                            else
                            {
                                CType argument = _pArguments.types.Item(ivar);

                                // the argument is decorated, but doesn't needs a 'ref' or 'out'
                                GetErrorContext().Error(ErrorCode.ERR_BadArgExtraRef, ivar + 1, (argument.IsParameterModifierType() && argument.AsParameterModifierType().isOut) ? "out" : "ref");
                            }
                        }
                        else
                        {
                            // if we tried to bind to an extensionmethod and the instance argument conversion failed then the method does not exist
                            // on the type at all. 
                            Symbol sym = _results.GetBestResult().Sym;
                            if (ivar == 0 && sym.IsMethodSymbol() && sym.AsMethodSymbol().IsExtension() && _pGroup.GetOptionalObject() != null &&
                                !_pExprBinder.canConvertInstanceParamForExtension(_pGroup.GetOptionalObject(), sym.AsMethodSymbol().Params.Item(0)))
                            {
                                if (!_pGroup.GetOptionalObject().type.getBogus())
                                {
                                    GetErrorContext().Error(ErrorCode.ERR_BadInstanceArgType, _pGroup.GetOptionalObject().type, var);
                                }
                            }
                            else
                            {
                                GetErrorContext().Error(ErrorCode.ERR_BadArgType, ivar + 1, new ErrArg(_pArguments.types.Item(ivar), ErrArgFlags.Unique), new ErrArg(var, ErrArgFlags.Unique));
                            }
                        }
                    }
                }
            }

            private bool ReportErrorsForCollectionAdd()
            {
                for (int ivar = 0; ivar < _pArguments.carg; ivar++)
                {
                    CType var = _pBestParameters.Item(ivar);
                    if (var.IsParameterModifierType())
                    {
                        GetErrorContext().ErrorRef(ErrorCode.ERR_InitializerAddHasParamModifiers, _results.GetBestResult());
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
