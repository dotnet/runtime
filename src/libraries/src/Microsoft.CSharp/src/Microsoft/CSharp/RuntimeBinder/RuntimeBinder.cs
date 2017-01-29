// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CSharp.RuntimeBinder.Semantics;
using Microsoft.CSharp.RuntimeBinder.Syntax;
using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal sealed class RuntimeBinder
    {
        #region Singleton Implementation

        // The double checking lock, static lock initializer, and volatile instance
        // field are all here to make the singleton thread-safe. Please see Richter,
        // "CLR via C#" Ch. 24 for more information. This implementation was chosen
        // because construction of the RuntimeBinder is expensive.

        private static readonly object s_singletonLock = new object();
        private static volatile RuntimeBinder s_instance;

        public static RuntimeBinder GetInstance()
        {
            if (s_instance == null)
            {
                lock (s_singletonLock)
                {
                    if (s_instance == null)
                    {
                        s_instance = new RuntimeBinder();
                    }
                }
            }

            return s_instance;
        }

        #endregion

        /////////////////////////////////////////////////////////////////////////////////
        // Members

        private SymbolTable _symbolTable;
        private CSemanticChecker _semanticChecker;
        private SymbolLoader SymbolLoader { get { return _semanticChecker.GetSymbolLoader(); } }
        private ExprFactory _exprFactory;
        private OutputContext _outputContext;
        private NameGenerator _nameGenerator;
        private BindingContext _bindingContext;
        private ExpressionBinder _binder;
        private RuntimeBinderController _controller;

        private readonly object _bindLock = new object();

        // This class is used to keep the tuple of runtime object values and 
        // the type that we want to use for the argument. This is different than the runtime
        // value's type because unless the static time type was dynamic, we want to use the
        // static time type. Also, we may have null values, in which case we would not be 
        // able to get the type.
        private sealed class ArgumentObject
        {
            internal Type Type;
            internal object Value;
            internal CSharpArgumentInfo Info;
        }

        /////////////////////////////////////////////////////////////////////////////////
        // Methods

        #region BookKeeping
        private RuntimeBinder()
        {
            Reset();
        }

        private void Reset()
        {
            _controller = new RuntimeBinderController();
            _semanticChecker = new LangCompiler(_controller, new NameManager());

            BSYMMGR bsymmgr = _semanticChecker.getBSymmgr();
            NameManager nameManager = _semanticChecker.GetNameManager();

            InputFile infile = bsymmgr.GetMiscSymFactory().CreateMDInfile(nameManager.Lookup(""), (mdToken)0);
            infile.SetAssemblyID(bsymmgr.AidAlloc(infile));
            infile.AddToAlias(KAID.kaidThisAssembly);
            infile.AddToAlias(KAID.kaidGlobal);

            _symbolTable = new SymbolTable(
                bsymmgr.GetSymbolTable(),
                bsymmgr.GetSymFactory(),
                nameManager,
                _semanticChecker.GetTypeManager(),
                bsymmgr,
                _semanticChecker,
                infile);
            _semanticChecker.getPredefTypes().Init(_semanticChecker.GetErrorContext(), _symbolTable);
            _semanticChecker.GetTypeManager().InitTypeFactory(_symbolTable);
            SymbolLoader.getPredefinedMembers().RuntimeBinderSymbolTable = _symbolTable;
            SymbolLoader.SetSymbolTable(_symbolTable);

            _exprFactory = new ExprFactory(_semanticChecker.GetSymbolLoader().GetGlobalSymbolContext());
            _outputContext = new OutputContext();
            _nameGenerator = new NameGenerator();
            _bindingContext = BindingContext.CreateInstance(
                _semanticChecker,
                _exprFactory,
                _outputContext,
                _nameGenerator,
                false,
                true,
                false,
                false,
                false,
                false,
                0);
            _binder = new ExpressionBinder(_bindingContext);
        }

        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        public Expression Bind(
            DynamicMetaObjectBinder payload,
            IEnumerable<Expression> parameters,
            DynamicMetaObject[] args,
            out DynamicMetaObject deferredBinding)
        {
            // The lock is here to protect this instance of the binder from itself
            // when called on multiple threads. The cost in time of a single lock
            // on a single thread appears to be negligible and dominated by the cost
            // of the bind itself. My timing of 4000 consecutive dynamic calls with
            // bind, where the body of the called method is empty, are as follows
            // (five samples):
            //
            //   Without lock()         With lock()
            // = 00:00:10.7597696     = 00:00:10.7222606
            // = 00:00:10.0711116     = 00:00:10.1818496
            // = 00:00:09.9905507     = 00:00:10.1628693
            // = 00:00:09.9892183     = 00:00:10.0750007
            // = 00:00:09.9253234     = 00:00:10.0340266
            //
            // ...subsequent calls that were cache hits, i.e., already bound, took less
            // than 1/1000 sec for the whole 4000 of them.

            lock (_bindLock)
            {
                // this is a strategy for realizing correct binding when the symboltable
                // finds a name collision across different types, e.g. one dynamic binding
                // uses a type "N.T" and now a second binding uses a different type "N.T".

                // In order to make this work, we have to reset the symbol table and begin
                // the second binding over again when we detect the collision. So this is
                // something like a longjmp to the beginning of binding. For a single binding,
                // if we have to do this more than once, we give an ICE--this would be a
                // scenario that needs to know about both N.T's simultaneously to work.

                // See SymbolTable.LoadSymbolsFromType for more information.

                try
                {
                    return BindCore(payload, parameters, args, out deferredBinding);
                }
                catch (ResetBindException)
                {
                    Reset();
                    try
                    {
                        return BindCore(payload, parameters, args, out deferredBinding);
                    }
                    catch (ResetBindException)
                    {
                        Reset();
                        Debug.Assert(false, "More than one symbol table name collision in a single binding");
                        throw Error.InternalCompilerError();
                    }
                }
            }
        }

        private Expression BindCore(
            DynamicMetaObjectBinder payload,
            IEnumerable<Expression> parameters,
            DynamicMetaObject[] args,
            out DynamicMetaObject deferredBinding)
        {
            if (args.Length < 1)
            {
                throw Error.BindRequireArguments();
            }

            InitializeCallingContext(payload);
            ArgumentObject[] arguments = CreateArgumentArray(payload, parameters, args);

            // On any given bind call, we populate the symbol table with any new
            // conversions that we find for any of the types specified. We keep a
            // running SymbolTable so that we don't have to reflect over types if 
            // we've seen them already in the table.
            //
            // Once we've loaded all the standard conversions into the symbol table,
            // we can call into the binder to bind the actual call.

            ICSharpInvokeOrInvokeMemberBinder callOrInvoke = payload as ICSharpInvokeOrInvokeMemberBinder;
            PopulateSymbolTableWithPayloadInformation(payload, arguments[0].Type, arguments);
            AddConversionsForArguments(arguments);

            // When we do any bind, we perform the following steps:
            //
            // 1) Create a local variable scope which contains local variable symbols
            //    for each of the parameters, and the instance argument.
            // 2) If we have operators, then we don't need to do lookup. Otherwise, 
            //    look for the name and switch on the result - dispatch according to 
            //    the symbol kind. This results in an EXPR being bound that is the expression.
            // 3) Create the EXPRRETURN which returns the call and wrap it in
            //    an EXPRBOUNDLAMBDA which uses the local variable scope
            //    created in step (1) as its local scope.
            // 4) Call the ExpressionTreeRewriter to generate a set of EXPRCALLs
            //    that call the static ExpressionTree factory methods.
            // 5) Call the EXPRTreeToExpressionTreeVisitor to generate the actual
            //    Linq expression tree for the whole thing and return it.

            // (1) - Create the locals
            Dictionary<int, LocalVariableSymbol> dictionary = new Dictionary<int, LocalVariableSymbol>();
            Scope pScope = _semanticChecker.GetGlobalMiscSymFactory().CreateScope(null);
            PopulateLocalScope(payload, pScope, arguments, parameters, dictionary);

            // (1.5) - Check to see if we need to defer.
            DynamicMetaObject o = null;
            if (DeferBinding(payload, arguments, args, dictionary, out o))
            {
                deferredBinding = o;
                return null;
            }

            // (2) - look the thing up and dispatch.
            EXPR pResult = DispatchPayload(payload, arguments, dictionary);
            Debug.Assert(pResult != null);

            deferredBinding = null;
            Expression e = CreateExpressionTreeFromResult(parameters, arguments, pScope, pResult);
            return e;
        }
        #region Helpers

        private bool DeferBinding(
            DynamicMetaObjectBinder payload,
            ArgumentObject[] arguments,
            DynamicMetaObject[] args,
            Dictionary<int, LocalVariableSymbol> dictionary,
            out DynamicMetaObject deferredBinding)
        {
            // This method deals with any deferrals we need to do. We check deferrals up front
            // and bail early if we need to do them.

            // (1) InvokeMember deferral.
            //
            // This is the deferral for the d.Foo() scenario where Foo actually binds to a 
            // field or property, and not a method group that is invocable. We defer to
            // the standard GetMember/Invoke pattern.

            if (payload is CSharpInvokeMemberBinder)
            {
                ICSharpInvokeOrInvokeMemberBinder callPayload = payload as ICSharpInvokeOrInvokeMemberBinder;
                int arity = callPayload.TypeArguments != null ? callPayload.TypeArguments.Count : 0;
                MemberLookup mem = new MemberLookup();
                EXPR callingObject = CreateCallingObjectForCall(callPayload, arguments, dictionary);

                Debug.Assert(_bindingContext.ContextForMemberLookup() != null);
                SymWithType swt = _symbolTable.LookupMember(
                        callPayload.Name,
                        callingObject,
                        _bindingContext.ContextForMemberLookup(),
                        arity,
                        mem,
                        (callPayload.Flags & CSharpCallFlags.EventHookup) != 0,
                        true);

                if (swt != null && swt.Sym.getKind() != SYMKIND.SK_MethodSymbol)
                {
                    // The GetMember only has one argument, and we need to just take the first arg info.
                    CSharpGetMemberBinder getMember = new CSharpGetMemberBinder(callPayload.Name, false, callPayload.CallingContext, new CSharpArgumentInfo[] { callPayload.ArgumentInfo[0] });

                    // The Invoke has the remaining argument infos. However, we need to redo the first one
                    // to correspond to the GetMember result.
                    CSharpArgumentInfo[] argInfos = new CSharpArgumentInfo[callPayload.ArgumentInfo.Count];
                    callPayload.ArgumentInfo.CopyTo(argInfos, 0);

                    argInfos[0] = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null);
                    CSharpInvokeBinder invoke = new CSharpInvokeBinder(callPayload.Flags, callPayload.CallingContext, argInfos);

                    DynamicMetaObject[] newArgs = new DynamicMetaObject[args.Length - 1];
                    Array.Copy(args, 1, newArgs, 0, args.Length - 1);
                    deferredBinding = invoke.Defer(getMember.Defer(args[0]), newArgs);
                    return true;
                }
            }

            deferredBinding = null;
            return false;
        }

        private void InitializeCallingContext(DynamicMetaObjectBinder payload)
        {
            // Set the context if the payload specifies it. Currently we only use this for calls.
            Type t = null;
            bool bChecked = false;
            if (payload is ICSharpInvokeOrInvokeMemberBinder)
            {
                t = (payload as ICSharpInvokeOrInvokeMemberBinder).CallingContext;
            }
            else if (payload is CSharpGetMemberBinder)
            {
                t = (payload as CSharpGetMemberBinder).CallingContext;
            }
            else if (payload is CSharpSetMemberBinder)
            {
                CSharpSetMemberBinder b = (CSharpSetMemberBinder)payload;
                t = b.CallingContext;
                bChecked = b.IsChecked;
            }
            else if (payload is CSharpGetIndexBinder)
            {
                t = (payload as CSharpGetIndexBinder).CallingContext;
            }
            else if (payload is CSharpSetIndexBinder)
            {
                CSharpSetIndexBinder b = (CSharpSetIndexBinder)payload;
                t = b.CallingContext;
                bChecked = b.IsChecked;
            }
            else if (payload is CSharpUnaryOperationBinder)
            {
                CSharpUnaryOperationBinder b = (CSharpUnaryOperationBinder)payload;
                t = b.CallingContext;
                bChecked = b.IsChecked;
            }
            else if (payload is CSharpBinaryOperationBinder)
            {
                CSharpBinaryOperationBinder b = (CSharpBinaryOperationBinder)payload;
                t = b.CallingContext;
                bChecked = b.IsChecked;
            }
            else if (payload is CSharpConvertBinder)
            {
                CSharpConvertBinder b = (CSharpConvertBinder)payload;
                t = b.CallingContext;
                bChecked = b.IsChecked;
            }
            else if (payload is CSharpIsEventBinder)
            {
                t = (payload as CSharpIsEventBinder).CallingContext;
            }

            if (t != null)
            {
                AggregateSymbol agg = _symbolTable.GetCTypeFromType(t).AsAggregateType().GetOwningAggregate();
                _bindingContext.m_pParentDecl = _semanticChecker.GetGlobalSymbolFactory().CreateAggregateDecl(agg, null);
            }
            else
            {
                // The binding context lives across invocations! If we don't reset this, then later calls might
                // bind in a previous call's context.
                _bindingContext.m_pParentDecl = null;
            }

            _bindingContext.CheckedConstant = bChecked;
            _bindingContext.CheckedNormal = bChecked;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression CreateExpressionTreeFromResult(
            IEnumerable<Expression> parameters,
            ArgumentObject[] arguments,
            Scope pScope,
            EXPR pResult)
        {
            // (3) - Place the result in a return statement and create the EXPRBOUNDLAMBDA.
            EXPRBOUNDLAMBDA boundLambda = GenerateBoundLambda(arguments, pScope, pResult);

            // (4) - Rewrite the EXPRBOUNDLAMBDA into an expression tree.
            EXPR exprTree = ExpressionTreeRewriter.Rewrite(boundLambda, _exprFactory, SymbolLoader);

            // (5) - Create the actual Expression Tree
            Expression e = ExpressionTreeCallRewriter.Rewrite(SymbolLoader.GetTypeManager(), exprTree, parameters);
            return e;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ArgumentObject[] CreateArgumentArray(
                DynamicMetaObjectBinder payload,
                IEnumerable<Expression> parameters,
                DynamicMetaObject[] args)
        {
            // Check the payloads to see whether or not we need to get the runtime types for
            // these arguments.

            List<ArgumentObject> list = new List<ArgumentObject>();
            Func<DynamicMetaObjectBinder, CSharpArgumentInfo, Expression, DynamicMetaObject, int, Type> getArgumentType = null;
            Func<DynamicMetaObjectBinder, int, CSharpArgumentInfo> getArgumentInfo = null;

            // Quick delegate to set the type.
            if (payload is ICSharpInvokeOrInvokeMemberBinder)
            {
                getArgumentInfo = (p, index) => (p as ICSharpInvokeOrInvokeMemberBinder).ArgumentInfo[index];
            }
            else if (payload is CSharpBinaryOperationBinder)
            {
                getArgumentInfo = (p, index) => (p as CSharpBinaryOperationBinder).ArgumentInfo[index];
            }
            else if (payload is CSharpUnaryOperationBinder)
            {
                getArgumentInfo = (p, index) => (p as CSharpUnaryOperationBinder).ArgumentInfo[index];
            }
            else if (payload is CSharpGetMemberBinder)
            {
                getArgumentInfo = (p, index) => (p as CSharpGetMemberBinder).ArgumentInfo[index];
            }
            else if (payload is CSharpSetMemberBinder)
            {
                getArgumentInfo = (p, index) => (p as CSharpSetMemberBinder).ArgumentInfo[index];
            }
            else if (payload is CSharpGetIndexBinder)
            {
                getArgumentInfo = (p, index) => (p as CSharpGetIndexBinder).ArgumentInfo[index];
            }
            else if (payload is CSharpSetIndexBinder)
            {
                getArgumentInfo = (p, index) => (p as CSharpSetIndexBinder).ArgumentInfo[index];
            }
            else if (payload is CSharpConvertBinder || payload is CSharpIsEventBinder)
            {
                getArgumentInfo = (p, index) => CSharpArgumentInfo.None;
            }
            else
            {
                Debug.Assert(false, "Unknown payload kind");
                throw Error.InternalCompilerError();
            }
            getArgumentType = (p, argInfo, param, arg, index) =>
                {
                    Type t = argInfo.UseCompileTimeType ? param.Type : arg.LimitType;
                    Debug.Assert(t != null);

                    if ((argInfo.Flags & (CSharpArgumentInfoFlags.IsRef | CSharpArgumentInfoFlags.IsOut)) != 0)
                    {
                        // If we have a ref our an out parameter, make the byref type.
                        // If we have the receiver of a call or invoke that is ref, it must be because of 
                        // a struct caller. Don't persist the ref for that.
                        if (!(index == 0 && IsBinderThatCanHaveRefReceiver(p)))
                        {
                            t = t.MakeByRefType();
                        }
                    }
                    else if (!argInfo.UseCompileTimeType)
                    {
                        // If we don't have ref or out, then pick the best type to represent this value.
                        // If the runtime value has a type that is not accessible, then we pick an
                        // accessible type that is "closest" in some sense, where we recursively widen
                        // components of type that can validly vary covariantly.

                        // This ensures that the type we pick is something that the user could have written.

                        CType actualType = _symbolTable.GetCTypeFromType(t);
                        CType bestType;

                        bool res = _semanticChecker.GetTypeManager().GetBestAccessibleType(_semanticChecker, _bindingContext, actualType, out bestType);
                        
                        // Since the actual type of these arguments are never going to be pointer
                        // types or ref/out types (they are in fact boxed into an object), we have
                        // a guarantee that we will always be able to find a best accessible type
                        // (which, in the worst case, may be object).
                        Debug.Assert(res, "Unexpected failure of GetBestAccessibleType in construction of argument array");

                        t = bestType.AssociatedSystemType;
                    }

                    return t;
                };

            int i = 0;
            foreach (var curParam in parameters)
            {
                ArgumentObject a = new ArgumentObject();
                a.Value = args[i].Value;
                a.Info = getArgumentInfo(payload, i);
                a.Type = getArgumentType(payload, a.Info, curParam, args[i], i);

                Debug.Assert(a.Type != null);
                list.Add(a);

                ++i;
            }

            return list.ToArray();
        }

        /////////////////////////////////////////////////////////////////////////////////

        private bool IsBinderThatCanHaveRefReceiver(DynamicMetaObjectBinder binder)
        {
            // This is true for any binder that is eligible to take value type receiver 
            // objects as a ref (for mutable operations). Such as calls ("v.M(d)"),
            // and indexers ("v[d] = v[d]"). Note that properties are not here because they
            // are only dispatched dynamically when the receiver is dynamic, and hence boxed.
            return binder is ICSharpInvokeOrInvokeMemberBinder || binder is CSharpSetIndexBinder || binder is CSharpGetIndexBinder;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private void PopulateSymbolTableWithPayloadInformation(
            DynamicMetaObjectBinder payload,
            Type callingType,
            ArgumentObject[] arguments)
        {
            ICSharpInvokeOrInvokeMemberBinder callOrInvoke;
            CSharpGetMemberBinder getmember;
            CSharpSetMemberBinder setmember;

            if ((callOrInvoke = payload as ICSharpInvokeOrInvokeMemberBinder) != null)
            {
                Type type;

                if (callOrInvoke.StaticCall)
                {
                    if (arguments[0].Value == null || !(arguments[0].Value is Type))
                    {
                        Debug.Assert(false, "Cannot make static call without specifying a type");
                        throw Error.InternalCompilerError();
                    }
                    type = arguments[0].Value as Type;
                }
                else
                {
                    type = callingType;
                }
                _symbolTable.PopulateSymbolTableWithName(
                    callOrInvoke.Name,
                    callOrInvoke.TypeArguments,
                    type);

                // If it looks like we're invoking a get_ or a set_, load the property as well.
                // This is because we need COM indexed properties called via method calls to 
                // work the same as it used to.
                if (callOrInvoke.Name.StartsWith("set_", StringComparison.Ordinal) ||
                    callOrInvoke.Name.StartsWith("get_", StringComparison.Ordinal))
                {
                    _symbolTable.PopulateSymbolTableWithName(
                        callOrInvoke.Name.Substring(4), //remove prefix
                        callOrInvoke.TypeArguments,
                        type);
                }
            }
            else if ((getmember = payload as CSharpGetMemberBinder) != null)
            {
                _symbolTable.PopulateSymbolTableWithName(
                    getmember.Name,
                    null,
                    arguments[0].Type);
            }
            else if ((setmember = payload as CSharpSetMemberBinder) != null)
            {
                _symbolTable.PopulateSymbolTableWithName(
                    setmember.Name,
                    null,
                    arguments[0].Type);
            }
            else if (payload is CSharpGetIndexBinder || payload is CSharpSetIndexBinder)
            {
                _symbolTable.PopulateSymbolTableWithName(
                    SpecialNames.Indexer,
                    null,
                    arguments[0].Type);
            }
            else if (payload is CSharpBinaryOperationBinder)
            {
                CSharpBinaryOperationBinder op = payload as CSharpBinaryOperationBinder;
                if (GetCLROperatorName(op.Operation) == null)
                {
                    Debug.Assert(false, "Unknown operator: " + op.Operation);
                    throw Error.InternalCompilerError();
                }
                _symbolTable.PopulateSymbolTableWithName(
                    GetCLROperatorName(op.Operation),
                    null,
                    arguments[0].Type);
                _symbolTable.PopulateSymbolTableWithName(
                    GetCLROperatorName(op.Operation),
                    null,
                    arguments[1].Type);
            }
            else if (payload is CSharpUnaryOperationBinder)
            {
                CSharpUnaryOperationBinder op = payload as CSharpUnaryOperationBinder;
                _symbolTable.PopulateSymbolTableWithName(
                    GetCLROperatorName(op.Operation),
                    null,
                    arguments[0].Type);
            }
            else if (payload is CSharpIsEventBinder)
            {
                CSharpIsEventBinder op = payload as CSharpIsEventBinder;

                // Populate the symbol table with the LHS.
                _symbolTable.PopulateSymbolTableWithName(
                    op.Name,
                    null,
                    arguments[0].Info.IsStaticType ? arguments[0].Value as Type : arguments[0].Type);
            }
            else if (!(payload is CSharpConvertBinder))
            {
                // Conversions don't need to do anything, since they're just conversions!
                // After we add payload information, we add conversions for all argument
                // types anyway, so that will get handled there.
                //
                // All other unknown payload types will generate an error.

                Debug.Assert(false, "Unknown payload kind");
                throw Error.InternalCompilerError();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private void AddConversionsForArguments(ArgumentObject[] arguments)
        {
            foreach (ArgumentObject arg in arguments)
            {
                _symbolTable.AddConversionsForType(arg.Type);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR DispatchPayload(
            DynamicMetaObjectBinder payload,
            ArgumentObject[] arguments,
            Dictionary<int, LocalVariableSymbol> dictionary)
        {
            EXPR pResult = null;
            if (payload is CSharpBinaryOperationBinder)
            {
                pResult = BindBinaryOperation(payload as CSharpBinaryOperationBinder, arguments, dictionary);
            }
            else if (payload is CSharpUnaryOperationBinder)
            {
                pResult = BindUnaryOperation(payload as CSharpUnaryOperationBinder, arguments, dictionary);
            }
            else if (payload is CSharpSetMemberBinder)
            {
                pResult = BindAssignment(payload as CSharpSetMemberBinder, arguments, dictionary);
            }
            else if (payload is CSharpConvertBinder)
            {
                Debug.Assert(arguments.Length == 1);
                {
                    CSharpConvertBinder conversion = payload as CSharpConvertBinder;
                    switch (conversion.ConversionKind)
                    {
                        case CSharpConversionKind.ImplicitConversion:
                            pResult = BindImplicitConversion(arguments, conversion.Type, dictionary, false);
                            break;
                        case CSharpConversionKind.ExplicitConversion:
                            pResult = BindExplicitConversion(arguments, conversion.Type, dictionary);
                            break;
                        case CSharpConversionKind.ArrayCreationConversion:
                            pResult = BindImplicitConversion(arguments, conversion.Type, dictionary, true);
                            break;
                        default:
                            Debug.Assert(false, "Unknown conversion kind");
                            throw Error.InternalCompilerError();
                    }
                }
            }
            else if (payload is ICSharpInvokeOrInvokeMemberBinder)
            {
                EXPR callingObject = CreateCallingObjectForCall(payload as ICSharpInvokeOrInvokeMemberBinder, arguments, dictionary);
                pResult = BindCall(payload as ICSharpInvokeOrInvokeMemberBinder, callingObject, arguments, dictionary);
            }
            else if (payload is CSharpGetMemberBinder)
            {
                Debug.Assert(arguments.Length == 1);
                pResult = BindProperty(payload, arguments[0], dictionary[0], null, false);
            }
            else if (payload is CSharpGetIndexBinder)
            {
                EXPR indexerArguments = CreateArgumentListEXPR(arguments, dictionary, 1, arguments.Length);
                pResult = BindProperty(payload, arguments[0], dictionary[0], indexerArguments, false);
            }
            else if (payload is CSharpSetIndexBinder)
            {
                pResult = BindAssignment(payload as CSharpSetIndexBinder, arguments, dictionary);
            }
            else if (payload is CSharpIsEventBinder)
            {
                pResult = BindIsEvent(payload as CSharpIsEventBinder, arguments, dictionary);
            }
            else
            {
                Debug.Assert(false, "Unknown payload kind");
                throw Error.InternalCompilerError();
            }
            return pResult;
        }

        /////////////////////////////////////////////////////////////////////////////////
        // We take the ArgumentObjects to verify - if the parameter expression tells us 
        // we have a ref parameter, but the argument object tells us we're not passed by ref,
        // then it means it was a ref that the compiler had to insert. This is used when
        // we have a call off of a struct for example. If thats the case, don't treat the 
        // local as a ref type.

        private void PopulateLocalScope(
            DynamicMetaObjectBinder payload,
            Scope pScope,
            ArgumentObject[] arguments,
            IEnumerable<Expression> parameterExpressions,
            Dictionary<int, LocalVariableSymbol> dictionary)
        {
            // We use the compile time types for the local variables, and then 
            // cast them to the runtime types for the expression tree.

            int i = 0;
            foreach (Expression parameter in parameterExpressions)
            {
                CType type = _symbolTable.GetCTypeFromType(parameter.Type);

                // Make sure we're not setting ref for the receiver of a call - the argument
                // will be marked as ref if we're calling off a struct, but we don't want 
                // to persist that in our system.
                bool isFirstParamOfCallOrInvoke = false;
                if (i == 0 && IsBinderThatCanHaveRefReceiver(payload))
                {
                    isFirstParamOfCallOrInvoke = true;
                }

                // If we have a ref or out, get the parameter modifier type.
                if ((parameter is ParameterExpression && (parameter as ParameterExpression).IsByRef) &&
                    (arguments[i].Info.IsByRef || arguments[i].Info.IsOut))
                {
                    // If we're the first param of a call or invoke, and we're ref, it must be
                    // because of structs. Don't persist the parameter modifier type.
                    if (!isFirstParamOfCallOrInvoke)
                    {
                        type = _semanticChecker.GetTypeManager().GetParameterModifier(type, arguments[i].Info.IsOut);
                    }
                }
                LocalVariableSymbol local = _semanticChecker.GetGlobalSymbolFactory().CreateLocalVar(_semanticChecker.GetNameManager().Add("p" + i), pScope, type);
                local.fUsedInAnonMeth = true;

                dictionary.Add(i++, local);
                isFirstParamOfCallOrInvoke = false;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPRBOUNDLAMBDA GenerateBoundLambda(
            ArgumentObject[] arguments,
            Scope pScope,
            EXPR call)
        {
            // We don't actually need the real delegate type here - we just need SOME delegate type.
            // This is because we never attempt any conversions on the lambda itself.
            AggregateType delegateType = _symbolTable.GetCTypeFromType(typeof(Func<>)).AsAggregateType();
            LocalVariableSymbol thisLocal = _semanticChecker.GetGlobalSymbolFactory().CreateLocalVar(_semanticChecker.GetNameManager().Add("this"), pScope, _symbolTable.GetCTypeFromType(typeof(object)));
            thisLocal.isThis = true;
            EXPRBOUNDLAMBDA boundLambda = _exprFactory.CreateAnonymousMethod(delegateType);
            EXPRUNBOUNDLAMBDA unboundLambda = _exprFactory.CreateLambda();

            List<Type> paramTypes = new List<Type>();
            foreach (ArgumentObject o in arguments)
            {
                paramTypes.Add(o.Type);
            }
            boundLambda.Initialize(pScope);

            EXPRRETURN returnStatement = _exprFactory.CreateReturn(0, pScope, call);
            EXPRBLOCK block = _exprFactory.CreateBlock(null, returnStatement, pScope);
            boundLambda.OptionalBody = block;
            return boundLambda;
        }

        #region ExprCreation

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR CreateLocal(Type type, bool bIsOut, LocalVariableSymbol local)
        {
            CType ctype = _symbolTable.GetCTypeFromType(type);
            if (bIsOut)
            {
                Debug.Assert(ctype.IsParameterModifierType());
                ctype = _semanticChecker.GetTypeManager().GetParameterModifier(
                    ctype.AsParameterModifierType().GetParameterType(),
                    true);
            }

            // If we can convert, do that. If not, cast it.
            EXPRLOCAL exprLocal = _exprFactory.CreateLocal(EXPRFLAG.EXF_LVALUE, local);
            EXPR result = _binder.tryConvert(exprLocal, ctype);
            if (result == null)
            {
                result = _binder.mustCast(exprLocal, ctype);
            }
            result.flags |= EXPRFLAG.EXF_LVALUE;
            return result;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR CreateArgumentListEXPR(
            ArgumentObject[] arguments,
            Dictionary<int, LocalVariableSymbol> dictionary,
            int startIndex,
            int endIndex)
        {
            EXPR args = null;
            EXPR last = null;

            if (arguments != null)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    ArgumentObject argument = arguments[i];
                    EXPR arg = CreateArgumentEXPR(argument, dictionary[i]);

                    if (args == null)
                    {
                        args = arg;
                        last = args;
                    }
                    else
                    {
                        // Lists are right-heavy.
                        _exprFactory.AppendItemToList(arg, ref args, ref last);
                    }
                }
            }
            return args;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR CreateArgumentEXPR(ArgumentObject argument, LocalVariableSymbol local)
        {
            EXPR arg;
            if (argument.Info.LiteralConstant)
            {
                if (argument.Value == null)
                {
                    if (argument.Info.UseCompileTimeType)
                    {
                        arg = _exprFactory.CreateConstant(_symbolTable.GetCTypeFromType(argument.Type), new CONSTVAL());
                    }
                    else
                    {
                        arg = _exprFactory.CreateNull();
                    }
                }
                else
                {
                    arg = _exprFactory.CreateConstant(_symbolTable.GetCTypeFromType(argument.Type), new CONSTVAL(argument.Value));
                }
            }
            else
            {
                // If we have a dynamic argument and it was null, the type is going to be Object.
                // But we want it to be typed NullType so we can have null conversions.

                if (!argument.Info.UseCompileTimeType && argument.Value == null)
                {
                    arg = _exprFactory.CreateNull();
                }
                else
                {
                    arg = CreateLocal(argument.Type, argument.Info.IsOut, local);
                }
            }

            // Now check if we have a named thing. If so, wrap this thing in a named argument.
            if (argument.Info.NamedArgument)
            {
                Debug.Assert(argument.Info.Name != null);
                arg = _exprFactory.CreateNamedArgumentSpecification(SymbolTable.GetName(argument.Info.Name, _semanticChecker.GetNameManager()), arg);
            }

            // If we have an object that was "dynamic" at compile time, we need
            // to be able to convert it to every interface that the actual value
            // implements. This allows conversion binders and overload resolution
            // to behave as though type information is available for these EXPRs,
            // even though it may be the case that the actual runtime type is
            // inaccessible and therefore unused.

            // This comes in handy for, e.g., iterators (they are nested private
            // classes), and COM RCWs without type information (they do not expose
            // their interfaces in a usual way).

            // It is critical that arg.RuntimeObject is non-null only when the
            // compile time type of the argument is dynamic, otherwise normal C#
            // semantics on typed arguments will be broken.

            if (!argument.Info.UseCompileTimeType && argument.Value != null)
            {
                arg.RuntimeObject = argument.Value;
                arg.RuntimeObjectActualType = _symbolTable.GetCTypeFromType(argument.Value.GetType());
            }

            return arg;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPRMEMGRP CreateMemberGroupEXPR(
            string Name,
            IList<Type> typeArguments,
            EXPR callingObject,
            SYMKIND kind)
        {
            Name name = SymbolTable.GetName(Name, _semanticChecker.GetNameManager());
            AggregateType callingType;

            if (callingObject.type.IsArrayType())
            {
                callingType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_ARRAY);
            }
            else if (callingObject.type.IsNullableType())
            {
                callingType = callingObject.type.AsNullableType().GetAts(_semanticChecker.GetSymbolLoader().GetErrorContext());
            }
            else if (callingObject.type.IsAggregateType())
            {
                callingType = callingObject.type.AsAggregateType();
            }
            else
            {
                callingType = null;
                Debug.Assert(false, "MemberGroup on non-array, non-aggregate");
            }

            List<CType> callingTypes = new List<CType>();

            // The C# binder expects that only the base virtual method is inserted
            // into the list of candidates, and only the type containing the base
            // virtual method is inserted into the list of types. However, since we
            // don't want to do all the logic, we're just going to insert every type
            // that has a member of the given name, and allow the C# binder to filter
            // out all overrides.
            //
            // Find that set of types now.
            symbmask_t mask = symbmask_t.MASK_MethodSymbol;
            switch (kind)
            {
                case SYMKIND.SK_PropertySymbol:
                case SYMKIND.SK_IndexerSymbol:
                    mask = symbmask_t.MASK_PropertySymbol;
                    break;
                case SYMKIND.SK_MethodSymbol:
                    mask = symbmask_t.MASK_MethodSymbol;
                    break;
                default:
                    Debug.Assert(false, "Unhandled kind");
                    break;
            }

            // If we have a constructor, only find its type.
            bool bIsConstructor = name == SymbolLoader.GetNameManager().GetPredefinedName(PredefinedName.PN_CTOR);
            for (AggregateType t = callingType; t != null; t = t.GetBaseClass())
            {
                if (_symbolTable.AggregateContainsMethod(t.GetOwningAggregate(), Name, mask))
                {
                    callingTypes.Add(t);
                }

                // If we have a constructor, run the loop once for the constructor's type, and thats it.
                if (bIsConstructor)
                {
                    break;
                }
            }

            // If this is a WinRT type we have to add all collection interfaces that have this method 
            // as well so that overload resolution can find them.
            if (callingType.IsWindowsRuntimeType())
            {
                TypeArray collectioniFaces = callingType.GetWinRTCollectionIfacesAll(SymbolLoader);

                for (int i = 0; i < collectioniFaces.size; i++)
                {
                    CType t = collectioniFaces.Item(i);
                    // Collection interfaces will be aggregates.
                    Debug.Assert(t.IsAggregateType());

                    if (_symbolTable.AggregateContainsMethod(t.AsAggregateType().GetOwningAggregate(), Name, mask))
                    {
                        callingTypes.Add(t);
                    }
                }
            }

            EXPRFLAG flags = EXPRFLAG.EXF_USERCALLABLE;
            // If its a delegate, mark that on the memgroup.
            if (Name == SpecialNames.Invoke && callingObject.type.isDelegateType())
            {
                flags |= EXPRFLAG.EXF_DELEGATE;
            }

            // For a constructor, we need to seed the memgroup with the constructor flag.
            if (Name == SpecialNames.Constructor)
            {
                flags |= EXPRFLAG.EXF_CTOR;
            }

            // If we have an indexer, mark that.
            if (Name == SpecialNames.Indexer)
            {
                flags |= EXPRFLAG.EXF_INDEXER;
            }

            TypeArray typeArgumentsAsTypeArray = BSYMMGR.EmptyTypeArray();
            if (typeArguments != null && typeArguments.Count > 0)
            {
                typeArgumentsAsTypeArray = _semanticChecker.getBSymmgr().AllocParams(
                    _symbolTable.GetCTypeArrayFromTypes(typeArguments));
            }
            EXPRMEMGRP memgroup = _exprFactory.CreateMemGroup(// Tree
                flags, name, typeArgumentsAsTypeArray, kind, callingType, null, null, new CMemberLookupResults(
                    _semanticChecker.getBSymmgr().AllocParams(callingTypes.Count, callingTypes.ToArray()),
                    name));
            if (callingObject.isCLASS())
            {
                memgroup.SetOptionalLHS(callingObject);
            }
            else
            {
                memgroup.SetOptionalObject(callingObject);
            }
            return memgroup;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR CreateProperty(
            SymWithType swt,
            EXPR callingObject,
            BindingFlag flags)
        {
            // For a property, we simply create the EXPRPROP for the thing, call the
            // expression tree rewriter, rewrite it, and send it on its way.

            PropertySymbol property = swt.Prop();
            AggregateType propertyType = swt.GetType();
            PropWithType pwt = new PropWithType(property, propertyType);
            EXPRMEMGRP pMemGroup = CreateMemberGroupEXPR(property.name.Text, null, callingObject, SYMKIND.SK_PropertySymbol);

            return _binder.BindToProperty(// For a static property instance, don't set the object.
                    callingObject.isCLASS() ? null : callingObject, pwt, flags, null, null, pMemGroup);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR CreateIndexer(SymWithType swt, EXPR callingObject, EXPR arguments, BindingFlag bindFlags)
        {
            IndexerSymbol index = swt.Sym as IndexerSymbol;
            AggregateType ctype = swt.GetType();
            EXPRMEMGRP memgroup = CreateMemberGroupEXPR(index.name.Text, null, callingObject, SYMKIND.SK_PropertySymbol);

            EXPR result = _binder.BindMethodGroupToArguments(bindFlags, memgroup, arguments);
            return ReorderArgumentsForNamedAndOptional(callingObject, result);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR CreateArray(EXPR callingObject, EXPR optionalIndexerArguments)
        {
            return _binder.BindArrayIndexCore(0, callingObject, optionalIndexerArguments);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR CreateField(
            SymWithType swt,
            EXPR callingObject)
        {
            // For a field, simply create the EXPRFIELD and our caller takes care of the rest.

            FieldSymbol fieldSymbol = swt.Field();
            CType returnType = fieldSymbol.GetType();
            AggregateType fieldType = swt.GetType();
            FieldWithType fwt = new FieldWithType(fieldSymbol, fieldType);

            EXPR field = _binder.BindToField(callingObject.isCLASS() ? null : callingObject, fwt, 0);
            return field;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPREVENT CreateEvent(
            SymWithType swt,
            EXPR callingObject)
        {
            EventSymbol eventSymbol = swt.Event();
            EXPREVENT e = _exprFactory.CreateEvent(eventSymbol.type, callingObject, new EventWithType(eventSymbol, swt.GetType()));
            return e;
        }
        #endregion

        #endregion

        #region Calls
        /////////////////////////////////////////////////////////////////////////////////

        private EXPR CreateCallingObjectForCall(
            ICSharpInvokeOrInvokeMemberBinder payload,
            ArgumentObject[] arguments,
            Dictionary<int, LocalVariableSymbol> dictionary)
        {
            // Here we have a regular call, so create the calling object off of the first
            // parameter and pass it through.
            EXPR callingObject;
            if (payload.StaticCall)
            {
                if (arguments[0].Value == null || !(arguments[0].Value is Type))
                {
                    Debug.Assert(false, "Cannot make static call without specifying a type");
                    throw Error.InternalCompilerError();
                }
                Type t = arguments[0].Value as Type;
                callingObject = _exprFactory.CreateClass(_symbolTable.GetCTypeFromType(t), null, t.GetTypeInfo().ContainsGenericParameters ?
                        _exprFactory.CreateTypeArguments(SymbolLoader.getBSymmgr().AllocParams(_symbolTable.GetCTypeArrayFromTypes(t.GetGenericArguments())), null) : null);
            }
            else
            {
                // If we have a null argument, just bail and throw.
                if (!arguments[0].Info.UseCompileTimeType && arguments[0].Value == null)
                {
                    throw Error.NullReferenceOnMemberException();
                }

                callingObject = _binder.mustConvert(
                    CreateArgumentEXPR(arguments[0], dictionary[0]),
                    _symbolTable.GetCTypeFromType(arguments[0].Type));

                if (arguments[0].Type.GetTypeInfo().IsValueType && callingObject.isCAST())
                {
                    // If we have a struct type, unbox it.
                    callingObject.flags |= EXPRFLAG.EXF_UNBOXRUNTIME;
                }
            }
            return callingObject;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR BindCall(
            ICSharpInvokeOrInvokeMemberBinder payload,
            EXPR callingObject,
            ArgumentObject[] arguments,
            Dictionary<int, LocalVariableSymbol> dictionary)
        {
            if (payload is InvokeBinder && !callingObject.type.isDelegateType())
            {
                throw Error.BindInvokeFailedNonDelegate();
            }

            EXPR pResult = null;
            int arity = payload.TypeArguments != null ? payload.TypeArguments.Count : 0;
            MemberLookup mem = new MemberLookup();

            Debug.Assert(_bindingContext.ContextForMemberLookup() != null);
            SymWithType swt = _symbolTable.LookupMember(
                    payload.Name,
                    callingObject,
                    _bindingContext.ContextForMemberLookup(),
                    arity,
                    mem,
                    (payload.Flags & CSharpCallFlags.EventHookup) != 0,
                    true);
            if (swt == null)
            {
                mem.ReportErrors();
                Debug.Assert(false, "Why didn't member lookup report an error?");
            }

            if (swt.Sym.getKind() != SYMKIND.SK_MethodSymbol)
            {
                Debug.Assert(false, "Unexpected type returned from lookup");
                throw Error.InternalCompilerError();
            }

            // At this point, we're set up to do binding. We need to do the following:
            //
            // 1) Create the EXPRLOCALs for the arguments, linking them to the local
            //    variable symbols defined above.
            // 2) Create the EXPRMEMGRP for the call and the EXPRLOCAL for the object
            //    of the call, and link the correct local variable symbol as above.
            // 3) Do overload resolution to get back an EXPRCALL.
            //
            // Our caller takes care of the rest.

            // First we need to check the sym that we got back. If we got back a static 
            // method, then we may be in the situation where the user called the method
            // via a simple name call through the phantom overload. If thats the case,
            // then we want to sub in a type instead of the object.
            EXPRMEMGRP memGroup = CreateMemberGroupEXPR(payload.Name, payload.TypeArguments, callingObject, swt.Sym.getKind());
            if ((payload.Flags & CSharpCallFlags.SimpleNameCall) != 0)
            {
                callingObject.flags |= EXPRFLAG.EXF_SIMPLENAME;
            }

            if ((payload.Flags & CSharpCallFlags.EventHookup) != 0)
            {
                mem = new MemberLookup();
                SymWithType swtEvent = _symbolTable.LookupMember(
                        payload.Name.Split('_')[1],
                        callingObject,
                        _bindingContext.ContextForMemberLookup(),
                        arity,
                        mem,
                        (payload.Flags & CSharpCallFlags.EventHookup) != 0,
                        true);
                if (swtEvent == null)
                {
                    mem.ReportErrors();
                    Debug.Assert(false, "Why didn't member lookup report an error?");
                }

                CType eventCType = null;
                if (swtEvent.Sym.getKind() == SYMKIND.SK_FieldSymbol)
                {
                    eventCType = swtEvent.Field().GetType();
                }
                else if (swtEvent.Sym.getKind() == SYMKIND.SK_EventSymbol)
                {
                    eventCType = swtEvent.Event().type;
                }

                Type eventType = SymbolLoader.GetTypeManager().SubstType(eventCType, swtEvent.Ats).AssociatedSystemType;

                if (eventType != null)
                {
                    // If we have an event hookup, first find the event itself.
                    BindImplicitConversion(new ArgumentObject[] { arguments[1] }, eventType, dictionary, false);
                }
                memGroup.flags &= ~EXPRFLAG.EXF_USERCALLABLE;

                if (swtEvent.Sym.getKind() == SYMKIND.SK_EventSymbol && swtEvent.Event().IsWindowsRuntimeEvent)
                {
                    return BindWinRTEventAccessor(
                                    new EventWithType(swtEvent.Event(), swtEvent.Ats),
                                    callingObject,
                                    arguments,
                                    dictionary,
                                    payload.Name.StartsWith("add_", StringComparison.Ordinal)); //isAddAccessor?
                }
            }

            // Check if we have a potential call to an indexed property accessor.
            // If so, we'll flag overload resolution to let us call non-callables.
            if ((payload.Name.StartsWith("set_", StringComparison.Ordinal) && swt.Sym.AsMethodSymbol().Params.Size > 1) ||
                (payload.Name.StartsWith("get_", StringComparison.Ordinal) && swt.Sym.AsMethodSymbol().Params.Size > 0))
            {
                memGroup.flags &= ~EXPRFLAG.EXF_USERCALLABLE;
            }

            pResult = _binder.BindMethodGroupToArguments(// Tree
                BindingFlag.BIND_RVALUEREQUIRED | BindingFlag.BIND_STMTEXPRONLY, memGroup, CreateArgumentListEXPR(arguments, dictionary, 1, arguments.Length));

            // If overload resolution failed, throw an error.
            if (pResult == null || !pResult.isOK())
            {
                throw Error.BindCallFailedOverloadResolution();
            }
            CheckForConditionalMethodError(pResult);

            return ReorderArgumentsForNamedAndOptional(callingObject, pResult);
        }

        private EXPR BindWinRTEventAccessor(EventWithType ewt, EXPR callingObject, ArgumentObject[] arguments, Dictionary<int, LocalVariableSymbol> dictionary, bool isAddAccessor)
        {
            // We want to generate either:
            // WindowsRuntimeMarshal.AddEventHandler<delegType>(new Func<delegType, EventRegistrationToken>(x.add_foo), new Action<EventRegistrationToken>(x.remove_foo), value)
            // or
            // WindowsRuntimeMarshal.RemoveEventHandler<delegType>(new Action<EventRegistrationToken>(x.remove_foo), value)

            Type evtType = ewt.Event().type.AssociatedSystemType;

            // Get new Action<EventRegistrationToken>(x.remove_foo)
            MethPropWithInst removemwi = new MethPropWithInst(ewt.Event().methRemove, ewt.Ats);
            EXPRMEMGRP removeMethGrp = _exprFactory.CreateMemGroup(callingObject, removemwi);
            removeMethGrp.flags &= ~EXPRFLAG.EXF_USERCALLABLE;
            Type eventRegistrationTokenType = SymbolTable.EventRegistrationTokenType;
            Type actionType = Expression.GetActionType(eventRegistrationTokenType);
            EXPR removeMethArg = _binder.mustConvert(removeMethGrp, _symbolTable.GetCTypeFromType(actionType));

            // The value
            EXPR delegateVal = CreateArgumentEXPR(arguments[1], dictionary[1]);
            EXPRLIST args;
            string methodName;

            if (isAddAccessor)
            {
                // Get new Func<delegType, EventRegistrationToken>(x.add_foo)
                MethPropWithInst addmwi = new MethPropWithInst(ewt.Event().methAdd, ewt.Ats);
                EXPRMEMGRP addMethGrp = _exprFactory.CreateMemGroup(callingObject, addmwi);
                addMethGrp.flags &= ~EXPRFLAG.EXF_USERCALLABLE;
                Type funcType = Expression.GetFuncType(evtType, eventRegistrationTokenType);
                EXPR addMethArg = _binder.mustConvert(addMethGrp, _symbolTable.GetCTypeFromType(funcType));

                args = _exprFactory.CreateList(addMethArg, removeMethArg, delegateVal);
                methodName = SymbolLoader.GetNameManager().GetPredefName(PredefinedName.PN_ADDEVENTHANDLER).Text;
            }
            else
            {
                args = _exprFactory.CreateList(removeMethArg, delegateVal);
                methodName = SymbolLoader.GetNameManager().GetPredefName(PredefinedName.PN_REMOVEEVENTHANDLER).Text;
            }

            // WindowsRuntimeMarshal.Add\RemoveEventHandler(...)
            Type windowsRuntimeMarshalType = SymbolTable.WindowsRuntimeMarshalType;
            _symbolTable.PopulateSymbolTableWithName(methodName, new List<Type> { evtType }, windowsRuntimeMarshalType);
            EXPRCLASS marshalClass = _exprFactory.CreateClass(_symbolTable.GetCTypeFromType(windowsRuntimeMarshalType), null, null);
            EXPRMEMGRP addEventGrp = CreateMemberGroupEXPR(methodName, new List<Type> { evtType }, marshalClass, SYMKIND.SK_MethodSymbol);
            EXPR expr = _binder.BindMethodGroupToArguments(
                BindingFlag.BIND_RVALUEREQUIRED | BindingFlag.BIND_STMTEXPRONLY,
                addEventGrp,
                args);

            return expr;
        }

        private void CheckForConditionalMethodError(EXPR pExpr)
        {
            Debug.Assert(pExpr.isCALL());
            if (pExpr.isCALL())
            {
                // This mimics the behavior of the native CompilerSymbolLoader in GetConditionalSymbols. Override
                // methods cannot have the conditional attribute, but implicitly acquire it from their slot.

                EXPRCALL call = pExpr.asCALL();

                MethodSymbol method = call.mwi.Meth();
                if (method.isOverride)
                {
                    method = method.swtSlot.Meth();
                }

                object[] conditions = method.AssociatedMemberInfo.GetCustomAttributes(typeof(ConditionalAttribute), false).ToArray();
                if (conditions.Length > 0)
                {
                    throw Error.BindCallToConditionalMethod(method.name);
                }
            }
        }

        private EXPR ReorderArgumentsForNamedAndOptional(EXPR callingObject, EXPR pResult)
        {
            EXPR arguments;
            AggregateType type;
            MethodOrPropertySymbol methprop;
            EXPRMEMGRP memgroup;
            TypeArray typeArgs;

            if (pResult.isCALL())
            {
                EXPRCALL call = pResult.asCALL();
                arguments = call.GetOptionalArguments();
                type = call.mwi.Ats;
                methprop = call.mwi.Meth();
                memgroup = call.GetMemberGroup();
                typeArgs = call.mwi.TypeArgs;
            }
            else
            {
                Debug.Assert(pResult.isPROP());
                EXPRPROP prop = pResult.asPROP();
                arguments = prop.GetOptionalArguments();
                type = prop.pwtSlot.Ats;
                methprop = prop.pwtSlot.Prop();
                memgroup = prop.GetMemberGroup();
                typeArgs = null;
            }

            ArgInfos argInfo = new ArgInfos();
            bool b;
            argInfo.carg = ExpressionBinder.CountArguments(arguments, out b);
            _binder.FillInArgInfoFromArgList(argInfo, arguments);

            // We need to substitute type parameters BEFORE getting the most derived one because
            // we're binding against the base method, and the derived method may change the 
            // generic arguments. 
            TypeArray parameters = SymbolLoader.GetTypeManager().SubstTypeArray(methprop.Params, type, typeArgs);
            methprop = ExpressionBinder.GroupToArgsBinder.FindMostDerivedMethod(SymbolLoader, methprop, callingObject.type);
            ExpressionBinder.GroupToArgsBinder.ReOrderArgsForNamedArguments(
                methprop,
                parameters,
                type,
                memgroup,
                argInfo,
                _semanticChecker.GetTypeManager(),
                _exprFactory,
                SymbolLoader);
            {
                EXPR pList = null;

                // We reordered, so make a new list of them and set them on the constructor.
                // Go backwards cause lists are right-flushed.
                // Also perform the conversions to the right types.
                for (int i = argInfo.carg - 1; i >= 0; i--)
                {
                    EXPR pArg = argInfo.prgexpr[i];

                    // Strip the name-ness away, since we don't need it.
                    pArg = StripNamedArgument(pArg);

                    // Perform the correct conversion.
                    pArg = _binder.tryConvert(pArg, parameters[i]);
                    if (pList == null)
                    {
                        pList = pArg;
                    }
                    else
                    {
                        pList = _exprFactory.CreateList(pArg, pList);
                    }
                }
                if (pResult.isCALL())
                {
                    pResult.asCALL().SetOptionalArguments(pList);
                }
                else
                {
                    pResult.asPROP().SetOptionalArguments(pList);
                }
            }
            return pResult;
        }

        private EXPR StripNamedArgument(EXPR pArg)
        {
            if (pArg.isNamedArgumentSpecification())
            {
                pArg = pArg.asNamedArgumentSpecification().Value;
            }
            else if (pArg.isARRINIT())
            {
                pArg.asARRINIT().SetOptionalArguments(StripNamedArguments(pArg.asARRINIT().GetOptionalArguments()));
            }

            return pArg;
        }

        private EXPR StripNamedArguments(EXPR pArg)
        {
            if (pArg.isLIST())
            {
                EXPRLIST list = pArg.asLIST();
                while (list != null)
                {
                    list.SetOptionalElement(StripNamedArgument(list.GetOptionalElement()));

                    if (list.GetOptionalNextListNode().isLIST())
                    {
                        list = list.GetOptionalNextListNode().asLIST();
                    }
                    else
                    {
                        list.SetOptionalNextListNode(StripNamedArgument(list.GetOptionalNextListNode()));
                        break;
                    }
                }
            }
            return StripNamedArgument(pArg);
        }
        #endregion

        #region Operators
        #region UnaryOperators
        /////////////////////////////////////////////////////////////////////////////////

        private EXPR BindUnaryOperation(
            CSharpUnaryOperationBinder payload,
            ArgumentObject[] arguments,
            Dictionary<int, LocalVariableSymbol> dictionary)
        {
            if (arguments.Length != 1)
            {
                throw Error.BindUnaryOperatorRequireOneArgument();
            }

            OperatorKind op = GetOperatorKind(payload.Operation);
            EXPR arg1 = CreateArgumentEXPR(arguments[0], dictionary[0]);
            arg1.errorString = Operators.GetDisplayName(GetOperatorKind(payload.Operation));

            if (op == OperatorKind.OP_TRUE || op == OperatorKind.OP_FALSE)
            {
                // For true and false, we try to convert to bool first. If that
                // doesn't work, then we look for user defined operators.
                EXPR result = _binder.tryConvert(arg1, SymbolLoader.GetReqPredefType(PredefinedType.PT_BOOL));
                if (result != null && op == OperatorKind.OP_FALSE)
                {
                    // If we can convert to bool, we need to negate the thing if we're looking for false.
                    result = _binder.BindStandardUnaryOperator(OperatorKind.OP_LOGNOT, result);
                }

                if (result == null)
                {
                    result = _binder.bindUDUnop(op == OperatorKind.OP_TRUE ? ExpressionKind.EK_TRUE : ExpressionKind.EK_FALSE, arg1);
                }

                // If the result is STILL null, then that means theres no implicit conversion to bool,
                // and no user-defined operators for true and false. Just do a must convert to report
                // the error.
                if (result == null)
                {
                    result = _binder.mustConvert(arg1, SymbolLoader.GetReqPredefType(PredefinedType.PT_BOOL));
                }
                return result;
            }
            return _binder.BindStandardUnaryOperator(op, arg1);
        }
        #endregion

        #region BinaryOperators

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR BindBinaryOperation(
                CSharpBinaryOperationBinder payload,
                ArgumentObject[] arguments,
                Dictionary<int, LocalVariableSymbol> dictionary)
        {
            if (arguments.Length != 2)
            {
                throw Error.BindBinaryOperatorRequireTwoArguments();
            }

            ExpressionKind ek = Operators.GetExpressionKind(GetOperatorKind(payload.Operation, payload.IsLogicalOperation));
            EXPR arg1 = CreateArgumentEXPR(arguments[0], dictionary[0]);
            EXPR arg2 = CreateArgumentEXPR(arguments[1], dictionary[1]);

            arg1.errorString = Operators.GetDisplayName(GetOperatorKind(payload.Operation, payload.IsLogicalOperation));
            arg2.errorString = Operators.GetDisplayName(GetOperatorKind(payload.Operation, payload.IsLogicalOperation));

            if (ek > ExpressionKind.EK_MULTIOFFSET)
            {
                ek = (ExpressionKind)(ek - ExpressionKind.EK_MULTIOFFSET);
            }
            return _binder.BindStandardBinop(ek, arg1, arg2);
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        private static OperatorKind GetOperatorKind(ExpressionType p)
        {
            return GetOperatorKind(p, false);
        }

        private static OperatorKind GetOperatorKind(ExpressionType p, bool bIsLogical)
        {
            switch (p)
            {
                default:
                    Debug.Assert(false, "Unknown operator: " + p);
                    throw Error.InternalCompilerError();

                // Binary Operators
                case ExpressionType.Add:
                    return OperatorKind.OP_ADD;
                case ExpressionType.Subtract:
                    return OperatorKind.OP_SUB;
                case ExpressionType.Multiply:
                    return OperatorKind.OP_MUL;
                case ExpressionType.Divide:
                    return OperatorKind.OP_DIV;
                case ExpressionType.Modulo:
                    return OperatorKind.OP_MOD;
                case ExpressionType.LeftShift:
                    return OperatorKind.OP_LSHIFT;
                case ExpressionType.RightShift:
                    return OperatorKind.OP_RSHIFT;
                case ExpressionType.LessThan:
                    return OperatorKind.OP_LT;
                case ExpressionType.GreaterThan:
                    return OperatorKind.OP_GT;
                case ExpressionType.LessThanOrEqual:
                    return OperatorKind.OP_LE;
                case ExpressionType.GreaterThanOrEqual:
                    return OperatorKind.OP_GE;
                case ExpressionType.Equal:
                    return OperatorKind.OP_EQ;
                case ExpressionType.NotEqual:
                    return OperatorKind.OP_NEQ;
                case ExpressionType.And:
                    return bIsLogical ? OperatorKind.OP_LOGAND : OperatorKind.OP_BITAND;
                case ExpressionType.ExclusiveOr:
                    return OperatorKind.OP_BITXOR;
                case ExpressionType.Or:
                    return bIsLogical ? OperatorKind.OP_LOGOR : OperatorKind.OP_BITOR;

                // Binary in place operators.
                case ExpressionType.AddAssign:
                    return OperatorKind.OP_ADDEQ;
                case ExpressionType.SubtractAssign:
                    return OperatorKind.OP_SUBEQ;
                case ExpressionType.MultiplyAssign:
                    return OperatorKind.OP_MULEQ;
                case ExpressionType.DivideAssign:
                    return OperatorKind.OP_DIVEQ;
                case ExpressionType.ModuloAssign:
                    return OperatorKind.OP_MODEQ;
                case ExpressionType.AndAssign:
                    return OperatorKind.OP_ANDEQ;
                case ExpressionType.ExclusiveOrAssign:
                    return OperatorKind.OP_XOREQ;
                case ExpressionType.OrAssign:
                    return OperatorKind.OP_OREQ;
                case ExpressionType.LeftShiftAssign:
                    return OperatorKind.OP_LSHIFTEQ;
                case ExpressionType.RightShiftAssign:
                    return OperatorKind.OP_RSHIFTEQ;

                // Unary Operators
                case ExpressionType.Negate:
                    return OperatorKind.OP_NEG;
                case ExpressionType.UnaryPlus:
                    return OperatorKind.OP_UPLUS;
                case ExpressionType.Not:
                    return OperatorKind.OP_LOGNOT;
                case ExpressionType.OnesComplement:
                    return OperatorKind.OP_BITNOT;
                case ExpressionType.IsTrue:
                    return OperatorKind.OP_TRUE;
                case ExpressionType.IsFalse:
                    return OperatorKind.OP_FALSE;

                // Increment/Decrement.
                case ExpressionType.Increment:
                    return OperatorKind.OP_PREINC;
                case ExpressionType.Decrement:
                    return OperatorKind.OP_PREDEC;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static string GetCLROperatorName(ExpressionType p)
        {
            switch (p)
            {
                default:
                    return null;

                // Binary Operators
                case ExpressionType.Add:
                    return SpecialNames.CLR_Add;
                case ExpressionType.Subtract:
                    return SpecialNames.CLR_Subtract;
                case ExpressionType.Multiply:
                    return SpecialNames.CLR_Multiply;
                case ExpressionType.Divide:
                    return SpecialNames.CLR_Division;
                case ExpressionType.Modulo:
                    return SpecialNames.CLR_Modulus;
                case ExpressionType.LeftShift:
                    return SpecialNames.CLR_LShift;
                case ExpressionType.RightShift:
                    return SpecialNames.CLR_RShift;
                case ExpressionType.LessThan:
                    return SpecialNames.CLR_LT;
                case ExpressionType.GreaterThan:
                    return SpecialNames.CLR_GT;
                case ExpressionType.LessThanOrEqual:
                    return SpecialNames.CLR_LTE;
                case ExpressionType.GreaterThanOrEqual:
                    return SpecialNames.CLR_GTE;
                case ExpressionType.Equal:
                    return SpecialNames.CLR_Equality;
                case ExpressionType.NotEqual:
                    return SpecialNames.CLR_Inequality;
                case ExpressionType.And:
                    return SpecialNames.CLR_BitwiseAnd;
                case ExpressionType.ExclusiveOr:
                    return SpecialNames.CLR_ExclusiveOr;
                case ExpressionType.Or:
                    return SpecialNames.CLR_BitwiseOr;

                // "op_LogicalNot";
                case ExpressionType.AddAssign:
                    return SpecialNames.CLR_InPlaceAdd;
                case ExpressionType.SubtractAssign:
                    return SpecialNames.CLR_InPlaceSubtract;
                case ExpressionType.MultiplyAssign:
                    return SpecialNames.CLR_InPlaceMultiply;
                case ExpressionType.DivideAssign:
                    return SpecialNames.CLR_InPlaceDivide;
                case ExpressionType.ModuloAssign:
                    return SpecialNames.CLR_InPlaceModulus;
                case ExpressionType.AndAssign:
                    return SpecialNames.CLR_InPlaceBitwiseAnd;
                case ExpressionType.ExclusiveOrAssign:
                    return SpecialNames.CLR_InPlaceExclusiveOr;
                case ExpressionType.OrAssign:
                    return SpecialNames.CLR_InPlaceBitwiseOr;
                case ExpressionType.LeftShiftAssign:
                    return SpecialNames.CLR_InPlaceLShift;
                case ExpressionType.RightShiftAssign:
                    return SpecialNames.CLR_InPlaceRShift;

                // Unary Operators
                case ExpressionType.Negate:
                    return SpecialNames.CLR_UnaryNegation;
                case ExpressionType.UnaryPlus:
                    return SpecialNames.CLR_UnaryPlus;
                case ExpressionType.Not:
                    return SpecialNames.CLR_LogicalNot;
                case ExpressionType.OnesComplement:
                    return SpecialNames.CLR_OnesComplement;
                case ExpressionType.IsTrue:
                    return SpecialNames.CLR_True;
                case ExpressionType.IsFalse:
                    return SpecialNames.CLR_False;

                case ExpressionType.Increment:
                    return SpecialNames.CLR_PreIncrement;
                case ExpressionType.Decrement:
                    return SpecialNames.CLR_PreDecrement;
            }
        }

        #endregion

        #region Properties
        /////////////////////////////////////////////////////////////////////////////////

        private EXPR BindProperty(
            DynamicMetaObjectBinder payload,
            ArgumentObject argument,
            LocalVariableSymbol local,
            EXPR optionalIndexerArguments,
            bool fEventsPermitted)
        {
            // If our argument is a static type, then we're calling a static property.
            EXPR callingObject = argument.Info.IsStaticType ?
                _exprFactory.CreateClass(_symbolTable.GetCTypeFromType(argument.Value as Type), null, null) :
                CreateLocal(argument.Type, argument.Info.IsOut, local);

            if (!argument.Info.UseCompileTimeType && argument.Value == null)
            {
                throw Error.NullReferenceOnMemberException();
            }

            // If our argument is a struct type, unbox it.
            if (argument.Type.GetTypeInfo().IsValueType && callingObject.isCAST())
            {
                // If we have a struct type, unbox it.
                callingObject.flags |= EXPRFLAG.EXF_UNBOXRUNTIME;
            }
            string name = GetName(payload);
            BindingFlag bindFlags = GetBindingFlags(payload);

            MemberLookup mem = new MemberLookup();
            SymWithType swt = _symbolTable.LookupMember(name, callingObject, _bindingContext.ContextForMemberLookup(), 0, mem, false, false);
            if (swt == null)
            {
                if (optionalIndexerArguments != null)
                {
                    int numIndexArguments = ExpressionIterator.Count(optionalIndexerArguments);
                    // We could have an array access here. See if its just an array.
                    if ((argument.Type.IsArray && argument.Type.GetArrayRank() == numIndexArguments) ||
                        argument.Type == typeof(string))
                    {
                        return CreateArray(callingObject, optionalIndexerArguments);
                    }
                }
                mem.ReportErrors();
                Debug.Assert(false, "Why didn't member lookup report an error?");
            }

            switch (swt.Sym.getKind())
            {
                case SYMKIND.SK_MethodSymbol:
                    throw Error.BindPropertyFailedMethodGroup(name);

                case SYMKIND.SK_PropertySymbol:
                    if (swt.Sym is IndexerSymbol)
                    {
                        return CreateIndexer(swt, callingObject, optionalIndexerArguments, bindFlags);
                    }
                    else
                    {
                        BindingFlag flags = 0;
                        if (payload is CSharpGetMemberBinder || payload is CSharpGetIndexBinder)
                        {
                            flags = BindingFlag.BIND_RVALUEREQUIRED;
                        }

                        // Properties can be LValues.
                        callingObject.flags |= EXPRFLAG.EXF_LVALUE;
                        return CreateProperty(swt, callingObject, flags);
                    }

                case SYMKIND.SK_FieldSymbol:
                    return CreateField(swt, callingObject);

                case SYMKIND.SK_EventSymbol:
                    if (fEventsPermitted)
                    {
                        return CreateEvent(swt, callingObject);
                    }
                    else
                    {
                        throw Error.BindPropertyFailedEvent(name);
                    }

                default:
                    Debug.Assert(false, "Unexpected type returned from lookup");
                    throw Error.InternalCompilerError();
            }
        }

        #endregion

        #region Casts
        /////////////////////////////////////////////////////////////////////////////////

        private EXPR BindImplicitConversion(
            ArgumentObject[] arguments,
            Type returnType,
            Dictionary<int, LocalVariableSymbol> dictionary,
            bool bIsArrayCreationConversion)
        {
            if (arguments.Length != 1)
            {
                throw Error.BindImplicitConversionRequireOneArgument();
            }

            // Load the conversions on the target.
            _symbolTable.AddConversionsForType(returnType);

            EXPR argument = CreateArgumentEXPR(arguments[0], dictionary[0]);
            CType destinationType = _symbolTable.GetCTypeFromType(returnType);

            if (bIsArrayCreationConversion)
            {
                // If we are converting for an array index, we want to convert to int, uint,
                // long, or ulong, depending on what the argument will allow. However, since
                // the compiler had to pick a particular type for the return value when it
                // made the callsite, we need to make sure that we ultimately return a type
                // of that value. So we "mustConvert" to the best type that chooseArrayIndexType
                // can find, and then we cast the result of that to the returnType, which is
                // incidentally Int32 in the existing compiler. For that cast, we do not consider
                // user defined conversions (since the convert is guaranteed to return one of
                // the primitive types), and we check for overflow since we don't want truncation.

                CType pDestType = _binder.chooseArrayIndexType(argument);
                if (null == pDestType)
                {
                    pDestType = SymbolLoader.GetReqPredefType(PredefinedType.PT_INT, true);
                }

                return _binder.mustCast(
                    _binder.mustConvert(argument, pDestType),
                    destinationType,
                    CONVERTTYPE.CHECKOVERFLOW | CONVERTTYPE.NOUDC);
            }

            return _binder.mustConvert(argument, destinationType);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR BindExplicitConversion(ArgumentObject[] arguments, Type returnType, Dictionary<int, LocalVariableSymbol> dictionary)
        {
            if (arguments.Length != 1)
            {
                throw Error.BindExplicitConversionRequireOneArgument();
            }

            // Load the conversions on the target.
            _symbolTable.AddConversionsForType(returnType);

            EXPR argument = CreateArgumentEXPR(arguments[0], dictionary[0]);
            CType destinationType = _symbolTable.GetCTypeFromType(returnType);

            return _binder.mustCast(argument, destinationType);
        }

        #endregion

        #region Assignments

        /////////////////////////////////////////////////////////////////////////////////

        private EXPR BindAssignment(
            DynamicMetaObjectBinder payload,
            ArgumentObject[] arguments,
            Dictionary<int, LocalVariableSymbol> dictionary)
        {
            if (arguments.Length < 2)
            {
                throw Error.BindBinaryAssignmentRequireTwoArguments();
            }
            string name = GetName(payload);

            // Find the lhs and rhs.
            EXPR indexerArguments = null;
            bool bIsCompound = false;

            if (payload is CSharpSetIndexBinder)
            {
                // Get the list of indexer arguments - this is the list of arguments minus the last one.
                indexerArguments = CreateArgumentListEXPR(arguments, dictionary, 1, arguments.Length - 1);
                bIsCompound = (payload as CSharpSetIndexBinder).IsCompoundAssignment;
            }
            else
            {
                bIsCompound = (payload as CSharpSetMemberBinder).IsCompoundAssignment;
            }
            _symbolTable.PopulateSymbolTableWithName(name, null, arguments[0].Type);
            EXPR lhs = BindProperty(payload, arguments[0], dictionary[0], indexerArguments, false);

            int indexOfLast = arguments.Length - 1;
            EXPR rhs = CreateArgumentEXPR(arguments[indexOfLast], dictionary[indexOfLast]);

            if (arguments[0] == null)
            {
                throw Error.BindBinaryAssignmentFailedNullReference();
            }

            return _binder.bindAssignment(lhs, rhs, bIsCompound);
        }
        #endregion

        #region Events
        /////////////////////////////////////////////////////////////////////////////////

        private EXPR BindIsEvent(
            CSharpIsEventBinder binder,
            ArgumentObject[] arguments,
            Dictionary<int, LocalVariableSymbol> dictionary)
        {
            // The IsEvent binder will never be called without an instance object. This 
            // is because the compiler only gen's this code for dynamic dots.

            EXPR callingObject = CreateLocal(arguments[0].Type, false, dictionary[0]);
            MemberLookup mem = new MemberLookup();
            CType boolType = SymbolLoader.GetReqPredefType(PredefinedType.PT_BOOL);
            bool result = false;

            if (arguments[0].Value == null)
            {
                throw Error.NullReferenceOnMemberException();
            }

            Debug.Assert(_bindingContext.ContextForMemberLookup() != null);
            SymWithType swt = _symbolTable.LookupMember(
                    binder.Name,
                    callingObject,
                    _bindingContext.ContextForMemberLookup(),
                    0,
                    mem,
                    false,
                    false);

            // If lookup returns an actual event, then this is an event.
            if (swt != null && swt.Sym.getKind() == SYMKIND.SK_EventSymbol)
            {
                result = true;
            }

            // If lookup returns the backing field of a field-like event, then
            // this is an event. This is due to the Dev10 design change around
            // the binding of +=, and the fact that the "IsEvent" binding question
            // is only ever asked about the LHS of a += or -=.
            if (swt != null && swt.Sym.getKind() == SYMKIND.SK_FieldSymbol && swt.Sym.AsFieldSymbol().isEvent)
            {
                result = true;
            }

            return _exprFactory.CreateConstant(boolType, ConstValFactory.GetBool(result));
        }
        #endregion

        private string GetName(DynamicMetaObjectBinder payload)
        {
            string result = null;
            if (payload is CSharpGetMemberBinder)
            {
                result = ((CSharpGetMemberBinder)payload).Name;
            }
            else if (payload is CSharpSetMemberBinder)
            {
                result = ((CSharpSetMemberBinder)payload).Name;
            }
            else if (payload is CSharpGetIndexBinder || payload is CSharpSetIndexBinder)
            {
                result = SpecialNames.Indexer;
            }

            Debug.Assert(result != null);
            return result;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private BindingFlag GetBindingFlags(DynamicMetaObjectBinder payload)
        {
            if ((payload is CSharpGetMemberBinder) ||
                (payload is CSharpGetIndexBinder))
            {
                return BindingFlag.BIND_RVALUEREQUIRED;
            }
            return 0;
        }
    }
}
