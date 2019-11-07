// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal readonly partial struct ExpressionBinder
    {
        // ----------------------------------------------------------------------------
        // This class takes an EXPRMEMGRP and a set of arguments and binds the arguments
        // to the best applicable method in the group.
        // ----------------------------------------------------------------------------

        internal sealed class GroupToArgsBinderResult
        {
            public MethPropWithInst BestResult { get; set; }

            public MethPropWithInst InaccessibleResult { get; }

            public MethPropWithInst UninferableResult { get; }

            public GroupToArgsBinderResult()
            {
                BestResult = new MethPropWithInst();
                InaccessibleResult = new MethPropWithInst();
                UninferableResult = new MethPropWithInst();
            }

            private static int NumberOfErrorTypes(TypeArray pTypeArgs)
            {
                int nCount = 0;
                for (int i = 0; i < pTypeArgs.Count; i++)
                {
                    if (pTypeArgs[i] == null)
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
                    int max = pTypeArgs1.Count > pTypeArgs2.Count ? pTypeArgs2.Count : pTypeArgs1.Count;

                    // If we don't have a winner yet, go through each element's type args.
                    for (int i = 0; i < max; i++)
                    {
                        if (pTypeArgs1[i] is AggregateType aggArg1)
                        {
                            leftErrors += NumberOfErrorTypes(aggArg1.TypeArgsAll);
                        }
                        if (pTypeArgs2[i] is AggregateType aggArg2)
                        {
                            rightErrors += NumberOfErrorTypes(aggArg2.TypeArgsAll);
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
