// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using MissingDependency.Leaf;

namespace MissingDependency.Mid
{
    public class MidClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string UseLeaf()
        {
            return new LeafClass().GetValue();
        }
    }
}
