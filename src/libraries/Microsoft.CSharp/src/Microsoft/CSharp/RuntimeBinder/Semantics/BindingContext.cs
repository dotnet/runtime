// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // This is the interface for the BindingContext, which is
    // consumed by the StatementBinder.
    // ----------------------------------------------------------------------------

    internal sealed class BindingContext
    {
        public BindingContext(CSemanticChecker semanticChecker, ExprFactory exprFactory)
        {
            Debug.Assert(semanticChecker != null);
            ExprFactory = exprFactory;
            SemanticChecker = semanticChecker;
            SymbolLoader = semanticChecker.SymbolLoader;
        }

        public BindingContext(BindingContext parent)
            : this(parent.SemanticChecker, parent.ExprFactory)
        {
            // We copy the context object, but leave checking false.
            ContextForMemberLookup = parent.ContextForMemberLookup;
        }

        //The SymbolLoader can be retrieved from SemanticChecker,
        //but that is a virtual call that is showing up on the profiler. Retrieve
        //the SymbolLoader once at construction and return a cached copy.

        // PERFORMANCE: Is this cache still necessary?
        public SymbolLoader SymbolLoader { get; }

        public AggregateDeclaration ContextForMemberLookup { get; set; }

        public CSemanticChecker SemanticChecker { get; }

        public ExprFactory ExprFactory { get; }

        public bool Checked { get; set; }
    }
}
