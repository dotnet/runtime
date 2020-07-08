// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    partial class SortableDependencyNode
    {
        // Custom sort order. Used to override the default sorting mechanics.
        public int CustomSort = int.MaxValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static partial void ApplyCustomSort(SortableDependencyNode x, SortableDependencyNode y, ref int result)
        {
            result = x.CustomSort.CompareTo(y.CustomSort);
        }
    }
}
