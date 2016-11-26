// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal partial class ExpressionBinder
    {
        // ----------------------------------------------------------------------------
        // This class takes an EXPRMEMGRP and a set of arguments and binds the arguments
        // to the best applicable method in the group.
        // ----------------------------------------------------------------------------

        internal class GroupToArgsBinderResult
        {
            public MethPropWithInst BestResult;
            public MethPropWithInst GetBestResult() { return BestResult; }
            public MethPropWithInst AmbiguousResult;
            public MethPropWithInst GetAmbiguousResult() { return AmbiguousResult; }
            public MethPropWithInst InaccessibleResult;
            public MethPropWithInst GetInaccessibleResult() { return InaccessibleResult; }
            public MethPropWithInst UninferableResult;
            public MethPropWithInst GetUninferableResult() { return UninferableResult; }
            public MethPropWithInst InconvertibleResult;
            public GroupToArgsBinderResult()
            {
                BestResult = new MethPropWithInst();
                AmbiguousResult = new MethPropWithInst();
                InaccessibleResult = new MethPropWithInst();
                UninferableResult = new MethPropWithInst();
                InconvertibleResult = new MethPropWithInst();
                _inconvertibleResults = new List<MethPropWithInst>();
            }

            private readonly List<MethPropWithInst> _inconvertibleResults;

            /////////////////////////////////////////////////////////////////////////////////

            public void AddInconvertibleResult(
                MethodSymbol method,
                AggregateType currentType,
                TypeArray currentTypeArgs)
            {
                if (InconvertibleResult.Sym == null)
                {
                    // This is the first one, so set it for error reporting usage.
                    InconvertibleResult.Set(method, currentType, currentTypeArgs);
                }
                _inconvertibleResults.Add(new MethPropWithInst(method, currentType, currentTypeArgs));
            }

            /////////////////////////////////////////////////////////////////////////////////

            private static int NumberOfErrorTypes(TypeArray pTypeArgs)
            {
                int nCount = 0;
                for (int i = 0; i < pTypeArgs.Size; i++)
                {
                    if (pTypeArgs.Item(i).IsErrorType())
                    {
                        nCount++;
                    }
                }
                return nCount;
            }

            private static bool IsBetterThanCurrent(TypeArray pTypeArgs1, TypeArray pTypeArgs2)
            {
                int leftErrors = NumberOfErrorTypes(pTypeArgs1);
                int rightErrors = NumberOfErrorTypes(pTypeArgs2);

                if (leftErrors == rightErrors)
                {
                    int max = pTypeArgs1.Size > pTypeArgs2.Size ? pTypeArgs2.Size : pTypeArgs1.Size;

                    // If we don't have a winner yet, go through each element's type args.
                    for (int i = 0; i < max; i++)
                    {
                        if (pTypeArgs1.Item(i).IsAggregateType())
                        {
                            leftErrors += NumberOfErrorTypes(pTypeArgs1.Item(i).AsAggregateType().GetTypeArgsAll());
                        }
                        if (pTypeArgs2.Item(i).IsAggregateType())
                        {
                            rightErrors += NumberOfErrorTypes(pTypeArgs2.Item(i).AsAggregateType().GetTypeArgsAll());
                        }
                    }
                }
                return rightErrors < leftErrors;
            }

            public bool IsBetterUninferableResult(TypeArray pTypeArguments)
            {
                if (UninferableResult.Sym == null)
                {
                    // If we don't even have a result, then its definitely better.
                    return true;
                }
                if (pTypeArguments == null)
                {
                    return false;
                }
                return IsBetterThanCurrent(UninferableResult.TypeArgs, pTypeArguments);
            }
        }
    }
}
