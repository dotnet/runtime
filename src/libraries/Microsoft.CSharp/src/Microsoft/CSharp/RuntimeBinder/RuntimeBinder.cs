// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Semantics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal sealed class RuntimeBinder
    {
        #region Singleton Implementation

        private static readonly Lazy<RuntimeBinder> s_lazyInstance = new Lazy<RuntimeBinder>(() => new RuntimeBinder());

        public static RuntimeBinder GetInstance()
        {
            return s_lazyInstance.Value;
        }

        #endregion

        /////////////////////////////////////////////////////////////////////////////////
        // Members

        private SymbolTable _symbolTable;
        private CSemanticChecker _semanticChecker;
        private SymbolLoader SymbolLoader => _semanticChecker.SymbolLoader;

        private ExprFactory _exprFactory;
        private BindingContext _bindingContext;
        private ExpressionBinder _binder;

        private readonly object _bindLock = new object();

        /////////////////////////////////////////////////////////////////////////////////
        // Methods

        #region BookKeeping
        private RuntimeBinder()
        {
            Reset();
        }

        private void Reset()
        {
            _semanticChecker = new CSemanticChecker();

            BSYMMGR bsymmgr = _semanticChecker.getBSymmgr();

            _symbolTable = new SymbolTable(
                bsymmgr.GetSymbolTable(),
                bsymmgr.GetSymFactory(),
                _semanticChecker.GetTypeManager(),
                bsymmgr,
                _semanticChecker);
            _semanticChecker.getPredefTypes().Init(_symbolTable);
            _semanticChecker.GetTypeManager().InitTypeFactory(_symbolTable);
            SymbolLoader.getPredefinedMembers().RuntimeBinderSymbolTable = _symbolTable;
            SymbolLoader.SetSymbolTable(_symbolTable);

            _exprFactory = new ExprFactory(_semanticChecker.SymbolLoader.GetGlobalSymbolContext());
            _bindingContext = new BindingContext(_semanticChecker, _exprFactory);
            _binder = new ExpressionBinder(_bindingContext);
        }

        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        public Expression Bind(
            DynamicMetaObjectBinder payload,
            Expression[] parameters,
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

            ICSharpBinder binder = payload as ICSharpBinder;
            Debug.Assert(binder != null);

            lock (_bindLock)
            {
                return BindCore(binder, parameters, args, out deferredBinding);
            }
        }

        private Expression BindCore(
            ICSharpBinder payload,
            Expression[] parameters,
            DynamicMetaObject[] args,
            out DynamicMetaObject deferredBinding)
        {
            Debug.Assert(args.Length >= 1);

            InitializeCallingContext(payload);
            ArgumentObject[] arguments = CreateArgumentArray(payload, parameters, args);

            // On any given bind call, we populate the symbol table with any new
            // conversions that we find for any of the types specified. We keep a
            // running SymbolTable so that we don't have to reflect over types if 
            // we've seen them already in the table.
            //
            // Once we've loaded all the standard conversions into the symbol table,
            // we can call into the binder to bind the actual call.

            payload.PopulateSymbolTableWithName(_symbolTable, arguments[0].Type, arguments);
            AddConversionsForArguments(arguments);

            // When we do any bind, we perform the following steps:
            //
            // 1) Create a local variable scope which contains local variable symbols
            //    for each of the parameters, and the instance argument.
            // 2) If we have operators, then we don't need to do lookup. Otherwise, 
            //    look for the name and switch on the result - dispatch according to 
            //    the symbol kind. This results in an Expr being bound that is the expression.
            // 3) Create the EXPRRETURN which returns the call and wrap it in
            //    an EXPRBOUNDLAMBDA which uses the local variable scope
            //    created in step (1) as its local scope.
            // 4) Call the ExpressionTreeRewriter to generate a set of EXPRCALLs
            //    that call the static ExpressionTree factory methods.
            // 5) Call the EXPRTreeToExpressionTreeVisitor to generate the actual
            //    Linq expression tree for the whole thing and return it.

            // (1) - Create the locals
            Scope pScope = _semanticChecker.GetGlobalSymbolFactory().CreateScope();
            LocalVariableSymbol[] locals = PopulateLocalScope(payload, pScope, arguments, parameters);

            // (1.5) - Check to see if we need to defer.
            if (DeferBinding(payload, arguments, args, locals, out deferredBinding))
            {
                return null;
            }

            // (2) - look the thing up and dispatch.
            Expr pResult = payload.DispatchPayload(this, arguments, locals);
            Debug.Assert(pResult != null);

            return CreateExpressionTreeFromResult(parameters, pScope, pResult);
        }

        #region Helpers

        private bool DeferBinding(
            ICSharpBinder payload,
            ArgumentObject[] arguments,
            DynamicMetaObject[] args,
            LocalVariableSymbol[] locals,
            out DynamicMetaObject deferredBinding)
        {
            // This method deals with any deferrals we need to do. We check deferrals up front
            // and bail early if we need to do them.

            // (1) InvokeMember deferral.
            //
            // This is the deferral for the d.Foo() scenario where Foo actually binds to a 
            // field or property, and not a method group that is invocable. We defer to
            // the standard GetMember/Invoke pattern.

            CSharpInvokeMemberBinder callPayload = payload as CSharpInvokeMemberBinder;
            if (callPayload != null)
            {
                int arity = callPayload.TypeArguments?.Length ?? 0;
                MemberLookup mem = new MemberLookup();
                Expr callingObject = CreateCallingObjectForCall(callPayload, arguments, locals);

                SymWithType swt = _symbolTable.LookupMember(
                        callPayload.Name,
                        callingObject,
                        _bindingContext.ContextForMemberLookup,
                        arity,
                        mem,
                        (callPayload.Flags & CSharpCallFlags.EventHookup) != 0,
                        true);

                if (swt != null && swt.Sym.getKind() != SYMKIND.SK_MethodSymbol)
                {
                    // The GetMember only has one argument, and we need to just take the first arg info.
                    CSharpGetMemberBinder getMember = new CSharpGetMemberBinder(callPayload.Name, false, callPayload.CallingContext, new CSharpArgumentInfo[] { callPayload.GetArgumentInfo(0) });

                    // The Invoke has the remaining argument infos. However, we need to redo the first one
                    // to correspond to the GetMember result.
                    CSharpArgumentInfo[] argInfos = callPayload.ArgumentInfoArray();

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

        private void InitializeCallingContext(ICSharpBinder payload)
        {
            // Set the context if the payload specifies it. Currently we only use this for calls.
            Type t = payload.CallingContext;
            BindingContext bindingContext = _bindingContext;

            if (t != null)
            {
                AggregateSymbol agg = ((AggregateType)_symbolTable.GetCTypeFromType(t)).GetOwningAggregate();
                bindingContext.ContextForMemberLookup = _semanticChecker.GetGlobalSymbolFactory().CreateAggregateDecl(agg, null);
            }
            else
            {
                // The binding context lives across invocations! If we don't reset this, then later calls might
                // bind in a previous call's context.
                bindingContext.ContextForMemberLookup = null;
            }

            bindingContext.Checked = payload.IsChecked;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression CreateExpressionTreeFromResult(
            Expression[] parameters,
            Scope pScope,
            Expr pResult)
        {
            // (3) - Place the result in a return statement and create the ExprBoundLambda.
            ExprBoundLambda boundLambda = GenerateBoundLambda(pScope, pResult);

            // (4) - Rewrite the ExprBoundLambda into an expression tree.
            ExprBinOp exprTree = ExpressionTreeRewriter.Rewrite(boundLambda, _exprFactory, SymbolLoader);

            // (5) - Create the actual Expression Tree
            Expression e = ExpressionTreeCallRewriter.Rewrite(SymbolLoader.GetTypeManager(), exprTree, parameters);
            return e;
        }

        private Type GetArgumentType(ICSharpBinder p, CSharpArgumentInfo argInfo, Expression param, DynamicMetaObject arg, int index)
        {
            Type t = argInfo.UseCompileTimeType ? param.Type : arg.LimitType;
            Debug.Assert(t != null);

            if (argInfo.IsByRefOrOut)
            {
                // If we have a ref our an out parameter, make the byref type.
                // If we have the receiver of a call or invoke that is ref, it must be because of 
                // a struct caller. Don't persist the ref for that.
                if (!(index == 0 && p.IsBinderThatCanHaveRefReceiver))
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
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ArgumentObject[] CreateArgumentArray(
                ICSharpBinder payload,
                Expression[] parameters,
                DynamicMetaObject[] args)
        {
            // Check the payloads to see whether or not we need to get the runtime types for
            // these arguments.

            ArgumentObject[] array = new ArgumentObject[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                CSharpArgumentInfo info = payload.GetArgumentInfo(i);
                array[i] = new ArgumentObject(args[i].Value, info, GetArgumentType(payload, info, parameters[i], args[i], i));

                Debug.Assert(array[i].Type != null);
            }

            return array;
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal static void PopulateSymbolTableWithPayloadInformation(
            SymbolTable symbolTable, ICSharpInvokeOrInvokeMemberBinder callOrInvoke, Type callingType,
            ArgumentObject[] arguments)
        {
            Type type;

            if (callOrInvoke.StaticCall)
            {
                type = arguments[0].Value as Type;
                if (type == null)
                {
                    throw Error.BindStaticRequiresType(arguments[0].Info.Name);
                }
            }
            else
            {
                type = callingType;
            }
            symbolTable.PopulateSymbolTableWithName(
                callOrInvoke.Name,
                callOrInvoke.TypeArguments,
                type);

            // If it looks like we're invoking a get_ or a set_, load the property as well.
            // This is because we need COM indexed properties called via method calls to 
            // work the same as it used to.
            if (callOrInvoke.Name.StartsWith("set_", StringComparison.Ordinal) ||
                callOrInvoke.Name.StartsWith("get_", StringComparison.Ordinal))
            {
                symbolTable.PopulateSymbolTableWithName(
                    callOrInvoke.Name.Substring(4), //remove prefix
                    callOrInvoke.TypeArguments,
                    type);
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

        internal ExprWithArgs DispatchPayload(ICSharpInvokeOrInvokeMemberBinder payload, ArgumentObject[] arguments, LocalVariableSymbol[] locals) =>
            BindCall(payload, CreateCallingObjectForCall(payload, arguments, locals), arguments, locals);

        /////////////////////////////////////////////////////////////////////////////////
        // We take the ArgumentObjects to verify - if the parameter expression tells us 
        // we have a ref parameter, but the argument object tells us we're not passed by ref,
        // then it means it was a ref that the compiler had to insert. This is used when
        // we have a call off of a struct for example. If thats the case, don't treat the 
        // local as a ref type.

        private LocalVariableSymbol[] PopulateLocalScope(
            ICSharpBinder payload,
            Scope pScope,
            ArgumentObject[] arguments,
            Expression[] parameterExpressions)
        {
            // We use the compile time types for the local variables, and then 
            // cast them to the runtime types for the expression tree.
            LocalVariableSymbol[] locals = new LocalVariableSymbol[parameterExpressions.Length];

            for (int i = 0; i < parameterExpressions.Length; i++)
            {
                Expression parameter = parameterExpressions[i];
                CType type = _symbolTable.GetCTypeFromType(parameter.Type);

                // Make sure we're not setting ref for the receiver of a call - the argument
                // will be marked as ref if we're calling off a struct, but we don't want 
                // to persist that in our system.
                // If we're the first param of a call or invoke, and we're ref, it must be
                // because of structs. Don't persist the parameter modifier type.
                if (i != 0 || !payload.IsBinderThatCanHaveRefReceiver)
                {
                    // If we have a ref or out, get the parameter modifier type.
                    ParameterExpression paramExp = parameter as ParameterExpression;
                    if (paramExp != null && paramExp.IsByRef)
                    {
                        CSharpArgumentInfo info = arguments[i].Info;
                        if (info.IsByRefOrOut)
                        {
                            type = _semanticChecker.GetTypeManager().GetParameterModifier(type, info.IsOut);
                        }
                    }
                }
                LocalVariableSymbol local =
                    _semanticChecker.GetGlobalSymbolFactory()
                        .CreateLocalVar(NameManager.Add("p" + i), pScope, type);
                locals[i] = local;
            }

            return locals;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExprBoundLambda GenerateBoundLambda(
            Scope pScope,
            Expr call)
        {
            // We don't actually need the real delegate type here - we just need SOME delegate type.
            // This is because we never attempt any conversions on the lambda itself.
            AggregateType delegateType = _semanticChecker.SymbolLoader.GetPredefindType(PredefinedType.PT_FUNC);
            return _exprFactory.CreateAnonymousMethod(delegateType, pScope, call);
        }

        #region ExprCreation

        /////////////////////////////////////////////////////////////////////////////////

        private Expr CreateLocal(Type type, bool bIsOut, LocalVariableSymbol local)
        {
            CType ctype = _symbolTable.GetCTypeFromType(type);
            if (bIsOut)
            {
                Debug.Assert(ctype is ParameterModifierType);
                ctype = _semanticChecker.GetTypeManager()
                    .GetParameterModifier(((ParameterModifierType)ctype).GetParameterType(), true);
            }

            // If we can convert, do that. If not, cast it.
            ExprLocal exprLocal = _exprFactory.CreateLocal(local);
            Expr result = _binder.tryConvert(exprLocal, ctype);
            if (result == null)
            {
                result = _binder.mustCast(exprLocal, ctype);
            }
            result.Flags |= EXPRFLAG.EXF_LVALUE;
            return result;
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal Expr CreateArgumentListEXPR(
            ArgumentObject[] arguments,
            LocalVariableSymbol[] locals,
            int startIndex,
            int endIndex)
        {
            Expr args = null;
            Expr last = null;

            if (arguments != null)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    ArgumentObject argument = arguments[i];
                    Expr arg = CreateArgumentEXPR(argument, locals[i]);

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

        private Expr CreateArgumentEXPR(ArgumentObject argument, LocalVariableSymbol local)
        {
            Expr arg;
            if (argument.Info.LiteralConstant)
            {
                if (argument.Value == null)
                {
                    if (argument.Info.UseCompileTimeType)
                    {
                        arg = _exprFactory.CreateConstant(_symbolTable.GetCTypeFromType(argument.Type), default(ConstVal));
                    }
                    else
                    {
                        arg = _exprFactory.CreateNull();
                    }
                }
                else
                {
                    arg = _exprFactory.CreateConstant(_symbolTable.GetCTypeFromType(argument.Type), ConstVal.Get(argument.Value));
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
                arg = _exprFactory.CreateNamedArgumentSpecification(NameManager.Add(argument.Info.Name), arg);
            }

            // If we have an object that was "dynamic" at compile time, we need
            // to be able to convert it to every interface that the actual value
            // implements. This allows conversion binders and overload resolution
            // to behave as though type information is available for these Exprs,
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

        private ExprMemberGroup CreateMemberGroupEXPR(
            string Name,
            Type[] typeArguments,
            Expr callingObject,
            SYMKIND kind)
        {
            Name name = NameManager.Add(Name);
            AggregateType callingType;

            CType callingObjectType = callingObject.Type;
            if (callingObjectType is ArrayType)
            {
                callingType = _semanticChecker.SymbolLoader.GetPredefindType(PredefinedType.PT_ARRAY);
            }
            else if (callingObjectType is NullableType callingNub)
            {
                callingType = callingNub.GetAts();
            }
            else
            {
                callingType = (AggregateType)callingObjectType;
            }

            // The C# binder expects that only the base virtual method is inserted
            // into the list of candidates, and only the type containing the base
            // virtual method is inserted into the list of types. However, since we
            // don't want to do all the logic, we're just going to insert every type
            // that has a member of the given name, and allow the C# binder to filter
            // out all overrides.

            // CONSIDER: using a hashset to filter out duplicate interface types.
            // Adopt a smarter algorithm to filter types before creating the exception.
            HashSet<CType> distinctCallingTypes = new HashSet<CType>();
            List<CType> callingTypes = new List<CType>();

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
            bool bIsConstructor = name == NameManager.GetPredefinedName(PredefinedName.PN_CTOR);
            foreach(AggregateType t in callingType.TypeHierarchy)
            {
                if (_symbolTable.AggregateContainsMethod(t.GetOwningAggregate(), Name, mask) && distinctCallingTypes.Add(t))
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
                foreach (AggregateType t in callingType.GetWinRTCollectionIfacesAll(SymbolLoader).Items)
                {
                    if (_symbolTable.AggregateContainsMethod(t.GetOwningAggregate(), Name, mask) && distinctCallingTypes.Add(t))
                    {
                        callingTypes.Add(t);
                    }
                }
            }

            EXPRFLAG flags = EXPRFLAG.EXF_USERCALLABLE;
            // If its a delegate, mark that on the memgroup.
            if (Name == SpecialNames.Invoke && callingObject.Type.isDelegateType())
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
            if (typeArguments != null && typeArguments.Length > 0)
            {
                typeArgumentsAsTypeArray = _semanticChecker.getBSymmgr().AllocParams(
                    _symbolTable.GetCTypeArrayFromTypes(typeArguments));
            }
            ExprMemberGroup memgroup = _exprFactory.CreateMemGroup(// Tree
                flags, name, typeArgumentsAsTypeArray, kind, callingType, null, null, new CMemberLookupResults(
                    _semanticChecker.getBSymmgr().AllocParams(callingTypes.Count, callingTypes.ToArray()),
                    name));
            if (callingObject is ExprClass)
            {
                memgroup.OptionalLHS = callingObject;
            }
            else
            {
                memgroup.OptionalObject = callingObject;
            }
            return memgroup;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expr CreateProperty(
            SymWithType swt,
            Expr callingObject,
            BindingFlag flags)
        {
            // For a property, we simply create the EXPRPROP for the thing, call the
            // expression tree rewriter, rewrite it, and send it on its way.

            PropertySymbol property = swt.Prop();
            AggregateType propertyType = swt.GetType();
            PropWithType pwt = new PropWithType(property, propertyType);
            ExprMemberGroup pMemGroup = CreateMemberGroupEXPR(property.name.Text, null, callingObject, SYMKIND.SK_PropertySymbol);

            return _binder.BindToProperty(// For a static property instance, don't set the object.
                    callingObject is ExprClass ? null : callingObject, pwt, flags, null, pMemGroup);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExprWithArgs CreateIndexer(SymWithType swt, Expr callingObject, Expr arguments, BindingFlag bindFlags)
        {
            IndexerSymbol index = swt.Sym as IndexerSymbol;
            ExprMemberGroup memgroup = CreateMemberGroupEXPR(index.name.Text, null, callingObject, SYMKIND.SK_PropertySymbol);
            ExprWithArgs result = _binder.BindMethodGroupToArguments(bindFlags, memgroup, arguments);
            ReorderArgumentsForNamedAndOptional(callingObject, result);
            return result;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expr CreateArray(Expr callingObject, Expr optionalIndexerArguments)
        {
            return _binder.BindArrayIndexCore(callingObject, optionalIndexerArguments);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expr CreateField(
            SymWithType swt,
            Expr callingObject)
        {
            // For a field, simply create the EXPRFIELD and our caller takes care of the rest.

            FieldSymbol fieldSymbol = swt.Field();
            AggregateType fieldType = swt.GetType();
            FieldWithType fwt = new FieldWithType(fieldSymbol, fieldType);

            Expr field = _binder.BindToField(callingObject is ExprClass ? null : callingObject, fwt, 0);
            return field;
        }

        /////////////////////////////////////////////////////////////////////////////////

        #endregion

        #endregion

        #region Calls
        /////////////////////////////////////////////////////////////////////////////////

        private Expr CreateCallingObjectForCall(
            ICSharpInvokeOrInvokeMemberBinder payload,
            ArgumentObject[] arguments,
            LocalVariableSymbol[] locals)
        {
            // Here we have a regular call, so create the calling object off of the first
            // parameter and pass it through.
            Expr callingObject;
            if (payload.StaticCall)
            {
                Type t = arguments[0].Value as Type;
                Debug.Assert(t != null); // Would have thrown in PopulateSymbolTableWithPayloadInformation already

                callingObject = _exprFactory.CreateClass(_symbolTable.GetCTypeFromType(t));
            }
            else
            {
                // If we have a null argument, just bail and throw.
                if (!arguments[0].Info.UseCompileTimeType && arguments[0].Value == null)
                {
                    throw Error.NullReferenceOnMemberException();
                }

                callingObject = _binder.mustConvert(
                    CreateArgumentEXPR(arguments[0], locals[0]),
                    _symbolTable.GetCTypeFromType(arguments[0].Type));

                if (arguments[0].Type.IsValueType && callingObject is ExprCast)
                {
                    // If we have a struct type, unbox it.
                    callingObject.Flags |= EXPRFLAG.EXF_UNBOXRUNTIME;
                }
            }
            return callingObject;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExprWithArgs BindCall(
            ICSharpInvokeOrInvokeMemberBinder payload,
            Expr callingObject,
            ArgumentObject[] arguments,
            LocalVariableSymbol[] locals)
        {
            if (payload is InvokeBinder && !callingObject.Type.isDelegateType())
            {
                throw Error.BindInvokeFailedNonDelegate();
            }

            int arity = payload.TypeArguments?.Length ?? 0;
            MemberLookup mem = new MemberLookup();

            SymWithType swt = _symbolTable.LookupMember(
                    payload.Name,
                    callingObject,
                    _bindingContext.ContextForMemberLookup,
                    arity,
                    mem,
                    (payload.Flags & CSharpCallFlags.EventHookup) != 0,
                    true);
            if (swt == null)
            {
                throw mem.ReportErrors();
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
            ExprMemberGroup memGroup = CreateMemberGroupEXPR(payload.Name, payload.TypeArguments, callingObject, swt.Sym.getKind());
            if ((payload.Flags & CSharpCallFlags.SimpleNameCall) != 0)
            {
                callingObject.Flags |= EXPRFLAG.EXF_SIMPLENAME;
            }

            if ((payload.Flags & CSharpCallFlags.EventHookup) != 0)
            {
                mem = new MemberLookup();
                SymWithType swtEvent = _symbolTable.LookupMember(
                        payload.Name.Split('_')[1],
                        callingObject,
                        _bindingContext.ContextForMemberLookup,
                        arity,
                        mem,
                        (payload.Flags & CSharpCallFlags.EventHookup) != 0,
                        true);
                if (swtEvent == null)
                {
                    throw mem.ReportErrors();
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
                    BindImplicitConversion(new ArgumentObject[] { arguments[1] }, eventType, locals, false);
                }
                memGroup.Flags &= ~EXPRFLAG.EXF_USERCALLABLE;

                if (swtEvent.Sym.getKind() == SYMKIND.SK_EventSymbol && swtEvent.Event().IsWindowsRuntimeEvent)
                {
                    return BindWinRTEventAccessor(
                                    new EventWithType(swtEvent.Event(), swtEvent.Ats),
                                    callingObject,
                                    arguments,
                                    locals,
                                    payload.Name.StartsWith("add_", StringComparison.Ordinal)); //isAddAccessor?
                }
            }

            // Check if we have a potential call to an indexed property accessor.
            // If so, we'll flag overload resolution to let us call non-callables.
            if ((payload.Name.StartsWith("set_", StringComparison.Ordinal) && ((MethodSymbol)swt.Sym).Params.Count > 1) ||
                (payload.Name.StartsWith("get_", StringComparison.Ordinal) && ((MethodSymbol)swt.Sym).Params.Count > 0))
            {
                memGroup.Flags &= ~EXPRFLAG.EXF_USERCALLABLE;
            }

            ExprCall result = _binder.BindMethodGroupToArguments(// Tree
                BindingFlag.BIND_RVALUEREQUIRED | BindingFlag.BIND_STMTEXPRONLY, memGroup, CreateArgumentListEXPR(arguments, locals, 1, arguments.Length)) as ExprCall;

            Debug.Assert(result != null);

            CheckForConditionalMethodError(result);
            ReorderArgumentsForNamedAndOptional(callingObject, result);
            return result;
        }

        private ExprWithArgs BindWinRTEventAccessor(EventWithType ewt, Expr callingObject, ArgumentObject[] arguments, LocalVariableSymbol[] locals, bool isAddAccessor)
        {
            // We want to generate either:
            // WindowsRuntimeMarshal.AddEventHandler<delegType>(new Func<delegType, EventRegistrationToken>(x.add_foo), new Action<EventRegistrationToken>(x.remove_foo), value)
            // or
            // WindowsRuntimeMarshal.RemoveEventHandler<delegType>(new Action<EventRegistrationToken>(x.remove_foo), value)

            Type evtType = ewt.Event().type.AssociatedSystemType;

            // Get new Action<EventRegistrationToken>(x.remove_foo)
            MethPropWithInst removemwi = new MethPropWithInst(ewt.Event().methRemove, ewt.Ats);
            ExprMemberGroup removeMethGrp = _exprFactory.CreateMemGroup(callingObject, removemwi);
            removeMethGrp.Flags &= ~EXPRFLAG.EXF_USERCALLABLE;
            Type eventRegistrationTokenType = SymbolTable.EventRegistrationTokenType;
            Type actionType = Expression.GetActionType(eventRegistrationTokenType);
            Expr removeMethArg = _binder.mustConvert(removeMethGrp, _symbolTable.GetCTypeFromType(actionType));

            // The value
            Expr delegateVal = CreateArgumentEXPR(arguments[1], locals[1]);
            ExprList args;
            string methodName;

            if (isAddAccessor)
            {
                // Get new Func<delegType, EventRegistrationToken>(x.add_foo)
                MethPropWithInst addmwi = new MethPropWithInst(ewt.Event().methAdd, ewt.Ats);
                ExprMemberGroup addMethGrp = _exprFactory.CreateMemGroup(callingObject, addmwi);
                addMethGrp.Flags &= ~EXPRFLAG.EXF_USERCALLABLE;
                Type funcType = Expression.GetFuncType(evtType, eventRegistrationTokenType);
                Expr addMethArg = _binder.mustConvert(addMethGrp, _symbolTable.GetCTypeFromType(funcType));

                args = _exprFactory.CreateList(addMethArg, removeMethArg, delegateVal);
                methodName = NameManager.GetPredefinedName(PredefinedName.PN_ADDEVENTHANDLER).Text;
            }
            else
            {
                args = _exprFactory.CreateList(removeMethArg, delegateVal);
                methodName = NameManager.GetPredefinedName(PredefinedName.PN_REMOVEEVENTHANDLER).Text;
            }

            // WindowsRuntimeMarshal.Add\RemoveEventHandler(...)
            Type windowsRuntimeMarshalType = SymbolTable.WindowsRuntimeMarshalType;
            _symbolTable.PopulateSymbolTableWithName(methodName, new List<Type> { evtType }, windowsRuntimeMarshalType);
            ExprClass marshalClass = _exprFactory.CreateClass(_symbolTable.GetCTypeFromType(windowsRuntimeMarshalType));
            ExprMemberGroup addEventGrp = CreateMemberGroupEXPR(methodName, new [] { evtType }, marshalClass, SYMKIND.SK_MethodSymbol);
            return _binder.BindMethodGroupToArguments(
                BindingFlag.BIND_RVALUEREQUIRED | BindingFlag.BIND_STMTEXPRONLY,
                addEventGrp,
                args);
        }

        private static void CheckForConditionalMethodError(ExprCall call)
        {
            MethodSymbol method = call.MethWithInst.Meth();
            object[] conditions = method.AssociatedMemberInfo.GetCustomAttributes(typeof(ConditionalAttribute), true);
            if (conditions.Length > 0)
            {
                throw Error.BindCallToConditionalMethod(method.name);
            }
        }

        private void ReorderArgumentsForNamedAndOptional(Expr callingObject, ExprWithArgs result)
        {
            Expr arguments = result.OptionalArguments;
            AggregateType type;
            MethodOrPropertySymbol methprop;
            ExprMemberGroup memgroup;
            TypeArray typeArgs;

            if (result is ExprCall call)
            {
                type = call.MethWithInst.Ats;
                methprop = call.MethWithInst.Meth();
                memgroup = call.MemberGroup;
                typeArgs = call.MethWithInst.TypeArgs;
            }
            else
            {
                ExprProperty prop = result as ExprProperty;
                Debug.Assert(prop != null);
                type = prop.PropWithTypeSlot.Ats;
                methprop = prop.PropWithTypeSlot.Prop();
                memgroup = prop.MemberGroup;
                typeArgs = null;
            }

            ArgInfos argInfo = new ArgInfos
            {
                carg = ExpressionBinder.CountArguments(arguments)
            };
            _binder.FillInArgInfoFromArgList(argInfo, arguments);

            // We need to substitute type parameters BEFORE getting the most derived one because
            // we're binding against the base method, and the derived method may change the 
            // generic arguments. 
            TypeArray parameters = SymbolLoader.GetTypeManager().SubstTypeArray(methprop.Params, type, typeArgs);
            methprop = ExpressionBinder.GroupToArgsBinder.FindMostDerivedMethod(SymbolLoader, methprop, callingObject.Type);
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
                Expr pList = null;

                // We reordered, so make a new list of them and set them on the constructor.
                // Go backwards cause lists are right-flushed.
                // Also perform the conversions to the right types.
                for (int i = argInfo.carg - 1; i >= 0; i--)
                {
                    Expr pArg = argInfo.prgexpr[i];

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

                result.OptionalArguments = pList;
            }
        }

        private Expr StripNamedArgument(Expr pArg)
        {
            if (pArg is ExprNamedArgumentSpecification named)
            {
                pArg = named.Value;
            }
            else if (pArg is ExprArrayInit init)
            {
                init.OptionalArguments = StripNamedArguments(init.OptionalArguments);
            }

            return pArg;
        }

        private Expr StripNamedArguments(Expr pArg)
        {
            if (pArg is ExprList list)
            {
                for(;;)
                {
                    list.OptionalElement = StripNamedArgument(list.OptionalElement);

                    if (list.OptionalNextListNode is ExprList next)
                    {
                        list = next;
                    }
                    else
                    {
                        list.OptionalNextListNode = StripNamedArgument(list.OptionalNextListNode);
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

        internal Expr BindUnaryOperation(
            CSharpUnaryOperationBinder payload,
            ArgumentObject[] arguments,
            LocalVariableSymbol[] locals)
        {
            Debug.Assert(arguments.Length == 1);

            OperatorKind op = GetOperatorKind(payload.Operation);
            Expr arg1 = CreateArgumentEXPR(arguments[0], locals[0]);
            arg1.ErrorString = Operators.GetDisplayName(GetOperatorKind(payload.Operation));

            if (op == OperatorKind.OP_TRUE || op == OperatorKind.OP_FALSE)
            {
                // For true and false, we try to convert to bool first. If that
                // doesn't work, then we look for user defined operators.
                Expr result = _binder.tryConvert(arg1, SymbolLoader.GetPredefindType(PredefinedType.PT_BOOL));
                if (result != null && op == OperatorKind.OP_FALSE)
                {
                    // If we can convert to bool, we need to negate the thing if we're looking for false.
                    result = _binder.BindStandardUnaryOperator(OperatorKind.OP_LOGNOT, result);
                }

                if (result == null)
                {
                    result = _binder.bindUDUnop(op == OperatorKind.OP_TRUE ? ExpressionKind.True : ExpressionKind.False, arg1);
                }

                // If the result is STILL null, then that means theres no implicit conversion to bool,
                // and no user-defined operators for true and false. Just do a must convert to report
                // the error.
                if (result == null)
                {
                    result = _binder.mustConvert(arg1, SymbolLoader.GetPredefindType(PredefinedType.PT_BOOL));
                }
                return result;
            }
            return _binder.BindStandardUnaryOperator(op, arg1);
        }
        #endregion

        #region BinaryOperators

        /////////////////////////////////////////////////////////////////////////////////

        internal Expr BindBinaryOperation(
                CSharpBinaryOperationBinder payload,
                ArgumentObject[] arguments,
                LocalVariableSymbol[] locals)
        {
            Debug.Assert(arguments.Length == 2);

            ExpressionKind ek = Operators.GetExpressionKind(GetOperatorKind(payload.Operation, payload.IsLogicalOperation));
            Expr arg1 = CreateArgumentEXPR(arguments[0], locals[0]);
            Expr arg2 = CreateArgumentEXPR(arguments[1], locals[1]);

            arg1.ErrorString = Operators.GetDisplayName(GetOperatorKind(payload.Operation, payload.IsLogicalOperation));
            arg2.ErrorString = Operators.GetDisplayName(GetOperatorKind(payload.Operation, payload.IsLogicalOperation));

            if (ek > ExpressionKind.MultiOffset)
            {
                ek = (ExpressionKind)(ek - ExpressionKind.MultiOffset);
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

        #endregion

        #region Properties
        /////////////////////////////////////////////////////////////////////////////////

        internal Expr BindProperty(
            ICSharpBinder payload,
            ArgumentObject argument,
            LocalVariableSymbol local,
            Expr optionalIndexerArguments)
        {
            // If our argument is a static type, then we're calling a static property.
            Expr callingObject = argument.Info.IsStaticType ?
                _exprFactory.CreateClass(_symbolTable.GetCTypeFromType(argument.Value as Type)) :
                CreateLocal(argument.Type, argument.Info.IsOut, local);

            if (!argument.Info.UseCompileTimeType && argument.Value == null)
            {
                throw Error.NullReferenceOnMemberException();
            }

            // If our argument is a struct type, unbox it.
            if (argument.Type.IsValueType && callingObject is ExprCast)
            {
                // If we have a struct type, unbox it.
                callingObject.Flags |= EXPRFLAG.EXF_UNBOXRUNTIME;
            }
            string name = payload.Name;
            BindingFlag bindFlags = payload.BindingFlags;

            MemberLookup mem = new MemberLookup();
            SymWithType swt = _symbolTable.LookupMember(name, callingObject, _bindingContext.ContextForMemberLookup, 0, mem, false, false);
            if (swt == null)
            {
                if (optionalIndexerArguments != null)
                {
                    int numIndexArguments = ExpressionIterator.Count(optionalIndexerArguments);
                    // We could have an array access here. See if its just an array.
                    Type type = argument.Type;
                    Debug.Assert(type != typeof(string));
                    if (type.IsArray)
                    {
                        if (type.IsArray && type.GetArrayRank() != numIndexArguments)
                        {
                            throw _semanticChecker.ErrorContext.Error(ErrorCode.ERR_BadIndexCount, type.GetArrayRank());
                        }
                        
                        Debug.Assert(callingObject.Type is ArrayType);
                        return CreateArray(callingObject, optionalIndexerArguments);
                    }
                }
                throw mem.ReportErrors();
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
                        // Properties can be LValues.
                        callingObject.Flags |= EXPRFLAG.EXF_LVALUE;
                        return CreateProperty(swt, callingObject, payload.BindingFlags);
                    }

                case SYMKIND.SK_FieldSymbol:
                    return CreateField(swt, callingObject);

                case SYMKIND.SK_EventSymbol:
                    throw Error.BindPropertyFailedEvent(name);

                default:
                    Debug.Assert(false, "Unexpected type returned from lookup");
                    throw Error.InternalCompilerError();
            }
        }

        #endregion

        #region Casts
        /////////////////////////////////////////////////////////////////////////////////

        internal Expr BindImplicitConversion(
            ArgumentObject[] arguments,
            Type returnType,
            LocalVariableSymbol[] locals,
            bool bIsArrayCreationConversion)
        {
            Debug.Assert(arguments.Length == 1);

            // Load the conversions on the target.
            _symbolTable.AddConversionsForType(returnType);

            Expr argument = CreateArgumentEXPR(arguments[0], locals[0]);
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

                CType pDestType = _binder.ChooseArrayIndexType(argument);
                return _binder.mustCast(
                    _binder.mustConvert(argument, pDestType),
                    destinationType,
                    CONVERTTYPE.CHECKOVERFLOW | CONVERTTYPE.NOUDC);
            }

            return _binder.mustConvert(argument, destinationType);
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal Expr BindExplicitConversion(ArgumentObject[] arguments, Type returnType, LocalVariableSymbol[] locals)
        {
            Debug.Assert(arguments.Length == 1);

            // Load the conversions on the target.
            _symbolTable.AddConversionsForType(returnType);

            Expr argument = CreateArgumentEXPR(arguments[0], locals[0]);
            CType destinationType = _symbolTable.GetCTypeFromType(returnType);

            return _binder.mustCast(argument, destinationType);
        }

        #endregion

        #region Assignments

        /////////////////////////////////////////////////////////////////////////////////

        internal Expr BindAssignment(
            ICSharpBinder payload,
            ArgumentObject[] arguments,
            LocalVariableSymbol[] locals)
        {
            Debug.Assert(arguments.Length >= 2);
            Debug.Assert(arguments.All(a => a.Type != null));

            string name = payload.Name;

            // Find the lhs and rhs.
            Expr indexerArguments;
            bool bIsCompound;

            CSharpSetIndexBinder setIndexBinder = payload as CSharpSetIndexBinder;
            if (setIndexBinder != null)
            {
                // Get the list of indexer arguments - this is the list of arguments minus the last one.
                indexerArguments = CreateArgumentListEXPR(arguments, locals, 1, arguments.Length - 1);
                bIsCompound = setIndexBinder.IsCompoundAssignment;
            }
            else
            {
                indexerArguments = null;
                bIsCompound = (payload as CSharpSetMemberBinder).IsCompoundAssignment;
            }
            _symbolTable.PopulateSymbolTableWithName(name, null, arguments[0].Type);
            Expr lhs = BindProperty(payload, arguments[0], locals[0], indexerArguments);

            int indexOfLast = arguments.Length - 1;
            Expr rhs = CreateArgumentEXPR(arguments[indexOfLast], locals[indexOfLast]);
            return _binder.BindAssignment(lhs, rhs, bIsCompound);
        }
        #endregion

        #region Events
        /////////////////////////////////////////////////////////////////////////////////

        internal Expr BindIsEvent(
            CSharpIsEventBinder binder,
            ArgumentObject[] arguments,
            LocalVariableSymbol[] locals)
        {
            // The IsEvent binder will never be called without an instance object. This 
            // is because the compiler only gen's this code for dynamic dots.

            Expr callingObject = CreateLocal(arguments[0].Type, false, locals[0]);
            MemberLookup mem = new MemberLookup();
            CType boolType = SymbolLoader.GetPredefindType(PredefinedType.PT_BOOL);
            bool result = false;

            if (arguments[0].Value == null)
            {
                throw Error.NullReferenceOnMemberException();
            }

            SymWithType swt = _symbolTable.LookupMember(
                    binder.Name,
                    callingObject,
                    _bindingContext.ContextForMemberLookup,
                    0,
                    mem,
                    false,
                    false);

            if (swt != null)
            {
                // If lookup returns an actual event, then this is an event.
                if (swt.Sym.getKind() == SYMKIND.SK_EventSymbol)
                {
                    result = true;
                }

                // If lookup returns the backing field of a field-like event, then
                // this is an event. This is due to the Dev10 design change around
                // the binding of +=, and the fact that the "IsEvent" binding question
                // is only ever asked about the LHS of a += or -=.
                else if (swt.Sym is FieldSymbol field && field.isEvent)
                {
                    result = true;
                }
            }

            return _exprFactory.CreateConstant(boolType, ConstVal.Get(result));
        }
        #endregion
    }
}
