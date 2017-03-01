// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // This is the interface for the BindingContext, which is
    // consumed by the StatementBinder.
    // ----------------------------------------------------------------------------

    internal sealed class OutputContext
    {
        public LocalVariableSymbol m_pThisPointer;
        public MethodSymbol m_pCurrentMethodSymbol;
        public bool m_bUnsafeErrorGiven;
    };

    internal enum UNSAFESTATES
    {
        UNSAFESTATES_Unsafe,
        UNSAFESTATES_Safe,
        UNSAFESTATES_Unknown,
    };

    internal class BindingContext
    {
        public static BindingContext CreateInstance(
                CSemanticChecker pSemanticChecker,
                ExprFactory exprFactory,
                OutputContext outputContext,
                NameGenerator nameGenerator,
                bool bflushLocalVariableTypesForEachStatement,
                bool bAllowUnsafeBlocks,
                bool bIsOptimizingSwitchAndArrayInit,
                bool bShowReachability,
                bool bWrapNonExceptionThrows,
                bool bInRefactoring,
                KAID aidLookupContext
            )
        {
            return new BindingContext(
                pSemanticChecker,
                exprFactory,
                outputContext,
                nameGenerator,
                bflushLocalVariableTypesForEachStatement,
                bAllowUnsafeBlocks,
                bIsOptimizingSwitchAndArrayInit,
                bShowReachability,
                bWrapNonExceptionThrows,
                bInRefactoring,
                aidLookupContext);
        }

        private BindingContext(
            CSemanticChecker pSemanticChecker,
            ExprFactory exprFactory,
            OutputContext outputContext,
            NameGenerator nameGenerator,
            bool bflushLocalVariableTypesForEachStatement,
            bool bAllowUnsafeBlocks,
            bool bIsOptimizingSwitchAndArrayInit,
            bool bShowReachability,
            bool bWrapNonExceptionThrows,
            bool bInRefactoring,
            KAID aidLookupContext
            )
        {
            m_ExprFactory = exprFactory;
            m_outputContext = outputContext;
            m_pNameGenerator = nameGenerator;
            m_pInputFile = null;
            m_pParentDecl = null;
            m_pContainingAgg = null;
            m_pCurrentSwitchType = null;
            m_pOriginalConstantField = null;
            m_pCurrentFieldSymbol = null;
            m_pImplicitlyTypedLocal = null;
            m_pOuterScope = null;
            m_pFinallyScope = null;
            m_pTryScope = null;
            m_pCatchScope = null;
            m_pCurrentScope = null;
            m_pSwitchScope = null;
            m_pCurrentBlock = null;
            m_UnsafeState = UNSAFESTATES.UNSAFESTATES_Unknown;
            m_FinallyNestingCount = 0;
            m_bInsideTryOfCatch = false;
            m_bInFieldInitializer = false;
            m_bInBaseConstructorCall = false;
            m_bAllowUnsafeBlocks = bAllowUnsafeBlocks;
            m_bIsOptimizingSwitchAndArrayInit = bIsOptimizingSwitchAndArrayInit;
            m_bShowReachability = bShowReachability;
            m_bWrapNonExceptionThrows = bWrapNonExceptionThrows;
            m_bInRefactoring = bInRefactoring;
            m_bInAttribute = false;
            m_bRespectSemanticsAndReportErrors = true;
            m_bflushLocalVariableTypesForEachStatement = bflushLocalVariableTypesForEachStatement;
            m_ppamis = null;
            m_pamiCurrent = null;
            m_pInitType = null;
            m_returnErrorSink = null;

            Debug.Assert(pSemanticChecker != null);
            this.SemanticChecker = pSemanticChecker;
            this.SymbolLoader = SemanticChecker.GetSymbolLoader();
            m_outputContext.m_pThisPointer = null;
            m_outputContext.m_pCurrentMethodSymbol = null;

            m_aidExternAliasLookupContext = aidLookupContext;
            CheckedNormal = false;
            CheckedConstant = false;
        }
        protected BindingContext(BindingContext parent)
        {
            m_ExprFactory = parent.m_ExprFactory;
            m_outputContext = parent.m_outputContext;
            m_pNameGenerator = parent.m_pNameGenerator;
            m_pInputFile = parent.m_pInputFile;
            m_pParentDecl = parent.m_pParentDecl;
            m_pContainingAgg = parent.m_pContainingAgg;
            m_pCurrentSwitchType = parent.m_pCurrentSwitchType;
            m_pOriginalConstantField = parent.m_pOriginalConstantField;
            m_pCurrentFieldSymbol = parent.m_pCurrentFieldSymbol;
            m_pImplicitlyTypedLocal = parent.m_pImplicitlyTypedLocal;
            m_pOuterScope = parent.m_pOuterScope;
            m_pFinallyScope = parent.m_pFinallyScope;
            m_pTryScope = parent.m_pTryScope;
            m_pCatchScope = parent.m_pCatchScope;
            m_pCurrentScope = parent.m_pCurrentScope;
            m_pSwitchScope = parent.m_pSwitchScope;
            m_pCurrentBlock = parent.m_pCurrentBlock;
            m_ppamis = parent.m_ppamis;
            m_pamiCurrent = parent.m_pamiCurrent;
            m_UnsafeState = parent.m_UnsafeState;
            m_FinallyNestingCount = parent.m_FinallyNestingCount;
            m_bInsideTryOfCatch = parent.m_bInsideTryOfCatch;
            m_bInFieldInitializer = parent.m_bInFieldInitializer;
            m_bInBaseConstructorCall = parent.m_bInBaseConstructorCall;
            CheckedNormal = parent.CheckedNormal;
            CheckedConstant = parent.CheckedConstant;
            m_aidExternAliasLookupContext = parent.m_aidExternAliasLookupContext;

            m_bAllowUnsafeBlocks = parent.m_bAllowUnsafeBlocks;
            m_bIsOptimizingSwitchAndArrayInit = parent.m_bIsOptimizingSwitchAndArrayInit;
            m_bShowReachability = parent.m_bShowReachability;
            m_bWrapNonExceptionThrows = parent.m_bWrapNonExceptionThrows;
            m_bflushLocalVariableTypesForEachStatement = parent.m_bflushLocalVariableTypesForEachStatement;
            m_bInRefactoring = parent.m_bInRefactoring;
            m_bInAttribute = parent.m_bInAttribute;
            m_bRespectSemanticsAndReportErrors = parent.m_bRespectSemanticsAndReportErrors;
            m_pInitType = parent.m_pInitType;
            m_returnErrorSink = parent.m_returnErrorSink;

            Debug.Assert(parent.SemanticChecker != null);
            this.SemanticChecker = parent.SemanticChecker;
            this.SymbolLoader = SemanticChecker.GetSymbolLoader();
        }


        //The SymbolLoader can be retrieved from m_pSemanticChecker,
        //but that is a virtual call that is showing up on the profiler. Retrieve
        //the SymbolLoader once at ruction and return a cached copy.

        // PERFORMANCE: Is this cache still necessary?
        public SymbolLoader SymbolLoader { get; private set; }
        public Declaration m_pParentDecl;
        public Declaration ContextForMemberLookup() { return m_pParentDecl; }

        public OutputContext GetOutputContext()
        {
            return m_outputContext;
        }
        // Virtual Dispose method - this should only be called by FNCBRECCS's StatePusher.
        // It is used to clean up state in the output context for each overridden context.
        public virtual void Dispose()
        {
        }

        // State boolean questions.

        public bool InMethod()
        {
            return m_outputContext.m_pCurrentMethodSymbol != null;
        }
        public bool InStaticMethod()
        {
            return m_outputContext.m_pCurrentMethodSymbol != null &&
                m_outputContext.m_pCurrentMethodSymbol.isStatic;
        }
        public bool InConstructor()
        {
            return m_outputContext.m_pCurrentMethodSymbol != null &&
                m_outputContext.m_pCurrentMethodSymbol.IsConstructor();
        }
        public bool InAnonymousMethod()
        {
            return null != m_pamiCurrent;
        }
        public bool InFieldInitializer()
        {
            return m_bInFieldInitializer;
        }
        public bool IsThisPointer(EXPR expr)
        {
            bool localThis = expr.isANYLOCAL() && expr.asANYLOCAL().local == m_outputContext.m_pThisPointer;
            bool baseThis = false;
            return localThis || baseThis;
        }
        public bool RespectReadonly()
        {
            return m_bRespectSemanticsAndReportErrors;
        }
        public bool IsUnsafeContext()
        {
            return m_UnsafeState == UNSAFESTATES.UNSAFESTATES_Unsafe;
        }
        public bool ReportUnsafeErrors()
        {
            return !m_outputContext.m_bUnsafeErrorGiven && m_bRespectSemanticsAndReportErrors;
        }

        public AggregateSymbol ContainingAgg()
        {
            return m_pContainingAgg;
        }
        public LocalVariableSymbol GetThisPointer()
        {
            return m_outputContext.m_pThisPointer;
        }

        // Unsafe.
        public UNSAFESTATES GetUnsafeState()
        {
            return m_UnsafeState;
        }

        private KAID m_aidExternAliasLookupContext { get; }

        // Members.

        private ExprFactory m_ExprFactory;
        private OutputContext m_outputContext;
        private NameGenerator m_pNameGenerator;

        // Methods.

        private InputFile m_pInputFile;

        // symbols.

        // The parent declaration, for various name binding uses. This is either an
        // AggregateDeclaration (if parentAgg is non-null), or an NamespaceDeclaration (if parentAgg 
        // is null).
        // Note that parentAgg isn't enough for name binding if partial classes 
        // are used, because the using clauses in effect may be different and 
        // unsafe state may be different.  

        private AggregateSymbol m_pContainingAgg;
        private CType m_pCurrentSwitchType;
        private FieldSymbol m_pOriginalConstantField;
        private FieldSymbol m_pCurrentFieldSymbol;

        // If we are in a context where we are binding the right hand side of a declaration
        // like var y = (y=5), we need to keep track of what local we are attempting to
        // infer the type of, so that we can give an error if that local is used on the
        // right hand side.
        private LocalVariableSymbol m_pImplicitlyTypedLocal;

        // Scopes.

        private Scope m_pOuterScope;
        private Scope m_pFinallyScope; // innermost finally, or pOuterScope if none...
        private Scope m_pTryScope;     // innermost try, or pOuterScope if none...
        private Scope m_pCatchScope;   // innermost catch, or null if none
        private Scope m_pCurrentScope; // current scope
        private Scope m_pSwitchScope;  // innermost switch, or null if none

        private EXPRBLOCK m_pCurrentBlock;

        // m_ppamis points to the list of child anonymous methods of the current context.
        // That is, m_ppamis is where we will add an anonymous method should we find a
        // new one while binding.  If we are presently binding a nominal method then
        // m_ppamis points to the methinfo.pamis.  If we are presently binding an
        // anonymous method then it points to m_pamiCurrent.pamis.  If we are presently
        // in a context in which anonymous methods cannot occur (eg, binding an attribute)
        // then it is null.
        private List<EXPRBOUNDLAMBDA> m_ppamis;
        // If we are presently binding an anonymous method body then m_pamiCurrent points
        // to the anon meth info.  If we are binding either a method body or some other
        // statement context (eg, binding an attribute, etc) then m_pamiCurrent is null.
        private EXPRBOUNDLAMBDA m_pamiCurrent;

        // Unsafe states.

        private UNSAFESTATES m_UnsafeState;

        // Variable Counters.

        private int m_FinallyNestingCount;

        // The rest of the members.

        private bool m_bInsideTryOfCatch;
        private bool m_bInFieldInitializer;
        private bool m_bInBaseConstructorCall;
        private bool m_bAllowUnsafeBlocks;
        private bool m_bIsOptimizingSwitchAndArrayInit;
        private bool m_bShowReachability;
        private bool m_bWrapNonExceptionThrows;
        private bool m_bInRefactoring;
        private bool m_bInAttribute;

        private bool m_bflushLocalVariableTypesForEachStatement;
        private bool m_bRespectSemanticsAndReportErrors; // False if we're in the EE.

        private CType m_pInitType;

        private IErrorSink m_returnErrorSink;

        public CSemanticChecker SemanticChecker { get; }

        public ExprFactory GetExprFactory() { return m_ExprFactory; }

        public bool CheckedNormal { get; set; }
        public bool CheckedConstant { get; set; }
    }
}
