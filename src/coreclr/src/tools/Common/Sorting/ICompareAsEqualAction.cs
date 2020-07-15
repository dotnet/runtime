// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace ILCompiler
{
    internal interface ICompareAsEqualAction
    {
        void CompareAsEqual();
    }

    internal struct RequireTotalOrderAssert : ICompareAsEqualAction
    {
        public void CompareAsEqual()
        {
            Debug.Assert(false);
        }
    }

    internal struct AllowDuplicates : ICompareAsEqualAction
    {
        public void CompareAsEqual()
        {
        }
    }
}
