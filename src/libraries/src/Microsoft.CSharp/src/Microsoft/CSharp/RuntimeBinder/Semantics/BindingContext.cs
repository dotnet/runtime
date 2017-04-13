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
            SymbolLoader = semanticChecker.GetSymbolLoader();
        }

        public BindingContext(BindingContext parent)
        {
            Debug.Assert(parent.SemanticChecker != null);
            ExprFactory = parent.ExprFactory;
            ReportUnsafeErrors = parent.ReportUnsafeErrors;
            ContextForMemberLookup = parent.ContextForMemberLookup;
            CheckedNormal = parent.CheckedNormal;
            CheckedConstant = parent.CheckedConstant;
            SymbolLoader = (SemanticChecker = parent.SemanticChecker).GetSymbolLoader();
        }


        //The SymbolLoader can be retrieved from SemanticChecker,
        //but that is a virtual call that is showing up on the profiler. Retrieve
        //the SymbolLoader once at construction and return a cached copy.

        // PERFORMANCE: Is this cache still necessary?
        public SymbolLoader SymbolLoader { get; }

        public Declaration ContextForMemberLookup { get; set; }

        public bool ReportUnsafeErrors { get; set; } = true;

        public CSemanticChecker SemanticChecker { get; }

        public ExprFactory ExprFactory { get; }

        public bool CheckedNormal { get; set; }

        public bool CheckedConstant { get; set; }
    }
}
