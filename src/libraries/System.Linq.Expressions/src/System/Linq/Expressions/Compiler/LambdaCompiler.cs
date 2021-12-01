// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq.Expressions.Compiler
{
    internal interface ILocalCache
    {
        LocalBuilder GetLocal(Type type);

        void FreeLocal(LocalBuilder local);
    }

    /// <summary>
    /// LambdaCompiler is responsible for compiling individual lambda (LambdaExpression). The complete tree may
    /// contain multiple lambdas, the Compiler class is responsible for compiling the whole tree, individual
    /// lambdas are then compiled by the LambdaCompiler.
    /// </summary>
    internal sealed partial class LambdaCompiler : ILocalCache
    {
        private static int s_lambdaMethodIndex;

        private delegate void WriteBack(LambdaCompiler compiler);

        // Information on the entire lambda tree currently being compiled
        private readonly AnalyzedTree _tree;

        private readonly ILGenerator _ilg;

#if FEATURE_COMPILE_TO_METHODBUILDER
        // The TypeBuilder backing this method, if any
        private readonly TypeBuilder _typeBuilder;
#endif

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
        private readonly KeyedStack<Type, LocalBuilder> _freeLocals = new KeyedStack<Type, LocalBuilder>();

        /// <summary>
        /// Creates a lambda compiler that will compile to a dynamic method
        /// </summary>
        private LambdaCompiler(AnalyzedTree tree, LambdaExpression lambda)
        {
            Type[] parameterTypes = GetParameterTypes(lambda, typeof(Closure));

            int lambdaMethodIndex = Interlocked.Increment(ref s_lambdaMethodIndex);
            var method = new DynamicMethod(lambda.Name ?? ("lambda_method" + lambdaMethodIndex.ToString()), lambda.ReturnType, parameterTypes, true);

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

#if FEATURE_COMPILE_TO_METHODBUILDER
        /// <summary>
        /// Creates a lambda compiler that will compile into the provided MethodBuilder
        /// </summary>
        private LambdaCompiler(AnalyzedTree tree, LambdaExpression lambda, MethodBuilder method)
        {
            var scope = tree.Scopes[lambda];
            var hasClosureArgument = scope.NeedsClosure;

            Type[] paramTypes = GetParameterTypes(lambda, hasClosureArgument ? typeof(Closure) : null);

            method.SetReturnType(lambda.ReturnType);
            method.SetParameters(paramTypes);
            var parameters = lambda.Parameters;
            // parameters are index from 1, with closure argument we need to skip the first arg
            int startIndex = hasClosureArgument ? 2 : 1;
            for (int i = 0, n = parameters.Count; i < n; i++)
            {
                method.DefineParameter(i + startIndex, ParameterAttributes.None, parameters[i].Name);
            }

            _tree = tree;
            _lambda = lambda;
            _typeBuilder = (TypeBuilder)method.DeclaringType;
            _method = method;
            _hasClosureArgument = hasClosureArgument;

            _ilg = method.GetILGenerator();

            // These are populated by AnalyzeTree/VariableBinder
            _scope = scope;
            _boundConstants = tree.Constants[lambda];

            InitializeMethod();
        }
#endif

        /// <summary>
        /// Creates a lambda compiler for an inlined lambda
        /// </summary>
        private LambdaCompiler(
            LambdaCompiler parent,
            LambdaExpression lambda,
            InvocationExpression invocation)
        {
            _tree = parent._tree;
            _lambda = lambda;
            _method = parent._method;
            _ilg = parent._ilg;
            _hasClosureArgument = parent._hasClosureArgument;
#if FEATURE_COMPILE_TO_METHODBUILDER
            _typeBuilder = parent._typeBuilder;
#endif
            // inlined scopes are associated with invocation, not with the lambda
            _scope = _tree.Scopes[invocation];
            _boundConstants = parent._boundConstants;
        }

        private void InitializeMethod()
        {
            // See if we can find a return label, so we can emit better IL
            AddReturnLabel(_lambda);
            _boundConstants.EmitCacheConstants(this);
        }

        internal ILGenerator IL => _ilg;

        internal IParameterProvider Parameters => _lambda;

#if FEATURE_COMPILE_TO_METHODBUILDER
        internal bool CanEmitBoundConstants => _method is DynamicMethod;
#endif

        #region Compiler entry points

        /// <summary>
        /// Compiler entry point
        /// </summary>
        /// <param name="lambda">LambdaExpression to compile.</param>
        /// <returns>The compiled delegate.</returns>
        internal static Delegate Compile(LambdaExpression lambda)
        {
            lambda.ValidateArgumentCount();

            // 1. Bind lambda
            AnalyzedTree tree = AnalyzeLambda(ref lambda);

            // 2. Create lambda compiler
            LambdaCompiler c = new LambdaCompiler(tree, lambda);

            // 3. Emit
            c.EmitLambdaBody();

            // 4. Return the delegate.
            return c.CreateDelegate();
        }

#if FEATURE_COMPILE_TO_METHODBUILDER
        /// <summary>
        /// Mutates the MethodBuilder parameter, filling in IL, parameters,
        /// and return type.
        ///
        /// (probably shouldn't be modifying parameters/return type...)
        /// </summary>
        internal static void Compile(LambdaExpression lambda, MethodBuilder method)
        {
            // 1. Bind lambda
            AnalyzedTree tree = AnalyzeLambda(ref lambda);

            // 2. Create lambda compiler
            LambdaCompiler c = new LambdaCompiler(tree, lambda, method);

            // 3. Emit
            c.EmitLambdaBody();
        }
#endif

        #endregion

        private static AnalyzedTree AnalyzeLambda(ref LambdaExpression lambda)
        {
            // Spill the stack for any exception handling blocks or other
            // constructs which require entering with an empty stack
            lambda = StackSpiller.AnalyzeLambda(lambda);

            // Bind any variable references in this lambda
            return VariableBinder.Bind(lambda);
        }

        public LocalBuilder GetLocal(Type type) => _freeLocals.TryPop(type) ?? _ilg.DeclareLocal(type);

        public void FreeLocal(LocalBuilder local)
        {
            Debug.Assert(local != null);
            _freeLocals.Push(local.LocalType, local);
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

#if FEATURE_COMPILE_TO_METHODBUILDER
        private FieldBuilder CreateStaticField(string name, Type type)
        {
            // We are emitting into someone else's type. We don't want name
            // conflicts, so choose a long name that is unlikely to conflict.
            // Naming scheme chosen here is similar to what the C# compiler
            // uses.
            return _typeBuilder.DefineField("<ExpressionCompilerImplementationDetails>{" + System.Threading.Interlocked.Increment(ref s_counter) + "}" + name, type, FieldAttributes.Static | FieldAttributes.Private);
        }
#endif

        /// <summary>
        /// Creates an uninitialized field suitable for private implementation details
        /// Works with DynamicMethods or TypeBuilders.
        /// </summary>
        private MemberExpression CreateLazyInitializedField<T>(string name)
        {
#if FEATURE_COMPILE_TO_METHODBUILDER
            if (_method is DynamicMethod)
#else
            Debug.Assert(_method is DynamicMethod);
#endif
            {
                return Utils.GetStrongBoxValueField(Expression.Constant(new StrongBox<T>()));
            }
#if FEATURE_COMPILE_TO_METHODBUILDER
            else
            {
                return Expression.Field(null, CreateStaticField(name, typeof(T)));
            }
#endif
        }
    }
}
