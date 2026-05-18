// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer.DataFlow
{
    // This class is responsible for the interprocedural analysis of local variables.
    // It substitutes type arguments into the generic forward dataflow analysis,
    // creating a simpler abstraction that can track the values of local variables using Roslyn APIs.
    // The kinds of values tracked are still left as unspecified generic parameters TValue and TLattice.
    public abstract class LocalDataFlowAnalysis<TValue, TContext, TLattice, TContextLattice, TTransfer, TConditionValue>
        : ForwardDataFlowAnalysis<
            LocalStateAndContext<TValue, TContext>,
            LocalDataFlowState<TValue, TContext, TLattice, TContextLattice>,
            LocalStateAndContextLattice<TValue, TContext, TLattice, TContextLattice>,
            BlockProxy,
            RegionProxy,
            ControlFlowGraphProxy,
            TTransfer,
            TConditionValue
        >
        where TValue : struct, IEquatable<TValue>
        where TContext : struct, IEquatable<TContext>
        where TLattice : ILattice<TValue>
        where TContextLattice : ILattice<TContext>
        where TTransfer : LocalDataFlowVisitor<TValue, TContext, TLattice, TContextLattice, TConditionValue>
        where TConditionValue : struct, INegate<TConditionValue>
    {
        protected readonly OperationBlockAnalysisContext Context;

        private readonly IOperation OperationBlock;

        private static LocalStateAndContextLattice<TValue, TContext, TLattice, TContextLattice> GetLatticeAndEntryValue(
            TLattice lattice,
            TContextLattice contextLattice,
            TContext initialContext,
            out LocalStateAndContext<TValue, TContext> entryValue)
        {
            LocalStateAndContextLattice<TValue, TContext, TLattice, TContextLattice> localStateAndContextLattice = new(new(lattice), contextLattice);
            entryValue = new LocalStateAndContext<TValue, TContext>(default(LocalState<TValue>), initialContext);
            return localStateAndContextLattice;
        }

        // The initial value of the local dataflow is the empty local state (no tracked assignments),
        // with an initial context that must be specified by the derived class.
        protected LocalDataFlowAnalysis(
            OperationBlockAnalysisContext context,
            IOperation operationBlock,
            TLattice lattice,
            TContextLattice contextLattice,
            TContext initialContext)
            : base(GetLatticeAndEntryValue(lattice, contextLattice, initialContext, out var entryValue), entryValue)
        {
            Context = context;
            OperationBlock = operationBlock;
        }

        public bool InterproceduralAnalyze()
        {
            bool succeeded = true;
            ValueSetLattice<MethodBodyValue> methodGroupLattice = default;
            DictionaryLattice<LocalKey, Maybe<TValue>, MaybeLattice<TValue, TLattice>> hoistedLocalLattice = default;
            var interproceduralStateLattice = new InterproceduralStateLattice<TValue, TLattice>(
                methodGroupLattice, hoistedLocalLattice);
            var interproceduralState = interproceduralStateLattice.Top;

            var oldInterproceduralState = interproceduralState.Clone();

            if (OperationBlock is IAttributeOperation attribute)
            {
                succeeded &= AnalyzeAttribute(Context.OwningSymbol, attribute);
                return succeeded;
            }

            if (!TryGetDataFlowOwningSymbol(Context.OwningSymbol, out ISymbol owningSymbol))
                return succeeded;

            Debug.Assert(owningSymbol is not IMethodSymbol methodSymbol ||
                methodSymbol.MethodKind is not (MethodKind.LambdaMethod or MethodKind.LocalFunction));
            var startMethod = new MethodBodyValue(owningSymbol, Context.GetControlFlowGraph(OperationBlock));
            interproceduralState.TrackMethod(startMethod);

            while (!interproceduralState.Equals(oldInterproceduralState))
            {
                oldInterproceduralState = interproceduralState.Clone();

                Debug.Assert(!oldInterproceduralState.Methods.IsUnknown());
                foreach (var method in oldInterproceduralState.Methods.GetKnownValues())
                {
                    succeeded &= AnalyzeMethod(method, ref interproceduralState);
                }
            }
            return succeeded;
        }

        private static bool TryGetDataFlowOwningSymbol(ISymbol owningSymbol, out ISymbol dataFlowOwningSymbol)
        {
            dataFlowOwningSymbol = owningSymbol;

            if (owningSymbol is not INamedTypeSymbol namedType)
                return true;

            // Delegate types with default parameter values produce operation blocks whose
            // OwningSymbol is the delegate INamedTypeSymbol. These blocks contain only
            // simple constant initializers with no interesting dataflow, so skip them.
            if (namedType.TypeKind == TypeKind.Delegate)
                return false;

            // Primary constructors produce operation blocks whose OwningSymbol is the
            // containing INamedTypeSymbol. Analyze those blocks as the primary constructor
            // so that parameters and instance references are handled like normal constructors.
            foreach (IMethodSymbol constructor in namedType.InstanceConstructors)
            {
                if (constructor.IsImplicitlyDeclared)
                    continue;

                foreach (SyntaxReference syntaxReference in constructor.DeclaringSyntaxReferences)
                {
                    if (syntaxReference.GetSyntax() is TypeDeclarationSyntax { ParameterList: not null })
                    {
                        dataFlowOwningSymbol = constructor;
                        return true;
                    }
                }
            }

            // Let MethodBodyValue assert with the control-flow graph context if Roslyn
            // introduces another named-type-owned operation block shape.
            return true;
        }

        private bool AnalyzeAttribute(ISymbol owningSymbol, IAttributeOperation attribute)
        {
            var cfg = Context.GetControlFlowGraph(attribute);
            var lValueFlowCaptures = LValueFlowCapturesProvider.CreateLValueFlowCaptures(cfg);
            var visitor = GetVisitor(owningSymbol, cfg, lValueFlowCaptures, default);
            return Fixpoint(new ControlFlowGraphProxy(cfg), visitor);
        }

        private bool AnalyzeMethod(MethodBodyValue method, ref InterproceduralState<TValue, TLattice> interproceduralState)
        {
            var cfg = method.ControlFlowGraph;
            var lValueFlowCaptures = LValueFlowCapturesProvider.CreateLValueFlowCaptures(cfg);
            var visitor = GetVisitor(method.OwningSymbol, cfg, lValueFlowCaptures, interproceduralState);
            bool succeeded = Fixpoint(new ControlFlowGraphProxy(cfg), visitor);

            // The interprocedural state struct is stored as a field of the visitor and modified
            // in-place there, but we also need those modifications to be reflected here.
            interproceduralState = visitor.InterproceduralState;
            return succeeded;
        }

        protected abstract TTransfer GetVisitor(
            ISymbol owningSymbol,
            ControlFlowGraph methodCFG,
            ImmutableDictionary<CaptureId, FlowCaptureKind> lValueFlowCaptures,
            InterproceduralState<TValue, TLattice> interproceduralState);
    }
}
