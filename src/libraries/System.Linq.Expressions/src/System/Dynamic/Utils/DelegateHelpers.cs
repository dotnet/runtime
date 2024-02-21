// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Dynamic.Utils
{
    internal static class DelegateHelpers
    {
        // This can be flipped to false using feature switches at publishing time
        [FeatureCheck(typeof(RequiresDynamicCodeAttribute))]
#pragma warning disable IL4000
        internal static bool CanEmitObjectArrayDelegate => true;
#pragma warning restore IL4000

        // Separate class so that the it can be trimmed away and doesn't get conflated
        // with the Reflection.Emit statics below.
        private static class DynamicDelegateLightup
        {
            public static Func<Type, Func<object?[], object?>, Delegate> CreateObjectArrayDelegate { get; }
                = CreateObjectArrayDelegateInternal();

            private static Func<Type, Func<object?[], object?>, Delegate> CreateObjectArrayDelegateInternal()
            {
                // This is only supported by NativeAOT which always expects CanEmitObjectArrayDelegate to be false.
                // This check guards static constructor of trying to resolve 'Internal.Runtime.Augments.DynamicDelegateAugments'
                // on runtimes which do not support this private API.
                if (!CanEmitObjectArrayDelegate)
                {
                    return Type.GetType("Internal.Runtime.Augments.DynamicDelegateAugments, System.Private.CoreLib", throwOnError: true)!
                        .GetMethod("CreateObjectArrayDelegate")!
                        .CreateDelegate<Func<Type, Func<object?[], object?>, Delegate>>();
                }
                else
                {
                    return new Func<Type, Func<object?[], object?>, Delegate>((_x, _y) => throw new NotImplementedException());
                }
            }
        }

        private static class ForceAllowDynamicCodeLightup
        {
            public static Func<IDisposable>? ForceAllowDynamicCodeDelegate { get; }
                = ForceAllowDynamicCodeDelegateInternal();

            private static Func<IDisposable>? ForceAllowDynamicCodeDelegateInternal()
                => typeof(AssemblyBuilder)
                    .GetMethod("ForceAllowDynamicCode", BindingFlags.NonPublic | BindingFlags.Static)
                    ?.CreateDelegate<Func<IDisposable>>();
        }

        internal static Delegate CreateObjectArrayDelegate(Type delegateType, Func<object?[], object?> handler)
        {
            if (CanEmitObjectArrayDelegate)
            {
#pragma warning disable IL3050
                // Suppress analyzer warnings since they don't currently support feature flags
                return CreateObjectArrayDelegateRefEmit(delegateType, handler);
#pragma warning restore IL3050
            }
            else
            {
                return DynamicDelegateLightup.CreateObjectArrayDelegate(delegateType, handler);
            }
        }

        private static readonly CacheDict<Type, MethodInfo> s_thunks = new CacheDict<Type, MethodInfo>(256);
        private static readonly MethodInfo s_FuncInvoke = typeof(Func<object?[], object?>).GetMethod("Invoke")!;
        private static readonly MethodInfo s_ArrayEmpty = GetEmptyObjectArrayMethod();
        private static readonly MethodInfo[] s_ActionThunks = GetActionThunks();
        private static readonly MethodInfo[] s_FuncThunks = GetFuncThunks();
        private static int s_ThunksCreated;

        public static void ActionThunk(Func<object?[], object?> handler)
        {
            handler(Array.Empty<object?>());
        }

        public static void ActionThunk1<T1>(Func<object?[], object?> handler, T1 t1)
        {
            handler(new object?[] { t1 });
        }

        public static void ActionThunk2<T1, T2>(Func<object?[], object?> handler, T1 t1, T2 t2)
        {
            handler(new object?[] { t1, t2 });
        }

        public static TReturn FuncThunk<TReturn>(Func<object?[], object> handler)
        {
            return (TReturn)handler(Array.Empty<object>());
        }

        public static TReturn FuncThunk1<T1, TReturn>(Func<object?[], object> handler, T1 t1)
        {
            return (TReturn)handler(new object?[] { t1 });
        }

        public static TReturn FuncThunk2<T1, T2, TReturn>(Func<object?[], object> handler, T1 t1, T2 t2)
        {
            return (TReturn)handler(new object?[] { t1, t2 });
        }

        private static MethodInfo GetEmptyObjectArrayMethod() => ((Func<object[]>)Array.Empty<object>).GetMethodInfo();

        private static MethodInfo[] GetActionThunks()
        {
            Type delHelpers = typeof(DelegateHelpers);
            return new MethodInfo[]{delHelpers.GetMethod("ActionThunk")!,
                                    delHelpers.GetMethod("ActionThunk1")!,
                                    delHelpers.GetMethod("ActionThunk2")!};
        }

        private static MethodInfo[] GetFuncThunks()
        {
            Type delHelpers = typeof(DelegateHelpers);
            return new MethodInfo[]{delHelpers.GetMethod("FuncThunk")!,
                                    delHelpers.GetMethod("FuncThunk1")!,
                                    delHelpers.GetMethod("FuncThunk2")!};
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "The above ActionThunk and FuncThunk methods don't have trimming annotations.")]
        [RequiresDynamicCode(Expression.GenericMethodRequiresDynamicCode)]
        private static MethodInfo? GetCSharpThunk(Type returnType, bool hasReturnValue, ParameterInfo[] parameters)
        {
            try
            {
                if (parameters.Length > 2)
                {
                    return null; // Don't use C# thunks for more than 2 parameters
                }

                if (returnType.IsByRefLike || returnType.IsByRef || returnType.IsPointer)
                {
                    return null; // Don't use C# thunks for types that cannot be generic arguments
                }

                foreach (ParameterInfo parameter in parameters)
                {
                    Type parameterType = parameter.ParameterType;
                    if (parameterType.IsByRefLike || parameterType.IsByRef || parameterType.IsPointer)
                    {
                        return null; // Don't use C# thunks for types that cannot be generic arguments
                    }
                }

                int thunkTypeArgCount = parameters.Length;
                if (hasReturnValue)
                    thunkTypeArgCount++;

                Type[] thunkTypeArgs = thunkTypeArgCount == 0 ? Type.EmptyTypes : new Type[thunkTypeArgCount];
                for (int i = 0; i < parameters.Length; i++)
                {
                    thunkTypeArgs[i] = parameters[i].ParameterType;
                }

                MethodInfo uninstantiatedMethod;

                if (hasReturnValue)
                {
                    thunkTypeArgs[thunkTypeArgs.Length - 1] = returnType;
                    uninstantiatedMethod = s_FuncThunks[parameters.Length];
                }
                else
                {
                    uninstantiatedMethod = s_ActionThunks[parameters.Length];
                }

                return (thunkTypeArgs.Length > 0) ?
                    uninstantiatedMethod.MakeGenericMethod(thunkTypeArgs) :
                    uninstantiatedMethod;
            }
            catch
            {
                // If unable to instantiate thunk, fall back to dynamic method creation
                // This is expected to happen for cases such as function pointer types as arguments
                // Or new forms of types added to the typesystem in the future that aren't compatible with
                // generics
                return null;
            }
        }

        // We will generate the following code:
        //
        // object ret;
        // object[] args = new object[parameterCount];
        // args[0] = param0;
        // args[1] = param1;
        //  ...
        // try {
        //      ret = handler.Invoke(args);
        // } finally {
        //      param0 = (T0)args[0];   // only generated for each byref argument
        // }
        // return (TRet)ret;
        [RequiresDynamicCode("Ref emit requires dynamic code.")]
        private static Delegate CreateObjectArrayDelegateRefEmit(Type delegateType, Func<object?[], object?> handler)
        {
            if (!s_thunks.TryGetValue(delegateType, out MethodInfo? thunkMethod))
            {
                MethodInfo delegateInvokeMethod = delegateType.GetInvokeMethod();

                Type returnType = delegateInvokeMethod.ReturnType;
                bool hasReturnValue = returnType != typeof(void);

                ParameterInfo[] parameters = delegateInvokeMethod.GetParametersCached();

                thunkMethod = GetCSharpThunk(returnType, hasReturnValue, parameters);

                if (thunkMethod == null)
                {
                    static IDisposable? CreateForceAllowDynamicCodeScope()
                    {
                        if (!RuntimeFeature.IsDynamicCodeSupported)
                        {
                            // Force 'new DynamicMethod' to not throw even though RuntimeFeature.IsDynamicCodeSupported is false.
                            // If we are running on a runtime that supports dynamic code, even though the feature switch is off,
                            // for example when running on CoreClr with PublishAot=true, this will allow IL to be emitted.
                            // If we are running on a runtime that really doesn't support dynamic code, like NativeAOT,
                            // CanEmitObjectArrayDelegate will be flipped to 'false', and this method won't be invoked.
                            return ForceAllowDynamicCodeLightup.ForceAllowDynamicCodeDelegate?.Invoke();
                        }

                        return null;
                    }

                    using IDisposable? forceAllowDynamicCodeScope = CreateForceAllowDynamicCodeScope();

                    int thunkIndex = Interlocked.Increment(ref s_ThunksCreated);
                    Type[] paramTypes = new Type[parameters.Length + 1];
                    paramTypes[0] = typeof(Func<object[], object>);

                    StringBuilder thunkName = new StringBuilder();
                    thunkName.Append("Thunk");
                    thunkName.Append(thunkIndex);
                    if (hasReturnValue)
                    {
                        thunkName.Append("ret_");
                        thunkName.Append(returnType.Name);
                    }

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        thunkName.Append('_');
                        thunkName.Append(parameters[i].ParameterType.Name);
                        paramTypes[i + 1] = parameters[i].ParameterType;
                    }

                    DynamicMethod dynamicThunkMethod = new DynamicMethod(thunkName.ToString(), returnType, paramTypes);
                    thunkMethod = dynamicThunkMethod;
                    ILGenerator ilgen = dynamicThunkMethod.GetILGenerator();

                    LocalBuilder argArray = ilgen.DeclareLocal(typeof(object[]));
                    LocalBuilder retValue = ilgen.DeclareLocal(typeof(object));

                    // create the argument array
                    if (parameters.Length == 0)
                    {
                        ilgen.Emit(OpCodes.Call, s_ArrayEmpty);
                    }
                    else
                    {
                        ilgen.Emit(OpCodes.Ldc_I4, parameters.Length);
                        ilgen.Emit(OpCodes.Newarr, typeof(object));
                    }
                    ilgen.Emit(OpCodes.Stloc, argArray);

                    // populate object array
                    bool hasRefArgs = false;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        bool paramIsByReference = parameters[i].ParameterType.IsByRef;
                        Type paramType = parameters[i].ParameterType;
                        if (paramIsByReference)
                            paramType = paramType.GetElementType()!;

                        hasRefArgs = hasRefArgs || paramIsByReference;

                        ilgen.Emit(OpCodes.Ldloc, argArray);
                        ilgen.Emit(OpCodes.Ldc_I4, i);
                        ilgen.Emit(OpCodes.Ldarg, i + 1);

                        if (paramIsByReference)
                        {
                            ilgen.Emit(OpCodes.Ldobj, paramType);
                        }
                        Type boxType = ConvertToBoxableType(paramType);
                        ilgen.Emit(OpCodes.Box, boxType);
                        ilgen.Emit(OpCodes.Stelem_Ref);
                    }

                    if (hasRefArgs)
                    {
                        ilgen.BeginExceptionBlock();
                    }

                    // load delegate
                    ilgen.Emit(OpCodes.Ldarg_0);

                    // load array
                    ilgen.Emit(OpCodes.Ldloc, argArray);

                    // invoke Invoke
                    ilgen.Emit(OpCodes.Callvirt, s_FuncInvoke);
                    ilgen.Emit(OpCodes.Stloc, retValue);

                    if (hasRefArgs)
                    {
                        // copy back ref/out args
                        ilgen.BeginFinallyBlock();
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (parameters[i].ParameterType.IsByRef)
                            {
                                Type byrefToType = parameters[i].ParameterType.GetElementType()!;

                                // update parameter
                                ilgen.Emit(OpCodes.Ldarg, i + 1);
                                ilgen.Emit(OpCodes.Ldloc, argArray);
                                ilgen.Emit(OpCodes.Ldc_I4, i);
                                ilgen.Emit(OpCodes.Ldelem_Ref);
                                ilgen.Emit(OpCodes.Unbox_Any, byrefToType);
                                ilgen.Emit(OpCodes.Stobj, byrefToType);
                            }
                        }
                        ilgen.EndExceptionBlock();
                    }

                    if (hasReturnValue)
                    {
                        ilgen.Emit(OpCodes.Ldloc, retValue);
                        ilgen.Emit(OpCodes.Unbox_Any, ConvertToBoxableType(returnType));
                    }

                    ilgen.Emit(OpCodes.Ret);
                }

                s_thunks[delegateType] = thunkMethod;
            }

            return thunkMethod.CreateDelegate(delegateType, handler);
        }

        private static Type ConvertToBoxableType(Type t)
        {
            return (t.IsPointer) ? typeof(IntPtr) : t;
        }
    }
}
