// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using static System.Linq.Expressions.CachedReflectionInfo;

namespace System.Linq.Expressions.Compiler
{
    internal enum VariableStorageKind
    {
        Local,
        Hoisted
    }

    /// <summary>
    /// CompilerScope is the data structure which the Compiler keeps information
    /// related to compiling scopes. It stores the following information:
    ///   1. Parent relationship (for resolving variables)
    ///   2. Information about hoisted variables
    ///   3. Information for resolving closures
    ///
    /// Instances are produced by VariableBinder, which does a tree walk
    /// looking for scope nodes: LambdaExpression, BlockExpression, and CatchBlock.
    /// </summary>
    [RequiresDynamicCode(Expression.StrongBoxRequiresDynamicCode)]
    internal sealed partial class CompilerScope
    {
        /// <summary>
        /// parent scope, if any
        /// </summary>
        private CompilerScope? _parent;

        /// <summary>
        /// The expression node for this scope
        /// Can be LambdaExpression, BlockExpression, or CatchBlock
        /// </summary>
        internal readonly object Node;

        /// <summary>
        /// True if this node corresponds to an IL method.
        /// Can only be true if the Node is a LambdaExpression.
        /// But inlined lambdas will have it set to false.
        /// </summary>
        internal readonly bool IsMethod;

        /// <summary>
        /// Does this scope (or any inner scope) close over variables from any
        /// parent scope?
        /// Populated by VariableBinder
        /// </summary>
        internal bool NeedsClosure;

        /// <summary>
        /// Variables defined in this scope, and whether they're hoisted or not
        /// Populated by VariableBinder
        /// </summary>
        internal readonly Dictionary<ParameterExpression, VariableStorageKind> Definitions = new Dictionary<ParameterExpression, VariableStorageKind>();

        /// <summary>
        /// Each variable referenced within this scope, and how often it was referenced
        /// Populated by VariableBinder
        /// </summary>
        internal Dictionary<ParameterExpression, int>? ReferenceCount;

        /// <summary>
        /// Scopes whose variables were merged into this one
        ///
        /// Created lazily as we create hundreds of compiler scopes w/o merging scopes when compiling rules.
        /// </summary>
        internal HashSet<BlockExpression>? MergedScopes;

        /// <summary>
        /// The scope's hoisted locals, if any.
        /// Provides storage for variables that are referenced from nested lambdas
        /// </summary>
        private HoistedLocals? _hoistedLocals;

        /// <summary>
        /// The closed over hoisted locals
        /// </summary>
        private HoistedLocals? _closureHoistedLocals;

        /// <summary>
        /// Mutable dictionary that maps non-hoisted variables to either local
        /// slots or argument slots
        /// </summary>
        private readonly Dictionary<ParameterExpression, Storage> _locals = new Dictionary<ParameterExpression, Storage>();

        internal CompilerScope(object node, bool isMethod)
        {
            Node = node;
            IsMethod = isMethod;
            IReadOnlyList<ParameterExpression> variables = GetVariables(node);

            Definitions = new Dictionary<ParameterExpression, VariableStorageKind>(variables.Count);
            foreach (ParameterExpression v in variables)
            {
                Definitions.Add(v, VariableStorageKind.Local);
            }
        }

        /// <summary>
        /// This scope's hoisted locals, or the closed over locals, if any
        /// Equivalent to: _hoistedLocals ?? _closureHoistedLocals
        /// </summary>
        internal HoistedLocals? NearestHoistedLocals
        {
            get { return _hoistedLocals ?? _closureHoistedLocals; }
        }

        /// <summary>
        /// Called when entering a lambda/block. Performs all variable allocation
        /// needed, including creating hoisted locals and IL locals for accessing
        /// parent locals
        /// </summary>
        internal CompilerScope Enter(LambdaCompiler lc, CompilerScope? parent)
        {
            SetParent(lc, parent);

            AllocateLocals(lc);

            if (IsMethod && _closureHoistedLocals != null)
            {
                EmitClosureAccess(lc, _closureHoistedLocals);
            }

            EmitNewHoistedLocals(lc);

            if (IsMethod)
            {
                EmitCachedVariables();
            }

            return this;
        }

        /// <summary>
        /// Frees unnamed locals, clears state associated with this compiler
        /// </summary>
        internal CompilerScope? Exit()
        {
            // free scope's variables
            if (!IsMethod)
            {
                foreach (Storage storage in _locals.Values)
                {
                    storage.FreeLocal();
                }
            }

            // Clear state that is associated with this parent
            // (because the scope can be reused in another context)
            CompilerScope? parent = _parent;
            _parent = null;
            _hoistedLocals = null;
            _closureHoistedLocals = null;
            _locals.Clear();

            return parent;
        }

        #region RuntimeVariablesExpression support

        internal void EmitVariableAccess(LambdaCompiler lc, ReadOnlyCollection<ParameterExpression> vars)
        {
            if (NearestHoistedLocals != null && vars.Count > 0)
            {
                // Find what array each variable is on & its index
                var indexes = new ArrayBuilder<long>(vars.Count);

                foreach (ParameterExpression variable in vars)
                {
                    // For each variable, find what array it's defined on
                    ulong parents = 0;
                    HoistedLocals? locals = NearestHoistedLocals;
                    while (!locals.Indexes.ContainsKey(variable))
                    {
                        parents++;
                        locals = locals.Parent;
                        Debug.Assert(locals != null);
                    }

                    // combine the number of parents we walked, with the
                    // real index of variable to get the index to emit.
                    ulong index = (parents << 32) | (uint)locals.Indexes[variable];

                    indexes.UncheckedAdd((long)index);
                }

                EmitGet(NearestHoistedLocals.SelfVariable);
                lc.EmitConstantArray(indexes.ToArray());
                lc.IL.Emit(OpCodes.Call, RuntimeOps_CreateRuntimeVariables_ObjectArray_Int64Array);
            }
            else
            {
                // No visible variables
                lc.IL.Emit(OpCodes.Call, RuntimeOps_CreateRuntimeVariables);
            }
        }

        #endregion

        #region Variable access

        /// <summary>
        /// Adds a new virtual variable corresponding to an IL local
        /// </summary>
        internal void AddLocal(LambdaCompiler gen, ParameterExpression variable)
        {
            _locals.Add(variable, new LocalStorage(gen, variable));
        }

        internal void EmitGet(ParameterExpression variable)
        {
            ResolveVariable(variable).EmitLoad();
        }

        internal void EmitSet(ParameterExpression variable)
        {
            ResolveVariable(variable).EmitStore();
        }

        internal void EmitAddressOf(ParameterExpression variable)
        {
            ResolveVariable(variable).EmitAddress();
        }

        private Storage ResolveVariable(ParameterExpression variable)
        {
            return ResolveVariable(variable, NearestHoistedLocals);
        }

        /// <summary>
        /// Resolve a local variable in this scope or a closed over scope
        /// Throws if the variable is not defined
        /// </summary>
        private Storage ResolveVariable(ParameterExpression variable, HoistedLocals? hoistedLocals)
        {
            // Search IL locals and arguments, but only in this lambda
            for (CompilerScope? s = this; s != null; s = s._parent)
            {
                if (s._locals.TryGetValue(variable, out Storage? storage))
                {
                    return storage;
                }

                // if this is a lambda, we're done
                if (s.IsMethod)
                {
                    break;
                }
            }

            // search hoisted locals
            for (HoistedLocals? h = hoistedLocals; h != null; h = h.Parent)
            {
                int index;
                if (h.Indexes.TryGetValue(variable, out index))
                {
                    return new ElementBoxStorage(
                        ResolveVariable(h.SelfVariable, hoistedLocals),
                        index,
                        variable
                    );
                }
            }

            //
            // If this is an unbound variable in the lambda, the error will be
            // thrown from VariableBinder. So an error here is generally caused
            // by an internal error, e.g. a scope was created but it bypassed
            // VariableBinder.
            //
            throw Error.UndefinedVariable(variable.Name, variable.Type, CurrentLambdaName);
        }

        #endregion

        private void SetParent(LambdaCompiler lc, CompilerScope? parent)
        {
            Debug.Assert(_parent == null && parent != this);
            _parent = parent;

            if (NeedsClosure && _parent != null)
            {
                _closureHoistedLocals = _parent.NearestHoistedLocals;
            }

            ReadOnlyCollection<ParameterExpression> hoistedVars = GetVariables().Where(p => Definitions[p] == VariableStorageKind.Hoisted).ToReadOnly();

            if (hoistedVars.Count > 0)
            {
                _hoistedLocals = new HoistedLocals(_closureHoistedLocals, hoistedVars);
                AddLocal(lc, _hoistedLocals.SelfVariable);
            }
        }

        // Emits creation of the hoisted local storage
        private void EmitNewHoistedLocals(LambdaCompiler lc)
        {
            if (_hoistedLocals == null)
            {
                return;
            }

            // create the array
            lc.IL.EmitPrimitive(_hoistedLocals.Variables.Count);
            lc.IL.Emit(OpCodes.Newarr, typeof(object));

            // initialize all elements
            int i = 0;
            foreach (ParameterExpression v in _hoistedLocals.Variables)
            {
                // array[i] = new StrongBox<T>(...);
                lc.IL.Emit(OpCodes.Dup);
                lc.IL.EmitPrimitive(i++);
                Type boxType = typeof(StrongBox<>).MakeGenericType(v.Type);

                int index;
                if (IsMethod && (index = lc.Parameters.IndexOf(v)) >= 0)
                {
                    // array[i] = new StrongBox<T>(argument);
                    lc.EmitLambdaArgument(index);
                    lc.IL.Emit(OpCodes.Newobj, boxType.GetConstructor(new Type[] { v.Type })!);
                }
                else if (v == _hoistedLocals.ParentVariable)
                {
                    // array[i] = new StrongBox<T>(closure.Locals);
                    ResolveVariable(v, _closureHoistedLocals).EmitLoad();
                    lc.IL.Emit(OpCodes.Newobj, boxType.GetConstructor(new Type[] { v.Type })!);
                }
                else
                {
                    // array[i] = new StrongBox<T>();
                    lc.IL.Emit(OpCodes.Newobj, boxType.GetConstructor(Type.EmptyTypes)!);
                }
                // if we want to cache this into a local, do it now
                if (ShouldCache(v))
                {
                    lc.IL.Emit(OpCodes.Dup);
                    CacheBoxToLocal(lc, v);
                }
                lc.IL.Emit(OpCodes.Stelem_Ref);
            }

            // store it
            EmitSet(_hoistedLocals.SelfVariable);
        }

        // If hoisted variables are referenced "enough", we cache the
        // StrongBox<T> in an IL local, which saves an array index and a cast
        // when we go to look it up later
        private void EmitCachedVariables()
        {
            if (ReferenceCount == null)
            {
                return;
            }

            foreach (KeyValuePair<ParameterExpression, int> refCount in ReferenceCount)
            {
                if (ShouldCache(refCount.Key, refCount.Value))
                {
                    if (ResolveVariable(refCount.Key) is ElementBoxStorage storage)
                    {
                        storage.EmitLoadBox();
                        CacheBoxToLocal(storage.Compiler, refCount.Key);
                    }
                }
            }
        }

        private bool ShouldCache(ParameterExpression v, int refCount)
        {
            // This caching is too aggressive in the face of conditionals and
            // switch. Also, it is too conservative for variables used inside
            // of loops.
            return refCount > 2 && !_locals.ContainsKey(v);
        }

        private bool ShouldCache(ParameterExpression v)
        {
            if (ReferenceCount == null)
            {
                return false;
            }

            int refCount;
            return ReferenceCount.TryGetValue(v, out refCount) && ShouldCache(v, refCount);
        }

        [RequiresDynamicCode(Expression.StrongBoxRequiresDynamicCode)]
        private void CacheBoxToLocal(LambdaCompiler lc, ParameterExpression v)
        {
            Debug.Assert(ShouldCache(v) && !_locals.ContainsKey(v));
            var local = new LocalBoxStorage(lc, v);
            local.EmitStoreBox();
            _locals.Add(v, local);
        }

        // Creates IL locals for accessing closures
        private void EmitClosureAccess(LambdaCompiler lc, HoistedLocals? locals)
        {
            if (locals == null)
            {
                return;
            }

            EmitClosureToVariable(lc, locals);

            while ((locals = locals.Parent) != null)
            {
                ParameterExpression v = locals.SelfVariable;
                var local = new LocalStorage(lc, v);
                local.EmitStore(ResolveVariable(v));
                _locals.Add(v, local);
            }
        }

        private void EmitClosureToVariable(LambdaCompiler lc, HoistedLocals locals)
        {
            lc.EmitClosureArgument();
            lc.IL.Emit(OpCodes.Ldfld, Closure_Locals);
            AddLocal(lc, locals.SelfVariable);
            EmitSet(locals.SelfVariable);
        }

        // Allocates slots for IL locals or IL arguments
        private void AllocateLocals(LambdaCompiler lc)
        {
            foreach (ParameterExpression v in GetVariables())
            {
                if (Definitions[v] == VariableStorageKind.Local)
                {
                    //
                    // If v is in lc.Parameters, it is a parameter.
                    // Otherwise, it is a local variable.
                    //
                    // Also, for inlined lambdas we'll create a local, which
                    // is possibly a byref local if the parameter is byref.
                    //
                    Storage s;
                    if (IsMethod && lc.Parameters.Contains(v))
                    {
                        s = new ArgumentStorage(lc, v);
                    }
                    else
                    {
                        s = new LocalStorage(lc, v);
                    }
                    _locals.Add(v, s);
                }
            }
        }

        private IEnumerable<ParameterExpression> GetVariables() =>
            MergedScopes == null ? GetVariables(Node) : GetVariablesIncludingMerged();

        private IEnumerable<ParameterExpression> GetVariablesIncludingMerged()
        {
            foreach (ParameterExpression param in GetVariables(Node))
            {
                yield return param;
            }

            foreach (BlockExpression scope in MergedScopes!)
            {
                foreach (ParameterExpression param in scope.Variables)
                {
                    yield return param;
                }
            }
        }

        private static IReadOnlyList<ParameterExpression> GetVariables(object scope)
        {
            if (scope is LambdaExpression lambda)
            {
                return new ParameterList(lambda);
            }
            if (scope is BlockExpression block)
            {
                return block.Variables;
            }
            return new[] { ((CatchBlock)scope).Variable! };
        }

        private string? CurrentLambdaName
        {
            get
            {
                CompilerScope? s = this;
                while (s != null)
                {
                    if (s.Node is LambdaExpression lambda)
                    {
                        return lambda.Name;
                    }
                    s = s._parent;
                }
                throw ContractUtils.Unreachable;
            }
        }
    }

    internal static class ParameterProviderExtensions
    {
        public static int IndexOf(this IParameterProvider provider, ParameterExpression? parameter)
        {
            for (int i = 0, n = provider.ParameterCount; i < n; i++)
            {
                if (provider.GetParameter(i) == parameter)
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool Contains(this IParameterProvider provider, ParameterExpression? parameter)
        {
            return provider.IndexOf(parameter) >= 0;
        }
    }

    internal sealed class ParameterList : IReadOnlyList<ParameterExpression>
    {
        private readonly IParameterProvider _provider;

        public ParameterList(IParameterProvider provider)
        {
            _provider = provider;
        }

        public ParameterExpression this[int index]
        {
            get
            {
                return _provider.GetParameter(index);
            }
        }

        public int Count => _provider.ParameterCount;

        public IEnumerator<ParameterExpression> GetEnumerator()
        {
            for (int i = 0, n = _provider.ParameterCount; i < n; i++)
            {
                yield return _provider.GetParameter(i);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
