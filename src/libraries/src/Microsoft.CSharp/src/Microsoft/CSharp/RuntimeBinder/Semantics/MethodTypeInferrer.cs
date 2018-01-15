// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class MethodTypeInferrer
    {
        private enum NewInferenceResult
        {
            InferenceFailed,
            MadeProgress,
            NoProgress,
            Success
        }

        [Flags]
        private enum Dependency
        {
            Unknown = 0x00,
            NotDependent = 0x01,
            DependsMask = 0x10,
            Indirect = 0x12
        }
        private readonly SymbolLoader _symbolLoader;
        private readonly ExpressionBinder _binder;
        private readonly TypeArray _pMethodTypeParameters;
        private readonly TypeArray _pMethodFormalParameterTypes;
        private readonly ArgInfos _pMethodArguments;
        private readonly List<CType>[] _pExactBounds;
        private readonly List<CType>[] _pUpperBounds;
        private readonly List<CType>[] _pLowerBounds;
        private readonly CType[] _pFixedResults;
        private Dependency[][] _ppDependencies;
        private bool _dependenciesDirty;

        /*
        SPEC:

        CType inference occurs as part of the compile-time processing of a method invocation
        and takes place before the overload resolution step of the invocation. When a
        particular method group is specified in a method invocation, and no CType arguments
        are specified as part of the method invocation, CType inference is applied to each
        generic method in the method group. If CType inference succeeds, then the inferred
        CType arguments are used to determine the types of formal parameters for subsequent 
        overload resolution. If overload resolution chooses a generic method as the one to
        invoke then the inferred CType arguments are used as the actual CType arguments for the
        invocation. If CType inference for a particular method fails, that method does not
        participate in overload resolution. The failure of CType inference, in and of itself,
        does not cause a compile-time error. However, it often leads to a compile-time error
        when overload resolution then fails to find any applicable methods.

        If the supplied number of arguments is different than the number of parameters in
        the method, then inference immediately fails. Otherwise, assume that the generic
        method has the following signature:

        Tr M<X1...Xn>(T1 x1 ... Tm xm)

        With a method call of the form M(E1...Em) the task of CType inference is to find
        unique CType arguments S1...Sn for each of the CType parameters X1...Xn so that the
        call M<S1...Sn>(E1...Em)becomes valid.

        During the process of inference each CType parameter Xi is either fixed to a particular
        CType Si or unfixed with an associated set of bounds. Each of the bounds is some CType T.
        Each bound is classified as an upper bound, lower bound or exact bound.
        Initially each CType variable Xi is unfixed with an empty set of bounds.


        */

        // This file contains the implementation for method CType inference on calls (with
        // arguments, and method CType inference on conversion of method groups to delegate
        // types (which will not have arguments.)

        ////////////////////////////////////////////////////////////////////////////////

        public static bool Infer(
            ExpressionBinder binder,
            SymbolLoader symbolLoader,
            MethodSymbol pMethod,
            TypeArray pMethodFormalParameterTypes,
            ArgInfos pMethodArguments,
            out TypeArray ppInferredTypeArguments)
        {
            Debug.Assert(pMethod != null);
            Debug.Assert(pMethod.typeVars.Count > 0);
            Debug.Assert(pMethod.isParamArray || pMethod.Params == pMethodFormalParameterTypes);
            ppInferredTypeArguments = null;
            if (pMethodFormalParameterTypes.Count == 0 || pMethod.InferenceMustFail())
            {
                return false;
            }
            Debug.Assert(pMethodArguments != null);
            Debug.Assert(pMethodFormalParameterTypes != null);
            Debug.Assert(pMethodArguments.carg <= pMethodFormalParameterTypes.Count);

            var inferrer = new MethodTypeInferrer(binder, symbolLoader,
                pMethodFormalParameterTypes, pMethodArguments,
                pMethod.typeVars);
            bool success = inferrer.InferTypeArgs();

            ppInferredTypeArguments = inferrer.GetResults();
            return success;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // Fixed, unfixed and bounded CType parameters
        //
        // SPEC: During the process of inference each CType parameter is either fixed to
        // SPEC: a particular CType or unfixed with an associated set of bounds. Each of
        // SPEC: the bounds is of some CType T. Initially each CType parameter is unfixed
        // SPEC: with an empty set of bounds.

        private MethodTypeInferrer(
            ExpressionBinder exprBinder, SymbolLoader symLoader,
            TypeArray pMethodFormalParameterTypes, ArgInfos pMethodArguments,
            TypeArray pMethodTypeParameters)
        {
            _binder = exprBinder;
            _symbolLoader = symLoader;
            _pMethodFormalParameterTypes = pMethodFormalParameterTypes;
            _pMethodArguments = pMethodArguments;
            _pMethodTypeParameters = pMethodTypeParameters;
            _pFixedResults = new CType[pMethodTypeParameters.Count];
            _pLowerBounds = new List<CType>[pMethodTypeParameters.Count];
            _pUpperBounds = new List<CType>[pMethodTypeParameters.Count];
            _pExactBounds = new List<CType>[pMethodTypeParameters.Count];
            for (int iBound = 0; iBound < pMethodTypeParameters.Count; ++iBound)
            {
                _pLowerBounds[iBound] = new List<CType>();
                _pUpperBounds[iBound] = new List<CType>();
                _pExactBounds[iBound] = new List<CType>();
            }
            _ppDependencies = null;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private TypeArray GetResults() => GetGlobalSymbols().AllocParams(_pFixedResults);

        ////////////////////////////////////////////////////////////////////////////////

        private bool IsUnfixed(int iParam)
        {
            Debug.Assert(0 <= iParam);
            Debug.Assert(iParam < _pMethodTypeParameters.Count);
            return _pFixedResults[iParam] == null;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool IsUnfixed(TypeParameterType pParam)
        {
            Debug.Assert(pParam != null);
            Debug.Assert(pParam.IsMethodTypeParameter());
            int iParam = pParam.GetIndexInTotalParameters();
            Debug.Assert(_pMethodTypeParameters[iParam] == pParam);
            return IsUnfixed(iParam);
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool AllFixed()
        {
            for (int iParam = 0; iParam < _pMethodTypeParameters.Count; ++iParam)
            {
                if (IsUnfixed(iParam))
                {
                    return false;
                }
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private void AddLowerBound(TypeParameterType pParam, CType pBound)
        {
            Debug.Assert(IsUnfixed(pParam));
            int iParam = pParam.GetIndexInTotalParameters();
            if (!_pLowerBounds[iParam].Contains(pBound))
            {
                _pLowerBounds[iParam].Add(pBound);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////

        private void AddUpperBound(TypeParameterType pParam, CType pBound)
        {
            Debug.Assert(IsUnfixed(pParam));
            int iParam = pParam.GetIndexInTotalParameters();
            if (!_pUpperBounds[iParam].Contains(pBound))
            {
                _pUpperBounds[iParam].Add(pBound);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////

        private void AddExactBound(TypeParameterType pParam, CType pBound)
        {
            Debug.Assert(IsUnfixed(pParam));
            int iParam = pParam.GetIndexInTotalParameters();
            if (!_pExactBounds[iParam].Contains(pBound))
            {
                _pExactBounds[iParam].Add(pBound);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool HasBound(int iParam)
        {
            Debug.Assert(0 <= iParam);
            Debug.Assert(iParam < _pMethodTypeParameters.Count);
            return !_pLowerBounds[iParam].IsEmpty() ||
                !_pExactBounds[iParam].IsEmpty() ||
                !_pUpperBounds[iParam].IsEmpty();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // Phases
        //

        private bool InferTypeArgs()
        {
            // SPEC: CType inference takes place in phases. Each phase will try to infer CType 
            // SPEC: arguments for more CType parameters based on the findings of the previous
            // SPEC: phase. The first phase makes some initial inferences of bounds, whereas
            // SPEC: the second phase fixes CType parameters to specific types and infers further
            // SPEC: bounds. The second phase may have to be repeated a number of times.
            InferTypeArgsFirstPhase();
            return InferTypeArgsSecondPhase();
        }

        ////////////////////////////////////////////////////////////////////////////////

        private static bool IsReallyAType(CType pType) =>
            !(pType is NullType) && !(pType is VoidType) && !(pType is MethodGroupType);

        ////////////////////////////////////////////////////////////////////////////////
        //
        // The first phase
        //

        private void InferTypeArgsFirstPhase()
        {
            Debug.Assert(_pMethodFormalParameterTypes != null);
            Debug.Assert(_pMethodArguments != null);
            Debug.Assert(_pMethodArguments.carg <= _pMethodFormalParameterTypes.Count);

            // SPEC: For each of the method arguments Ei:
            for (int iArg = 0; iArg < _pMethodArguments.carg; iArg++)
            {
                // SPEC ISSUE: We never deduce anything helpful from an filled-in
                // SPEC ISSUE: optional parameter and sometimes deduce something harmful.
                // SPEC ISSUE: Ex: Foo<T>(T t = default(T)) -- we do not want to add
                // SPEC ISSUE: "T" to the bound set of "T" in this case and produce
                // SPEC ISSUE: a "chicken and egg" problem. 
                // SPEC ISSUE: We should put language in the spec saying that we skip 
                // SPEC ISSUE: inference on any argument that was created via the 
                // SPEC ISSUE: optional parameter mechanism.
                Expr pExpr = _pMethodArguments.prgexpr[iArg];

                if (pExpr.IsOptionalArgument)
                {
                    continue;
                }

                CType pDest = _pMethodFormalParameterTypes[iArg];

                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // RUNTIME BINDER ONLY CHANGE
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //
                // dynamic operands enter method type inference with their
                // actual runtime type, and in this way can infer implemented
                // types that are not visible on more public types. (for ex.,
                // private sealed classes that implement IEnumerable, as in iterators).

                CType pSource = pExpr.RuntimeObjectActualType ?? _pMethodArguments.types[iArg];

                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // END RUNTIME BINDER ONLY CHANGE
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!


                bool wasOutOrRef = false;
                if (pDest is ParameterModifierType modDest)
                {
                    pDest = modDest.GetParameterType();
                    wasOutOrRef = true;
                }
                if (pSource is ParameterModifierType modSource)
                {
                    pSource = modSource.GetParameterType();
                }
                // If the argument is a TYPEORNAMESPACEERROR and the pSource is an
                // error CType, then we want to set it to the generic error CType 
                // that has no name text. This is because of the following scenario:
                //
                // void M<T>(T t) { }
                // void Foo()
                // {
                //     UnknownType t;
                //     M(t);
                //     M(undefinedVariable);
                // }
                //
                // In the first call to M, we'll have an EXPRLOCAL with an error CType,
                // which is correct - we want the parameter help to display that we've
                // got an inferred CType of UnknownType, which is an error CType since 
                // its undefined.
                //
                // However, for the M in the second call, we DON'T want to display parameter
                // help that gives undefinedVariable as the CType parameter for T, because
                // there is no parameter of that name, let alone that CType. This appears
                // as an EXPRTYPEORNAMESPACEERROR with an ErrorType. We create a new error sym
                // without the CType name.

                // SPEC:  If Ei is an anonymous function, an explicit CType parameter
                // SPEC:   inference is made from Ei to Ti.

                // (We cannot make an output CType inference from a method group
                // at this time because we have no fixed types yet to use for
                // overload resolution.)

                // SPEC:  Otherwise, if Ei has a CType U then a lower-bound inference 
                // SPEC:   or exact inference is made from U to Ti.

                // SPEC:  Otherwise, no inference is made for this argument

                if (IsReallyAType(pSource))
                {
                    if (wasOutOrRef)
                    {
                        ExactInference(pSource, pDest);
                    }
                    else
                    {
                        LowerBoundInference(pSource, pDest);
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // The second phase
        //

        private bool InferTypeArgsSecondPhase()
        {
            // SPEC: The second phase proceeds as follows:
            // SPEC:  If no unfixed CType parameters exist then CType inference succeeds.
            // SPEC:  Otherwise, if there exists one or more arguments Ei with corresponding
            // SPEC:   parameter CType Ti such that:
            // SPEC:     o the output CType of Ei with CType Ti contains at least one unfixed
            // SPEC:       CType parameter Xj, and
            // SPEC:     o none of the input types of Ei with CType Ti contains any unfixed
            // SPEC:       CType parameter Xj, 
            // SPEC:   then an output CType inference is made from all such Ei to Ti. 
            // SPEC:  Whether or not the previous step actually made an inference, we must
            // SPEC:   now fix at least one CType parameter, as follows:
            // SPEC:  If there exists one or more CType parameters Xi such that 
            // SPEC:     o Xi is unfixed, and
            // SPEC:     o Xi has a non-empty set of bounds, and
            // SPEC:     o Xi does not depend on any Xj 
            // SPEC:   then each such Xi is fixed. If any fixing operation fails then CType
            // SPEC:   inference fails.
            // SPEC:  Otherwise, if there exists one or more CType parameters Xi such that
            // SPEC:     o Xi is unfixed, and
            // SPEC:     o Xi has a non-empty set of bounds, and
            // SPEC:     o there is at least one CType parameter Xj that depends on Xi
            // SPEC:   then each such Xi is fixed. If any fixing operation fails then
            // SPEC:   CType inference fails.
            // SPEC:  Otherwise, we are unable to make progress and there are unfixed parameters.
            // SPEC:   CType inference fails. 
            // SPEC:  If CType inference neither succeeds nor fails then the second phase is
            // SPEC:   repeated until CType inference succeeds or fails. (Since each repetition of
            // SPEC:   the second phase either succeeds, fails or fixes an unfixed CType parameter,
            // SPEC:   the algorithm must terminate with no more repetitions than the number
            // SPEC:   of CType parameters.

            InitializeDependencies();

            while (true)
            {
                NewInferenceResult res = DoSecondPhase();
                Debug.Assert(res != NewInferenceResult.NoProgress);
                if (res == NewInferenceResult.InferenceFailed)
                {
                    return false;
                }
                if (res == NewInferenceResult.Success)
                {
                    return true;
                }
                // Otherwise, we made some progress last time; do it again.
            }
        }

        ////////////////////////////////////////////////////////////////////////////////

        private NewInferenceResult DoSecondPhase()
        {
            // SPEC:  If no unfixed CType parameters exist then CType inference succeeds.
            if (AllFixed())
            {
                return NewInferenceResult.Success;
            }
            // SPEC:  Otherwise, if there exists one or more arguments Ei with
            // SPEC:   corresponding parameter CType Ti such that:
            // SPEC:     o the output CType of Ei with CType Ti contains at least one unfixed
            // SPEC:       CType parameter Xj, and
            // SPEC:     o none of the input types of Ei with CType Ti contains any unfixed
            // SPEC:       CType parameter Xj,
            // SPEC:   then an output CType inference is made from all such Ei to Ti.

            // Irrelevant to dynamic binding.

            // SPEC:  Whether or not the previous step actually made an inference, we
            // SPEC:   must now fix at least one CType parameter, as follows:
            // SPEC:  If there exists one or more CType parameters Xi such that
            // SPEC:     o Xi is unfixed, and
            // SPEC:     o Xi has a non-empty set of bounds, and
            // SPEC:     o Xi does not depend on any Xj
            // SPEC:   then each such Xi is fixed. If any fixing operation fails then
            // SPEC:   CType inference fails.

            NewInferenceResult res = FixNondependentParameters();
            if (res != NewInferenceResult.NoProgress)
            {
                return res;
            }
            // SPEC:  Otherwise, if there exists one or more CType parameters Xi such that
            // SPEC:     o Xi is unfixed, and
            // SPEC:     o Xi has a non-empty set of bounds, and
            // SPEC:     o there is at least one CType parameter Xj that depends on Xi
            // SPEC:   then each such Xi is fixed. If any fixing operation fails then
            // SPEC:   CType inference fails.
            res = FixDependentParameters();
            if (res != NewInferenceResult.NoProgress)
            {
                return res;
            }
            // SPEC:  Otherwise, we are unable to make progress and there are
            // SPEC:   unfixed parameters. CType inference fails.
            return NewInferenceResult.InferenceFailed;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private NewInferenceResult FixNondependentParameters()
        {
            // SPEC:  Otherwise, if there exists one or more CType parameters Xi such that
            // SPEC:     o Xi is unfixed, and
            // SPEC:     o Xi has a non-empty set of bounds, and
            // SPEC:     o Xi does not depend on any Xj
            // SPEC:   then each such Xi is fixed.

            // Dependency is only defined for unfixed parameters. Therefore, fixing
            // a parameter may cause all of its dependencies to become no longer
            // dependent on anything. We need to first determine which parameters need to be 
            // fixed, and then fix them all at once.

            bool[] pNeedsFixing = new bool[_pMethodTypeParameters.Count];
            int iParam;
            NewInferenceResult res = NewInferenceResult.NoProgress;
            for (iParam = 0; iParam < _pMethodTypeParameters.Count; iParam++)
            {
                if (IsUnfixed(iParam) && HasBound(iParam) && !DependsOnAny(iParam))
                {
                    pNeedsFixing[iParam] = true;
                    res = NewInferenceResult.MadeProgress;
                }
            }
            for (iParam = 0; iParam < _pMethodTypeParameters.Count; iParam++)
            {
                // Fix as much as you can, even if there are errors.  That will
                // help with intellisense.
                if (pNeedsFixing[iParam])
                {
                    if (!Fix(iParam))
                    {
                        res = NewInferenceResult.InferenceFailed;
                    }
                }
            }
            return res;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private NewInferenceResult FixDependentParameters()
        {
            // SPEC:  All unfixed CType parameters Xi are fixed for which all of the following hold:
            // SPEC:    There is at least one CType parameter Xj that depends on Xi.
            // SPEC:    Xi has a non-empty set of bounds.

            // As above, we must collect up everything that needs fixing first,
            // and then fix them.

            bool[] pNeedsFixing = new bool[_pMethodTypeParameters.Count];
            int iParam;
            NewInferenceResult res = NewInferenceResult.NoProgress;
            for (iParam = 0; iParam < _pMethodTypeParameters.Count; iParam++)
            {
                if (IsUnfixed(iParam) && HasBound(iParam) && AnyDependsOn(iParam))
                {
                    pNeedsFixing[iParam] = true;
                    res = NewInferenceResult.MadeProgress;
                }
            }
            for (iParam = 0; iParam < _pMethodTypeParameters.Count; iParam++)
            {
                // Fix as much as you can, even if there are errors.  That will
                // help with intellisense.
                if (pNeedsFixing[iParam])
                {
                    if (!Fix(iParam))
                    {
                        res = NewInferenceResult.InferenceFailed;
                    }
                }
            }
            return res;
        }

        private void InitializeDependencies()
        {
            // We track dependencies by a two-d square array that gives the known
            // relationship between every pair of CType parameters. The relationship
            // is one of:
            //
            //  Unknown relationship
            //  known to be not dependent
            //  known to depend directly
            //  known to depend indirectly
            //
            // Since dependency is only defined on unfixed CType parameters, fixing a CType
            // parameter causes all dependencies involving that parameter to go to
            // the "known to be not dependent" state. Since dependency is a transitive property,
            // this means that doing so may require recalculating the indirect dependencies
            // from the now possibly smaller set of dependencies.
            //
            // Therefore, when we detect that the dependency state has possibly changed
            // due to fixing, we change all "depends indirectly" back into "unknown" and
            // recalculate from the remaining "depends directly".
            //
            // This algorithm thereby yields an extremely bad (but extremely unlikely) worst
            // case for asymptotic performance. Suppose there are n CType parameters.
            // "DependsTransitivelyOn" below costs O(n) because it must potentially check
            // all n CType parameters to see if there is any k such that Xj => Xk => Xi.
            // "DeduceDependencies" calls "DependsTransitivelyOn" for each "Unknown"
            // pair, and there could be O(n^2) such pairs, so DependsTransitivelyOn is
            // worst-case O(n^3).  And we could have to recalculate the dependency graph
            // after each CType parameter is fixed in turn, so that would be O(n) calls to
            // DependsTransitivelyOn, giving this algorithm a worst case of O(n^4).
            //
            // Of course, in reality, n is going to almost always be on the order of
            // "smaller than 5", and there will not be O(n^2) dependency relationships 
            // between CType parameters; it is far more likely that the transitivity chains
            // will be very short and not branch or loop at all. This is much more likely to
            // be an O(n^2) algorithm in practice.

            Debug.Assert(_ppDependencies == null);
            _ppDependencies = new Dependency[_pMethodTypeParameters.Count][];
            for (int iParam = 0; iParam < _pMethodTypeParameters.Count; ++iParam)
            {
                _ppDependencies[iParam] = new Dependency[_pMethodTypeParameters.Count];
            }

            DeduceAllDependencies();
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool DependsOn(int iParam, int jParam)
        {
            Debug.Assert(_ppDependencies != null);

            // SPEC: Xj depends on Xi if Xj depends directly on Xi, or if Xi depends
            // SPEC: directly on Xk and Xk depends on Xj. Thus "depends on" is the
            // SPEC: transitive but not reflexive closure of "depends directly on".

            Debug.Assert(0 <= iParam && iParam < _pMethodTypeParameters.Count);
            Debug.Assert(0 <= jParam && jParam < _pMethodTypeParameters.Count);

            if (_dependenciesDirty)
            {
                SetIndirectsToUnknown();
                DeduceAllDependencies();
            }
            return 0 != ((_ppDependencies[iParam][jParam]) & Dependency.DependsMask);
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool DependsTransitivelyOn(int iParam, int jParam)
        {
            Debug.Assert(_ppDependencies != null);
            Debug.Assert(0 <= iParam && iParam < _pMethodTypeParameters.Count);
            Debug.Assert(0 <= jParam && jParam < _pMethodTypeParameters.Count);

            // Can we find Xk such that Xi depends on Xk and Xk depends on Xj?
            // If so, then Xi depends indirectly on Xj.  (Note that there is
            // a minor optimization here -- the spec comment above notes that
            // we want Xi to depend DIRECTLY on Xk, and Xk to depend directly
            // or indirectly on Xj. But if we already know that Xi depends
            // directly OR indirectly on Xk and Xk depends on Xj, then that's
            // good enough.)

            for (int kParam = 0; kParam < _pMethodTypeParameters.Count; ++kParam)
            {
                if (0 != ((_ppDependencies[iParam][kParam]) & Dependency.DependsMask) &&
                    0 != ((_ppDependencies[kParam][jParam]) & Dependency.DependsMask))
                {
                    return true;
                }
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private void DeduceAllDependencies()
        {
            bool madeProgress;
            do
            {
                madeProgress = DeduceDependencies();
            } while (madeProgress);
            SetUnknownsToNotDependent();
            _dependenciesDirty = false;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool DeduceDependencies()
        {
            Debug.Assert(_ppDependencies != null);
            bool madeProgress = false;
            for (int iParam = 0; iParam < _pMethodTypeParameters.Count; ++iParam)
            {
                for (int jParam = 0; jParam < _pMethodTypeParameters.Count; ++jParam)
                {
                    if (_ppDependencies[iParam][jParam] == Dependency.Unknown)
                    {
                        if (DependsTransitivelyOn(iParam, jParam))
                        {
                            _ppDependencies[iParam][jParam] = Dependency.Indirect;
                            madeProgress = true;
                        }
                    }
                }
            }
            return madeProgress;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private void SetUnknownsToNotDependent()
        {
            Debug.Assert(_ppDependencies != null);
            for (int iParam = 0; iParam < _pMethodTypeParameters.Count; ++iParam)
            {
                for (int jParam = 0; jParam < _pMethodTypeParameters.Count; ++jParam)
                {
                    if (_ppDependencies[iParam][jParam] == Dependency.Unknown)
                    {
                        _ppDependencies[iParam][jParam] = Dependency.NotDependent;
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////

        private void SetIndirectsToUnknown()
        {
            Debug.Assert(_ppDependencies != null);
            for (int iParam = 0; iParam < _pMethodTypeParameters.Count; ++iParam)
            {
                for (int jParam = 0; jParam < _pMethodTypeParameters.Count; ++jParam)
                {
                    if (_ppDependencies[iParam][jParam] == Dependency.Indirect)
                    {
                        _ppDependencies[iParam][jParam] = Dependency.Unknown;
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // A fixed parameter never depends on anything, nor is depended upon by anything.

        private void UpdateDependenciesAfterFix(int iParam)
        {
            if (_ppDependencies == null)
            {
                return;
            }
            for (int jParam = 0; jParam < _pMethodTypeParameters.Count; ++jParam)
            {
                _ppDependencies[iParam][jParam] = Dependency.NotDependent;
                _ppDependencies[jParam][iParam] = Dependency.NotDependent;
            }
            _dependenciesDirty = true;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool DependsOnAny(int iParam)
        {
            Debug.Assert(0 <= iParam && iParam < _pMethodTypeParameters.Count);
            for (int jParam = 0; jParam < _pMethodTypeParameters.Count; ++jParam)
            {
                if (DependsOn(iParam, jParam))
                {
                    return true;
                }
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool AnyDependsOn(int iParam)
        {
            Debug.Assert(0 <= iParam && iParam < _pMethodTypeParameters.Count);
            for (int jParam = 0; jParam < _pMethodTypeParameters.Count; ++jParam)
            {
                if (DependsOn(jParam, iParam))
                {
                    return true;
                }
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // Exact inferences
        //
        private void ExactInference(CType pSource, CType pDest)
        {
            // SPEC: An exact inference from a CType U to a CType V is made as follows:

            // SPEC:  If V is one of the unfixed Xi then U is added to the set of
            // SPEC:   exact bounds for Xi.
            if (ExactTypeParameterInference(pSource, pDest))
            {
                return;
            }

            // SPEC:  Otherwise, if U is an array CType UE[...] and V is an array CType VE[...]
            // SPEC:   of the same rank then an exact inference from UE to VE is made.
            if (ExactArrayInference(pSource, pDest))
            {
                return;
            }

            // SPEC:  Otherwise, if U is the CType U1? and V is the CType V1? then an
            // SPEC:   exact inference is made from U to V.

            if (ExactNullableInference(pSource, pDest))
            {
                return;
            }

            // SPEC:  Otherwise, if V is a constructed CType C<V1...Vk> and U is a constructed
            // SPEC:   CType C<U1...Uk> then an exact inference is made
            // SPEC:    from each Ui to the corresponding Vi.

            if (ExactConstructedInference(pSource, pDest))
            {
                return;
            }
            // SPEC:  Otherwise no inferences are made.
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool ExactTypeParameterInference(CType pSource, CType pDest)
        {
            // SPEC:  If V is one of the unfixed Xi then U is added to the set of bounds
            // SPEC:   for Xi.
            if (pDest is TypeParameterType pTPType)
            {
                if (pTPType.IsMethodTypeParameter() && IsUnfixed(pTPType))
                {
                    AddExactBound(pTPType, pSource);
                    return true;
                }
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool ExactArrayInference(CType pSource, CType pDest)
        {
            // SPEC:  Otherwise, if U is an array CType UE[...] and V is an array CType VE[...]
            // SPEC:   of the same rank then an exact inference from UE to VE is made.
            if (!(pSource is ArrayType pArraySource) || !(pDest is ArrayType pArrayDest))
            {
                return false;
            }

            if (pArraySource.rank != pArrayDest.rank || pArraySource.IsSZArray != pArrayDest.IsSZArray)
            {
                return false;
            }

            ExactInference(pArraySource.GetElementType(), pArrayDest.GetElementType());
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool ExactNullableInference(CType pSource, CType pDest)
        {
            // SPEC:  Otherwise, if U is the CType U1? and V is the CType V1? 
            // SPEC:   then an exact inference is made from U to V.
            if (!(pSource is NullableType nubSource) || !(pDest is NullableType nubDest))
            {
                return false;
            }

            ExactInference(nubSource.GetUnderlyingType(), nubDest.GetUnderlyingType());
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool ExactConstructedInference(CType pSource, CType pDest)
        {
            // SPEC:  Otherwise, if V is a constructed CType C<V1...Vk> and U is a constructed
            // SPEC:   CType C<U1...Uk> then an exact inference 
            // SPEC:   is made from each Ui to the corresponding Vi.

            if (!(pSource is AggregateType pConstructedSource) || !(pDest is AggregateType pConstructedDest)
                || pConstructedSource.GetOwningAggregate() != pConstructedDest.GetOwningAggregate())
            {
                return false;
            }

            ExactTypeArgumentInference(pConstructedSource, pConstructedDest);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private void ExactTypeArgumentInference(
            AggregateType pSource, AggregateType pDest)

        {
            Debug.Assert(pSource != null);
            Debug.Assert(pDest != null);
            Debug.Assert(pSource.GetOwningAggregate() == pDest.GetOwningAggregate());

            TypeArray pSourceArgs = pSource.GetTypeArgsAll();
            TypeArray pDestArgs = pDest.GetTypeArgsAll();

            Debug.Assert(pSourceArgs != null);
            Debug.Assert(pDestArgs != null);
            Debug.Assert(pSourceArgs.Count == pDestArgs.Count);

            for (int arg = 0; arg < pSourceArgs.Count; ++arg)
            {
                ExactInference(pSourceArgs[arg], pDestArgs[arg]);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // Lower-bound inferences
        //
        private void LowerBoundInference(CType pSource, CType pDest)
        {
            // SPEC: A lower-bound inference from a CType U to a CType V is made as follows:

            // SPEC:  If V is one of the unfixed Xi then U is added to the set of 
            // SPEC:   lower bounds for Xi.

            if (LowerBoundTypeParameterInference(pSource, pDest))
            {
                return;
            }

            // SPEC:  Otherwise, if U is an array CType Ue[...] and V is either an array
            // SPEC:   CType Ve[...] of the same rank, or if U is a one-dimensional array
            // SPEC:   CType Ue[] and V is one of IEnumerable<Ve>, ICollection<Ve> or
            // SPEC:   IList<Ve> then
            // SPEC:    if Ue is known to be a reference CType then a lower-bound inference
            // SPEC:     from Ue to Ve is made.
            // SPEC:    otherwise an exact inference from Ue to Ve is made.

            if (LowerBoundArrayInference(pSource, pDest))
            {
                return;
            }

            // SPEC:  Otherwise, if V is nullable CType V1? and U is nullable CType U1?
            // SPEC:   then an exact inference is made from U1 to V1.

            if (ExactNullableInference(pSource, pDest))
            {
                return;
            }

            // At this point we could also do an inference from non-nullable U
            // to nullable V.
            // 
            // We tried implementing lower bound nullable inference as follows:
            // 
            //  Otherwise, if V is nullable CType V1? and U is a non-nullable 
            //   struct CType then an exact inference is made from U to V1.
            // 
            // However, this causes an unfortunate interaction with 
            // our implementation of section 15.2 of
            // the specification. Namely, it appears that the code which
            // checks whether a given method is compatible with
            // a delegate CType assumes that if method CType inference succeeds,
            // then the inferred types are compatible with the delegate types.
            // This is not necessarily so; the inferred types could be compatible
            // via a conversion other than reference or identity.
            // 
            // We should take an action item to investigate this problem.
            // Until then, we will turn off the proposed lower bound nullable
            // inference.

            // if (LowerBoundNullableInference(pSource, pDest))
            // {
            //     return;
            // }

            // SPEC: Otherwise... many cases for constructed generic types.

            if (LowerBoundConstructedInference(pSource, pDest))
            {
                return;
            }
            // SPEC:  Otherwise, no inferences are made.
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool LowerBoundTypeParameterInference(CType pSource, CType pDest)
        {
            // SPEC:  If V is one of the unfixed Xi then U is added to the set of bounds
            // SPEC:   for Xi.
            if (pDest is TypeParameterType pTPType)
            {
                if (pTPType.IsMethodTypeParameter() && IsUnfixed(pTPType))
                {
                    AddLowerBound(pTPType, pSource);
                    return true;
                }
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool LowerBoundArrayInference(CType pSource, CType pDest)
        {
            // SPEC:  Otherwise, if U is an array CType Ue[...] and V is either an array
            // SPEC:   CType Ve[...] of the same rank, or if U is a one-dimensional array
            // SPEC:   CType Ue[] and V is one of IEnumerable<Ve>, ICollection<Ve>, 
            // SPEC:   IList<Ve>, IReadOnlyCollection<Ve> or IReadOnlyList<Ve> then
            // SPEC:    if Ue is known to be a reference CType then a lower-bound inference
            // SPEC:     from Ue to Ve is made.
            // SPEC:    otherwise an exact inference from Ue to Ve is made.

            // Consider the following:
            //
            // abstract class B<T> { public abstract M<U>(U u) : where U : T; }
            // class D : B<int[]> {
            //   static void M<X>(X[] x) { }
            //   public override M<U>(U u) { M(u); } // should infer M<int>
            // }

            if (!(pSource is ArrayType pArraySource))
            {
                return false;
            }

            CType pElementSource = pArraySource.GetElementType();
            CType pElementDest;

            if (pDest is ArrayType pArrayDest)
            {
                if (pArrayDest.rank != pArraySource.rank || pArrayDest.IsSZArray != pArraySource.IsSZArray)
                {
                    return false;
                }
                pElementDest = pArrayDest.GetElementType();
            }
            else if (pDest.isPredefType(PredefinedType.PT_G_IENUMERABLE) ||
                pDest.isPredefType(PredefinedType.PT_G_ICOLLECTION) ||
                pDest.isPredefType(PredefinedType.PT_G_ILIST) ||
                pDest.isPredefType(PredefinedType.PT_G_IREADONLYCOLLECTION) ||
                pDest.isPredefType(PredefinedType.PT_G_IREADONLYLIST))
            {
                if (!pArraySource.IsSZArray)
                {
                    return false;
                }
                AggregateType pAggregateDest = (AggregateType)pDest;
                pElementDest = pAggregateDest.GetTypeArgsThis()[0];
            }
            else
            {
                return false;
            }

            if (pElementSource.IsRefType())
            {
                LowerBoundInference(pElementSource, pElementDest);
            }
            else
            {
                ExactInference(pElementSource, pElementDest);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////

        /*
        bool LowerBoundNullableInference(CType pSource, CType pDest)
        {
            // SPEC ISSUE: As noted above, the spec does not clearly call out how
            // SPEC ISSUE: to do CType inference to a nullable target. I propose the
            // SPEC ISSUE: following:
            // SPEC ISSUE:
            // SPEC ISSUE:  Otherwise, if V is nullable CType V1? and U is a 
            // SPEC ISSUE:   non-nullable struct CType then an exact inference is made from U to V1.

            if (!pDest.IsNullableType() || !pSource.isStructType() || pSource.IsNullableType())
            {
                return false;
            }
            ExactInference(pSource, pDest.AsNullableType().GetUnderlyingType());
            return true;
        }
         * */

        ////////////////////////////////////////////////////////////////////////////////

        private bool LowerBoundConstructedInference(CType pSource, CType pDest)
        {
            if (!(pDest is AggregateType pConstructedDest))
            {
                return false;
            }

            TypeArray pDestArgs = pConstructedDest.GetTypeArgsAll();
            if (pDestArgs.Count == 0)
            {
                return false;
            }

            // SPEC:  Otherwise, if V is a constructed class or struct CType C<V1...Vk> 
            // SPEC:   and U is C<U1...Uk> then an exact inference
            // SPEC:   is made from each Ui to the corresponding Vi.

            // SPEC:  Otherwise, if V is a constructed interface or delegate CType C<V1...Vk> 
            // SPEC:   and U is C<U1...Uk> then an exact inference,
            // SPEC:   lower bound inference or upper bound inference
            // SPEC:   is made from each Ui to the corresponding Vi.

            if (pSource is AggregateType aggSource &&
                aggSource.GetOwningAggregate() == pConstructedDest.GetOwningAggregate())
            {
                if (aggSource.isInterfaceType() || aggSource.isDelegateType())
                {
                    LowerBoundTypeArgumentInference(aggSource, pConstructedDest);
                }
                else
                {
                    ExactTypeArgumentInference(aggSource, pConstructedDest);
                }
                return true;
            }

            // SPEC:  Otherwise, if V is a class CType C<V1...Vk> and U is a class CType which
            // SPEC:   inherits directly or indirectly from C<U1...Uk> then an exact ...
            // SPEC:  ... and U is a CType parameter with effective base class ...
            // SPEC:  ... and U is a CType parameter with an effective base class which inherits ...

            if (LowerBoundClassInference(pSource, pConstructedDest))
            {
                return true;
            }

            // SPEC:  Otherwise, if V is an interface CType C<V1...Vk> and U is a class CType
            // SPEC:   or struct CType and there is a unique set U1...Uk such that U directly 
            // SPEC:   or indirectly implements C<U1...Uk> then an exact ...
            // SPEC:  ... and U is an interface CType ...
            // SPEC:  ... and U is a CType parameter ...

            if (LowerBoundInterfaceInference(pSource, pConstructedDest))
            {
                return true;
            }

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool LowerBoundClassInference(CType pSource, AggregateType pDest)
        {
            if (!pDest.isClassType())
            {
                return false;
            }

            // SPEC:  Otherwise, if V is a class CType C<V1...Vk> and U is a class CType which
            // SPEC:   inherits directly or indirectly from C<U1...Uk> 
            // SPEC:   then an exact inference is made from each Ui to the corresponding Vi.
            // SPEC:  Otherwise, if V is a class CType C<V1...Vk> and U is a CType parameter
            // SPEC:   with effective base class C<U1...Uk> 
            // SPEC:   then an exact inference is made from each Ui to the corresponding Vi.
            // SPEC:  Otherwise, if V is a class CType C<V1...Vk> and U is a CType parameter
            // SPEC:   with an effective base class which inherits directly or indirectly from
            // SPEC:   C<U1...Uk> then an exact inference is made
            // SPEC:   from each Ui to the corresponding Vi.

            AggregateType pSourceBase = null;

            if (pSource.isClassType())
            {
                pSourceBase = (pSource as AggregateType).GetBaseClass();
            }

            while (pSourceBase != null)
            {
                if (pSourceBase.GetOwningAggregate() == pDest.GetOwningAggregate())
                {
                    ExactTypeArgumentInference(pSourceBase, pDest);
                    return true;
                }
                pSourceBase = pSourceBase.GetBaseClass();
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool LowerBoundInterfaceInference(CType pSource, AggregateType pDest)
        {
            if (!pDest.isInterfaceType())
            {
                return false;
            }

            // SPEC:  Otherwise, if V is an interface CType C<V1...Vk> and U is a class CType
            // SPEC:   or struct CType and there is a unique set U1...Uk such that U directly 
            // SPEC:   or indirectly implements C<U1...Uk> then an
            // SPEC:   exact, upper-bound, or lower-bound inference ...
            // SPEC:  ... and U is an interface CType ...
            // SPEC:  ... and U is a CType parameter ...

            //TypeArray pInterfaces = null;

            if (!pSource.isStructType() && !pSource.isClassType() &&
                !pSource.isInterfaceType() && !(pSource is TypeParameterType))
            {
                return false;
            }

            var interfaces = pSource.AllPossibleInterfaces();
            AggregateType pInterface = null;
            foreach (AggregateType pCurrent in interfaces)
            {
                if (pCurrent.GetOwningAggregate() == pDest.GetOwningAggregate())
                {
                    if (pInterface == null)
                    {
                        pInterface = pCurrent;
                    }
                    else if (pInterface != pCurrent)
                    {
                        // Not unique. Bail out.
                        return false;
                    }
                }
            }
            if (pInterface == null)
            {
                return false;
            }
            LowerBoundTypeArgumentInference(pInterface, pDest);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private void LowerBoundTypeArgumentInference(
            AggregateType pSource, AggregateType pDest)
        {
            // SPEC: The choice of inference for the i-th CType argument is made
            // SPEC: based on the declaration of the i-th CType parameter of C, as
            // SPEC: follows:
            // SPEC:  if Ui is known to be of reference CType and the i-th CType parameter
            // SPEC:   was declared as covariant then a lower bound inference is made.
            // SPEC:  if Ui is known to be of reference CType and the i-th CType parameter
            // SPEC:   was declared as contravariant then an upper bound inference is made.
            // SPEC:  otherwise, an exact inference is made.

            Debug.Assert(pSource != null);
            Debug.Assert(pDest != null);
            Debug.Assert(pSource.GetOwningAggregate() == pDest.GetOwningAggregate());

            TypeArray pTypeParams = pSource.GetOwningAggregate().GetTypeVarsAll();
            TypeArray pSourceArgs = pSource.GetTypeArgsAll();
            TypeArray pDestArgs = pDest.GetTypeArgsAll();

            Debug.Assert(pTypeParams != null);
            Debug.Assert(pSourceArgs != null);
            Debug.Assert(pDestArgs != null);

            Debug.Assert(pTypeParams.Count == pSourceArgs.Count);
            Debug.Assert(pTypeParams.Count == pDestArgs.Count);

            for (int arg = 0; arg < pSourceArgs.Count; ++arg)
            {
                TypeParameterType pTypeParam = (TypeParameterType)pTypeParams[arg];
                CType pSourceArg = pSourceArgs[arg];
                CType pDestArg = pDestArgs[arg];

                if (pSourceArg.IsRefType() && pTypeParam.Covariant)
                {
                    LowerBoundInference(pSourceArg, pDestArg);
                }
                else if (pSourceArg.IsRefType() && pTypeParam.Contravariant)
                {
                    UpperBoundInference(pSourceArgs[arg], pDestArgs[arg]);
                }
                else
                {
                    ExactInference(pSourceArgs[arg], pDestArgs[arg]);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // Upper-bound inferences
        //
        private void UpperBoundInference(CType pSource, CType pDest)
        {
            // SPEC: An upper-bound inference from a CType U to a CType V is made as follows:

            // SPEC:  If V is one of the unfixed Xi then U is added to the set of 
            // SPEC:   upper bounds for Xi.

            if (UpperBoundTypeParameterInference(pSource, pDest))
            {
                return;
            }

            // SPEC:  Otherwise, if V is an array CType Ve[...] and U is an array
            // SPEC:   CType Ue[...] of the same rank, or if V is a one-dimensional array
            // SPEC:   CType Ve[] and U is one of IEnumerable<Ue>, ICollection<Ue> or
            // SPEC:   IList<Ue> then
            // SPEC:    if Ue is known to be a reference CType then an upper-bound inference
            // SPEC:     from Ue to Ve is made.
            // SPEC:    otherwise an exact inference from Ue to Ve is made.

            if (UpperBoundArrayInference(pSource, pDest))
            {
                return;
            }

            // SPEC:  Otherwise, if V is nullable CType V1? and U is nullable CType U1?
            // SPEC:   then an exact inference is made from U1 to V1.

            if (ExactNullableInference(pSource, pDest))
            {
                return;
            }

            // SPEC:  Otherwise... cases for constructed types

            if (UpperBoundConstructedInference(pSource, pDest))
            {
                return;
            }
            // SPEC:  Otherwise, no inferences are made.
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool UpperBoundTypeParameterInference(CType pSource, CType pDest)
        {
            // SPEC:  If V is one of the unfixed Xi then U is added to the set of upper bounds
            // SPEC:   for Xi.
            if (pDest is TypeParameterType pTPType)
            {
                if (pTPType.IsMethodTypeParameter() && IsUnfixed(pTPType))
                {
                    AddUpperBound(pTPType, pSource);
                    return true;
                }
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool UpperBoundArrayInference(CType pSource, CType pDest)
        {
            // SPEC:  Otherwise, if V is an array CType Ve[...] and U is an array
            // SPEC:   CType Ue[...] of the same rank, or if V is a one-dimensional array
            // SPEC:   CType Ve[] and U is one of IEnumerable<Ue>, ICollection<Ue>,
            // SPEC:   IList<Ue>, IReadOnlyCollection<Ue> or IReadOnlyList<Ue> then
            // SPEC:    if Ue is known to be a reference CType then an upper-bound inference
            // SPEC:     from Ue to Ve is made.
            // SPEC:    otherwise an exact inference from Ue to Ve is made.

            if (!(pDest is ArrayType pArrayDest))
            {
                return false;
            }

            CType pElementDest = pArrayDest.GetElementType();
            CType pElementSource;

            if (pSource is ArrayType pArraySource)
            {
                if (pArrayDest.rank != pArraySource.rank || pArrayDest.IsSZArray != pArraySource.IsSZArray)
                {
                    return false;
                }
                pElementSource = pArraySource.GetElementType();
            }
            else if (pSource.isPredefType(PredefinedType.PT_G_IENUMERABLE) ||
                pSource.isPredefType(PredefinedType.PT_G_ICOLLECTION) ||
                pSource.isPredefType(PredefinedType.PT_G_ILIST) ||
                pSource.isPredefType(PredefinedType.PT_G_IREADONLYLIST) ||
                pSource.isPredefType(PredefinedType.PT_G_IREADONLYCOLLECTION))
            {
                if (!pArrayDest.IsSZArray)
                {
                    return false;
                }
                AggregateType pAggregateSource = (AggregateType)pSource;
                pElementSource = pAggregateSource.GetTypeArgsThis()[0];
            }
            else
            {
                return false;
            }

            if (pElementSource.IsRefType())
            {
                UpperBoundInference(pElementSource, pElementDest);
            }
            else
            {
                ExactInference(pElementSource, pElementDest);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool UpperBoundConstructedInference(CType pSource, CType pDest)
        {
            if (!(pSource is AggregateType pConstructedSource))
            {
                return false;
            }

            TypeArray pSourceArgs = pConstructedSource.GetTypeArgsAll();
            if (pSourceArgs.Count == 0)
            {
                return false;
            }

            // SPEC:  Otherwise, if V is a constructed CType C<V1...Vk> and U is
            // SPEC:   C<U1...Uk> then an exact inference,
            // SPEC:   lower bound inference or upper bound inference
            // SPEC:   is made from each Ui to the corresponding Vi.

            if (pDest is AggregateType aggDest &&
                pConstructedSource.GetOwningAggregate() == aggDest.GetOwningAggregate())
            {
                if (aggDest.isInterfaceType() || aggDest.isDelegateType())
                {
                    UpperBoundTypeArgumentInference(pConstructedSource, aggDest);
                }
                else
                {
                    ExactTypeArgumentInference(pConstructedSource, aggDest);
                }
                return true;
            }

            // SPEC:  Otherwise, if U is a class CType C<U1...Uk> and V is a class CType which
            // SPEC:   inherits directly or indirectly from C<V1...Vk> then an exact ...

            if (UpperBoundClassInference(pConstructedSource, pDest))
            {
                return true;
            }

            // SPEC:  Otherwise, if U is an interface CType C<U1...Uk> and V is a class CType
            // SPEC:   or struct CType and there is a unique set V1...Vk such that V directly 
            // SPEC:   or indirectly implements C<V1...Vk> then an exact ...
            // SPEC:  ... and U is an interface CType ...

            if (UpperBoundInterfaceInference(pConstructedSource, pDest))
            {
                return true;
            }

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool UpperBoundClassInference(AggregateType pSource, CType pDest)
        {
            if (!pSource.isClassType() || !pDest.isClassType())
            {
                return false;
            }

            // SPEC:  Otherwise, if U is a class CType C<U1...Uk> and V is a class CType which
            // SPEC:   inherits directly or indirectly from C<V1...Vk> then an exact 
            // SPEC:   inference is made from each Ui to the corresponding Vi.

            AggregateType pDestBase = ((AggregateType)pDest).GetBaseClass();

            while (pDestBase != null)
            {
                if (pDestBase.GetOwningAggregate() == pSource.GetOwningAggregate())
                {
                    ExactTypeArgumentInference(pSource, pDestBase);
                    return true;
                }
                pDestBase = pDestBase.GetBaseClass();
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private bool UpperBoundInterfaceInference(AggregateType pSource, CType pDest)
        {
            if (!pSource.isInterfaceType())
            {
                return false;
            }

            // SPEC:  Otherwise, if U is an interface CType C<U1...Uk> and V is a class CType
            // SPEC:   or struct CType and there is a unique set V1...Vk such that V directly 
            // SPEC:   or indirectly implements C<V1...Vk> then an exact ...
            // SPEC:  ... and U is an interface CType ...

            if (!pDest.isStructType() && !pDest.isClassType() &&
                !pDest.isInterfaceType())
            {
                return false;
            }

            var interfaces = pDest.AllPossibleInterfaces();
            AggregateType pInterface = null;
            foreach (AggregateType pCurrent in interfaces)
            {
                if (pCurrent.GetOwningAggregate() == pSource.GetOwningAggregate())
                {
                    if (pInterface == null)
                    {
                        pInterface = pCurrent;
                    }
                    else if (pInterface != pCurrent)
                    {
                        // Not unique. Bail out.
                        return false;
                    }
                }
            }
            if (pInterface == null)
            {
                return false;
            }
            UpperBoundTypeArgumentInference(pInterface, pDest as AggregateType);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private void UpperBoundTypeArgumentInference(
            AggregateType pSource, AggregateType pDest)
        {
            // SPEC: The choice of inference for the i-th CType argument is made
            // SPEC: based on the declaration of the i-th CType parameter of C, as
            // SPEC: follows:
            // SPEC:  if Ui is known to be of reference CType and the i-th CType parameter
            // SPEC:   was declared as covariant then an upper-bound inference is made.
            // SPEC:  if Ui is known to be of reference CType and the i-th CType parameter
            // SPEC:   was declared as contravariant then a lower-bound inference is made.
            // SPEC:  otherwise, an exact inference is made.

            Debug.Assert(pSource != null);
            Debug.Assert(pDest != null);
            Debug.Assert(pSource.GetOwningAggregate() == pDest.GetOwningAggregate());

            TypeArray pTypeParams = pSource.GetOwningAggregate().GetTypeVarsAll();
            TypeArray pSourceArgs = pSource.GetTypeArgsAll();
            TypeArray pDestArgs = pDest.GetTypeArgsAll();

            Debug.Assert(pTypeParams != null);
            Debug.Assert(pSourceArgs != null);
            Debug.Assert(pDestArgs != null);

            Debug.Assert(pTypeParams.Count == pSourceArgs.Count);
            Debug.Assert(pTypeParams.Count == pDestArgs.Count);

            for (int arg = 0; arg < pSourceArgs.Count; ++arg)
            {
                TypeParameterType pTypeParam = (TypeParameterType)pTypeParams[arg];
                CType pSourceArg = pSourceArgs[arg];
                CType pDestArg = pDestArgs[arg];

                if (pSourceArg.IsRefType() && pTypeParam.Covariant)
                {
                    UpperBoundInference(pSourceArg, pDestArg);
                }
                else if (pSourceArg.IsRefType() && pTypeParam.Contravariant)
                {
                    LowerBoundInference(pSourceArgs[arg], pDestArgs[arg]);
                }
                else
                {
                    ExactInference(pSourceArgs[arg], pDestArgs[arg]);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // Fixing
        //
        private bool Fix(int iParam)
        {
            Debug.Assert(IsUnfixed(iParam));

            // SPEC: An unfixed CType parameter with a set of bounds is fixed as follows:

            // SPEC:  The set of candidate types starts out as the set of all types in
            // SPEC:   the bounds.

            // SPEC:  We then examine each bound in turn. For each exact bound U of Xi,
            // SPEC:   all types which are not identical to U are removed from the candidate set.

            // Optimization: if we have two or more exact bounds, fixing is impossible.

            if (_pExactBounds[iParam].Count >= 2)
            {
                return false;
            }

            List<CType> initialCandidates = new List<CType>();

            // Optimization: if we have one exact bound then we need not add any
            // inexact bounds; we're just going to remove them anyway.

            if (_pExactBounds[iParam].IsEmpty())
            {
                HashSet<CType> typeSet = new HashSet<CType>();

                foreach (CType pCurrent in _pLowerBounds[iParam])
                {
                    if (typeSet.Add(pCurrent))
                    {
                        initialCandidates.Add(pCurrent);
                    }
                }

                foreach (CType pCurrent in _pUpperBounds[iParam])
                {
                    if (typeSet.Add(pCurrent))
                    {
                        initialCandidates.Add(pCurrent);
                    }
                }
            }
            else
            {
                initialCandidates.Add(_pExactBounds[iParam].Head());
            }

            if (initialCandidates.IsEmpty())
            {
                return false;
            }

            // SPEC:   For each lower bound U of Xi all types to which there is not an
            // SPEC:   implicit conversion from U are removed from the candidate set.

            foreach (CType pBound in _pLowerBounds[iParam])
            {
                List<CType> removeList = new List<CType>();
                foreach (CType pCandidate in initialCandidates)
                {
                    if (pBound != pCandidate && !_binder.canConvert(pBound, pCandidate))
                    {
                        removeList.Add(pCandidate);
                    }
                }
                foreach (CType pRemove in removeList)
                {
                    initialCandidates.Remove(pRemove);
                }
            }

            // SPEC:   For each upper bound U of Xi all types from which there is not an
            // SPEC:   implicit conversion to U are removed from the candidate set.
            foreach (CType pBound in _pUpperBounds[iParam])
            {
                List<CType> removeList = new List<CType>();
                foreach (CType pCandidate in initialCandidates)
                {
                    if (pBound != pCandidate && !_binder.canConvert(pCandidate, pBound))
                    {
                        removeList.Add(pCandidate);
                    }
                }
                foreach (CType pRemove in removeList)
                {
                    initialCandidates.Remove(pRemove);
                }
            }

            // SPEC:  If among the remaining candidate types there is a unique CType V from
            // SPEC:   which there is an implicit conversion to all the other candidate
            // SPEC:   types, then the parameter is fixed to V.

            CType pBest = null;
            foreach (CType pCandidate in initialCandidates)
            {
                foreach (CType pCandidate2 in initialCandidates)
                {
                    if (pCandidate != pCandidate2 && !_binder.canConvert(pCandidate2, pCandidate))
                    {
                        goto OuterBreak;
                    }
                }
                if (pBest != null)
                {
                    // best candidate is not unique
                    return false;
                }
                pBest = pCandidate;
            OuterBreak:
                ;
            }

            if (pBest == null)
            {
                // no best candidate
                return false;
            }

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // RUNTIME BINDER ONLY CHANGE
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // 
            // just as we fix each individual type parameter, we need to
            // ensure that we infer accessible type parameters, and so we
            // widen them when necessary using the same technique that we
            // used to alter the types at the beginning of binding. that
            // way we get an accessible type, and if it so happens that
            // the selected type is inappropriate (for conversions) then
            // we let overload resolution sort it out. 
            //
            // since we can never infer ref/out or pointer types here, we
            // are more or less guaranteed a best accessible type. However,
            // in the interest of safety, if it becomes impossible to
            // choose a "best accessible" type, then we will fail type
            // inference so we do not try to pass the inaccessible type
            // back to overload resolution.

            CType pBestAccessible;
            if (GetTypeManager().GetBestAccessibleType(_binder.GetSemanticChecker(), _binder.GetContext(), pBest, out pBestAccessible))
            {
                pBest = pBestAccessible;
            }
            else
            {
                Debug.Assert(false, "Method type inference could not find an accessible type over the best candidate in fixed");
                return false;
            }

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // END RUNTIME BINDER ONLY CHANGE
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            _pFixedResults[iParam] = pBest;
            UpdateDependenciesAfterFix(iParam);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // Helper methods
        //

        ////////////////////////////////////////////////////////////////////////////////


        private SymbolLoader GetSymbolLoader()
        {
            return _symbolLoader;
        }

        ////////////////////////////////////////////////////////////////////////////////

        private TypeManager GetTypeManager()
        {
            return GetSymbolLoader().GetTypeManager();
        }

        ////////////////////////////////////////////////////////////////////////////////

        private BSYMMGR GetGlobalSymbols()
        {
            return GetSymbolLoader().getBSymmgr();
        }
    }
}
