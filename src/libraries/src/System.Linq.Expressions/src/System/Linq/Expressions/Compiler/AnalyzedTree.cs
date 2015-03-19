// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Compiler
{
    internal sealed class AnalyzedTree
    {
        internal readonly Dictionary<object, CompilerScope> Scopes = new Dictionary<object, CompilerScope>();
        internal readonly Dictionary<LambdaExpression, BoundConstants> Constants = new Dictionary<LambdaExpression, BoundConstants>();

        // Created by VariableBinder
        internal AnalyzedTree()
        {
        }
    }
}
