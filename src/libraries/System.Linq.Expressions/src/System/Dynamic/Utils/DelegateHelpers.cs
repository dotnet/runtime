// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

#if !FEATURE_DYNAMIC_DELEGATE
using System.Reflection.Emit;
#endif

namespace System.Dynamic.Utils
{
    internal static class DelegateHelpers
    {
        internal static Delegate CreateObjectArrayDelegate(Type delegateType, Func<object[], object> handler)
        {
#if !FEATURE_DYNAMIC_DELEGATE
            return CreateObjectArrayDelegateRefEmit(delegateType, handler);
#else
            return Internal.Runtime.Augments.DynamicDelegateAugments.CreateObjectArrayDelegate(delegateType, handler);
#endif
        }


#if !FEATURE_DYNAMIC_DELEGATE

        private static readonly MethodInfo s_FuncInvoke = typeof(Func<object[], object>).GetMethod("Invoke");
        private static readonly MethodInfo s_ArrayEmpty = typeof(Array).GetMethod(nameof(Array.Empty)).MakeGenericMethod(typeof(object));

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
        private static Delegate CreateObjectArrayDelegateRefEmit(Type delegateType, Func<object[], object> handler)
        {
            MethodInfo delegateInvokeMethod = delegateType.GetInvokeMethod();

            Type returnType = delegateInvokeMethod.ReturnType;
            bool hasReturnValue = returnType != typeof(void);

            ParameterInfo[] parameters = delegateInvokeMethod.GetParametersCached();
            Type[] paramTypes = new Type[parameters.Length + 1];
            paramTypes[0] = typeof(Func<object[], object>);
            for (int i = 0; i < parameters.Length; i++)
            {
                paramTypes[i + 1] = parameters[i].ParameterType;
            }

            DynamicMethod thunkMethod = new DynamicMethod("Thunk", returnType, paramTypes);
            ILGenerator ilgen = thunkMethod.GetILGenerator();

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
                    paramType = paramType.GetElementType();

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
                        Type byrefToType = parameters[i].ParameterType.GetElementType();

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

            // TODO: we need to cache these.
            return thunkMethod.CreateDelegate(delegateType, handler);
        }

        private static Type ConvertToBoxableType(Type t)
        {
            return (t.IsPointer) ? typeof(IntPtr) : t;
        }

#endif
    }
}
