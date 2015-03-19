// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq.Expressions.Compiler
{
    /// <summary>
    /// LambdaCompiler is responsible for compiling individual lambda (LambdaExpression). The complete tree may
    /// contain multiple lambdas, the Compiler class is reponsible for compiling the whole tree, individual
    /// lambdas are then compiled by the LambdaCompiler.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    internal sealed partial class LambdaCompiler
    {
        private delegate void WriteBack();

        // Information on the entire lambda tree currently being compiled
        private readonly AnalyzedTree _tree;

        private readonly ILGenerator _ilg;

        // The TypeBuilder backing this method, if any
        private readonly TypeBuilder _typeBuilder;

        private readonly MethodInfo _method;

        // Currently active LabelTargets and their mapping to IL labels
        private LabelScopeInfo _labelBlock = new LabelScopeInfo(null, LabelScopeKind.Lambda);
        // Mapping of labels used for "long" jumps (jumping out and into blocks)
        private readonly Dictionary<LabelTarget, LabelInfo> _labelInfo = new Dictionary<LabelTarget, LabelInfo>();

        // The currently active variable scope
        private CompilerScope _scope;

        // The lambda we are compiling
        private readonly LambdaExpression _lambda;

        // True if the method's first argument is of type Closure
        private readonly bool _hasClosureArgument;

        // Runtime constants bound to the delegate
        private readonly BoundConstants _boundConstants;

        // Free list of locals, so we reuse them rather than creating new ones
        private readonly KeyedQueue<Type, LocalBuilder> _freeLocals = new KeyedQueue<Type, LocalBuilder>();

        /// <summary>
        /// Creates a lambda compiler that will compile to a dynamic method
        /// </summary>
        private LambdaCompiler(AnalyzedTree tree, LambdaExpression lambda)
        {
            Type[] parameterTypes = GetParameterTypes(lambda).AddFirst(typeof(Closure));

            var method = new DynamicMethod(lambda.Name ?? "lambda_method", lambda.ReturnType, parameterTypes, true);

            _tree = tree;
            _lambda = lambda;
            _method = method;

            // In a Win8 immersive process user code is not allowed to access non-W8P framework APIs through 
            // reflection or RefEmit. Framework code, however, is given an exemption.
            // This is to make sure that user code cannot access non-W8P framework APIs via ExpressionTree.

            // TODO: This API is not available, is there an alternative way to achieve the same.
            // method.ProfileAPICheck = true; 

            _ilg = method.GetILGenerator();

            _hasClosureArgument = true;

            // These are populated by AnalyzeTree/VariableBinder
            _scope = tree.Scopes[lambda];
            _boundConstants = tree.Constants[lambda];

            InitializeMethod();
        }

        /// <summary>
        /// Creates a lambda compiler that will compile into the provided Methodbuilder
        /// </summary>
        private LambdaCompiler(AnalyzedTree tree, LambdaExpression lambda, MethodBuilder method)
        {
            _hasClosureArgument = tree.Scopes[lambda].NeedsClosure;
            Type[] paramTypes = GetParameterTypes(lambda);
            if (_hasClosureArgument)
            {
                paramTypes = paramTypes.AddFirst(typeof(Closure));
            }

            method.SetReturnType(lambda.ReturnType);
            method.SetParameters(paramTypes);
            var paramNames = lambda.Parameters.Map(p => p.Name);
            // parameters are index from 1, with closure argument we need to skip the first arg
            int startIndex = _hasClosureArgument ? 2 : 1;
            for (int i = 0; i < paramNames.Length; i++)
            {
                method.DefineParameter(i + startIndex, ParameterAttributes.None, paramNames[i]);
            }

            _tree = tree;
            _lambda = lambda;
            _typeBuilder = (TypeBuilder)method.DeclaringType.GetTypeInfo();
            _method = method;

            _ilg = method.GetILGenerator();

            // These are populated by AnalyzeTree/VariableBinder
            _scope = tree.Scopes[lambda];
            _boundConstants = tree.Constants[lambda];

            InitializeMethod();
        }

        /// <summary>
        /// Creates a lambda compiler for an inlined lambda
        /// </summary>
        private LambdaCompiler(LambdaCompiler parent, LambdaExpression lambda)
        {
            _tree = parent._tree;
            _lambda = lambda;
            _method = parent._method;
            _ilg = parent._ilg;
            _hasClosureArgument = parent._hasClosureArgument;
            _typeBuilder = parent._typeBuilder;
            _scope = _tree.Scopes[lambda];
            _boundConstants = parent._boundConstants;
        }

        private void InitializeMethod()
        {
            // See if we can find a return label, so we can emit better IL
            AddReturnLabel(_lambda);
            _boundConstants.EmitCacheConstants(this);
        }

        public override string ToString()
        {
            return _method.ToString();
        }

        internal ILGenerator IL
        {
            get { return _ilg; }
        }

        internal ReadOnlyCollection<ParameterExpression> Parameters
        {
            get { return _lambda.Parameters; }
        }

        internal bool CanEmitBoundConstants
        {
            get { return _method is DynamicMethod; }
        }

        #region Compiler entry points

        /// <summary>
        /// Compiler entry point
        /// </summary>
        /// <param name="lambda">LambdaExpression to compile.</param>
        /// <param name="debugInfoGenerator">Debug info generator.</param>
        /// <returns>The compiled delegate.</returns>
        internal static Delegate Compile(LambdaExpression lambda)
        {
            // 1. Bind lambda
            AnalyzedTree tree = AnalyzeLambda(ref lambda);

            // 2. Create lambda compiler
            LambdaCompiler c = new LambdaCompiler(tree, lambda);

            // 3. Emit
            c.EmitLambdaBody();

            // 4. Return the delegate.
            return c.CreateDelegate();
        }

        #endregion

        private static AnalyzedTree AnalyzeLambda(ref LambdaExpression lambda)
        {
            // Spill the stack for any exception handling blocks or other
            // constructs which require entering with an empty stack
            lambda = StackSpiller.AnalyzeLambda(lambda);

            // Bind any variable references in this lambda
            return VariableBinder.Bind(lambda);
        }

        internal LocalBuilder GetLocal(Type type)
        {
            Debug.Assert(type != null);

            LocalBuilder local;
            if (_freeLocals.TryDequeue(type, out local))
            {
                Debug.Assert(type == local.LocalType);
                return local;
            }

            return _ilg.DeclareLocal(type);
        }

        internal void FreeLocal(LocalBuilder local)
        {
            if (local != null)
            {
                _freeLocals.Enqueue(local.LocalType, local);
            }
        }

        internal LocalBuilder GetNamedLocal(Type type, ParameterExpression variable)
        {
            Debug.Assert(type != null && variable != null);

            LocalBuilder lb = _ilg.DeclareLocal(type);
            return lb;
        }

        /// <summary>
        /// Gets the argument slot corresponding to the parameter at the given
        /// index. Assumes that the method takes a certain number of prefix
        /// arguments, followed by the real parameters stored in Parameters
        /// </summary>
        internal int GetLambdaArgument(int index)
        {
            return index + (_hasClosureArgument ? 1 : 0) + (_method.IsStatic ? 0 : 1);
        }

        /// <summary>
        /// Returns the index-th argument. This method provides access to the actual arguments
        /// defined on the lambda itself, and excludes the possible 0-th closure argument.
        /// </summary>
        internal void EmitLambdaArgument(int index)
        {
            _ilg.EmitLoadArg(GetLambdaArgument(index));
        }

        internal void EmitClosureArgument()
        {
            Debug.Assert(_hasClosureArgument, "must have a Closure argument");
            Debug.Assert(_method.IsStatic, "must be a static method");
            _ilg.EmitLoadArg(0);
        }

        private Delegate CreateDelegate()
        {
            Debug.Assert(_method is DynamicMethod);

            return _method.CreateDelegate(_lambda.Type, new Closure(_boundConstants.ToArray(), null));
        }

        private FieldBuilder CreateStaticField(string name, Type type)
        {
            // We are emitting into someone else's type. We don't want name
            // conflicts, so choose a long name that is unlikely to confict.
            // Naming scheme chosen here is similar to what the C# compiler
            // uses.
            return _typeBuilder.DefineField("<ExpressionCompilerImplementationDetails>{" + Interlocked.Increment(ref s_counter) + "}" + name, type, FieldAttributes.Static | FieldAttributes.Private);
        }

        /// <summary>
        /// Creates an unitialized field suitible for private implementation details
        /// Works with DynamicMethods or TypeBuilders.
        /// </summary>
        private MemberExpression CreateLazyInitializedField<T>(string name)
        {
            if (_method is DynamicMethod)
            {
                return Expression.Field(Expression.Constant(new StrongBox<T>(default(T))), "Value");
            }
            else
            {
                return Expression.Field(null, CreateStaticField(name, typeof(T)));
            }
        }
    }
}
