// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class CompilerComparer : TypeSystemComparer, IComparer<ISortableNode>
    {
        public static new CompilerComparer Instance { get; } = new CompilerComparer();

        public int Compare(ISortableNode x, ISortableNode y)
        {
            if (x == y)
            {
                return 0;
            }

            int codeX = x.ClassCode;
            int codeY = y.ClassCode;
            if (codeX == codeY)
            {
                Debug.Assert(x.GetType() == y.GetType());

                int result = x.CompareToImpl(y, this);

                // We did a reference equality check above so an "Equal" result is not expected
                Debug.Assert(result != 0);

                return result;
            }
            else
            {
                Debug.Assert(x.GetType() != y.GetType());
                return codeY > codeX ? -1 : 1;
            }
        }
    }
}
