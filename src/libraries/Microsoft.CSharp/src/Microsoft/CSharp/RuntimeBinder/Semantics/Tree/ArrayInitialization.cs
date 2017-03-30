// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprArrayInit : Expr
    {
        public Expr OptionalArguments { get; set; }

        public Expr OptionalArgumentDimensions { get; set; }

        // The Exprs bound as the size of the array.
        public int[] DimensionSizes { get; set; }

        public int DimensionSize { get; set; }

        public bool GeneratedForParamArray { get; set; }
    }
}
