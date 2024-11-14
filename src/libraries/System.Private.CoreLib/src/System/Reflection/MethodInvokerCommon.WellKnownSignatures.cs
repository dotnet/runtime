// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using static System.Reflection.InvokerEmitUtil;

namespace System.Reflection
{
    internal static partial class MethodInvokerCommon
    {
        public static bool TryGetWellKnownSignatureFor0Args(in InvokeSignatureInfoKey signatureInfo, [NotNullWhen(true)] out Delegate? func)
        {
            Debug.Assert(signatureInfo.ParameterTypes.Length == 0);

            if (signatureInfo.DeclaringType != typeof(object))
            {
                func = null!;
                return false;
            }

            Type returnType = signatureInfo.ReturnType;

            if (returnType == typeof(void))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceVoid);
            }
            else if (returnType == typeof(int))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<int>);
            }
            else if (returnType == typeof(object))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<object>);
            }

            // todo: add more types

            func = null!;
            return false;
        }

        public static bool TryGetWellKnownSignatureFor1Arg(in InvokeSignatureInfoKey signatureInfo, [NotNullWhen(true)] out Delegate? func)
        {
            Debug.Assert(signatureInfo.ParameterTypes.Length == 1);

            if (signatureInfo.DeclaringType != typeof(object) || signatureInfo.ReturnType != typeof(void))
            {
                func = null!;
                return false;
            }

            Type arg1Type = signatureInfo.ParameterTypes[0];
            if (arg1Type == typeof(int))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<int>);
            }
            else if (arg1Type == typeof(object))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<object>);
            }

            // todo: add more types

            func = null!;
            return false;
        }

        private static unsafe object? CallInstanceVoid(object? obj, IntPtr functionPointer) { ((delegate* managed<object?, void>)functionPointer)(obj); return null; }
        private static unsafe object? CallInstanceAny<TReturn>(object? obj, IntPtr functionPointer) => ((delegate* managed<object?, TReturn>)functionPointer)(obj);
        private static unsafe object? CallInstanceAnyVoid<TArg1>(object? obj, IntPtr functionPointer, object? arg1) { ((delegate* managed<object?, TArg1, void>)functionPointer)(obj, (TArg1)arg1!); return null; }
    }
}
