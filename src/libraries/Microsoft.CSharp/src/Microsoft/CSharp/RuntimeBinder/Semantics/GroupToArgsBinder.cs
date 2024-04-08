// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // This class takes an EXPRMEMGRP and a set of arguments and binds the arguments
    // to the best applicable method in the group.
    // ----------------------------------------------------------------------------

    internal readonly partial struct ExpressionBinder
    {
        internal sealed class GroupToArgsBinder
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
            private readonly ExprMemberGroup _pGroup;
            private readonly ArgInfos _pArguments;
            private readonly ArgInfos _pOriginalArguments;
            private readonly NamedArgumentsKind _namedArgumentsKind;
            private AggregateType _pCurrentType;
            private MethodOrPropertySymbol _pCurrentSym;
            private TypeArray _pCurrentTypeArgs;
            private TypeArray _pCurrentParameters;
            private int _nArgBest;
            // end of current namespaces extension method list
            private readonly GroupToArgsBinderResult _results;
            private readonly List<CandidateFunctionMember> _methList;
            private readonly MethPropWithInst _mpwiParamTypeConstraints;
            private readonly MethPropWithInst _mpwiBogus;
            private readonly MethPropWithInst _misnamed;
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

            public GroupToArgsBinder(ExpressionBinder exprBinder, BindingFlag bindFlags, ExprMemberGroup grp, ArgInfos args, ArgInfos originalArgs, NamedArgumentsKind namedArgumentsKind)
            {
                Debug.Assert(grp != null);
                Debug.Assert(args != null);

                _pExprBinder = exprBinder;
                _fCandidatesUnsupported = false;
                _fBindFlags = bindFlags;
                _pGroup = grp;
                _pArguments = args;
                _pOriginalArguments = originalArgs;
                _namedArgumentsKind = namedArgumentsKind;
                _pCurrentType = null;
                _pCurrentSym = null;
                _pCurrentTypeArgs = null;
                _pCurrentParameters = null;
                _nArgBest = -1;
                _results = new GroupToArgsBinderResult();
                _methList = new List<CandidateFunctionMember>();
                _mpwiParamTypeConstraints = new MethPropWithInst();
                _mpwiBogus = new MethPropWithInst();
                _misnamed = new MethPropWithInst();
                _mpwiCantInferInstArg = new MethPropWithInst();
                _mwtBadArity = new MethWithType();
                _HiddenTypes = new List<CType>();
            }

            // ----------------------------------------------------------------------------
            // This method does the actual binding.
            // ----------------------------------------------------------------------------

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            public void Bind()
            {
                Debug.Assert(_pGroup.SymKind == SYMKIND.SK_MethodSymbol || _pGroup.SymKind == SYMKIND.SK_PropertySymbol && 0 != (_pGroup.Flags & EXPRFLAG.EXF_INDEXER));

                LookForCandidates();
                if (!GetResultOfBind())
                {
                    throw ReportErrorsOnFailure();
                }
            }

            public GroupToArgsBinderResult GetResultsOfBind() => _results;

            private static CType GetTypeQualifier(ExprMemberGroup pGroup)
            {
                Debug.Assert(pGroup != null);

                return (pGroup.Flags & EXPRFLAG.EXF_CTOR) != 0 ? pGroup.ParentType : pGroup.OptionalObject?.Type;
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private void LookForCandidates()
            {
                bool fExpanded = false;
                bool bSearchForExpanded = true;
                bool allCandidatesUnsupported = true;
                bool lookedAtCandidates = false;

                // Calculate the mask based on the type of the sym we've found so far.  This
                // is to ensure that if we found a propsym (or methsym, or whatever) the
                // iterator will only return propsyms (or methsyms, or whatever)
                symbmask_t mask = (symbmask_t)(1 << (int)_pGroup.SymKind);

                CMemberLookupResults.CMethodIterator iterator = _pGroup.MemberLookupResults.GetMethodIterator(GetTypeQualifier(_pGroup), _pExprBinder.ContextForMemberLookup, _pGroup.TypeArgs.Count, _pGroup.Flags, mask, _namedArgumentsKind == NamedArgumentsKind.NonTrailing ? _pOriginalArguments : null);
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
                    // If we don't have Exprs, its because we're doing a method group conversion.
                    // In those scenarios, we never want to add named arguments or optional arguments.
                    if (_namedArgumentsKind == NamedArgumentsKind.Positioning)
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

                    if (!bFoundExpanded)
                    {
                        lookedAtCandidates = true;
                        allCandidatesUnsupported &= CSemanticChecker.CheckBogus(_pCurrentSym);

                        // If we have the wrong number of arguments and still have room in our cache of 20,
                        // then store it in our cache and go to the next sym.
                        if (_pCurrentParameters.Count != _pArguments.carg)
                        {
                            bSearchForExpanded = true;
                            continue;
                        }
                    }

                    // If we cant use the current symbol, then we've filtered it, so get the next one.

                    if (!iterator.CanUseCurrentSymbol)
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
                    bool fCanAccess = !iterator.IsCurrentSymbolInaccessible;
                    if (!fCanAccess && (!_methList.IsEmpty() || _results.InaccessibleResult))
                    {
                        // We'll never use this one for error reporting anyway, so just skip it.
                        bSearchForExpanded = false;
                        continue;
                    }

                    // Check misnamed.
                    bool misnamed = fCanAccess && iterator.IsCurrentSymbolMisnamed;
                    if (misnamed && (!_methList.IsEmpty() || _results.InaccessibleResult || _misnamed))
                    {
                        bSearchForExpanded = false;
                        continue;
                    }

                    // Check bogus.
                    bool fBogus = fCanAccess && !misnamed && iterator.IsCurrentSymbolBogus;
                    if (fBogus && (!_methList.IsEmpty() || _results.InaccessibleResult || _mpwiBogus || _misnamed))
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
                        Debug.Assert(!_results.InaccessibleResult);
                        _results.InaccessibleResult.Set(_pCurrentSym, _pCurrentType, _pCurrentTypeArgs);
                    }
                    else if (misnamed)
                    {
                        Debug.Assert(!_misnamed);
                        _misnamed.Set(_pCurrentSym, _pCurrentType, _pCurrentTypeArgs);
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

                        if (_pCurrentType.IsInterfaceType)
                        {
                            foreach (AggregateType type in _pCurrentType.IfacesAll.Items)
                            {
                                Debug.Assert(type.IsInterfaceType);
                                _HiddenTypes.Add(type);
                            }

                            // Mark object.
                            AggregateType typeObject = SymbolLoader.GetPredefindType(PredefinedType.PT_OBJECT);
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

            private static void CopyArgInfos(ArgInfos src, ArgInfos dst)
            {
                dst.carg = src.carg;
                dst.types = src.types;
                dst.prgexpr.Clear();
                for (int i = 0; i < src.prgexpr.Count; i++)
                {
                    dst.prgexpr.Add(src.prgexpr[i]);
                }
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool GetResultOfBind()
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
                        CType pTypeThrough = _pGroup.OptionalObject?.Type;
                        pmethBest = _pExprBinder.FindBestMethod(_methList, pTypeThrough, _pArguments, out CandidateFunctionMember pAmbig1, out CandidateFunctionMember pAmbig2);

                        if (null == pmethBest)
                        {
                            if (pAmbig1.@params != pAmbig2.@params ||
                                pAmbig1.mpwi.MethProp().Params.Count != pAmbig2.mpwi.MethProp().Params.Count ||
                                pAmbig1.mpwi.TypeArgs != pAmbig2.mpwi.TypeArgs ||
                                pAmbig1.mpwi.GetType() != pAmbig2.mpwi.GetType() ||
                                pAmbig1.mpwi.MethProp().Params == pAmbig2.mpwi.MethProp().Params)
                            {
                                throw ErrorHandling.Error(ErrorCode.ERR_AmbigCall, pAmbig1.mpwi, pAmbig2.mpwi);
                            }

                            // The two signatures are identical so don't use the type args in the error message.
                            throw ErrorHandling.Error(ErrorCode.ERR_AmbigCall, pAmbig1.mpwi.MethProp(), pAmbig2.mpwi.MethProp());
                        }
                    }

                    // This is the "success" exit path.
                    Debug.Assert(pmethBest != null);
                    _results.BestResult = pmethBest.mpwi;

                    // Record our best match in the memgroup as well. This is temporary.

                    ReportErrorsOnSuccess();

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

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool ReOrderArgsForNamedArguments()
            {
                // First we need to find the method that we're actually trying to call.
                MethodOrPropertySymbol methprop = FindMostDerivedMethod(_pCurrentSym, _pGroup.OptionalObject);
                if (methprop == null)
                {
                    return false;
                }

                int numParameters = _pCurrentParameters.Count;

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
                    methprop, _pCurrentParameters, _pCurrentType, _pGroup, _pArguments);
                return _bArgumentsChangedForNamedOrOptionalArguments;
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            internal static bool ReOrderArgsForNamedArguments(
                MethodOrPropertySymbol methprop, TypeArray pCurrentParameters, AggregateType pCurrentType, ExprMemberGroup pGroup, ArgInfos pArguments)
            {
                // We use the param count from pCurrentParameters because they may have been resized
                // for param arrays.
                int numParameters = pCurrentParameters.Count;

                Expr[] pExprArguments = new Expr[numParameters];

                // Now go through the parameters. First set all positional arguments in the new argument
                // set, then for the remainder, look for a named argument with a matching name.
                int index = 0;
                Expr paramArrayArgument = null;
                TypeArray @params = TypeManager.SubstTypeArray(
                    pCurrentParameters,
                    pCurrentType,
                    pGroup.TypeArgs);
                foreach (Name name in methprop.ParameterNames)
                {
                    // This can happen if we had expanded our param array to size 0.
                    if (index >= pCurrentParameters.Count)
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
                        pArguments.prgexpr[index] is ExprArrayInit arrayInit && arrayInit.GeneratedForParamArray)
                    {
                        paramArrayArgument = pArguments.prgexpr[index];
                    }

                    // Positional.
                    if (index < pArguments.carg &&
                        !(pArguments.prgexpr[index] is ExprNamedArgumentSpecification) &&
                        !(pArguments.prgexpr[index] is ExprArrayInit arrayInitPos && arrayInitPos.GeneratedForParamArray))
                    {
                        pExprArguments[index] = pArguments.prgexpr[index++];
                        continue;
                    }

                    // Look for names.
                    Expr pNewArg = FindArgumentWithName(pArguments, name);
                    if (pNewArg == null)
                    {
                        if (methprop.IsParameterOptional(index))
                        {
                            pNewArg = GenerateOptionalArgument(methprop, @params[index], index);
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
                CType[] prgTypes = new CType[pCurrentParameters.Count];
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
                    prgTypes[i] = pArguments.prgexpr[i].Type;
                }
                pArguments.carg = pCurrentParameters.Count;
                pArguments.types = TypeArray.Allocate(prgTypes);
                return true;
            }

            /////////////////////////////////////////////////////////////////////////////////

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private static Expr GenerateOptionalArgument(MethodOrPropertySymbol methprop, CType type, int index)
            {
                CType pParamType = type;
                CType pRawParamType = type.StripNubs();

                Expr optionalArgument;
                if (methprop.HasDefaultParameterValue(index))
                {
                    CType pConstValType = methprop.GetDefaultParameterValueConstValType(index);
                    ConstVal cv = methprop.GetDefaultParameterValue(index);

                    if (pConstValType.IsPredefType(PredefinedType.PT_DATETIME) &&
                        (pRawParamType.IsPredefType(PredefinedType.PT_DATETIME) || pRawParamType.IsPredefType(PredefinedType.PT_OBJECT) || pRawParamType.IsPredefType(PredefinedType.PT_VALUE)))
                    {
                        // This is the specific case where we want to create a DateTime
                        // but the constval that stores it is a long.

                        AggregateType dateTimeType = SymbolLoader.GetPredefindType(PredefinedType.PT_DATETIME);
                        optionalArgument = ExprFactory.CreateConstant(dateTimeType, ConstVal.Get(DateTime.FromBinary(cv.Int64Val)));
                    }
                    else if (pConstValType.IsSimpleOrEnumOrString)
                    {
                        // In this case, the constval is a simple type (all the numerics, including
                        // decimal), or an enum or a string. This covers all the substantial values,
                        // and everything else that can be encoded is just null or default(something).

                        // For enum parameters, we create a constant of the enum type. For everything
                        // else, we create the appropriate constant.

                        optionalArgument = ExprFactory.CreateConstant(
                            pRawParamType.IsEnumType && pConstValType == pRawParamType.UnderlyingEnumType
                                ? pRawParamType
                                : pConstValType,
                            cv);
                    }
                    else if ((pParamType.IsReferenceType || pParamType is NullableType) && cv.IsNullRef)
                    {
                        // We have an "= null" default value with a reference type or a nullable type.

                        optionalArgument = ExprFactory.CreateNull();
                    }
                    else
                    {
                        // We have a default value that is encoded as a nullref, and that nullref is
                        // interpreted as default(something). For instance, the pParamType could be
                        // a type parameter type or a non-simple value type.

                        optionalArgument = ExprFactory.CreateZeroInit(pParamType);
                    }
                }
                else
                {
                    // There was no default parameter specified, so generally use default(T),
                    // except for some cases when the parameter type in metatdata is object.

                    if (pParamType.IsPredefType(PredefinedType.PT_OBJECT))
                    {
                        if (methprop.MarshalAsObject(index))
                        {
                            // For [opt] parameters of type object, if we have marshal(iunknown),
                            // marshal(idispatch), or marshal(interface), then we emit a null.

                            optionalArgument = ExprFactory.CreateNull();
                        }
                        else
                        {
                            // Otherwise, we generate Type.Missing

                            AggregateSymbol agg = SymbolLoader.GetPredefAgg(PredefinedType.PT_MISSING);
                            Name name = NameManager.GetPredefinedName(PredefinedName.PN_CAP_VALUE);
                            FieldSymbol field = SymbolLoader.LookupAggMember(name, agg, symbmask_t.MASK_FieldSymbol) as FieldSymbol;
                            FieldWithType fwt = new FieldWithType(field, agg.getThisType());
                            ExprField exprField = ExprFactory.CreateField(agg.getThisType(), null, fwt);
                            optionalArgument = ExprFactory.CreateCast(type, exprField);
                        }
                    }
                    else
                    {
                        // Every type aside from object that doesn't have a default value gets
                        // its default value.

                        optionalArgument = ExprFactory.CreateZeroInit(pParamType);
                    }
                }

                Debug.Assert(optionalArgument != null);
                optionalArgument.IsOptionalArgument = true;
                return optionalArgument;
            }

            private static MethodOrPropertySymbol FindMostDerivedMethod(MethodOrPropertySymbol pMethProp, Expr pObject) =>
                FindMostDerivedMethod(pMethProp, pObject?.Type);

            public static MethodOrPropertySymbol FindMostDerivedMethod(MethodOrPropertySymbol pMethProp, CType pType)
            {
                bool bIsIndexer = false;

                if (!(pMethProp is MethodSymbol method))
                {
                    PropertySymbol prop = (PropertySymbol)pMethProp;
                    method = prop.GetterMethod ?? prop.SetterMethod;
                    if (method == null)
                    {
                        return null;
                    }

                    bIsIndexer = prop is IndexerSymbol;
                }

                if (!method.isVirtual || pType == null)
                {
                    // if pType is null, this must be a static call.
                    return method;
                }

                // Now get the slot method.
                var slotMethod = method.swtSlot?.Meth();
                if (slotMethod != null)
                {
                    method = slotMethod;
                }

                if (!(pType is AggregateType agg))
                {
                    // Not something that can have overrides anyway.
                    return method;
                }

                for (AggregateSymbol pAggregate = agg.OwningAggregate;
                        pAggregate?.GetBaseAgg() != null;
                        pAggregate = pAggregate.GetBaseAgg())
                {
                    for (MethodOrPropertySymbol meth = SymbolLoader.LookupAggMember(method.name, pAggregate, symbmask_t.MASK_MethodSymbol | symbmask_t.MASK_PropertySymbol) as MethodOrPropertySymbol;
                            meth != null;
                            meth = meth.LookupNext(symbmask_t.MASK_MethodSymbol | symbmask_t.MASK_PropertySymbol) as MethodOrPropertySymbol)
                    {
                        if (!meth.isOverride)
                        {
                            continue;
                        }
                        if (meth.swtSlot.Sym != null && meth.swtSlot.Sym == method)
                        {
                            if (bIsIndexer)
                            {
                                Debug.Assert(meth is MethodSymbol);
                                return ((MethodSymbol)meth).getProperty();
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
                // an override, but we won't have the slots set up correctly, and will
                // not find the base type in the inheritance hierarchy. The second is that
                // we're calling off of the base itself.
                Debug.Assert(method.parent is AggregateSymbol);
                return method;
            }


            /////////////////////////////////////////////////////////////////////////////////

            private bool HasOptionalParameters()
            {
                MethodOrPropertySymbol methprop = FindMostDerivedMethod(_pCurrentSym, _pGroup.OptionalObject);
                return methprop != null && methprop.HasOptionalParameters();
            }

            /////////////////////////////////////////////////////////////////////////////////
            // Returns true if we can either add enough optional parameters to make the
            // argument list match, or if we don't need to at all.

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool AddArgumentsForOptionalParameters()
            {
                if (_pCurrentParameters.Count <= _pArguments.carg)
                {
                    // If we have enough arguments, or too many, no need to add any optionals here.
                    return true;
                }

                // First we need to find the method that we're actually trying to call.
                MethodOrPropertySymbol methprop = FindMostDerivedMethod(_pCurrentSym, _pGroup.OptionalObject);
                if (methprop == null)
                {
                    return false;
                }

                // If we're here, we know we're not in a named argument case. As such, we can
                // just generate defaults for every missing argument.
                int i = _pArguments.carg;
                int index = 0;
                TypeArray @params = TypeManager.SubstTypeArray(
                    _pCurrentParameters,
                    _pCurrentType,
                    _pGroup.TypeArgs);
                Expr[] pArguments = new Expr[_pCurrentParameters.Count - i];
                for (; i < @params.Count; i++, index++)
                {
                    if (!methprop.IsParameterOptional(i))
                    {
                        // We don't have an optional here, but we need to fill it in.
                        return false;
                    }

                    pArguments[index] = GenerateOptionalArgument(methprop, @params[i], i);
                }

                // Success. Lets copy them in now.
                for (int n = 0; n < index; n++)
                {
                    _pArguments.prgexpr.Add(pArguments[n]);
                }
                CType[] prgTypes = new CType[@params.Count];
                for (int n = 0; n < @params.Count; n++)
                {
                    prgTypes[n] = _pArguments.prgexpr[n].Type;
                }
                _pArguments.types = TypeArray.Allocate(prgTypes);
                _pArguments.carg = @params.Count;
                _bArgumentsChangedForNamedOrOptionalArguments = true;
                return true;
            }

            /////////////////////////////////////////////////////////////////////////////////

            private static Expr FindArgumentWithName(ArgInfos pArguments, Name pName)
            {
                List<Expr> prgexpr = pArguments.prgexpr;
                for (int i = 0; i < pArguments.carg; i++)
                {
                    Expr expr = prgexpr[i];
                    if (expr is ExprNamedArgumentSpecification named && named.Name == pName)
                    {
                        return expr;
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
                    if (!(_pArguments.prgexpr[i] is ExprNamedArgumentSpecification named))
                    {
                        if (!currentPosition.IsEmpty())
                        {
                            currentPosition = currentPosition.Tail();
                        }
                        continue;
                    }

                    Name name = named.Name;
                    if (!methprop.ParameterNames.Contains(name))
                    {
                        _pInvalidSpecifiedName ??= name;
                        return false;
                    }
                    else if (!currentPosition.Contains(name))
                    {
                        _pNameUsedInPositionalArgument ??= name;
                        return false;
                    }

                    if (!names.Add(name))
                    {
                        _pDuplicateSpecifiedName ??= name;
                        return false;
                    }
                }
                return true;
            }

            // This method returns true if we have another sym to consider.
            // If we've found a match in the current type, and have no more syms to consider in this type, then we
            // return false.
            private bool GetNextSym(CMemberLookupResults.CMethodIterator iterator)
            {
                if (!iterator.MoveNext())
                {
                    return false;
                }
                _pCurrentSym = iterator.CurrentSymbol;
                AggregateType type = iterator.CurrentType;

                // If our current type is null, this is our first iteration, so set the type.
                // If our current type is not null, and we've got a new type now, and we've already matched
                // a symbol, then bail out.

                if (_pCurrentType != type &&
                        _pCurrentType != null &&
                        !_methList.IsEmpty() &&
                        !_methList.Head().mpwi.GetType().IsInterfaceType)
                {
                    return false;
                }

                _pCurrentType = type;

                // We have a new type. If this type is hidden, we need another type.

                while (_HiddenTypes.Contains(_pCurrentType))
                {
                    // Move through this type and get the next one.
                    for (; iterator.CurrentType == _pCurrentType; iterator.MoveNext()) ;
                    _pCurrentSym = iterator.CurrentSymbol;
                    _pCurrentType = iterator.CurrentType;

                    if (iterator.AtEnd)
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
                    for (int i = _pArguments.carg; i < _pCurrentSym.Params.Count; i++)
                    {
                        if (_pCurrentSym.IsParameterOptional(i))
                        {
                            numOptionals++;
                        }
                    }
                    if (_pArguments.carg + numOptionals < _pCurrentParameters.Count - 1)
                    {
                        return false;
                    }
                }

                Debug.Assert(_methList.IsEmpty() || _methList.Head().mpwi.MethProp() != _pCurrentSym);
                // Construct the expanded params.
                return TryGetExpandedParams(_pCurrentSym.Params, _pArguments.carg, out _pCurrentParameters);
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private Result DetermineCurrentTypeArgs()
            {
                TypeArray typeArgs = _pGroup.TypeArgs;

                // Get the type args.
                if (_pCurrentSym is MethodSymbol methSym && methSym.typeVars.Count != typeArgs.Count)
                {
                    // Can't infer if some type args are specified.
                    if (typeArgs.Count > 0)
                    {
                        if (!_mwtBadArity)
                        {
                            _mwtBadArity.Set(methSym, _pCurrentType);
                        }
                        return Result.Failure_NoSearchForExpanded;
                    }
                    Debug.Assert(methSym.typeVars.Count > 0);

                    // Try to infer. If we have an errorsym in the type arguments, we know we cant infer,
                    // but we want to attempt it anyway. We'll mark this as "cant infer" so that we can
                    // report the appropriate error, but we'll continue inferring, since we want
                    // error sym to go to any type.

                    bool inferenceSucceeded = MethodTypeInferrer.Infer(
                        _pExprBinder, methSym, _pCurrentParameters, _pArguments, out _pCurrentTypeArgs);

                    if (!inferenceSucceeded)
                    {
                        if (_results.IsBetterUninferableResult(_pCurrentTypeArgs))
                        {
                            TypeArray pTypeVars = methSym.typeVars;
                            if (pTypeVars != null && _pCurrentTypeArgs != null && pTypeVars.Count == _pCurrentTypeArgs.Count)
                            {
                                _mpwiCantInferInstArg.Set(methSym, _pCurrentType, _pCurrentTypeArgs);
                            }
                            else
                            {
                                _mpwiCantInferInstArg.Set(methSym, _pCurrentType, pTypeVars);
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

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private bool ArgumentsAreConvertible()
            {
                bool containsErrorSym = false;
                if (_pArguments.carg != 0)
                {
                    UpdateArguments();
                    for (int ivar = 0; ivar < _pArguments.carg; ivar++)
                    {
                        CType var = _pCurrentParameters[ivar];
                        bool constraintErrors = !TypeBind.CheckConstraints(var, CheckConstraintsFlags.NoErrors);
                        if (constraintErrors && !DoesTypeArgumentsContainErrorSym(var))
                        {
                            _mpwiParamTypeConstraints.Set(_pCurrentSym, _pCurrentType, _pCurrentTypeArgs);
                            return false;
                        }
                    }

                    for (int ivar = 0; ivar < _pArguments.carg; ivar++)
                    {
                        CType var = _pCurrentParameters[ivar];
                        containsErrorSym |= DoesTypeArgumentsContainErrorSym(var);
                        bool fresult;

                        Expr pArgument = _pArguments.prgexpr[ivar];

                        // If we have a named argument, strip it to do the conversion.
                        if (pArgument is ExprNamedArgumentSpecification named)
                        {
                            pArgument = named.Value;
                        }

                        fresult = _pExprBinder.canConvert(pArgument, var);

                        // Mark this as a legitimate error if we didn't have any error syms.
                        if (!fresult && !containsErrorSym)
                        {
                            if (ivar > _nArgBest)
                            {
                                _nArgBest = ivar;

                                // If we already have best method for instance methods don't overwrite with extensions
                                if (!_results.BestResult)
                                {
                                    _results.BestResult.Set(_pCurrentSym, _pCurrentType, _pCurrentTypeArgs);
                                }
                            }
                            else if (ivar == _nArgBest && _pArguments.types[ivar] != var)
                            {
                                // this is to eliminate the paranoid case of types that are equal but can't convert
                                // (think ErrorType != ErrorType)
                                // See if they just differ in out / ref.
                                CType argStripped = _pArguments.types[ivar] is ParameterModifierType modArg ?
                                    modArg.ParameterType : _pArguments.types[ivar];
                                CType varStripped = var is ParameterModifierType modVar ? modVar.ParameterType : var;

                                if (argStripped == varStripped)
                                {
                                    // If we already have best method for instance methods don't overwrite with extensions
                                    if (!_results.BestResult)
                                    {
                                        _results.BestResult.Set(_pCurrentSym, _pCurrentType, _pCurrentTypeArgs);
                                    }
                                }
                            }

                            return false;
                        }
                    }
                }

                if (containsErrorSym)
                {
                    if (_results.IsBetterUninferableResult(_pCurrentTypeArgs) && _pCurrentSym is MethodSymbol meth)
                    {
                        // If we're an instance method then mark us down.
                        _results.UninferableResult.Set(meth, _pCurrentType, _pCurrentTypeArgs);
                    }

                    return false;
                }

                return true;
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private void UpdateArguments()
            {
                // Parameter types might have changed as a result of
                // method type inference.

                _pCurrentParameters = TypeManager.SubstTypeArray(
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
                for (int iParam = 0; iParam < _pCurrentParameters.Count; ++iParam)
                {
                    Expr pArgument = _pArguments.prgexpr[iParam];
                    if (!pArgument.IsOptionalArgument)
                    {
                        continue;
                    }
                    CType pType = _pCurrentParameters[iParam];

                    if (pType == pArgument.Type)
                    {
                        continue;
                    }

                    // Argument has changed its type because of method type inference. Recompute it.
                    if (pMethod == null)
                    {
                        pMethod = FindMostDerivedMethod(_pCurrentSym, _pGroup.OptionalObject);
                        Debug.Assert(pMethod != null);
                    }
                    Debug.Assert(pMethod.IsParameterOptional(iParam));
                    Expr pArgumentNew = GenerateOptionalArgument(pMethod, _pCurrentParameters[iParam], iParam);
                    _pArguments.prgexpr[iParam] = pArgumentNew;
                }
            }

            private static bool DoesTypeArgumentsContainErrorSym(CType var)
            {
                if (!(var is AggregateType varAgg))
                {
                    return false;
                }

                TypeArray typeVars = varAgg.TypeArgsAll;
                for (int i = 0; i < typeVars.Count; i++)
                {
                    CType type = typeVars[i];
                    if (type == null)
                    {
                        return true;
                    }
                    else if (type is AggregateType)
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

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private void ReportErrorsOnSuccess()
            {
                // used for Methods and Indexers
                Debug.Assert(_pGroup.SymKind == SYMKIND.SK_MethodSymbol || _pGroup.SymKind == SYMKIND.SK_PropertySymbol && 0 != (_pGroup.Flags & EXPRFLAG.EXF_INDEXER));
                Debug.Assert(_pGroup.TypeArgs.Count == 0 || _pGroup.SymKind == SYMKIND.SK_MethodSymbol);
                Debug.Assert(0 == (_pGroup.Flags & EXPRFLAG.EXF_USERCALLABLE) || _results.BestResult.MethProp().isUserCallable());

                if (_pGroup.SymKind == SYMKIND.SK_MethodSymbol)
                {
                    Debug.Assert(_results.BestResult.MethProp() is MethodSymbol);

                    if (_results.BestResult.TypeArgs.Count > 0)
                    {
                        // Check method type variable constraints.
                        TypeBind.CheckMethConstraints(new MethWithInst(_results.BestResult));
                    }
                }
            }

            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            private RuntimeBinderException ReportErrorsOnFailure()
            {
                // First and foremost, report if the user specified a name more than once.
                if (_pDuplicateSpecifiedName != null)
                {
                    return ErrorHandling.Error(ErrorCode.ERR_DuplicateNamedArgument, _pDuplicateSpecifiedName);
                }

                Debug.Assert(_methList.IsEmpty());
                // Report inaccessible.
                if (_results.InaccessibleResult)
                {
                    // We might have called this, but it is inaccessible...
                    return CSemanticChecker.ReportAccessError(_results.InaccessibleResult, _pExprBinder.ContextForMemberLookup, GetTypeQualifier(_pGroup));
                }

                if (_misnamed)
                {
                    // Get exception immediately for the non-trailing named argument being misplaced.
                    // Handle below for the name not being present at all.
                    List<Name> paramNames = FindMostDerivedMethod(_misnamed.MethProp(), _pGroup.OptionalObject).ParameterNames;
                    for (int i = 0; ; ++i)
                    {
                        if (i == _pOriginalArguments.carg)
                        {
                            // If we're here we had the correct name used for the first params argument.
                            // Report it as not matching the correct number of arguments below.
                            break;
                        }

                        if (_pOriginalArguments.prgexpr[i] is ExprNamedArgumentSpecification named)
                        {
                            Name name = named.Name;
                            if (paramNames[i] != name)
                            {
                                // We have the bad name. Is it misplaced or absent?
                                if (paramNames.Contains(name))
                                {
                                    return ErrorHandling.Error(ErrorCode.ERR_BadNonTrailingNamedArgument, name);
                                }

                                // Let this be handled by _pInvalidSpecifiedName handling.
                                _pInvalidSpecifiedName = name;
                                break;
                            }
                        }
                    }
                }
                else if (_mpwiBogus)
                {
                    // We might have called this, but it is bogus...
                    return ErrorHandling.Error(ErrorCode.ERR_BindToBogus, _mpwiBogus);
                }

                bool bUseDelegateErrors = false;
                Name nameErr = _pGroup.Name;

                // Check for an invoke.
                if (_pGroup.OptionalObject?.Type != null &&
                    _pGroup.OptionalObject.Type.IsDelegateType &&
                    _pGroup.Name == NameManager.GetPredefinedName(PredefinedName.PN_INVOKE))
                {
                    Debug.Assert(!_results.BestResult || _results.BestResult.MethProp().getClass().IsDelegate());
                    Debug.Assert(!_results.BestResult || _results.BestResult.GetType().OwningAggregate.IsDelegate());
                    bUseDelegateErrors = true;
                    nameErr = ((AggregateType)_pGroup.OptionalObject.Type).OwningAggregate.name;
                }

                if (_results.BestResult)
                {
                    // If we had some invalid arguments for best matching.
                    return ReportErrorsForBestMatching(bUseDelegateErrors);
                }

                if (_results.UninferableResult || _mpwiCantInferInstArg)
                {
                    if (!_results.UninferableResult)
                    {
                        //copy the extension method for which instance argument type inference failed
                        _results.UninferableResult.Set(_mpwiCantInferInstArg.Sym as MethodSymbol, _mpwiCantInferInstArg.GetType(), _mpwiCantInferInstArg.TypeArgs);
                    }
                    Debug.Assert(_results.UninferableResult.Sym is MethodSymbol);

                    MethWithType mwtCantInfer = new MethWithType(_results.UninferableResult.Meth(), _results.UninferableResult.GetType());
                    return ErrorHandling.Error(ErrorCode.ERR_CantInferMethTypeArgs, mwtCantInfer);
                }

                if (_mwtBadArity)
                {
                    int cvar = _mwtBadArity.Meth().typeVars.Count;
                    return ErrorHandling.Error(cvar > 0 ? ErrorCode.ERR_BadArity : ErrorCode.ERR_HasNoTypeVars, _mwtBadArity, new ErrArgSymKind(_mwtBadArity.Meth()), _pArguments.carg);
                }

                if (_mpwiParamTypeConstraints)
                {
                    // This will always report an error
                    TypeBind.CheckMethConstraints(new MethWithInst(_mpwiParamTypeConstraints));
                    Debug.Fail("Unreachable");
                    return null;
                }

                if (_pInvalidSpecifiedName != null)
                {
                    // Give a better message for delegate invoke.
                    return _pGroup.OptionalObject?.Type is AggregateType agg && agg.OwningAggregate.IsDelegate()
                        ? ErrorHandling.Error(
                            ErrorCode.ERR_BadNamedArgumentForDelegateInvoke, agg.OwningAggregate.name,
                            _pInvalidSpecifiedName)
                        : ErrorHandling.Error(ErrorCode.ERR_BadNamedArgument, _pGroup.Name, _pInvalidSpecifiedName);
                }

                if (_pNameUsedInPositionalArgument != null)
                {
                    return ErrorHandling.Error(ErrorCode.ERR_NamedArgumentUsedInPositional, _pNameUsedInPositionalArgument);
                }

                // The number of arguments must be wrong.

                if (_fCandidatesUnsupported)
                {
                    return ErrorHandling.Error(ErrorCode.ERR_BindToBogus, nameErr);
                }

                if (bUseDelegateErrors)
                {
                    Debug.Assert(0 == (_pGroup.Flags & EXPRFLAG.EXF_CTOR));
                    return ErrorHandling.Error(ErrorCode.ERR_BadDelArgCount, nameErr, _pArguments.carg);
                }

                if (0 != (_pGroup.Flags & EXPRFLAG.EXF_CTOR))
                {
                    Debug.Assert(!(_pGroup.ParentType is TypeParameterType));
                    return ErrorHandling.Error(ErrorCode.ERR_BadCtorArgCount, _pGroup.ParentType, _pArguments.carg);
                }

                return ErrorHandling.Error(ErrorCode.ERR_BadArgCount, nameErr, _pArguments.carg);
            }

            private RuntimeBinderException ReportErrorsForBestMatching(bool bUseDelegateErrors)
            {
                if (bUseDelegateErrors)
                {
                    // Point to the Delegate, not the Invoke method
                    return ErrorHandling.Error(ErrorCode.ERR_BadDelArgTypes, _results.BestResult.GetType());
                }

                return ErrorHandling.Error(ErrorCode.ERR_BadArgTypes, _results.BestResult);
            }
        }
    }
}
