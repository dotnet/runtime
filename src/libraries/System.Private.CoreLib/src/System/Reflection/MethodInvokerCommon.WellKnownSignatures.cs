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

            func = null!;

            if (signatureInfo.DeclaringType != typeof(object))
            {
                return false;
            }

            Type returnType = signatureInfo.ReturnType;

            if (returnType == typeof(void))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceVoid);
            }
            else if (returnType == typeof(bool))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<bool>);
            }
            else if (returnType == typeof(byte))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<byte>);
            }
            else if (returnType == typeof(char))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<char>);
            }
            else if (returnType == typeof(DateTime))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<DateTime>);
            }
            else if (returnType == typeof(DateTimeOffset))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<DateTimeOffset>);
            }
            else if (returnType == typeof(decimal))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<decimal>);
            }
            else if (returnType == typeof(double))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<double>);
            }
            else if (returnType == typeof(float))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<float>);
            }
            else if (returnType == typeof(Guid))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<Guid>);
            }
            else if (returnType == typeof(int))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<int>);
            }
            else if (returnType == typeof(IntPtr))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<IntPtr>);
            }
            else if (returnType == typeof(long))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<long>);
            }
            else if (returnType == typeof(object))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<object>);
            }
            else if (returnType == typeof(short))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<short>);
            }
            else if (returnType == typeof(sbyte))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<sbyte>);
            }
            else if (returnType == typeof(string))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<string>);
            }
            else if (returnType == typeof(ushort))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<ushort>);
            }
            else if (returnType == typeof(uint))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<uint>);
            }
            else if (returnType == typeof(UIntPtr))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<UIntPtr>);
            }
            else if (returnType == typeof(ulong))
            {
                func = new InvokeFunc_Obj0Args(CallInstanceAny<ulong>);
            }

            return func != null;
        }

        public static bool TryGetWellKnownSignatureFor1Arg(in InvokeSignatureInfoKey signatureInfo, [NotNullWhen(true)] out Delegate? func)
        {
            Debug.Assert(signatureInfo.ParameterTypes.Length == 1);

            func = null!;

            if (signatureInfo.DeclaringType != typeof(object) || signatureInfo.ReturnType != typeof(void))
            {
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
            else if (arg1Type == typeof(bool))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<bool>);
            }
            else if (arg1Type == typeof(byte))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<byte>);
            }
            else if (arg1Type == typeof(char))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<char>);
            }
            else if (arg1Type == typeof(DateTime))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<DateTime>);
            }
            else if (arg1Type == typeof(DateTimeOffset))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<DateTimeOffset>);
            }
            else if (arg1Type == typeof(decimal))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<decimal>);
            }
            else if (arg1Type == typeof(double))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<double>);
            }
            else if (arg1Type == typeof(float))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<float>);
            }
            else if (arg1Type == typeof(Guid))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<Guid>);
            }
            else if (arg1Type == typeof(int))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<int>);
            }
            else if (arg1Type == typeof(IntPtr))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<IntPtr>);
            }
            else if (arg1Type == typeof(long))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<long>);
            }
            else if (arg1Type == typeof(object))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<object>);
            }
            else if (arg1Type == typeof(short))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<short>);
            }
            else if (arg1Type == typeof(sbyte))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<sbyte>);
            }
            else if (arg1Type == typeof(string))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<string>);
            }
            else if (arg1Type == typeof(ushort))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<ushort>);
            }
            else if (arg1Type == typeof(uint))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<uint>);
            }
            else if (arg1Type == typeof(UIntPtr))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<UIntPtr>);
            }
            else if (arg1Type == typeof(ulong))
            {
                func = new InvokeFunc_Obj1Arg(CallInstanceAnyVoid<ulong>);
            }

            return func != null;
        }

        private static unsafe object? CallInstanceVoid(object? obj, IntPtr functionPointer) { ((delegate* managed<object?, void>)functionPointer)(obj); return null; }
        private static unsafe object? CallInstanceAny<TReturn>(object? obj, IntPtr functionPointer) => ((delegate* managed<object?, TReturn>)functionPointer)(obj);
        private static unsafe object? CallInstanceAnyVoid<TArg1>(object? obj, IntPtr functionPointer, object? arg1) { ((delegate* managed<object?, TArg1, void>)functionPointer)(obj, (TArg1)arg1!); return null; }
    }
}
